/**
 * ArdraKinetix Enterprise - Login Page Animations & Interactivity
 * Handled via GSAP and Modular DOM Event Handlers
 */

document.addEventListener('DOMContentLoaded', () => {
    initAmbientOrbs();
    initCard3DTilt();
});

/**
 * Initializes continuous organic ambient floating drift for background glow orbs.
 */
function initAmbientOrbs() {
    if (typeof gsap === 'undefined') return;

    gsap.to('#glow-orb-1', {
        x: 50,
        y: 35,
        duration: 8,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#glow-orb-2', {
        x: -45,
        y: -40,
        duration: 10,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#glow-orb-3', {
        x: 60,
        y: -45,
        duration: 12,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });

    gsap.to('#glow-orb-4', {
        x: -35,
        y: 40,
        duration: 9,
        repeat: -1,
        yoyo: true,
        ease: 'sine.inOut'
    });
}

/**
 * Initializes interactive 3D perspective tilt on the login glass card during mouse hover.
 */
function initCard3DTilt() {
    const card = document.getElementById('login-card-container');
    if (!card || window.innerWidth < 1024 || typeof gsap === 'undefined') return;

    card.addEventListener('mousemove', (e) => {
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left - rect.width / 2;
        const y = e.clientY - rect.top - rect.height / 2;

        gsap.to(card, {
            rotationY: x * 0.015,
            rotationX: -y * 0.015,
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
}

