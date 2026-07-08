// ============================================
// ОБЩАЯ ФУНКЦИОНАЛЬНОСТЬ
// ============================================

// Автоматическое скрытие алертов
$(document).ready(function() {
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
});

// Активный пункт меню при загрузке
$(document).ready(function() {
    const currentUrl = window.location.pathname;
    $('.menu-item').each(function() {
        const href = $(this).attr('href');
        if (href && href !== '#' && currentUrl.includes(href)) {
            $(this).addClass('active');
        }
    });
});

// ============================================
// ФИЛЬТРАЦИЯ ТАБЛИЦЫ (ТОЛЬКО ДЛЯ СТРАНИЦ С ТАБЛИЦЕЙ)
// ============================================
$(document).ready(function() {
    // Проверяем, существует ли таблица на странице
    if ($('#documentsTable').length > 0 || $('#patientsTable').length > 0) {
        
        // Определяем, какая таблица используется
        const tableId = $('#documentsTable').length > 0 ? '#documentsTable' : '#patientsTable';
        
        $('#applyFilters').on('click', function() {
            const search = $('#searchInput').val().toLowerCase();
            const type = $('#typeFilter').val();
            const hospital = $('#hospitalFilter').val().toLowerCase();
            const period = $('#periodFilter').val();
            const status = $('#statusFilter').val();

            $(`${tableId} tbody tr`).each(function() {
                const row = $(this);
                const name = row.find('td:eq(2)').text().toLowerCase();
                const fileType = row.find('td:eq(3)').text().trim();
                const hospitalCode = row.find('td:eq(4)').text().toLowerCase();
                const periodText = row.find('td:eq(5)').text().trim();
                const statusText = row.find('td:eq(7)').text().trim();

                let show = true;
                if (search && !name.includes(search) && !hospitalCode.includes(search)) show = false;
                if (type && fileType !== type) show = false;
                if (hospital && !hospitalCode.includes(hospital)) show = false;
                if (period && periodText !== period) show = false;
                if (status) {
                    const isSuccess = statusText.includes('Успешно');
                    if (status === 'success' && !isSuccess) show = false;
                    if (status === 'error' && isSuccess) show = false;
                }

                row.toggle(show);
            });

            // Обновляем счетчик
            const visibleCount = $(`${tableId} tbody tr:visible`).length;
            $('#recordCount').text(visibleCount);
        });

        $('#resetFilters').on('click', function() {
            $('#searchInput').val('');
            $('#typeFilter').val('');
            $('#hospitalFilter').val('');
            $('#periodFilter').val('');
            $('#statusFilter').val('');
            $('#applyFilters').click();
        });

        // Поиск по Enter
        $('.filter-group input').on('keypress', function(e) {
            if (e.key === 'Enter') {
                $('#applyFilters').click();
            }
        });
    }
});

// ============================================
// ПАГИНАЦИЯ (ДЛЯ СТРАНИЦ С ТАБЛИЦЕЙ)
// ============================================
$(document).ready(function() {
    if ($('#documentsTable').length > 0 || $('#patientsTable').length > 0) {
        $('.pagination .page-link').on('click', function(e) {
            const parent = $(this).closest('.page-item');
            if (parent.hasClass('disabled')) return;
            if ($(this).text() === '…') return;

            $('.pagination .page-item').removeClass('active');
            parent.addClass('active');

            $('.table-wrapper')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
    }
});

// ============================================
// КНОПКИ ДЕЙСТВИЙ (ДЛЯ СТРАНИЦ С ТАБЛИЦЕЙ)
// ============================================
$(document).ready(function() {
    if ($('.btn-sm-icon.primary, .btn-sm-icon.danger').length > 0) {
        $('.btn-sm-icon.primary, .btn-sm-icon.danger').on('click', function(e) {
            e.stopPropagation();
            const action = $(this).hasClass('primary') ? 'просмотр/скачивание' : 'удаление';
            alert(`Действие: ${action}`);
        });
    }
});

// ============================================
// ОБНОВЛЕНИЕ СЧЕТЧИКА ДОКУМЕНТОВ
// ============================================
$(document).ready(function() {
    // Проверяем, есть ли элемент счетчика
    if ($('#docCount').length > 0) {
        $.get('/Export/GetCount', function(data) {
            const badge = $('#docCount');
            if (badge.length) {
                badge.text(data.count || 0);
            }
        }).fail(function() {
            $('#docCount').hide();
        });
    }
});

// ============================================
// ОБНОВЛЕНИЕ СЧЕТЧИКА ПАЦИЕНТОВ
// ============================================
$(document).ready(function() {
    if ($('#patientsCount').length > 0) {
        $.get('/Patients/GetCount', function(data) {
            const badge = $('#patientsCount');
            if (badge.length) {
                badge.text(data.count || 0);
            }
        }).fail(function() {
            $('#patientsCount').hide();
        });
    }
});