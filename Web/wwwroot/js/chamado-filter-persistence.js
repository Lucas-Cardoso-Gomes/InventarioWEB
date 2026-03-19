window.addEventListener('pageshow', function (event) {
    const form = document.querySelector('#chamados-filter-form');
    if (!form) return;

    const storageKey = 'chamadoFilterState';

    // Function to save filter state to localStorage
    function saveFilterState() {
        const state = {
            statuses: Array.from(form.querySelectorAll('input[name="statuses"]:checked')).map(cb => cb.value),
            selectedAdmins: Array.from(form.querySelectorAll('input[name="selectedAdmins"]:checked')).map(cb => cb.value)
        };
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    // Function to load filter state from localStorage and apply it
    function loadFilterStateAndApply() {
        const savedStateJSON = localStorage.getItem(storageKey);
        if (!savedStateJSON) {
            return; // No saved state, do nothing.
        }

        const savedState = JSON.parse(savedStateJSON);

        // Restore checkbox states from the saved state
        form.querySelectorAll('input[name="statuses"]').forEach(cb => {
            cb.checked = (savedState.statuses || []).includes(cb.value);
        });
        form.querySelectorAll('input[name="selectedAdmins"]').forEach(cb => {
            cb.checked = (savedState.selectedAdmins || []).includes(cb.value);
        });

        // Check if the current URL's filters match the saved state.
        const urlParams = new URLSearchParams(window.location.search);
        const statusesFromUrl = urlParams.getAll('statuses');
        const adminsFromUrl = urlParams.getAll('selectedAdmins');

        // Helper to compare arrays regardless of element order
        const areArraysEqual = (arrA, arrB) => {
            if (arrA.length !== arrB.length) return false;
            const sortedA = [...arrA].sort();
            const sortedB = [...arrB].sort();
            return sortedA.every((val, index) => val === sortedB[index]);
        };

        const statusesMatch = areArraysEqual(savedState.statuses || [], statusesFromUrl);
        const adminsMatch = areArraysEqual(savedState.selectedAdmins || [], adminsFromUrl);

        if (!statusesMatch || !adminsMatch) {
            // If the state in localStorage doesn't match the URL, it means the page
            // was refreshed or loaded without the correct filters. Submit the form
            // to apply the saved filters and get the correct data.
            form.submit();
        }
    }

    // Add event listener to the form to save state on submit
    form.addEventListener('submit', saveFilterState);

    // Add event listener to the "Limpar" (Clear) button to clear the saved state
    const clearButton = form.querySelector('a[href="/Chamados/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    // Load and apply the filter state when the page loads
    loadFilterStateAndApply();
});
