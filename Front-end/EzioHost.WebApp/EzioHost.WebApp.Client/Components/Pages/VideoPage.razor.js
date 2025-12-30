export function playVideo(id, metadata) {
    console.log(metadata);
    const player = new window.Playerjs({
        id: id,
        file: metadata,
        hls: 1
    });
}