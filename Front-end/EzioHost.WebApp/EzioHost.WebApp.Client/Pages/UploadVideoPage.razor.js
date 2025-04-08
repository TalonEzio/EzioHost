export async function getFileStream(inputElement) {
    if (!inputElement || !inputElement.files.length) {
        throw new Error("No file selected");
    }

    const file = inputElement.files[0];
    return file.stream();
}