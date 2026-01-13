export function playVideo(id, metadata, subtitle) {
    console.log(metadata);
    const playerConfig = {
        id: id,
        file: metadata,
        hls: 1
    };
    
    if (subtitle && subtitle.trim() !== '') {
        playerConfig.subtitle = subtitle;
    }
    
    const player = new window.Playerjs(playerConfig);
}