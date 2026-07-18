(function () {
  'use strict';

  const page = document.querySelector('[data-search-result-page]');
  if (!page) return;

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const grid = page.querySelector('[data-search-product-grid]');
  const button = page.querySelector('[data-search-load-more]');

  if (!grid || !button) return;

  button.addEventListener('click', async () => {
    if (button.getAttribute('aria-busy') === 'true') return;

    button.setAttribute('aria-busy', 'true');
    grid.setAttribute('aria-busy', 'true');
    updateButtonLabel('Đang tải sản phẩm');

    try {
      const nextPage = Number.parseInt(button.dataset.nextPage || '2', 10);
      const requestUrl = new URL('/search/products', window.location.origin);
      const currentUrl = new URL(window.location.href);
      currentUrl.searchParams.forEach((value, key) => requestUrl.searchParams.append(key, value));
      requestUrl.searchParams.set('page', String(nextPage));

      const response = await fetch(requestUrl, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!response.ok) {
        throw new Error('Không thể tải thêm sản phẩm');
      }

      const container = document.createElement('div');
      container.innerHTML = await response.text();
      const productPage = container.querySelector('[data-search-product-page]');
      const nextItems = productPage
        ? Array.from(productPage.querySelectorAll('[data-search-product-item]'))
        : [];
      if (!productPage || nextItems.length === 0) {
        button.closest('.search-result-load-more')?.setAttribute('hidden', '');
        return;
      }

      nextItems.forEach((item, index) => {
        item.style.animationDelay = reducedMotion ? '0ms' : `${index * 35}ms`;
        item.classList.add('is-revealed');
        grid.append(item);
      });

      const hasMore = productPage.dataset.hasMore === 'true';
      const remaining = Number.parseInt(productPage.dataset.remainingCount || '0', 10);
      button.dataset.nextPage = String(nextPage + 1);
      button.dataset.remainingCount = String(remaining);

      document.dispatchEvent(new CustomEvent('product:cards-added', {
        detail: { root: grid }
      }));

      if (!hasMore || remaining === 0) {
        button.closest('.search-result-load-more')?.setAttribute('hidden', '');
        return;
      }

      updateButtonLabel(`Xem thêm ${remaining} sản phẩm`);
    } catch (error) {
      updateButtonLabel(error.message || 'Không thể tải thêm sản phẩm');
    } finally {
      button.setAttribute('aria-busy', 'false');
      grid.setAttribute('aria-busy', 'false');
    }
  });

  function updateButtonLabel(text) {
    const label = button.querySelector('span');
    if (label) {
      label.textContent = text;
    }
  }
})();
