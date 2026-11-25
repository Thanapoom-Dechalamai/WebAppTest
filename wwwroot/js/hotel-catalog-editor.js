(() => {
    const rows = document.querySelector("[data-room-rows]");
    const template = document.getElementById("hotel-room-template");
    const add = document.querySelector("[data-room-add]");
    if (!rows || !template || !add) return;

    function updateRows() {
        Array.from(rows.children).forEach((row, index) => {
            for (const element of row.querySelectorAll("[name], [id], [for], [data-valmsg-for]")) {
                for (const attribute of ["name", "id", "for", "data-valmsg-for"]) {
                    const value = element.getAttribute(attribute);
                    if (value) element.setAttribute(attribute, value
                        .replace(/Rooms\[\d+\]/g, `Rooms[${index}]`)
                        .replace(/Rooms_\d+__/g, `Rooms_${index}__`));
                }
            }
        });
        add.disabled = rows.querySelectorAll("[data-room-remove]").length >= 30;
    }

    add.addEventListener("click", () => {
        rows.insertAdjacentHTML("beforeend", template.innerHTML.replaceAll("__index__", String(rows.children.length)));
        updateRows();
        rows.lastElementChild.querySelector("input[name$='.Name']").focus();
    });
    rows.addEventListener("click", event => {
        const remove = event.target.closest("[data-room-remove]");
        if (!remove) return;
        remove.closest("[data-room-row]").remove();
        updateRows();
        add.focus();
    });
    rows.addEventListener("input", event => {
        if (!event.target.matches("input[name$='.Name']")) return;
        const heading = event.target.closest("[data-room-row]").querySelector("[data-room-heading]");
        heading.textContent = event.target.value.trim() || "ประเภทห้องใหม่";
    });
    updateRows();
})();
