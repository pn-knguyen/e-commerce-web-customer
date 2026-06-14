(function () {
  'use strict';

  const page = document.querySelector('[data-search-result-page]');
  if (!page) return;

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const grid = page.querySelector('[data-search-product-grid]');
  const button = page.querySelector('[data-search-load-more]');

  if (!grid || !button) return;

  button.addEventListener('click', () => {
    if (button.getAttribute('aria-busy') === 'true') return;

    const hiddenItems = Array.from(
      grid.querySelectorAll('[data-search-product-item][hidden]')
    );
    const batchSize = Number.parseInt(button.dataset.batchSize || '5', 10);
    const nextItems = hiddenItems.slice(0, batchSize);

    if (nextItems.length === 0) {
      button.closest('.search-result-load-more')?.setAttribute('hidden', '');
      return;
    }

    button.setAttribute('aria-busy', 'true');
    grid.setAttribute('aria-busy', 'true');
    updateButtonLabel('Đang tải sản phẩm');

    window.setTimeout(() => {
      nextItems.forEach((item, index) => {
        item.hidden = false;
        item.style.animationDelay = reducedMotion ? '0ms' : `${index * 35}ms`;
        item.classList.add('is-revealed');
      });

      const remaining = grid.querySelectorAll('[data-search-product-item][hidden]').length;
      button.dataset.remainingCount = String(remaining);
      button.setAttribute('aria-busy', 'false');
      grid.setAttribute('aria-busy', 'false');

      if (remaining === 0) {
        button.closest('.search-result-load-more')?.setAttribute('hidden', '');
        return;
      }

      updateButtonLabel(`Xem thêm ${remaining} sản phẩm`);
    }, reducedMotion ? 0 : 180);
  });

  function updateButtonLabel(text) {
    const label = button.querySelector('span');
    if (label) {
      label.textContent = text;
    }
  }
})();
