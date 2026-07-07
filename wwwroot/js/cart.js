(function () {
  'use strict';

  const page = document.querySelector('[data-cart-page]');

  if (!page) {
    return;
  }

  /* ─── Formatters ─────────────────────────────────────────────── */

  const formatPrice = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 });
  const money = (value) => `${formatPrice.format(value)}đ`;

  /* ─── Element refs ───────────────────────────────────────────── */

  const itemsContainer    = page.querySelector('[data-cart-items]');
  const toolbar           = page.querySelector('[data-cart-toolbar]');
  const emptyState        = page.querySelector('[data-cart-empty]');
  const selectAllCheckbox = page.querySelector('[data-cart-select-all]');
  const selectedCountEl   = page.querySelector('[data-cart-selected-count]');
  const deleteSelectedBtn = page.querySelector('[data-cart-delete-selected]');
  const clearButton       = page.querySelector('[data-cart-clear]');
  const titleCount        = page.querySelector('[data-cart-title-count]');
  const subtotalOutput    = page.querySelector('[data-cart-subtotal]');
  const shippingOutput    = page.querySelector('[data-cart-shipping]');
  const taxOutput         = page.querySelector('[data-cart-tax]');
  const totalOutput       = page.querySelector('[data-cart-total]');
  const checkoutButton    = page.querySelector('[data-cart-checkout]');
  const recommendationSummaryRow = page.querySelector('[data-cart-recommendation-summary]');
  const recommendationSubtotalOutput = page.querySelector('[data-cart-recommendation-subtotal]');

  /* ─── Helpers ────────────────────────────────────────────────── */

  function getItems() {
    return Array.from(page.querySelectorAll('[data-cart-item]'));
  }

  function getQuantity(item) {
    return Number(item.querySelector('[data-cart-quantity]').textContent) || 0;
  }

  function readCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  async function redirectToLoginIfNeeded(response) {
    if (response.status !== 401) return false;

    const result = await response.json().catch(() => ({}));
    const fallbackReturnUrl = `${window.location.pathname}${window.location.search}`;
    window.location.href = result.redirectUrl
      || `/Account/Login?returnUrl=${encodeURIComponent(fallbackReturnUrl)}`;
    return true;
  }

  function readCartItemPayload(item) {
    return {
      id: item.dataset.productId || '',
      name: item.querySelector('h2 a')?.textContent?.trim() || '',
      productUrl: item.querySelector('h2 a')?.getAttribute('href') || '',
      imageUrl: item.querySelector('img')?.getAttribute('src') || '',
      imageAlt: item.querySelector('img')?.getAttribute('alt') || '',
      variant: (() => {
        const btn = item.querySelector('.cart-item-variant');
        if (!btn) return '';

        return Array.from(btn.childNodes)
          .filter((node) => node.nodeType === Node.TEXT_NODE)
          .map((node) => node.textContent.trim())
          .join(' ')
          .trim();
      })(),
      unitPrice: parseFloat(item.dataset.unitPrice) || 0,
      quantity: getQuantity(item),
    };
  }

  function collectCartItems(options = {}) {
    return getItems()
      .filter((item) => !options.selectedOnly || isSelected(item))
      .map(readCartItemPayload)
      .filter((item) => item.quantity > 0);
  }

  function getRecommendationInputs() {
    return Array.from(
      page.querySelectorAll('[data-cart-recommendation-select]')
    );
  }

  function getSelectedRecommendationInputs() {
    return getRecommendationInputs().filter((input) => input.checked && !input.disabled);
  }

  function getRecommendationCard(element) {
    return element.closest('.cart-recommendation-card');
  }

  function getRecommendationQuantity(input) {
    const card = getRecommendationCard(input);
    const output = card?.querySelector('[data-cart-recommendation-quantity]');
    return Math.max(1, Number(output?.textContent) || 1);
  }

  function readRecommendationPayload(input) {
    return {
      id: input.dataset.cartId || '',
      name: input.dataset.cartName || '',
      productUrl: input.dataset.cartUrl || '',
      imageUrl: input.dataset.cartImage || '',
      imageAlt: input.dataset.cartAlt || '',
      variant: input.dataset.cartVariant || '',
      unitPrice: parseFloat(input.dataset.cartPrice) || 0,
      quantity: getRecommendationQuantity(input),
    };
  }

  function getRecommendationSubtotal() {
    return getSelectedRecommendationInputs().reduce((total, input) => {
      const price = Number(input.dataset.cartPrice) || 0;
      return total + price * getRecommendationQuantity(input);
    }, 0);
  }

  function syncRecommendationGroups(items) {
    const itemById = new Map(
      items.map((item) => [item.dataset.productId || '', item])
    );
    const groups = Array.from(
      page.querySelectorAll('[data-cart-recommendation-group]')
    );

    groups.forEach((group) => {
      const parentItem = itemById.get(group.dataset.parentProductId || '');
      const parentAvailable = Boolean(parentItem);
      const parentSelected = parentAvailable && isSelected(parentItem);

      group.hidden = !parentAvailable;
      group.classList.toggle('is-inactive', parentAvailable && !parentSelected);
      group.querySelectorAll('[data-cart-recommendation-select]').forEach((input) => {
        input.disabled = !parentSelected;
      });
    });
  }

  function isSelected(item) {
    const checkbox = item.querySelector('[data-cart-select]');
    return !checkbox || checkbox.checked;
  }

  function updateItem(item, nextQuantity) {
    const quantity       = Math.max(1, nextQuantity);
    const quantityOutput = item.querySelector('[data-cart-quantity]');
    const decreaseButton = item.querySelector('[data-cart-decrease]');

    quantityOutput.textContent = quantity;
    decreaseButton.disabled    = quantity <= 1;
  }

  function removeItemBlock(item) {
    const itemBlock = item?.closest('[data-cart-item-block]');
    (itemBlock || item)?.remove();
  }

  function updateRecommendationQuantity(card, nextQuantity) {
    if (!card) return;

    const increaseButton = card.querySelector('[data-cart-recommendation-increase]');
    const decreaseButton = card.querySelector('[data-cart-recommendation-decrease]');
    const quantityOutput = card.querySelector('[data-cart-recommendation-quantity]');
    const maxQuantity = Number(increaseButton?.dataset.maxQuantity) || 10;
    const quantity = Math.min(Math.max(1, nextQuantity), maxQuantity);

    if (quantityOutput) quantityOutput.textContent = quantity;
    if (decreaseButton) decreaseButton.disabled = quantity <= 1;
    if (increaseButton) increaseButton.disabled = quantity >= maxQuantity;
  }

  let persistTimer = 0;

  function scheduleCartPersist() {
    window.clearTimeout(persistTimer);
    persistTimer = window.setTimeout(() => {
      persistCartState();
    }, 180);
  }

  async function persistCartState() {
    try {
      const res = await fetch('/Cart/SaveSession', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': readCsrfToken(),
        },
        body: JSON.stringify(collectCartItems()),
      });

      if (await redirectToLoginIfNeeded(res)) return;
      if (!res.ok) return;

      const result = await res.json().catch(() => ({}));
      if (Number.isFinite(Number(result.count))) {
        window.updateCartCount?.(Number(result.count));
      }
    } catch (error) {
      console.warn('Cart session update failed.', error);
    }
  }

  /* ─── Master summary update ──────────────────────────────────── */

  function updateSummary() {
    const items         = getItems();
    syncRecommendationGroups(items);
    const selectedItems = items.filter(isSelected);
    const selectedCount = selectedItems.length;
    const itemCount     = items.reduce((t, i) => t + getQuantity(i), 0);
    const productSubtotal = selectedItems.reduce(
      (t, i) => t + (Number(i.dataset.unitPrice) || 0) * getQuantity(i), 0
    );
    const recommendationSubtotal = getRecommendationSubtotal();
    const shipping      = Number(shippingOutput?.dataset.value) || 0;
    const tax           = Number(taxOutput?.dataset.value) || 0;
    const total         = productSubtotal + recommendationSubtotal + shipping + tax;

    /* title badge */
    if (titleCount) titleCount.textContent = `(${itemCount})`;

    /* totals */
    if (subtotalOutput) subtotalOutput.textContent = money(productSubtotal);
    if (recommendationSubtotalOutput) {
      recommendationSubtotalOutput.textContent = `+${money(recommendationSubtotal)}`;
    }
    if (recommendationSummaryRow) {
      recommendationSummaryRow.hidden = recommendationSubtotal <= 0;
    }
    if (totalOutput)    totalOutput.textContent    = money(total);

    /* empty / visible */
    itemsContainer.hidden = items.length === 0;
    if (toolbar)    toolbar.hidden    = items.length === 0;
    emptyState.hidden     = items.length !== 0;
    /* checkout state */
    if (checkoutButton) {
      checkoutButton.classList.toggle('is-disabled', selectedCount === 0);
      checkoutButton.setAttribute('aria-disabled', String(selectedCount === 0));
    }

    /* legacy clear button (if any) */
    if (clearButton) clearButton.disabled = items.length === 0;

    /* delete-selected button */
    if (deleteSelectedBtn) {
      deleteSelectedBtn.disabled = selectedCount === 0;
    }

    /* global nav badge */
    window.updateCartCount?.(itemCount);

    /* select-all checkbox state */
    syncSelectAll(items, selectedCount);

    /* selected count label */
    if (selectedCountEl) {
      selectedCountEl.textContent = selectedCount > 0 ? `(${selectedCount})` : '';
    }

  }

  /* ─── Keep select-all checkbox in sync ──────────────────────── */

  function syncSelectAll(items, selectedCount) {
    if (!selectAllCheckbox) return;

    if (items.length === 0) {
      selectAllCheckbox.checked       = false;
      selectAllCheckbox.indeterminate = false;
    } else if (selectedCount === 0) {
      selectAllCheckbox.checked       = false;
      selectAllCheckbox.indeterminate = false;
    } else if (selectedCount === items.length) {
      selectAllCheckbox.checked       = true;
      selectAllCheckbox.indeterminate = false;
    } else {
      selectAllCheckbox.checked       = false;
      selectAllCheckbox.indeterminate = true;
    }
  }

  /* ─── Event delegation (click) ──────────────────────────────── */

  page.addEventListener('click', (event) => {
    const increaseButton   = event.target.closest('[data-cart-increase]');
    const decreaseButton   = event.target.closest('[data-cart-decrease]');
    const removeButton     = event.target.closest('[data-cart-remove]');
    const recommendationIncreaseButton = event.target.closest('[data-cart-recommendation-increase]');
    const recommendationDecreaseButton = event.target.closest('[data-cart-recommendation-decrease]');

    if (recommendationIncreaseButton) {
      const card = getRecommendationCard(recommendationIncreaseButton);
      const output = card?.querySelector('[data-cart-recommendation-quantity]');
      updateRecommendationQuantity(card, (Number(output?.textContent) || 1) + 1);
      updateSummary();
      return;
    }

    if (recommendationDecreaseButton) {
      const card = getRecommendationCard(recommendationDecreaseButton);
      const output = card?.querySelector('[data-cart-recommendation-quantity]');
      updateRecommendationQuantity(card, (Number(output?.textContent) || 1) - 1);
      updateSummary();
      return;
    }

    if (increaseButton) {
      const item        = increaseButton.closest('[data-cart-item]');
      const maxQuantity = Number(increaseButton.dataset.maxQuantity) || 10;
      updateItem(item, Math.min(getQuantity(item) + 1, maxQuantity));
      updateSummary();
      scheduleCartPersist();
      return;
    }

    if (decreaseButton) {
      const item = decreaseButton.closest('[data-cart-item]');
      updateItem(item, getQuantity(item) - 1);
      updateSummary();
      scheduleCartPersist();
      return;
    }

    if (removeButton) {
      const item = removeButton.closest('[data-cart-item]');
      removeItemBlock(item);
      updateSummary();
      scheduleCartPersist();
      return;
    }
  });

  /* ─── Per-item checkbox change ──────────────────────────────── */

  page.addEventListener('change', (event) => {
    if (event.target.matches('[data-cart-select]')) {
      updateSummary();
      return;
    }

    if (event.target.matches('[data-cart-recommendation-select]')) {
      updateSummary();
    }
  });

  /* ─── Select All ─────────────────────────────────────────────── */

  if (selectAllCheckbox) {
    selectAllCheckbox.addEventListener('change', () => {
      const checked = selectAllCheckbox.checked;
      getItems().forEach((item) => {
        const checkbox = item.querySelector('[data-cart-select]');
        if (checkbox) checkbox.checked = checked;
      });
      updateSummary();
    });
  }

  /* ─── Delete Selected ────────────────────────────────────────── */

  if (deleteSelectedBtn) {
    deleteSelectedBtn.addEventListener('click', () => {
      getItems().forEach((item) => {
        if (isSelected(item)) removeItemBlock(item);
      });
      updateSummary();
      scheduleCartPersist();
    });
  }

  /* ─── Legacy clear-all button (if present in markup) ─────────── */

  if (clearButton) {
    clearButton.addEventListener('click', () => {
      getItems().forEach(removeItemBlock);
      updateSummary();
      scheduleCartPersist();
    });
  }

  /* ─── Checkout intercept: save cart to server session first ──── */

  if (checkoutButton) {
    checkoutButton.addEventListener('click', async (event) => {
      // Only intercept real navigation (not disabled state)
      if (checkoutButton.classList.contains('is-disabled')) return;

      event.preventDefault();

      const selectedItems = collectCartItems({ selectedOnly: true });
      const selectedRecommendations = getSelectedRecommendationInputs()
        .map(readRecommendationPayload);
      const checkoutItems = [...selectedItems, ...selectedRecommendations];

      if (checkoutItems.length === 0) return;

      try {
        const res = await fetch('/Cart/PrepareCheckout', {
          method:  'POST',
          headers: {
            'Content-Type':                'application/json',
            'RequestVerificationToken':    readCsrfToken(),
          },
          body: JSON.stringify(checkoutItems),
        });

        if (await redirectToLoginIfNeeded(res)) return;

        if (!res.ok) {
          const error = await res.json().catch(() => ({}));
          window.alert(error.error || 'Không thể chuẩn bị đơn thanh toán. Vui lòng thử lại.');
          return;
        }

        const result = await res.json().catch(() => ({}));
        window.location.href = result.redirectUrl || checkoutButton.href;
      } catch {
        window.alert('Không thể kết nối để chuẩn bị đơn thanh toán. Vui lòng thử lại.');
      }
    });
  }

  /* ─── Init ───────────────────────────────────────────────────── */

  Array.from(page.querySelectorAll('.cart-recommendation-card')).forEach((card) => {
    const output = card.querySelector('[data-cart-recommendation-quantity]');
    updateRecommendationQuantity(card, Number(output?.textContent) || 1);
  });
  updateSummary();
})();
