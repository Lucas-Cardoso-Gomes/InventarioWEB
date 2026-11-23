window.addEventListener('pageshow', function (event) {
    const form = document.querySelector('#chamados-filter-form');
    if (!form) return;

    const storageKey = 'chamadoFilterState';

    function saveFilterState() {
        const state = {
            statuses: Array.from(form.querySelectorAll('input[name="statuses"]:checked')).map(cb => cb.value),
            selectedAdmins: Array.from(form.querySelectorAll('input[name="selectedAdmins"]:checked')).map(cb => cb.value)
        };
        localStorage.setItem(storageKey, JSON.stringify(state));
    }

    function loadFilterStateAndApply() {
        const savedStateJSON = localStorage.getItem(storageKey);
        if (!savedStateJSON) return;

        const savedState = JSON.parse(savedStateJSON);

        form.querySelectorAll('input[name="statuses"]').forEach(cb => {
            cb.checked = (savedState.statuses || []).includes(cb.value);
        });
        form.querySelectorAll('input[name="selectedAdmins"]').forEach(cb => {
            cb.checked = (savedState.selectedAdmins || []).includes(cb.value);
        });

        const urlParams = new URLSearchParams(window.location.search);
        const statusesFromUrl = urlParams.getAll('statuses');
        const adminsFromUrl = urlParams.getAll('selectedAdmins');

        const areArraysEqual = (arrA, arrB) => {
            if (arrA.length !== arrB.length) return false;
            const sortedA = [...arrA].sort();
            const sortedB = [...arrB].sort();
            return sortedA.every((val, index) => val === sortedB[index]);
        };

        const statusesMatch = areArraysEqual(savedState.statuses || [], statusesFromUrl);
        const adminsMatch = areArraysEqual(savedState.selectedAdmins || [], adminsFromUrl);

        if (!statusesMatch || !adminsMatch) {
            form.submit();
        }
    }

    form.addEventListener('submit', saveFilterState);

    const clearButton = form.querySelector('a[href="/Chamados/Index"]');
    if (clearButton) {
        clearButton.addEventListener('click', function() {
            localStorage.removeItem(storageKey);
        });
    }

    loadFilterStateAndApply();
});
