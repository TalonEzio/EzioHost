(function () {
    // 1. Initialize video player
    function initializePlayer() {
        try {
            const playerElement = document.getElementById("player");
            const videoData = playerElement?.getAttribute("data-video-player");
            const posterData = playerElement?.getAttribute("data-video-poster");

            const dumpUrlSubtitle = 'https://gist.githubusercontent.com/samdutton/ca37f3adaf4e23679957b8083e061177/raw/e19399fbccbc069a2af4266e5120ae6bad62699a/sample.vtt';

            if (videoData && playerElement) {
                if (window.Playerjs) {
                    new window.Playerjs({
                        id: "player",
                        file: videoData,
                        hls: 1,
                        subtitle: `[Vietnamese]${dumpUrlSubtitle},[English]${dumpUrlSubtitle},[AI]${dumpUrlSubtitle}`,
                        poster: posterData
                    });
                } else {
                    console.warn("Playerjs library not found");
                }
            }
        } catch (error) {
            console.error("Failed to load video player", error);
        }
    }

    // 2. Generate embed link and iframe code
    window.generateEmbedInfo = function () {
        try {
            const videoIdElement = document.getElementById("videoId");
            if (!videoIdElement) return;

            const videoId = videoIdElement.getAttribute("data-video-id");
            if (!videoId) return;

            const origin = window.location.origin;
            const embedLink = `${origin}/video-embed/${videoId}`;
            const iframeCode = `<iframe src="${embedLink}" width="560" height="315" frameborder="0" allowfullscreen></iframe>`;

            const embedLinkInput = document.getElementById("embedLink");
            if (embedLinkInput) embedLinkInput.value = embedLink;

            const iframeCodeTextarea = document.getElementById("iframeCode");
            if (iframeCodeTextarea) iframeCodeTextarea.value = iframeCode;

        } catch (error) {
            console.error("Failed to generate embed info", error);
        }
    };

    // 3. Copy to clipboard function (NEW API)
    window.copyToClipboard = function (elementId) {
        try {
            const element = document.getElementById(elementId);
            if (!element) return;

            const textToCopy = element.value || element.innerText;

            if (!navigator.clipboard) {
                fallbackCopyText(element);
                return;
            }

            navigator.clipboard.writeText(textToCopy)
                .then(() => {
                    showCopyFeedback(element);
                })
                .catch(err => {
                    console.error("Failed to copy: ", err);
                    alert("Lỗi khi sao chép. Vui lòng thử lại.");
                });

        } catch (error) {
            console.error("Error in copy function", error);
        }
    };

    function showCopyFeedback(element) {
        const button = element.nextElementSibling;
        if (!button) return;

        if (!button.dataset.originalHtml) {
            button.dataset.originalHtml = button.innerHTML;
            button.dataset.originalClass = button.className;
        }

        button.innerHTML = '<i class="bi bi-check-lg"></i> Đã sao chép!';
        button.className = 'btn btn-success';

        setTimeout(() => {
            button.innerHTML = button.dataset.originalHtml;
            button.className = button.dataset.originalClass;
        }, 2000);
    }
    function fallbackCopyText(element) {
        element.select();
        document.execCommand('copy');
        showCopyFeedback(element);
    }

    // 4. Initialize page
    function initializePage() {
        initializePlayer();
        generateEmbedInfo();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePage);
    } else {
        setTimeout(initializePage, 100);
    }
})();