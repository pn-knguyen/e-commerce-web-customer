(function () {
  'use strict';

  const root = document.querySelector('[data-header-search]');
  const layer = document.querySelector('[data-search-layer]');

  if (!root || !layer) {
    return;
  }

  const form = root.querySelector('[data-search-form]');
  const input = root.querySelector('[data-search-input]');
  const clearButton = root.querySelector('[data-search-clear]');
  const closeControls = layer.querySelectorAll('[data-search-close]');
  const panel = layer.querySelector('[data-search-panel]');
  const states = new Map(Array.from(layer.querySelectorAll('[data-search-state]'))
    .map((state) => [state.dataset.searchState, state]));
  const suggestionsContainer = layer.querySelector('[data-search-suggestions]');
  const productsContainer = layer.querySelector('[data-search-products]');
  const suggestionBlock = layer.querySelector('[data-search-suggestion-block]');
  const productBlock = layer.querySelector('[data-search-product-block]');
  const historyBlock = layer.querySelector('[data-search-history-block]');
  const clearHistoryButton = layer.querySelector('[data-search-clear-history]');
  const endpoint = root.dataset.searchEndpoint || '/search/suggest';

  let debounceTimer = null;
  let requestId = 0;

  function setState(name) {
    states.forEach((state, stateName) => {
      const isActive = stateName === name;
      state.hidden = !isActive;
      state.classList.toggle('is-active', isActive);
    });
  }

  function updatePosition() {
    if (!form || !panel) {
      return;
    }

    const rect = form.getBoundingClientRect();
    const header = document.getElementById('site-header');
    const headerBottom = header?.getBoundingClientRect().bottom || 0;
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
    const panelWidth = Math.min(Math.max(rect.width, 500), viewportWidth - 24);
    const minCenter = 12 + panelWidth / 2;
    const maxCenter = viewportWidth - 12 - panelWidth / 2;
    const preferredCenter = rect.left + rect.width / 2;
    const panelCenter = Math.min(Math.max(preferredCenter, minCenter), maxCenter);
    const panelLeft = panelCenter - panelWidth / 2;
    const arrowLeft = Math.min(Math.max(preferredCenter - panelLeft, 20), panelWidth - 20);
    const panelTop = Math.max(rect.bottom + 14, headerBottom + 12);

    layer.style.setProperty('--search-backdrop-top', `${Math.round(headerBottom)}px`);
    layer.style.setProperty('--search-panel-top', `${Math.round(panelTop)}px`);
    layer.style.setProperty('--search-panel-left', `${Math.round(panelCenter)}px`);
    layer.style.setProperty('--search-panel-width', `${Math.round(panelWidth)}px`);
    layer.style.setProperty('--search-arrow-left', `${Math.round(arrowLeft)}px`);
  }

  function openSearch() {
    closeCategoryMenu();
    closeAccountMenu();
    updatePosition();
    layer.hidden = false;
    form?.setAttribute('aria-expanded', 'true');
    document.body.classList.add('site-search-open');

    window.requestAnimationFrame(() => {
      layer.classList.add('is-open');
      syncState();
    });
  }

  function closeSearch() {
    layer.classList.remove('is-open');
    form?.setAttribute('aria-expanded', 'false');
    document.body.classList.remove('site-search-open');

    window.setTimeout(() => {
      if (!layer.classList.contains('is-open')) {
        layer.hidden = true;
      }
    }, 180);
  }

  function closeCategoryMenu() {
    const openCategoryMenu = document.querySelector('[data-category-menu].is-open');
    openCategoryMenu?.querySelector('[data-category-menu-close]')?.click();
  }

  function closeAccountMenu() {
    const openAccountMenu = document.querySelector('[data-account-layer].is-open');
    openAccountMenu?.querySelector('[data-account-close]')?.click();
  }

  function syncClearButton() {
    if (!clearButton || !input) {
      return;
    }

    clearButton.hidden = input.value.trim().length === 0;
  }

  function syncState() {
    if (!input) {
      return;
    }

    syncClearButton();
    const query = input.value.trim();
    if (!query) {
      setState('idle');
      return;
    }

    scheduleSearch(query);
  }

  function scheduleSearch(query) {
    window.clearTimeout(debounceTimer);
    debounceTimer = window.setTimeout(() => fetchResults(query), 120);
  }

  async function fetchResults(query) {
    const currentRequestId = ++requestId;

    try {
      const response = await fetch(`${endpoint}?q=${encodeURIComponent(query)}`, {
        headers: {
          Accept: 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Search request failed');
      }

      const data = await response.json();
      if (currentRequestId !== requestId) {
        return;
      }

      renderResults(data);
    } catch {
      renderResults({ suggestions: [], products: [] });
    }
  }

  function renderResults(data) {
    const suggestions = Array.isArray(data?.suggestions) ? data.suggestions : [];
    const products = Array.isArray(data?.products) ? data.products : [];

    if (suggestionsContainer) {
      suggestionsContainer.replaceChildren(...suggestions.map(createSuggestionLink));
    }

    if (productsContainer) {
      productsContainer.replaceChildren(...products.map(createProductLink));
    }

    if (suggestionBlock) {
      suggestionBlock.hidden = suggestions.length === 0;
    }

    if (productBlock) {
      productBlock.hidden = products.length === 0;
    }

    setState(suggestions.length > 0 || products.length > 0 ? 'results' : 'empty');
  }

  function createSuggestionLink(item) {
    const link = document.createElement('a');
    link.className = 'site-search-suggestion-link';
    link.href = item.url || `/search?q=${encodeURIComponent(item.label || '')}`;

    if (item.imageUrl) {
      const image = document.createElement('img');
      image.src = item.imageUrl;
      image.alt = item.imageAlt || item.label || 'Gợi ý tìm kiếm';
      image.width = 36;
      image.height = 36;
      image.loading = 'lazy';
      link.append(image);
    }

    const label = document.createElement('span');
    label.textContent = item.label || '';
    link.append(label);

    return link;
  }

  function createProductLink(item) {
    const link = document.createElement('a');
    link.className = 'site-search-product';
    link.href = item.url || `/search?q=${encodeURIComponent(item.name || '')}`;

    const image = document.createElement('img');
    image.src = item.imageUrl || '/images/logo-techstore-icon.svg';
    image.alt = item.imageAlt || item.name || 'Sản phẩm';
    image.width = 44;
    image.height = 44;
    image.loading = 'lazy';

    const copy = document.createElement('div');
    copy.className = 'site-search-product-copy';

    const name = document.createElement('p');
    name.className = 'site-search-product-name';
    name.textContent = item.name || '';

    const priceRow = document.createElement('div');
    priceRow.className = 'site-search-product-price';

    const price = document.createElement('strong');
    price.textContent = item.priceText || '';
    priceRow.append(price);

    if (item.oldPriceText) {
      const oldPrice = document.createElement('del');
      oldPrice.textContent = item.oldPriceText;
      priceRow.append(oldPrice);
    }

    copy.append(name, priceRow);
    link.append(image, copy);

    return link;
  }

  input?.addEventListener('focus', openSearch);
  form?.addEventListener('pointerdown', openSearch);
  input?.addEventListener('input', syncState);

  clearButton?.addEventListener('click', () => {
    input.value = '';
    input.focus({ preventScroll: true });
    syncState();
  });

  closeControls.forEach((control) => {
    control.addEventListener('click', closeSearch);
  });

  clearHistoryButton?.addEventListener('click', () => {
    if (historyBlock) {
      historyBlock.hidden = true;
    }
  });

  form?.addEventListener('submit', (event) => {
    if (!input?.value.trim()) {
      event.preventDefault();
      openSearch();
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && layer.classList.contains('is-open')) {
      event.preventDefault();
      closeSearch();
      input?.focus({ preventScroll: true });
    }
  });

  window.addEventListener('resize', () => {
    if (layer.classList.contains('is-open')) {
      updatePosition();
    }
  }, { passive: true });
})();
