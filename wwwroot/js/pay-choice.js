// Checkout: show the transfer fields only when "โอนเงิน" is picked. Without JS both stay visible.
(function () {
    document.querySelectorAll("[data-transfer]").forEach(function (box) {
        var form = box.closest("form");
        var radios = form.querySelectorAll("[data-pay]");
        function update() {
            var transfer = form.querySelector("[data-pay]:checked");
            var on = transfer && transfer.value === "Transfer";
            box.hidden = !on;
            box.querySelectorAll("input").forEach(function (i) { i.disabled = !on; });
        }
        radios.forEach(function (r) { r.addEventListener("change", update); });
        update();
    });
})();
