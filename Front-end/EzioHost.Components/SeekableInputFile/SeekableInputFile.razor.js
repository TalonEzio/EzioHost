const fileRegistry = new Map();

async function calculateFileHash(file) {
    const startTime = performance.now();
    const fileSizeMb = (file.size / (1024 * 1024)).toFixed(2);
    console.log(`[Hash] Bắt đầu tính hash cho file: ${file.name} (${fileSizeMb} MB)`);

    const tenMb = 10 * 1024 * 1024;
    const fiveMb = 5 * 1024 * 1024;

    let hash;
    if (file.size < tenMb) {
        console.log(`[Hash] File < 10MB, sử dụng full hash`);
        const readStart = performance.now();
        const arrayBuffer = await file.arrayBuffer();
        const readTime = (performance.now() - readStart).toFixed(2);
        console.log(`[Hash] Đọc file: ${readTime}ms`);

        const hashStart = performance.now();
        const hashBuffer = await crypto.subtle.digest("SHA-256", arrayBuffer);
        const hashTime = (performance.now() - hashStart).toFixed(2);
        console.log(`[Hash] Tính hash: ${hashTime}ms`);

        const hashArray = Array.from(new Uint8Array(hashBuffer));
        hash = hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
    } else {
        console.log(`[Hash] File >= 10MB, sử dụng partial hash (5MB đầu + 5MB cuối)`);
        const samples = [];

        const readStart = performance.now();
        const startChunk = file.slice(0, fiveMb);
        samples.push(new Uint8Array(await startChunk.arrayBuffer()));

        const endStart = Math.max(0, file.size - fiveMb);
        const endChunk = file.slice(endStart, file.size);
        samples.push(new Uint8Array(await endChunk.arrayBuffer()));
        const readTime = (performance.now() - readStart).toFixed(2);
        console.log(`[Hash] Đọc chunks (5MB đầu + 5MB cuối): ${readTime}ms`);

        const combineStart = performance.now();
        const totalSize = samples.reduce((sum, s) => sum + s.length, 0);
        const combined = new Uint8Array(totalSize);
        let pos = 0;
        for (const sample of samples) {
            combined.set(sample, pos);
            pos += sample.length;
        }

        const sizeBuffer = new window.TextEncoder().encode(file.size.toString());
        const finalBuffer = new Uint8Array(combined.length + sizeBuffer.length);
        finalBuffer.set(combined, 0);
        finalBuffer.set(sizeBuffer, combined.length);
        const combineTime = (performance.now() - combineStart).toFixed(2);
        console.log(`[Hash] Kết hợp chunks: ${combineTime}ms`);

        const hashStart = performance.now();
        const hashBuffer = await crypto.subtle.digest("SHA-256", finalBuffer);
        const hashTime = (performance.now() - hashStart).toFixed(2);
        console.log(`[Hash] Tính hash: ${hashTime}ms`);

        const hashArray = Array.from(new Uint8Array(hashBuffer));
        hash = hashArray.map(b => b.toString(16).padStart(2, "0")).join("");
    }

    const totalTime = (performance.now() - startTime).toFixed(2);
    console.log(`[Hash] Hoàn thành hash cho ${file.name}: ${totalTime}ms (${fileSizeMb} MB)`);
    console.log(`[Hash] Hash result: ${hash.substring(0, 16)}...`);

    return hash;
}

export async function listenFilesSelected(element, dotNetRef) {
    if (!element) return;
    element.addEventListener("change",
        async (e) => {
            const files = e.target.files;
            const fileListToSend = [];

            fileRegistry.clear();

            for (let i = 0; i < files.length; i++) {
                const file = files[i];
                const fileId = crypto.randomUUID();

                fileRegistry.set(fileId, file);
                const checksum = await calculateFileHash(file);

                fileListToSend.push({
                    Id: fileId,
                    Name: file.name,
                    Size: file.size,
                    ContentType: file.type,
                    Checksum: checksum
                });
            }
            await dotNetRef.invokeMethodAsync("OnFileSelected", fileListToSend);
        });
}


export async function readSlice(elementId, start, end) {
    const file = fileRegistry.get(elementId);
    if (!file) return null;

    const slice = file.slice(start, end);
    const buffer = await slice.arrayBuffer();

    console.log(buffer);
    return new Uint8Array(buffer);
}

export function disposeFile(elementId) {
    fileRegistry.delete(elementId);
}