document.addEventListener('DOMContentLoaded', function () {
    function initializeFilterableCheckboxList(container) {
        const searchInput = container.querySelector('.filterable-list-input');
        const items = container.querySelectorAll('.form-check');

        if (!searchInput) {
            return;
        }

        searchInput.addEventListener('input', function () {
            const searchTerm = searchInput.value.toLowerCase();

            items.forEach(function (item) {
                const label = item.querySelector('.form-check-label');
                if (label) {
                    const labelText = label.textContent.toLowerCase();
                    if (labelText.includes(searchTerm)) {
                        item.style.display = '';
                    } else {
                        item.style.display = 'none';
                    }
                }
            });
        });
    }

    const filterableLists = document.querySelectorAll('.filterable-list-container');
    filterableLists.forEach(function (list) {
        initializeFilterableCheckboxList(list);
    });
});
