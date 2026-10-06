document.querySelectorAll('[data-menu-toggle]').forEach(button => {
  const menu = document.getElementById(button.dataset.menuToggle);
  button.addEventListener('click', () => { menu.hidden = !menu.hidden; button.setAttribute('aria-expanded', String(!menu.hidden)); });
  menu.querySelectorAll('a').forEach(link => link.addEventListener('click', () => { menu.hidden = true; button.setAttribute('aria-expanded', 'false'); }));
});
