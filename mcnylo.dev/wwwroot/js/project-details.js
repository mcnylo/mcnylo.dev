function initializeProjectMediaCarousel() {
    const carousel = document.getElementById("project-media-carousel");

    if (!carousel) {
        return;
    }

    const mediaDisplay = document.getElementById("project-media-display");
    const mediaButtons = carousel.querySelectorAll("[data-project-media-button]");

    if (!mediaDisplay || mediaButtons.length === 0) {
        return;
    }

    mediaButtons.forEach((mediaButton) => {
        mediaButton.addEventListener("click", () => {
            setActiveProjectMedia(mediaButton, mediaDisplay, mediaButtons);
        });
    });

    const primaryMediaButton = carousel.querySelector("[data-project-media-button][data-is-primary='true']");
    const initialMediaButton = primaryMediaButton ?? mediaButtons[0];

    setActiveProjectMedia(initialMediaButton, mediaDisplay, mediaButtons);
}

function setActiveProjectMedia(mediaButton, mediaDisplay, mediaButtons) {
    mediaButtons.forEach((button) => {
        button.setAttribute("aria-selected", "false");
    });

    mediaButton.setAttribute("aria-selected", "true");

    const mediaType = normalizeProjectMediaType(mediaButton.dataset.mediaType);
    const mediaURL = mediaButton.dataset.mediaUrl ?? "";
    const altText = mediaButton.dataset.altText ?? "";

    mediaDisplay.replaceChildren();

    if (mediaType === "image") {
        mediaDisplay.appendChild(createProjectImageElement(mediaURL, altText));
        return;
    }

    if (isProjectVideoMedia(mediaType, mediaURL)) {
        mediaDisplay.appendChild(createProjectVideoElement(mediaURL, altText));
        return;
    }

    mediaDisplay.appendChild(createProjectMediaFallbackLink(mediaURL));
}

function normalizeProjectMediaType(mediaType) {
    return (mediaType ?? "").trim().toLowerCase();
}

function isProjectVideoMedia(mediaType, mediaURL) {
    return mediaType === "video"
        || mediaType.includes("youtube")
        || getYouTubeVideoId(mediaURL) !== "";
}

function createProjectImageElement(mediaURL, altText) {
    const image = document.createElement("img");

    image.src = mediaURL;
    image.alt = altText;
    image.className = "aspect-video w-full object-cover";

    return image;
}

function createProjectVideoElement(mediaURL, altText) {
    const youtubeEmbedURL = getYouTubeEmbedURL(mediaURL);

    if (youtubeEmbedURL) {
        return createProjectYouTubeElement(youtubeEmbedURL, altText);
    }

    return createProjectMediaFallbackLink(mediaURL);
}

function createProjectYouTubeElement(embedURL, altText) {
    const iframe = document.createElement("iframe");

    iframe.src = embedURL;
    iframe.title = altText || "Project video";
    iframe.className = "aspect-video w-full";
    iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";
    iframe.allowFullscreen = true;
    iframe.referrerPolicy = "strict-origin-when-cross-origin";

    return iframe;
}

function createProjectMediaFallbackLink(mediaURL) {
    const link = document.createElement("a");

    link.href = mediaURL;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.className = "flex aspect-video w-full items-center justify-center text-white/70";
    link.textContent = "Open media";

    return link;
}

function getYouTubeEmbedURL(mediaURL) {
    const videoId = getYouTubeVideoId(mediaURL);

    if (!videoId) {
        return "";
    }

    return `https://www.youtube-nocookie.com/embed/${videoId}`;
}

function getYouTubeVideoId(mediaURL) {
    try {
        const url = new URL(mediaURL);
        const hostname = url.hostname.toLowerCase();

        if (hostname.includes("youtu.be")) {
            return url.pathname.split("/").filter(Boolean)[0] ?? "";
        }

        const isYouTubeHost =
            hostname.includes("youtube.com") ||
            hostname.includes("youtube-nocookie.com");

        if (!isYouTubeHost) {
            return "";
        }

        const watchVideoId = url.searchParams.get("v");

        if (watchVideoId) {
            return watchVideoId;
        }

        const pathParts = url.pathname.split("/").filter(Boolean);

        const embedIndex = pathParts.indexOf("embed");
        const shortsIndex = pathParts.indexOf("shorts");
        const liveIndex = pathParts.indexOf("live");

        if (embedIndex >= 0 && pathParts[embedIndex + 1]) {
            return pathParts[embedIndex + 1];
        }

        if (shortsIndex >= 0 && pathParts[shortsIndex + 1]) {
            return pathParts[shortsIndex + 1];
        }

        if (liveIndex >= 0 && pathParts[liveIndex + 1]) {
            return pathParts[liveIndex + 1];
        }

        return "";
    }
    catch {
        return "";
    }
}

initializeProjectMediaCarousel();