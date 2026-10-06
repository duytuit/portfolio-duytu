document.querySelectorAll('[data-menu-toggle]').forEach(button => {
  const menu = document.getElementById(button.dataset.menuToggle);
  button.addEventListener('click', () => { menu.hidden = !menu.hidden; button.setAttribute('aria-expanded', String(!menu.hidden)); });
  menu.querySelectorAll('a').forEach(link => link.addEventListener('click', () => { menu.hidden = true; button.setAttribute('aria-expanded', 'false'); }));
});

// Initialize once per MVC page; libraries are served locally rather than by CDN.
const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
if (window.AOS && !reducedMotion.matches) {
  try {
    document.documentElement.classList.add('aos-enabled');
    AOS.init({ duration: 1200, offset: 80, once: false, mirror: false });
    window.addEventListener('load', () => AOS.refresh(), { once: true });
    // Keyboard navigation must reveal a section immediately, even before scrolling.
    document.addEventListener('focusin', event => {
      let element = event.target.closest('[data-aos]');
      while (element) {
        element.classList.add('aos-animate');
        element = element.parentElement?.closest('[data-aos]');
      }
    });
  } catch (error) {
    document.documentElement.classList.remove('aos-enabled');
    console.warn('Scroll animations could not initialize.', error);
  }
}

if (window.gsap) {
  const motion = gsap.matchMedia();
  motion.add('(prefers-reduced-motion: no-preference)', () => {
    const hero = document.querySelector('[data-hero-float]');
    if (!hero) return;
    hero.classList.add('gsap-animated');
    gsap.to(hero, { duration: 2, y: -100, yoyo: true, repeat: -1, ease: 'power1.inOut' });
    return () => hero.classList.remove('gsap-animated');
  });
}
