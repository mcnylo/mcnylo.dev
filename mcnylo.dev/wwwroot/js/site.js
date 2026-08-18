document.addEventListener("DOMContentLoaded", () => {
    const copyEmailButton = document.getElementById("copy-email");
    const copyEmailToast = document.getElementById("copy-email-toast");

    if (!copyEmailButton || !copyEmailToast) return;

    let toastTimeout;

    copyEmailButton.addEventListener("click", async () => {
        const email = `${copyEmailButton.dataset.emailUser}@${copyEmailButton.dataset.emailDomain}`;

        try {
            await navigator.clipboard.writeText(email);

            copyEmailToast.classList.remove("hidden");

            clearTimeout(toastTimeout);

            toastTimeout = setTimeout(() => {
                copyEmailToast.classList.add("hidden");
            }, 2500);
        }
        catch {
            console.error("Unable to copy email address to clipboard.");
        }
    });
});