export function clickFileInput(fileInput) {
    if (fileInput && fileInput.click) {
        fileInput.click();
    }
}

export function clickFileInputById(fileInputId) {
    const fileInput = document.getElementById(fileInputId);
    if (fileInput && fileInput.click) {
        fileInput.click();
    }
}

export function findFileIndex(fileInputId, fileName, fileSize) {
    const fileInput = document.getElementById(fileInputId);
    if (!fileInput || !fileInput.files) {
        return -1;
    }

    for (let i = 0; i < fileInput.files.length; i++) {
        const file = fileInput.files[i];
        if (file.name === fileName && file.size === fileSize) {
            return i;
        }
    }

    return -1;
}

export function setupDragAndDrop(dropZone, fileInputId, dotNetRef) {
    if (!dropZone || !fileInputId) {
        return;
    }

    const dropZoneElement = dropZone;
    const fileInputElement = document.getElementById(fileInputId);

    dropZoneElement.addEventListener("dragover",
        (e) => {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync("SetDragOver", true);
        });

    dropZoneElement.addEventListener("dragleave",
        (e) => {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync("SetDragOver", false);
        });

    dropZoneElement.addEventListener("drop",
        (e) => {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync("SetDragOver", false);

            if (!e.dataTransfer || !e.dataTransfer.files) {
                return;
            }

            const files = e.dataTransfer.files;

            if (fileInputElement && fileInputElement.files) {
                // Create a new DataTransfer object to set files
                const dataTransfer = new DataTransfer();

                // Add existing files
                for (let i = 0; i < fileInputElement.files.length; i++) {
                    dataTransfer.items.add(fileInputElement.files[i]);
                }

                // Add dropped files (only video files)
                for (let i = 0; i < files.length; i++) {
                    const file = files[i];
                    if (file.type.startsWith("video/")) {
                        dataTransfer.items.add(file);
                    }
                }

                // Set the files to the input
                fileInputElement.files = dataTransfer.files;

                // Trigger change event
                const changeEvent = new Event("change", { bubbles: true });
                fileInputElement.dispatchEvent(changeEvent);
            }
        });
}