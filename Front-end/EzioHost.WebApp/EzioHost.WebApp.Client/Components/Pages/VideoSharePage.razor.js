(function() {
    try {
        const playerElementId = "player";
        const playerElement = document.getElementById(playerElementId);
        const videoData = playerElement.getAttribute("data-video");

        if (videoData) {
            console.log("Load video ok");
            console.log(videoData);

            const player = new window.Playerjs({
                id: playerElementId,
                file: videoData,
                hls: 1
            });
        }
    } catch (error) {
        console.error("Failed to load video", error);
    }
})();