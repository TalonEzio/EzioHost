// Minimal JS helper for image compare - for calculating position
window.getPercentagePosition = function(element, clientX, clientY, isVertical) {
    if (!element) return 50;

    const rect = element.getBoundingClientRect();

    if (isVertical) {
        // Vertical orientation: use Y coordinate (top-bottom)
        const y = clientY - rect.top;
        const percentage = Math.max(0, Math.min(100, (y / rect.height) * 100));
        return Math.round(percentage);
    } else {
        // Horizontal orientation: use X coordinate (left-right)
        const x = clientX - rect.left;
        const percentage = Math.max(0, Math.min(100, (x / rect.width) * 100));
        return Math.round(percentage);
    }
};