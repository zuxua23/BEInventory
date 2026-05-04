    window.Table = {

        init(selector, { data, columns }) {

            if ($.fn.DataTable.isDataTable(selector)) {
                $(selector).DataTable().clear().destroy();
            }

            return $(selector).DataTable({
                data: data,
                responsive: true,
                pageLength: 5,
                lengthMenu: [[5, 10, 25], [5, 10, 25]],

                columns: columns,

                language: {
                    search: "_INPUT_",
                    searchPlaceholder: " Search...",
                    lengthMenu: "Show _MENU_",
                    paginate: { first: "«", last: "»", next: "›", previous: "‹" },
                    info: "Showing _START_ to _END_ of _TOTAL_ data"
                },

                dom: "<'row mb-3'<'col-md-6'l><'col-md-6'f>>" +
                    "<'table-responsive card-body p-0't>" +
                    "<'row mt-3'<'col-md-5'i><'col-md-7'p>>",

                autoWidth: false,
                deferRender: true,

                drawCallback: function () {

                    const api = this.api();
                    const info = api.page.info();

                    const current = info.page;
                    const total = info.pages;

                    let start = Math.max(current - 1, 0);
                    let end = Math.min(start + 3, total);

                    if (end - start < 3) {
                        start = Math.max(end - 3, 0);
                    }

                    const pagination = $(this).closest('.dataTables_wrapper').find('.dataTables_paginate ul');
                    pagination.find("li").hide();

                    pagination.find('.previous').show();
                    pagination.find('.next').show();

                    pagination.find('li:first').show();
                    pagination.find('li:last').show();

                    pagination.find('li').each(function (index) {
                        if (index - 1 >= start && index - 1 < end) {
                            $(this).show();
                        }
                    });
                }
            });
        },

        rowNumber() {
            return {
                data: null,
                render: (d, t, r, m) =>
                    m.row + m.settings._iDisplayStart + 1,
                orderable: false,
                searchable: false,
                className: "text-center",
                width: "5%"
            };
        },

        action(renderFn, visible = true) {
            return {
                data: null,
                render: renderFn,
                visible: visible,
                orderable: false,
                className: "text-center",
                width: "20%"
            };
        }
    };