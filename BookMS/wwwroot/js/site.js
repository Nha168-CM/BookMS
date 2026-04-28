// Book Management System - Global JS

// Confirm delete helper
function confirmDelete(form, message) {
    if (confirm(message || 'Are you sure you want to delete this item?')) {
        form.submit();
    }
}

// Format currency
function formatCurrency(amount) {
    return '$' + parseFloat(amount).toFixed(2).replace(/\d(?=(\d{3})+\.)/g, '$&,');
}

// Auto-dismiss alerts
document.addEventListener('DOMContentLoaded', function () {
    // Auto-close alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = window.bootstrap?.Alert?.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });

    // Activate current nav link tooltip for collapsed sidebar
    const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');
    navLinks.forEach(link => {
        link.setAttribute('title', link.querySelector('span')?.textContent || '');
    });

    // Table row click to navigate (optional)
    document.querySelectorAll('tr[data-href]').forEach(row => {
        row.style.cursor = 'pointer';
        row.addEventListener('click', () => window.location = row.dataset.href);
    });
});
