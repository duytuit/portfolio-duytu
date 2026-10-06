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

// Lightweight scroll feedback; one frame at most per scroll event.
const readingProgress = document.querySelector('[data-reading-progress]');
let progressScheduled = false;
function updateReadingProgress() {
  const distance = document.documentElement.scrollHeight - window.innerHeight;
  if (readingProgress) readingProgress.style.transform = `scaleX(${distance > 0 ? Math.min(1, Math.max(0, window.scrollY / distance)) : 0})`;
  progressScheduled = false;
}
window.addEventListener('scroll', () => {
  if (!progressScheduled) { progressScheduled = true; requestAnimationFrame(updateReadingProgress); }
}, { passive: true });
window.addEventListener('resize', updateReadingProgress);
updateReadingProgress();

const sectionLinks = [...document.querySelectorAll('.desktop-nav a:not(.nav-cta), .mobile-sidebar nav a:not(.nav-cta)')];
if ('IntersectionObserver' in window) {
  const sections = document.querySelectorAll('main > section[id]');
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      if (!entry.isIntersecting) continue;
      sectionLinks.forEach(link => {
        const target = link.getAttribute('href') === '/' ? 'home' : link.hash.slice(1);
        if (target === entry.target.id) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
      });
    }
  }, { rootMargin: '-15% 0px -65% 0px', threshold: 0 });
  sections.forEach(section => observer.observe(section));
}

if (window.gsap) {
  gsap.matchMedia().add('(prefers-reduced-motion: no-preference)', () => {
    const notes = gsap.utils.toArray('[data-floating-note]');
    notes.forEach((note, index) => gsap.to(note, { y: index % 2 ? 7 : -7, duration: 2.8 + index * .6, repeat: -1, yoyo: true, ease: 'sine.inOut' }));
  });
}

// Keep conceptual visuals visible until an editorial photograph successfully loads.
document.querySelectorAll('[data-editorial-photo]').forEach(image => {
  const revealPhoto = () => { if (image.naturalWidth > 0) image.parentElement.classList.add('photo-loaded'); };
  image.addEventListener('load', revealPhoto);
  if (image.complete) revealPhoto();
});

// Animate words without replacing line breaks, links or accented characters.
if (window.gsap && 'IntersectionObserver' in window) {
  gsap.matchMedia().add('(prefers-reduced-motion: no-preference)', () => {
    const headings = [...document.querySelectorAll('.hero-copy h1, main .section-title')];
    const originals = new Map();
    const observer = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        const heading = entry.target;
        heading.classList.add('is-revealing');
        gsap.fromTo(heading.querySelectorAll('.text-word'),
          { opacity: 0, y: 24, rotateX: -15 },
          { opacity: 1, y: 0, rotateX: 0, duration: .75, stagger: .08, ease: 'power3.out', clearProps: 'all', onComplete: () => heading.classList.remove('is-revealing') });
        observer.unobserve(heading);
      });
    }, { threshold: .15 });
    headings.forEach(heading => {
      originals.set(heading, heading.innerHTML);
      const walker = document.createTreeWalker(heading, NodeFilter.SHOW_TEXT);
      const nodes = [];
      while (walker.nextNode()) nodes.push(walker.currentNode);
      nodes.forEach(node => {
        if (!node.textContent.trim()) return;
        const fragment = document.createDocumentFragment();
        node.textContent.split(/(\s+)/).forEach(word => {
          if (/^\s+$/.test(word)) fragment.append(document.createTextNode(word));
          else { const span = document.createElement('span'); span.className = 'text-word'; span.textContent = word; fragment.append(span); }
        });
        node.replaceWith(fragment);
      });
      heading.classList.add('text-reveal');
      observer.observe(heading);
    });
    return () => {
      observer.disconnect();
      headings.forEach(heading => {
        gsap.killTweensOf(heading.querySelectorAll('.text-word'));
        heading.innerHTML = originals.get(heading);
        heading.classList.remove('text-reveal', 'is-revealing');
      });
    };
  });
}
