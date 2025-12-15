// Minimal JS helper for image compare - only for calculating position
window.getPercentagePosition = function(element, clientX) {
    if (!element) return 50;

    const rect = element.getBoundingClientRect();
    const x = clientX - rect.left;
    const percentage = Math.max(0, Math.min(100, (x / rect.width) * 100));

    return Math.round(percentage);
};