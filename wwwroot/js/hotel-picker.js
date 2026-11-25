// Add/edit user: show the hotel checkboxes only for the HotelStaff role, filter them by name or city,
// and keep a live count. Hidden checkboxes are disabled so they are not sent with another role.
(function () {
    document.querySelectorAll("[data-hotel-picker]").forEach(function (picker) {
        var form = picker.closest("form");
        var role = form && form.querySelector("select[name='Role']");
        var boxes = picker.querySelectorAll("input[name='HotelIds']");
        var count = picker.querySelector("[data-hotel-count]");
        var filter = picker.querySelector("[data-hotel-filter]");

        function updateCount() {
            var n = 0;
            boxes.forEach(function (b) { if (b.checked) n++; });
            if (count) count.textContent = "เลือกแล้ว " + n + " แห่ง";
        }
        function updateRole() {
            if (!role) return;
            var on = role.value === picker.getAttribute("data-role");
            picker.hidden = !on;
            boxes.forEach(function (b) { b.disabled = !on; });
        }
        boxes.forEach(function (b) { b.addEventListener("change", updateCount); });
        if (filter) filter.addEventListener("input", function () {
            var q = filter.value.trim().toLowerCase();
            picker.querySelectorAll("[data-hotel-search]").forEach(function (item) {
                item.hidden = q !== "" && item.getAttribute("data-hotel-search").indexOf(q) < 0;
            });
        });
        if (role) role.addEventListener("change", updateRole);
        updateRole();
        updateCount();
    });
})();
