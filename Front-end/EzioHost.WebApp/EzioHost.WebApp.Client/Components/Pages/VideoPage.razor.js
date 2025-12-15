export function playVideo(id, videoUrl) {
    console.log(videoUrl);
    const player = new window.Playerjs({
        id: id,
        file: videoUrl,
        hls: 1
    });
}