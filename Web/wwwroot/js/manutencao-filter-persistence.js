document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form[action="/Manutencoes/Index"]');
    if (!form) return;

    const storageKey = 'manutencaoFilterState';

    // Function to save filter state to localStorage
    function saveFilterState() {
        const state = {
            partNumber: form.querySelector('#partNumber').value,
            colaborador: form.querySelector('#colaborador').value,
            hostname: form.querySelector('#hostname').value
        };
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    // Function to load filter state from localStorage
    function loadFilterState() {
        const savedState = localStorage.getItem(storageKey);
        if (!savedState) return;

        const state = JSON.parse(savedState);

        if (state.partNumber) {
            form.querySelector('#partNumber').value = state.partNumber;
        }
        if (state.colaborador) {
            form.querySelector('#colaborador').value = state.colaborador;
        }
        if (state.hostname) {
            form.querySelector('#hostname').value = state.hostname;
        }
    }

    // Add event listener to the form to save state on submit
    form.addEventListener('submit', saveFilterState);

    // Add event listener to the "Limpar Filtros" (Clear Filters) button
    const clearButton = form.querySelector('a[href="/Manutencoes/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    // Load the filter state when the page loads
    loadFilterState();
});
