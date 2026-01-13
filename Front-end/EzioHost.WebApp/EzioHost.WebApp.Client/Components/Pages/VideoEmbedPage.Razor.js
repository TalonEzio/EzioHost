(function() {
    try {
        const playerElementId = "player";
        const playerElement = document.getElementById(playerElementId);
        const videoData = playerElement?.getAttribute("data-video");
        const subtitleData = playerElement?.getAttribute("data-video-subtitles");

        if (videoData) {
            console.log("Load video ok");
            console.log(videoData);

            const playerConfig = {
                id: playerElementId,
                file: videoData,
                hls: 1
            };

            if (subtitleData && subtitleData.trim() !== "") {
                playerConfig.subtitle = subtitleData;
            }

            const player = new window.Playerjs(playerConfig);
        }
    } catch (error) {
        console.error("Failed to load video", error);
    }
})();