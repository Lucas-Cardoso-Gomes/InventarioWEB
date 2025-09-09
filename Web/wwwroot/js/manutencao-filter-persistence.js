window.addEventListener('pageshow', function (event) {
    const form = document.querySelector('#manutencoes-filter-form');
    if (!form) return;

    const storageKey = 'manutencaoFilterState';
    const textInputNames = ['partNumber', 'colaborador', 'hostname'];

    function saveFilterState() {
        const state = {};
        textInputNames.forEach(name => {
            const input = form.querySelector(`input[name="${name}"]`);
            if (input) {
                state[name] = input.value;
            }
        });
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    function loadFilterStateAndApply() {
        const savedStateJSON = localStorage.getItem(storageKey);
        if (!savedStateJSON) return;

        const savedState = JSON.parse(savedStateJSON);

        // Restore text inputs
        let stateMatchesUrl = true;
        const urlParams = new URLSearchParams(window.location.search);

        textInputNames.forEach(name => {
            const input = form.querySelector(`input[name="${name}"]`);
            if (input) {
                const savedValue = savedState[name] || '';
                input.value = savedValue;

                const urlValue = urlParams.get(name) || '';
                if (savedValue !== urlValue) {
                    stateMatchesUrl = false;
                }
            }
        });

        if (!stateMatchesUrl) {
            form.submit();
        }
    }

    form.addEventListener('submit', saveFilterState);

    const clearButton = form.querySelector('a[href="/Manutencoes/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    loadFilterStateAndApply();
});
