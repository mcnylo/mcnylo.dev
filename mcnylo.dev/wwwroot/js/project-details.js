function initializeProjectMediaCarousel() {
    const display = document.getElementById("project-media-display");
    const buttons = document.querySelectorAll("[data-project-media-button]");

    if (!display || buttons.length === 0) {
        return;
    }

    buttons.forEach((button) => {
        button.addEventListener("click", () => {
            selectProjectMedia(display, buttons, button);
        });
    });

    const primaryButton = [...buttons].find((button) => button.dataset.isPrimary === "true") ?? buttons[0];

    selectProjectMedia(display, buttons, primaryButton);
}

function selectProjectMedia(display, buttons, selectedButton) {
    buttons.forEach((button) => {
        button.setAttribute("aria-selected", button === selectedButton ? "true" : "false");
    });

    clearProjectMediaDisplay(display);

    const mediaType = selectedButton.dataset.mediaType;
    const mediaUrl = selectedButton.dataset.mediaUrl ?? "";
    const altText = selectedButton.dataset.altText ?? "Project media";

    if (mediaType === "VIDEO") {
        renderProjectVideo(display, mediaUrl, altText);
        return;
    }

    renderProjectImage(display, mediaUrl, altText);
}

function clearProjectMediaDisplay(display) {
    while (display.firstChild) {
        display.removeChild(display.firstChild);
    }
}

function renderProjectImage(display, mediaUrl, altText) {
    const image = document.createElement("img");

    image.src = mediaUrl;
    image.alt = altText;
    image.className = "aspect-video w-full object-cover";

    display.appendChild(image);
}

function renderProjectVideo(display, mediaUrl, title) {
    const wrapper = document.createElement("div");

    wrapper.className = "aspect-video w-full";

    const iframe = document.createElement("iframe");

    iframe.src = mediaUrl;
    iframe.title = title || "Project video";
    iframe.loading = "lazy";
    iframe.referrerPolicy = "strict-origin-when-cross-origin";
    iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";
    iframe.allowFullscreen = true;
    iframe.className = "h-full w-full";

    wrapper.appendChild(iframe);
    display.appendChild(wrapper);
}

initializeProjectMediaCarousel();