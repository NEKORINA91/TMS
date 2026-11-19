// Live search: an input with data-filter-table="tableId" hides rows that don't match
document.addEventListener('input', function (e) {
    var box = e.target.closest('[data-filter-table]');
    if (!box) return;
    var q = box.value.toLowerCase();
    document.querySelectorAll('#' + box.dataset.filterTable + ' tbody tr').forEach(function (tr) {
        tr.style.display = tr.textContent.toLowerCase().indexOf(q) > -1 ? '' : 'none';
    });
});