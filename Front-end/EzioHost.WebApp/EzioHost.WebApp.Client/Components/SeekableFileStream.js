export function readFileSlice(inputFileId, fileIndex, offset, count, dotNetReference) {
    return new Promise((resolve, reject) => {
        const inputFile = document.getElementById(inputFileId);
        if (!inputFile || !inputFile.files || inputFile.files.length <= fileIndex) {
            reject("File not found.");
            return;
        }

        const file = inputFile.files[fileIndex];
        const slice = file.slice(offset, offset + count);

        const reader = new FileReader();
        reader.onload = function (event) {
            const byteArray = new Uint8Array(event.target.result);
            dotNetReference.invokeMethodAsync('ReceiveFileSlice', offset, byteArray);
            resolve();
        };
        reader.onerror = function (event) {
            reject(event.target.error);
        };
        reader.readAsArrayBuffer(slice);
    });
}

