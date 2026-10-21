window.setupScrollListener = function (selector, dotNetRef, methodName) {
    const el = document.querySelector(selector);
    if (!el) return;

    el.addEventListener('scroll', function () {
        if (el.scrollTop + el.clientHeight >= el.scrollHeight - 100) {
            dotNetRef.invokeMethodAsync(methodName);
        }
    });
};