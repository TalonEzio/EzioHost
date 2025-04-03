export function initTomSelect(id) {
    let _ = new window.TomSelect(`#${id}`,
        {
            plugins: ['remove_button'],
            persist: false,
            create: false,
            closeAfterSelect: false,
            hideSelected: false
        });
}

export function getSelectedValues(id) {
    let select = document.getElementById(id);
    return [...select.selectedOptions].map(option => parseInt(option.value));
}