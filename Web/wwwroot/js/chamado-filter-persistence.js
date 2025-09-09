document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form[action="/Chamados/Index"]');
    if (!form) return;

    const storageKey = 'chamadoFilterState';

    // Function to save filter state to localStorage
    function saveFilterState() {
        const state = {
            statuses: [],
            selectedAdmins: []
        };

        // Save status checkboxes
        const statusCheckboxes = form.querySelectorAll('input[name="statuses"]:checked');
        statusCheckboxes.forEach(cb => state.statuses.push(cb.value));

        // Save admin checkboxes
        const adminCheckboxes = form.querySelectorAll('input[name="selectedAdmins"]:checked');
        adminCheckboxes.forEach(cb => state.selectedAdmins.push(cb.value));

        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    // Function to load filter state from localStorage
    function loadFilterState() {
        const savedState = localStorage.getItem(storageKey);
        if (!savedState) return;

        const state = JSON.parse(savedState);

        // Restore status checkboxes
        if (state.statuses && state.statuses.length > 0) {
            const allStatusCheckboxes = form.querySelectorAll('input[name="statuses"]');
            allStatusCheckboxes.forEach(cb => {
                cb.checked = state.statuses.includes(cb.value);
            });
        }

        // Restore admin checkboxes
        if (state.selectedAdmins && state.selectedAdmins.length > 0) {
            const allAdminCheckboxes = form.querySelectorAll('input[name="selectedAdmins"]');
            allAdminCheckboxes.forEach(cb => {
                cb.checked = state.selectedAdmins.includes(cb.value);
            });
        }
    }

    // Add event listener to the form to save state on submit
    form.addEventListener('submit', saveFilterState);

    // Add event listener to the "Limpar" (Clear) button
    const clearButton = form.querySelector('a[href="/Chamados/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    // Load the filter state when the page loads
    loadFilterState();
});
