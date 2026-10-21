
// wwwroot/js/mudAutocompleteSelectAll.js
window.attachSelectAllToAutocomplete = (wrapperId) => {
    const wrapper = document.getElementById(wrapperId);
    if (!wrapper) return;

    // ✅ Event delegation — survives DOM replacements
    wrapper.addEventListener("mousedown", (e) => {
        const input = e.target.closest("input");
        if (!input) return;

        // Delay needed so Mud doesn't override selection
        setTimeout(() => input.select(), 0);
    });
};
