(() => {
    const hotel = document.querySelector("[data-team-hotel]");
    const room = document.querySelector("[data-team-room]");
    if (!hotel || !room) return;
    hotel.addEventListener("change", async () => {
        const selected = hotel.value;
        room.disabled = true;
        try {
            const request = new URL(hotel.dataset.roomEndpoint, window.location.origin);
            request.searchParams.set("hotelId", selected);
            const response = await fetch(request);
            if (!response.ok || hotel.value !== selected) return;
            const options = await response.json();
            room.replaceChildren(...options.map(option => new Option(option.name, option.id)));
        } catch {
            if (hotel.value === selected) room.replaceChildren(new Option("กรุณาเลือกโรงแรมอีกครั้ง", ""));
        } finally {
            if (hotel.value === selected) room.disabled = false;
        }
    });
})();
