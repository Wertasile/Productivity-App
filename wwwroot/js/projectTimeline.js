window.projectTimeline = {
    scrollToOffset(element, offset, sideColumnWidth) {
        if (!element) {
            return;
        }

        const safeOffset = Math.max(0, Number(offset) || 0);
        const stickyWidth = Math.max(0, Number(sideColumnWidth) || 0);
        const maxScrollLeft = Math.max(0, element.scrollWidth - element.clientWidth);
        const desiredScrollLeft = Math.min(maxScrollLeft, Math.max(0, safeOffset));

        element.scrollLeft = desiredScrollLeft;

        if (element.scrollLeft < desiredScrollLeft && stickyWidth > 0) {
            element.scrollLeft = Math.min(maxScrollLeft, desiredScrollLeft + stickyWidth);
        }
    },

    scrollByViewport(element, direction, factor) {
        if (!element) {
            return;
        }

        const normalizedDirection = direction >= 0 ? 1 : -1;
        const viewportFactor = Math.max(0.2, Number(factor) || 0.72);
        const delta = element.clientWidth * viewportFactor * normalizedDirection;

        element.scrollBy({
            left: delta,
            top: 0,
            behavior: "smooth"
        });
    }
};