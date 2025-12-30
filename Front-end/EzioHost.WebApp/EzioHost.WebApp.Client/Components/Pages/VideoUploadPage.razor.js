export function setupDragAndDrop(dropZone, dotNetRef) {
    if (!dropZone) return;

    const dropZoneElement = dropZone;
    const fileInputElement = document.getElementById("videoFileInput");

    if (!fileInputElement) {
        console.warn("File input element not found");
        return;
    }

    // Prevent default drag behaviors
    ["dragenter", "dragover", "dragleave", "drop"].forEach(eventName => {
        dropZoneElement.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Highlight drop zone when item is dragged over it
    ["dragenter", "dragover"].forEach(eventName => {
        dropZoneElement.addEventListener(eventName,
            () => {
                dropZoneElement.classList.add("drag-over");
                dotNetRef.invokeMethodAsync("SetDragOver", true);
            },
            false);
    });

    ["dragleave", "drop"].forEach(eventName => {
        dropZoneElement.addEventListener(eventName,
            () => {
                dropZoneElement.classList.remove("drag-over");
                dotNetRef.invokeMethodAsync("SetDragOver", false);
            },
            false);
    });

    // Handle dropped files
    dropZoneElement.addEventListener("drop",
        (e) => {
            const dt = e.dataTransfer;
            const files = dt.files;

            if (files.length === 0) return;

            // Filter only video files
            const videoFiles = Array.from(files).filter(file => file.type.startsWith("video/"));

            if (videoFiles.length === 0) {
                dotNetRef.invokeMethodAsync("ShowWarning", "Vui lòng chọn file video");
                return;
            }

            // Create a new DataTransfer object to set files
            const dataTransfer = new DataTransfer();

            // Add existing files
            if (fileInputElement.files) {
                for (let i = 0; i < fileInputElement.files.length; i++) {
                    dataTransfer.items.add(fileInputElement.files[i]);
                }
            }

            // Add dropped video files
            videoFiles.forEach(file => {
                dataTransfer.items.add(file);
            });

            // Set the files to the input
            fileInputElement.files = dataTransfer.files;

            // Trigger change event to notify SeekableInputFile
            const changeEvent = new Event("change", { bubbles: true });
            fileInputElement.dispatchEvent(changeEvent);
        },
        false);
}

export function clickFileInput() {
    const fileInput = document.getElementById("videoFileInput");
    if (fileInput && fileInput.click) {
        fileInput.click();
    }
}