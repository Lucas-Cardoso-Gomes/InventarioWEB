document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form[action="/Computadores/Index"]');
    if (!form) return;

    const storageKey = 'computerFilterState';

    // Function to save filter state to localStorage
    function saveFilterState() {
        const searchString = form.querySelector('#searchString').value;
        const state = {
            searchString: searchString,
            checkboxes: {}
        };

        const filterContainers = form.querySelectorAll('.filterable-list-container');
        filterContainers.forEach(container => {
            const checkboxes = container.querySelectorAll('.form-check-input');
            checkboxes.forEach(cb => {
                state.checkboxes[cb.id] = cb.checked;
            });
        });

        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    // Function to load filter state from localStorage
    function loadFilterState() {
        const savedState = localStorage.getItem(storageKey);
        if (!savedState) return;

        const state = JSON.parse(savedState);

        // Restore search string
        const searchStringInput = form.querySelector('#searchString');
        if (searchStringInput && state.searchString) {
            searchStringInput.value = state.searchString;
        }

        // Restore checkboxes
        if (state.checkboxes) {
            for (const [id, isChecked] of Object.entries(state.checkboxes)) {
                const checkbox = form.querySelector(`#${id}`);
                if (checkbox) {
                    checkbox.checked = isChecked;
                }
            }
        }
    }

    // Add event listener to the form to save state on submit
    form.addEventListener('submit', saveFilterState);

    // Add event listener to the "Limpar Filtros" (Clear Filters) button
    const clearButton = form.querySelector('a[href="/Computadores/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
            // The link will then navigate, effectively clearing the form
        });
    }

    // Load the filter state when the page loads
    loadFilterState();
});
