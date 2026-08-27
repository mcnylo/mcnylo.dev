function initializeProjectMediaCarousel() {
    const display = document.getElementById("project-media-display");
    const buttons = [...document.querySelectorAll("[data-project-media-button]")];
    const previousButton = document.querySelector("[data-project-media-previous]");
    const nextButton = document.querySelector("[data-project-media-next]");

    if (!display || buttons.length === 0) {
        return;
    }

    buttons.forEach((button) => {
        button.addEventListener("click", () => {
            hasManualSelection = true;
            stopProjectMediaAutoRotate(autoRotateTimer);

            selectProjectMedia(display, buttons, button);
        });
    });

    previousButton?.addEventListener("click", () => {
        hasManualSelection = true;
        stopProjectMediaAutoRotate(autoRotateTimer);

        selectProjectMediaByOffset(display, buttons, -1);
    });

    nextButton?.addEventListener("click", () => {
        hasManualSelection = true;
        stopProjectMediaAutoRotate(autoRotateTimer);

        selectProjectMediaByOffset(display, buttons, 1);
    });

    let autoRotateTimer = null;
    let hasManualSelection = false;

    const primaryButton = buttons.find((button) => button.dataset.isPrimary === "true") ?? buttons[0];

    selectProjectMedia(display, buttons, primaryButton);

    if (buttons.length > 1) {
        autoRotateTimer = startProjectMediaAutoRotate(display, buttons, () => hasManualSelection);
    }
}

function startProjectMediaAutoRotate(display, buttons, hasManualSelection) {
    return window.setInterval(() => {
        if (hasManualSelection()) {
            return;
        }

        selectProjectMediaByOffset(display, buttons, 1);
    }, 5000);
}

function stopProjectMediaAutoRotate(autoRotateTimer) {
    if (autoRotateTimer == null) {
        return;
    }

    window.clearInterval(autoRotateTimer);
}

function selectProjectMediaByOffset(display, buttons, offset) {
    const selectedIndex = buttons.findIndex((button) => button.getAttribute("aria-selected") === "true");
    const currentIndex = selectedIndex >= 0 ? selectedIndex : 0;
    const nextIndex = (currentIndex + offset + buttons.length) % buttons.length;

    selectProjectMedia(display, buttons, buttons[nextIndex]);
}

function selectProjectMedia(display, buttons, selectedButton) {
    buttons.forEach((button) => {
        button.setAttribute("aria-selected", button === selectedButton ? "true" : "false");
    });

    display.classList.add("opacity-0");

    window.setTimeout(() => {
        clearProjectMediaDisplay(display);

        const mediaType = selectedButton.dataset.mediaType;
        const mediaUrl = selectedButton.dataset.mediaUrl ?? "";
        const altText = selectedButton.dataset.altText ?? "Project media";

        if (mediaType === "VIDEO") {
            renderProjectVideo(display, mediaUrl, altText);
        }
        else {
            renderProjectImage(display, mediaUrl, altText);
        }

        display.classList.remove("opacity-0");
    }, 180);
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