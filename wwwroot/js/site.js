const themeButton = document.querySelector('[data-theme-toggle]');
const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
let savedTheme;
try { savedTheme = localStorage.getItem('portfolio-theme'); } catch {}
function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  const dark = theme === 'dark';
  if (!themeButton) return;
  themeButton.setAttribute('aria-pressed', String(dark));
  themeButton.setAttribute('aria-label', `Switch to ${dark ? 'light' : 'dark'} mode`);
  themeButton.querySelector('[data-theme-label]').textContent = dark ? 'Light' : 'Dark';
  themeButton.querySelector('[data-theme-icon]').textContent = dark ? '☀' : '☾';
}
applyTheme(document.documentElement.dataset.theme);
themeButton?.addEventListener('click', () => {
  savedTheme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
  applyTheme(savedTheme);
  try { localStorage.setItem('portfolio-theme', savedTheme); } catch {}
});
systemTheme.addEventListener('change', event => {
  if (savedTheme !== 'dark' && savedTheme !== 'light') applyTheme(event.matches ? 'dark' : 'light');
});

const header = document.querySelector('[data-site-header]');
const menu = document.getElementById('mobile-menu');
const menuButton = document.querySelector('[data-menu-open]');
if (menu && menuButton) {
  menuButton.addEventListener('click', () => {
    menu.showModal();
    document.body.classList.add('menu-open');
    menuButton.setAttribute('aria-expanded', 'true');
    header?.classList.remove('nav-hidden');
  });
  menu.querySelector('[data-menu-close]').addEventListener('click', () => menu.close());
  menu.querySelectorAll('a').forEach(link => link.addEventListener('click', () => menu.close()));
  menu.addEventListener('click', event => {
    const bounds = menu.getBoundingClientRect();
    if (event.target === menu && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) menu.close();
  });
  menu.addEventListener('close', () => {
    document.body.classList.remove('menu-open');
    menuButton.setAttribute('aria-expanded', 'false');
    header?.classList.remove('nav-hidden');
  });
  window.matchMedia('(min-width: 1024px)').addEventListener('change', event => {
    if (event.matches && menu.open) menu.close();
  });
}

let lastScrollY = Math.max(0, window.scrollY);
let scrollScheduled = false;
window.addEventListener('scroll', () => {
  if (scrollScheduled) return;
  scrollScheduled = true;
  window.requestAnimationFrame(() => {
    const currentY = Math.max(0, window.scrollY);
    const delta = currentY - lastScrollY;
    if (header) {
      if (currentY <= header.offsetHeight || menu?.open || header.contains(document.activeElement)) {
        header.classList.remove('nav-hidden');
        lastScrollY = currentY;
      } else if (Math.abs(delta) >= 6) {
        header.classList.toggle('nav-hidden', delta > 0);
        lastScrollY = currentY;
      }
    }
    scrollScheduled = false;
  });
}, { passive: true });

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
