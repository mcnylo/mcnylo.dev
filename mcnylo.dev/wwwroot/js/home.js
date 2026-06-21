const HOME_TYPEWRITER_TEXT = document.querySelector("#typewriter-text");

if (HOME_TYPEWRITER_TEXT) {
    const phrases = [
        "Developer Portfolio",
        "Current C# Developer",
        "Yes, I started in Help Desk",
        "Exploring Real-Time Systems Programming",
        "Don't forget to check out my other tech projects!"
    ]

    let phraseIndex = 0;
    let characterIndex = 0;

    let isDeleting = false;

    const TYPE_SPEED = 40;
    const DELETE_SPEED = 45;
    const PHRASE_TTL = 2000;
    const NO_PHRASE_TTL = 400;

    function typeLoop() {
        const CURRENT_PHRASE = phrases[phraseIndex];

        if (isDeleting) {
            characterIndex--;
        }
        else {
            characterIndex++;
        }

        HOME_TYPEWRITER_TEXT.textContent = CURRENT_PHRASE.substring(0, characterIndex);

        let delay;

        if (isDeleting) {
            delay = DELETE_SPEED;
        }
        else {
            delay = TYPE_SPEED;
        }

        if (!isDeleting && characterIndex === CURRENT_PHRASE.length) {
            delay = PHRASE_TTL;
            isDeleting = true;
        }
        else if (isDeleting && characterIndex === 0) {
            isDeleting = false;
            phraseIndex = (phraseIndex + 1) % phrases.length;
            delay = NO_PHRASE_TTL;
        }

        setTimeout(typeLoop, delay);
    }

    typeLoop();
}