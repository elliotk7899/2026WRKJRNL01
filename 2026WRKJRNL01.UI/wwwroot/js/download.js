// Fetches a URL and triggers a browser download using a Blob URL.
// Awaiting this from Blazor lets the caller know when the download has finished streaming.
// If dotNetHelper is provided, its OnDownloadProgress(int percent) method is invoked as bytes arrive.
window.downloadFileFromUrl = async function (url, suggestedFileName, dotNetHelper) {
    const response = await fetch(url, { credentials: 'include' });
    if (!response.ok) {
        throw new Error(`Download failed: ${response.status} ${response.statusText}`);
    }

    // Prefer the server-provided filename from Content-Disposition when present.
    let fileName = suggestedFileName || 'export.xlsx';
    const disposition = response.headers.get('Content-Disposition');
    if (disposition) {
        const match = /filename\*?=(?:UTF-\d['']*)?"?([^;"\r\n]+)"?/i.exec(disposition);
        if (match && match[1]) {
            fileName = decodeURIComponent(match[1].trim());
        }
    }

    const reportProgress = (percent) => {
        if (dotNetHelper) {
            // Fire-and-forget; ignore errors so a disposed helper doesn't break the download.
            dotNetHelper.invokeMethodAsync('OnDownloadProgress', percent).catch(() => { });
        }
    };

    let blob;
    const totalHeader = response.headers.get('Content-Length');
    const total = totalHeader ? parseInt(totalHeader, 10) : 0;

    if (response.body && total > 0) {
        // Stream the response so we can report incremental progress.
        const reader = response.body.getReader();
        const chunks = [];
        let received = 0;
        let lastPercent = -1;

        reportProgress(0);

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            chunks.push(value);
            received += value.length;

            const percent = Math.min(100, Math.floor((received / total) * 100));
            if (percent !== lastPercent) {
                lastPercent = percent;
                reportProgress(percent);
            }
        }

        blob = new Blob(chunks, { type: response.headers.get('Content-Type') || 'application/octet-stream' });
    }
    else {
        // No Content-Length (e.g., chunked). Fall back to a single blob read; progress is 0 → 100.
        reportProgress(0);
        blob = await response.blob();
    }

    reportProgress(100);

    const objectUrl = URL.createObjectURL(blob);
    try {
        const anchor = document.createElement('a');
        anchor.href = objectUrl;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
    } finally {
        URL.revokeObjectURL(objectUrl);
    }
};
