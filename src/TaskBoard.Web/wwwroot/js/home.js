/**
 * ArdraKinetix Enterprise - Home / Landing Page Interactivity
 * Handled via GSAP and Modular DOM Event Handlers
 */

document.addEventListener('DOMContentLoaded', () => {
    initHomeAmbientOrbs();
    initSmoothAnchors();
    initArchitectureCardHover();
});

/**
 * Initializes continuous ambient floating drift for background glow orbs.
 */
function initHomeAmbientOrbs() {
    if (typeof gsap === 'undefined') return;

    gsap.to('#home-glow-orb-1', {
        x: 60,
        y: 40,
        duration: 9,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#home-glow-orb-2', {
        x: -50,
        y: -45,
        duration: 11,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#home-glow-orb-3', {
        x: 70,
        y: -50,
        duration: 13,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#home-glow-orb-4', {
        x: -40,
        y: 50,
        duration: 10,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });
}

/**
 * Initializes smooth scrolling on internal navigation anchor links.
 */
function initSmoothAnchors() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#' || !targetId) return;

            const targetEl = document.querySelector(targetId);
            if (targetEl) {
                e.preventDefault();
                targetEl.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

/**
 * Interactive 3D tilt on key hero and architecture feature cards.
 */
function initArchitectureCardHover() {
    const cards = document.querySelectorAll('.interactive-tilt-card');
    if (!cards.length || window.innerWidth < 1024 || typeof gsap === 'undefined') return;

    cards.forEach(card => {
        card.addEventListener('mousemove', (e) => {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left - rect.width / 2;
            const y = e.clientY - rect.top - rect.height / 2;

            gsap.to(card, {
                rotationY: x * 0.012,
                rotationX: -y * 0.012,
                transformPerspective: 1000,
                ease: 'power1.out',
                duration: 0.3
            });
        });

        card.addEventListener('mouseleave', () => {
            gsap.to(card, {
                rotationY: 0,
                rotationX: 0,
                ease: 'power2.out',
                duration: 0.5
            });
        });
    });
}
