export function initTomSelect(selectId) {
    new window.TomSelect(`#${selectId}`, {
        plugins: ['remove_button'],
        placeholder: 'Select video types...',
        maxItems: null
    });
}

export function getSelectedValues(selectId) {
    const select = document.getElementById(selectId);
    if (!select) return [];
    
    const tomSelect = select.tomselect;
    if (tomSelect) {
        return tomSelect.getValue().map(v => parseInt(v));
    }
    
    // Fallback if TomSelect not initialized
    const selected = Array.from(select.selectedOptions).map(opt => parseInt(opt.value));
    return selected;
}
