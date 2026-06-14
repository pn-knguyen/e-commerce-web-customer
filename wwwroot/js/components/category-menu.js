(function () {
  'use strict';

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const megaControllers = new WeakMap();

  function createMegaMenuController(root) {
    const entries = Array.from(root.querySelectorAll('[data-category-entry]'));
    const stage = root.querySelector('[data-category-mega-stage]');
    const panels = Array.from(root.querySelectorAll('[data-category-mega-panel]'));

    if (!entries.length || !stage || !panels.length) {
      return null;
    }

    let closeTimer = null;
    let activeEntry = null;

    function clearCloseTimer() {
      window.clearTimeout(closeTimer);
      closeTimer = null;
    }

    function getPanel(entry) {
      const panelId = entry?.dataset.categoryMegaPanelId;
      const target = panelId ? document.getElementById(panelId) : null;
      return target && root.contains(target) ? target : null;
    }

    function activate(entry) {
      const targetPanel = getPanel(entry);
      if (!targetPanel) {
        return;
      }

      clearCloseTimer();
      activeEntry = entry;
      root.classList.add('has-active-mega');

      entries.forEach((item) => {
        const isActive = item === entry;
        item.classList.toggle('is-active', isActive);
        item.querySelector('[data-category-link]')
          ?.setAttribute('aria-expanded', String(isActive));
      });

      panels.forEach((megaPanel) => {
        const isActive = megaPanel === targetPanel;
        megaPanel.classList.toggle('is-active', isActive);
        megaPanel.hidden = !isActive;
        megaPanel.setAttribute('aria-hidden', String(!isActive));
      });
    }

    function close() {
      clearCloseTimer();
      activeEntry = null;
      root.classList.remove('has-active-mega');

      entries.forEach((entry) => {
        entry.classList.remove('is-active');
        entry.querySelector('[data-category-link]')
          ?.setAttribute('aria-expanded', 'false');
      });

      panels.forEach((megaPanel) => {
        megaPanel.classList.remove('is-active');
        megaPanel.hidden = true;
        megaPanel.setAttribute('aria-hidden', 'true');
      });
    }

    function scheduleClose() {
      clearCloseTimer();
      closeTimer = window.setTimeout(close, 260);
    }

    function isInsideRoot(target) {
      return target instanceof Node && root.contains(target);
    }

    root.addEventListener('pointerover', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (entry && root.contains(entry)) {
        activate(entry);
      }
    });

    root.addEventListener('mouseover', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (entry && root.contains(entry)) {
        activate(entry);
      }
    });

    root.addEventListener('pointerout', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (
        entry
        && root.contains(entry)
        && !isInsideRoot(event.relatedTarget)
      ) {
        scheduleClose();
      }
    });

    root.addEventListener('mouseout', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (
        entry
        && root.contains(entry)
        && !isInsideRoot(event.relatedTarget)
      ) {
        scheduleClose();
      }
    });

    root.addEventListener('focusin', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (entry && root.contains(entry)) {
        activate(entry);
      }
    });

    root.addEventListener('focus', (event) => {
      const entry = event.target.closest('[data-category-entry]');
      if (entry && root.contains(entry)) {
        activate(entry);
      }
    }, true);

    stage.addEventListener('pointerenter', clearCloseTimer);
    stage.addEventListener('mouseover', clearCloseTimer);
    stage.addEventListener('pointerleave', (event) => {
      if (!isInsideRoot(event.relatedTarget)) {
        scheduleClose();
      }
    });
    stage.addEventListener('mouseleave', (event) => {
      if (!isInsideRoot(event.relatedTarget)) {
        scheduleClose();
      }
    });

    root.addEventListener('mouseenter', clearCloseTimer);
    root.addEventListener('mouseleave', scheduleClose);

    root.addEventListener('focusout', () => {
      window.requestAnimationFrame(() => {
        if (!root.contains(document.activeElement)) {
          close();
        }
      });
    });

    root.addEventListener('keydown', (event) => {
      const activePanel = getPanel(activeEntry);

      if (
        event.key === 'ArrowRight'
        && activeEntry?.contains(document.activeElement)
      ) {
        const firstMegaLink = activePanel?.querySelector('.site-category-mega-link');
        if (firstMegaLink) {
          event.preventDefault();
          firstMegaLink.focus();
        }
        return;
      }

      if (
        event.key === 'ArrowLeft'
        && activePanel?.contains(document.activeElement)
      ) {
        const activeCategoryLink = activeEntry?.querySelector('[data-category-link]');
        if (activeCategoryLink) {
          event.preventDefault();
          activeCategoryLink.focus();
        }
        return;
      }

      if (
        event.key === 'Escape'
        && activeEntry
        && !root.closest('[data-category-menu]')
      ) {
        const activeCategoryLink = activeEntry.querySelector('[data-category-link]');
        event.preventDefault();
        close();
        activeCategoryLink?.focus({ preventScroll: true });
      }
    });

    root.dataset.categoryMegaReady = 'true';

    return {
      close
    };
  }

  document.querySelectorAll('[data-category-mega-root]').forEach((root) => {
    const controller = createMegaMenuController(root);
    if (controller) {
      megaControllers.set(root, controller);
    }
  });

  const trigger = document.querySelector('[data-category-menu-trigger]');
  const menu = document.querySelector('[data-category-menu]');
  const panel = menu?.querySelector('[data-category-menu-panel]');

  if (!trigger || !menu || !panel) {
    return;
  }

  const closeControls = menu.querySelectorAll('[data-category-menu-close]');
  const megaController = megaControllers.get(panel);
  const focusableSelector = 'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])';
  let closeTimer = null;

  function updatePosition() {
    const header = document.getElementById('site-header');
    const headerBottom = header?.getBoundingClientRect().bottom;

    if (headerBottom) {
      menu.style.setProperty('--category-menu-top', `${Math.round(headerBottom)}px`);
    }
  }

  function closeSearch() {
    const searchLayer = document.querySelector('[data-search-layer].is-open');
    searchLayer?.querySelector('[data-search-close]')?.click();
  }

  function closeAccountMenu() {
    const accountLayer = document.querySelector('[data-account-layer].is-open');
    accountLayer?.querySelector('[data-account-close]')?.click();
  }

  function getFocusableElements() {
    return Array.from(panel.querySelectorAll(focusableSelector))
      .filter((element) => !element.hidden && element.getClientRects().length > 0);
  }

  function openMenu() {
    window.clearTimeout(closeTimer);
    closeSearch();
    closeAccountMenu();
    updatePosition();
    menu.hidden = false;
    trigger.setAttribute('aria-expanded', 'true');
    trigger.setAttribute('aria-label', 'Đóng danh mục sản phẩm');
    document.body.classList.add('site-category-menu-open');

    window.requestAnimationFrame(() => {
      menu.classList.add('is-open');
      panel.focus({ preventScroll: true });
    });
  }

  function closeMenu(restoreFocus = true) {
    if (menu.hidden) {
      return;
    }

    megaController?.close();
    menu.classList.remove('is-open');
    trigger.setAttribute('aria-expanded', 'false');
    trigger.setAttribute('aria-label', 'Mở danh mục sản phẩm');
    document.body.classList.remove('site-category-menu-open');

    const finishClose = () => {
      if (!menu.classList.contains('is-open')) {
        menu.hidden = true;
      }
    };

    if (reducedMotion.matches) {
      finishClose();
    } else {
      closeTimer = window.setTimeout(finishClose, 180);
    }

    if (restoreFocus) {
      trigger.focus({ preventScroll: true });
    }
  }

  trigger.addEventListener('click', () => {
    if (menu.hidden || !menu.classList.contains('is-open')) {
      openMenu();
    } else {
      closeMenu();
    }
  });

  closeControls.forEach((control) => {
    control.addEventListener('click', () => closeMenu());
  });

  menu.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeMenu();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const focusableElements = getFocusableElements();
    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    if (!firstElement || !lastElement) {
      event.preventDefault();
      panel.focus({ preventScroll: true });
      return;
    }

    if (document.activeElement === panel) {
      event.preventDefault();
      (event.shiftKey ? lastElement : firstElement).focus();
    } else if (event.shiftKey && document.activeElement === firstElement) {
      event.preventDefault();
      lastElement.focus();
    } else if (!event.shiftKey && document.activeElement === lastElement) {
      event.preventDefault();
      firstElement.focus();
    }
  });

  window.addEventListener('resize', () => {
    if (!menu.hidden) {
      updatePosition();
    }
  }, { passive: true });
})();
