(function () {
  'use strict';

  const root = document.querySelector('[data-header-account]');
  const layer = document.querySelector('[data-account-layer]');

  if (!root || !layer) {
    return;
  }

  const trigger = root.querySelector('[data-account-trigger]');
  const panel = layer.querySelector('[data-account-panel]');
  const closeControls = layer.querySelectorAll('[data-account-close]');
  const filters = Array.from(layer.querySelectorAll('[data-account-filter]'));
  const notifications = Array.from(layer.querySelectorAll('[data-account-notification]'));

  let closeTimer = null;

  function updatePosition() {
    if (!trigger || !panel) {
      return;
    }

    const rect = trigger.getBoundingClientRect();
    const header = document.getElementById('site-header');
    const headerBottom = header?.getBoundingClientRect().bottom || 0;
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const panelWidth = Math.min(350, viewportWidth - 24);
    const minCenter = 12 + panelWidth / 2;
    const maxCenter = viewportWidth - 12 - panelWidth / 2;
    const preferredCenter = rect.left + rect.width / 2;
    const panelCenter = Math.min(Math.max(preferredCenter, minCenter), maxCenter);
    const panelLeft = panelCenter - panelWidth / 2;
    const arrowLeft = Math.min(Math.max(preferredCenter - panelLeft, 22), panelWidth - 22);
    const panelTop = Math.max(rect.bottom + 12, headerBottom + 10);

    layer.style.setProperty('--account-backdrop-top', `${Math.round(headerBottom)}px`);
    layer.style.setProperty('--account-panel-top', `${Math.round(panelTop)}px`);
    layer.style.setProperty('--account-panel-left', `${Math.round(panelCenter)}px`);
    layer.style.setProperty('--account-panel-width', `${Math.round(panelWidth)}px`);
    layer.style.setProperty('--account-arrow-left', `${Math.round(arrowLeft)}px`);
  }

  function openAccount() {
    window.clearTimeout(closeTimer);
    closeSearch();
    closeCategoryMenu();
    updatePosition();

    layer.hidden = false;
    trigger?.setAttribute('aria-expanded', 'true');
    document.body.classList.add('site-account-open');

    window.requestAnimationFrame(() => {
      layer.classList.add('is-open');
    });
  }

  function closeAccount(restoreFocus = false) {
    layer.classList.remove('is-open');
    trigger?.setAttribute('aria-expanded', 'false');
    document.body.classList.remove('site-account-open');

    closeTimer = window.setTimeout(() => {
      if (!layer.classList.contains('is-open')) {
        layer.hidden = true;
      }
    }, 180);

    if (restoreFocus) {
      trigger?.focus({ preventScroll: true });
    }
  }

  function closeSearch() {
    const searchLayer = document.querySelector('[data-search-layer].is-open');
    searchLayer?.querySelector('[data-search-close]')?.click();
  }

  function closeCategoryMenu() {
    const categoryMenu = document.querySelector('[data-category-menu].is-open');
    categoryMenu?.querySelector('[data-category-menu-close]')?.click();
  }

  function setFilter(filterName) {
    filters.forEach((filter) => {
      const isActive = filter.dataset.accountFilter === filterName;
      filter.classList.toggle('is-active', isActive);
      filter.setAttribute('aria-selected', String(isActive));
    });

    notifications.forEach((notification) => {
      const isVisible = filterName === 'all'
        || notification.dataset.accountCategory === filterName;
      notification.hidden = !isVisible;
    });
  }

  trigger?.addEventListener('click', () => {
    if (layer.hidden || !layer.classList.contains('is-open')) {
      openAccount();
      return;
    }

    closeAccount(true);
  });

  closeControls.forEach((control) => {
    control.addEventListener('click', () => closeAccount());
  });

  filters.forEach((filter) => {
    filter.addEventListener('click', () => {
      setFilter(filter.dataset.accountFilter || 'all');
    });
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && layer.classList.contains('is-open')) {
      event.preventDefault();
      closeAccount(true);
    }
  });

  window.addEventListener('resize', () => {
    if (layer.classList.contains('is-open')) {
      updatePosition();
    }
  }, { passive: true });
})();
