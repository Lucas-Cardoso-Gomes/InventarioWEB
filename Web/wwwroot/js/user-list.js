document.addEventListener('DOMContentLoaded', function () {
    const columnMenu = document.getElementById('column-visibility-menu');
    const table = document.querySelector('.table');

    const allColumns = Array.from(table.querySelectorAll('thead th[class^="col-"]'))
        .map(th => ({
            id: th.className,
            name: th.textContent.trim()
        }));

    const defaultVisibleColumns = [
        'col-nome',
        'col-login',
        'col-role',
        'col-colaborador'
    ];

    let visibleColumns = JSON.parse(localStorage.getItem('visibleUserColumns')) || defaultVisibleColumns;

    function applyVisibility() {
        allColumns.forEach(column => {
            const elements = table.querySelectorAll(`.${column.id}`);
            const shouldBeVisible = visibleColumns.includes(column.id);
            elements.forEach(el => {
                el.style.display = shouldBeVisible ? '' : 'none';
            });
        });
    }

    function savePreferences() {
        localStorage.setItem('visibleUserColumns', JSON.stringify(visibleColumns));
    }

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

    columnMenu.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    applyVisibility();
});
