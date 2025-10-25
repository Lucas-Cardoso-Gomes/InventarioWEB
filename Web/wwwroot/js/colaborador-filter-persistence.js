window.addEventListener('pageshow', function (event) {
    const form = document.querySelector('#colaboradores-filter-form');
    if (!form) return;

    const storageKey = 'colaboradorFilterState';
    const checkboxNames = [
        'CurrentFiliais', 'CurrentSetores', 'CurrentSmartphones',
        'CurrentTelefoneFixos', 'CurrentRamais', 'CurrentCoordenadores'
    ];

    function saveFilterState() {
        const state = {
            searchString: form.querySelector('input[name="searchString"]').value
        };
        checkboxNames.forEach(name => {
            state[name] = Array.from(form.querySelectorAll(`input[name="${name}"]:checked`)).map(cb => cb.value);
        });
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    function loadFilterState() {
        const savedStateJSON = localStorage.getItem(storageKey);
        if (savedStateJSON) {
            const savedState = JSON.parse(savedStateJSON);
            form.querySelector('input[name="searchString"]').value = savedState.searchString || '';
            checkboxNames.forEach(name => {
                if (savedState[name]) {
                    form.querySelectorAll(`input[name="${name}"]`).forEach(cb => {
                        cb.checked = savedState[name].includes(cb.value);
                    });
                }
            });
        }
    }

    form.addEventListener('submit', saveFilterState);

    const clearButton = document.querySelector('#clear-filters-btn');
    if (clearButton) {
        clearButton.addEventListener('click', function(e) {
            localStorage.removeItem(storageKey);
        });
    }

    loadFilterState();
});
