(function () {
  'use strict';

  const trigger = document.querySelector('[data-category-menu-trigger]');
  const menu = document.querySelector('[data-category-menu]');
  const panel = menu?.querySelector('[data-category-menu-panel]');

  if (!trigger || !menu || !panel) {
    return;
  }

  const closeControls = menu.querySelectorAll('[data-category-menu-close]');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const focusableSelector = 'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])';
  let closeTimer = null;

  function updatePosition() {
    const header = document.getElementById('site-header');
    const headerBottom = header?.getBoundingClientRect().bottom;

    if (headerBottom) {
      menu.style.setProperty('--category-menu-top', `${Math.round(headerBottom)}px`);
    }
  }

  function getFocusableElements() {
    return Array.from(panel.querySelectorAll(focusableSelector))
      .filter((element) => !element.hidden);
  }

  function openMenu() {
    window.clearTimeout(closeTimer);
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
