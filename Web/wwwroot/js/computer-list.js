document.addEventListener('DOMContentLoaded', function () {
    const columnMenu = document.getElementById('column-visibility-menu');
    const table = document.querySelector('.table');

    // Define all columns based on the th classes (col-*)
    const allColumns = Array.from(table.querySelectorAll('thead th[class^="col-"]'))
        .map(th => ({
            id: th.className,
            name: th.textContent.trim()
        }));

    // Define default visible columns as requested by the user
    const defaultVisibleColumns = [
        'col-ip',
        'col-mac',
        'col-usuario',
        'col-hostname',
        'col-processador',
        'col-ram',
        'col-so',
        'col-datacoleta'
    ];

    // Load saved preferences from localStorage or use defaults
    let visibleColumns = JSON.parse(localStorage.getItem('visibleComputerColumns')) || defaultVisibleColumns;

    // Function to apply visibility settings
    function applyVisibility() {
        allColumns.forEach(column => {
            const elements = table.querySelectorAll(`.${column.id}`);
            const shouldBeVisible = visibleColumns.includes(column.id);
            elements.forEach(el => {
                el.style.display = shouldBeVisible ? '' : 'none';
            });
        });
    }

    // Function to save preferences to localStorage
    function savePreferences() {
        localStorage.setItem('visibleComputerColumns', JSON.stringify(visibleColumns));
    }

    // Populate the dropdown menu
    allColumns.forEach(column => {
        const li = document.createElement('li');
        li.className = 'dropdown-item';

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.id = `check-${column.id}`;
        checkbox.className = 'form-check-input';
        checkbox.checked = visibleColumns.includes(column.id);

        const label = document.createElement('label');
        label.className = 'form-check-label ms-2';
        label.htmlFor = `check-${column.id}`;
        label.textContent = column.name;

        li.appendChild(checkbox);
        li.appendChild(label);
        columnMenu.appendChild(li);

        // Add event listener
        checkbox.addEventListener('change', () => {
            if (checkbox.checked) {
                if (!visibleColumns.includes(column.id)) {
                    visibleColumns.push(column.id);
                }
            } else {
                visibleColumns = visibleColumns.filter(id => id !== column.id);
            }
            applyVisibility();
            savePreferences();
        });
    });

    // Prevent dropdown from closing when clicking on items
    columnMenu.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    // Initial application of visibility
    applyVisibility();
});
