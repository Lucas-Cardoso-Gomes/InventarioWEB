window.addEventListener('pageshow', function (event) {
    const form = document.querySelector('#computadores-filter-form');
    if (!form) return;

    const storageKey = 'computerFilterState';
    const checkboxNames = [
        'CurrentFabricantes',
        'CurrentSOs',
        'CurrentProcessadorFabricantes',
        'CurrentRamTipos',
        'CurrentProcessadores',
        'CurrentRams'
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

    function loadFilterStateAndApply() {
        const savedStateJSON = localStorage.getItem(storageKey);
        if (!savedStateJSON) return;

        const savedState = JSON.parse(savedStateJSON);

        // Restore search string
        form.querySelector('input[name="searchString"]').value = savedState.searchString || '';

        // Restore checkboxes
        checkboxNames.forEach(name => {
            if (savedState[name]) {
                form.querySelectorAll(`input[name="${name}"]`).forEach(cb => {
                    cb.checked = savedState[name].includes(cb.value);
                });
            }
        });

        // Check if state matches URL and submit if it doesn't
        const urlParams = new URLSearchParams(window.location.search);
        let stateMatchesUrl = true;

        if ((savedState.searchString || '') !== (urlParams.get('searchString') || '')) {
            stateMatchesUrl = false;
        }

        const areArraysEqual = (arrA, arrB) => {
            if (!arrA && !arrB) return true;
            if (!arrA || !arrB || arrA.length !== arrB.length) return false;
            const sortedA = [...arrA].sort();
            const sortedB = [...arrB].sort();
            return sortedA.every((val, index) => val === sortedB[index]);
        };

        if (stateMatchesUrl) { // Only check arrays if the simple string already matches
            for (const name of checkboxNames) {
                const fromState = savedState[name] || [];
                const fromUrl = urlParams.getAll(name);
                if (!areArraysEqual(fromState, fromUrl)) {
                    stateMatchesUrl = false;
                    break;
                }
            }
        }

        if (!stateMatchesUrl) {
            form.submit();
        }
    }

    form.addEventListener('submit', saveFilterState);

    const clearButton = form.querySelector('a[href="/Computadores/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    loadFilterStateAndApply();
});
