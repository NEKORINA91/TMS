// Live search: an input with data-filter-table="tableId" hides rows that don't match
document.addEventListener('input', function (e) {
    var box = e.target.closest('[data-filter-table]');
    if (!box) return;
    var q = box.value.toLowerCase();
    document.querySelectorAll('#' + box.dataset.filterTable + ' tbody tr').forEach(function (tr) {
        tr.style.display = tr.textContent.toLowerCase().indexOf(q) > -1 ? '' : 'none';
    });
});

document.addEventListener('DOMContentLoaded', function () {

    // 1. Put every table in a rounded white card that scrolls sideways on phones
    document.querySelectorAll('main table.table').forEach(function (t) {
        if (t.closest('.table-card') || t.closest('.card') || t.closest('.table-responsive')) return;
        var wrap = document.createElement('div');
        wrap.className = 'table-card';
        t.parentNode.insertBefore(wrap, t);
        wrap.appendChild(t);
    });

    // 2. Turn plain status words in table cells into coloured badges
    var map = {
        'paid': 'success', 'present': 'success', 'active': 'success', 'completed': 'success', 'processed': 'success', 'holiday': 'success',
        'pending': 'warning', 'due': 'warning', 'late': 'warning', 'event': 'primary',
        'overdue': 'danger', 'absent': 'danger', 'cancelled': 'danger', 'inactive': 'secondary', 'deactivated': 'secondary'
    };
    document.querySelectorAll('main table td').forEach(function (td) {
        if (td.children.length > 0) return;
        var key = td.textContent.trim().toLowerCase();
        if (map[key]) {
            var b = document.createElement('span');
            b.className = 'badge bg-' + map[key];
            b.textContent = td.textContent.trim();
            td.textContent = '';
            td.appendChild(b);
        }
    });
});
