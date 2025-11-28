export async function loadVideo(videoId) {
    try {
        const response = await fetch(`/api/video/${videoId}`);
        const videoData = await response.json();
        if (videoData && videoData.m3U8Location) {
            const player = new window.Playerjs({
                id: "player",
                file: videoData.m3U8Location,
                hls: 1
            });
        }
    } catch (error) {
        console.error("Failed to load video", error);
    }
}

