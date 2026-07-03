(function () {
  'use strict';

  const TOKEN_SELECTOR = 'input[name="__RequestVerificationToken"]';
  const DEFAULT_FILLED_ICON = '#product-card-icon-heart-filled';
  const DEFAULT_OUTLINE_ICON = '#product-card-icon-heart';

  function getAntiForgeryToken() {
    return document.querySelector(TOKEN_SELECTOR)?.value || '';
  }

  function showMessage(message, type = 'default') {
    if (!message) return;
    if (typeof window.showToast === 'function') {
      window.showToast(message, type);
      return;
    }
    window.alert(message);
  }

  function getWishlistState(button) {
    return button.classList.contains('is-active')
      || button.getAttribute('aria-pressed') === 'true';
  }

  function setWishlistIcon(button, isWishlisted) {
    const icon = button.querySelector('use');
    if (!icon) return;

    const iconId = isWishlisted
      ? button.dataset.filledIcon || DEFAULT_FILLED_ICON
      : button.dataset.outlineIcon || DEFAULT_OUTLINE_ICON;

    icon.setAttribute('href', iconId);
    icon.setAttributeNS('http://www.w3.org/1999/xlink', 'href', iconId);
  }

  function setWishlistState(button, isWishlisted) {
    const productName = button.dataset.productName || '';
    const visibleLabel = isWishlisted
      ? button.dataset.activeLabel || 'Đã thích'
      : button.dataset.inactiveLabel || 'Yêu thích';
    const label = button.querySelector('span');

    button.classList.toggle('is-active', isWishlisted);
    button.setAttribute('aria-pressed', String(isWishlisted));
    button.setAttribute(
      'aria-label',
      isWishlisted
        ? `Xóa ${productName} khỏi danh sách yêu thích`
        : `Thêm ${productName} vào danh sách yêu thích`
    );
    setWishlistIcon(button, isWishlisted);

    if (label) {
      label.textContent = visibleLabel;
    }
  }

  async function postWishlist(url, body, options = {}) {
    const redirectOnUnauthorized = options.redirectOnUnauthorized !== false;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        RequestVerificationToken: getAntiForgeryToken()
      },
      body: JSON.stringify(body)
    });

    let data = {};
    try {
      data = await response.json();
    } catch {
      data = {};
    }

    if (response.status === 401) {
      if (redirectOnUnauthorized) {
        window.location.href = data.loginUrl || '/Account/Login';
      }
      return null;
    }

    if (!response.ok) {
      throw new Error(data.message || 'Không thể cập nhật danh sách yêu thích.');
    }

    return data;
  }

  async function syncInitialWishlistStates() {
    const buttons = Array.from(document.querySelectorAll('[data-product-wishlist]'));
    const productIds = [...new Set(buttons
      .map((button) => button.dataset.productId)
      .filter(Boolean))];

    if (productIds.length === 0 || !getAntiForgeryToken()) {
      return;
    }

    try {
      const data = await postWishlist('/wishlist/status', { productIds }, { redirectOnUnauthorized: false });
      const statuses = data?.statuses || {};
      buttons.forEach((button) => {
        const productId = button.dataset.productId;
        if (Object.prototype.hasOwnProperty.call(statuses, productId)) {
          setWishlistState(button, Boolean(statuses[productId]));
        } else {
          setWishlistIcon(button, getWishlistState(button));
        }
      });
    } catch {
      // Trạng thái yêu thích chỉ là nâng cấp UX, không chặn trang sản phẩm.
      buttons.forEach((button) => setWishlistIcon(button, getWishlistState(button)));
    }
  }

  document.addEventListener('click', async (event) => {
    const button = event.target.closest('[data-product-wishlist]');

    if (!button || button.disabled || button.classList.contains('is-loading')) {
      return;
    }

    const productId = button.dataset.productId;
    if (!productId) {
      showMessage('Sản phẩm không hợp lệ.', 'error');
      return;
    }

    const previousState = getWishlistState(button);
    const nextState = !previousState;

    // Cập nhật UI ngay để thao tác có cảm giác phản hồi tức thì.
    setWishlistState(button, nextState);
    button.classList.add('is-loading');
    button.disabled = true;

    try {
      const data = await postWishlist('/wishlist/toggle', { productId });
      if (!data) {
        setWishlistState(button, previousState);
        return;
      }

      const isWishlisted = Boolean(data.isWishlisted);
      setWishlistState(button, isWishlisted);
      showMessage(data.message, isWishlisted ? 'success' : 'default');

      document.dispatchEvent(new CustomEvent('product:wishlist-change', {
        detail: {
          productId,
          isWishlisted
        }
      }));
    } catch (error) {
      setWishlistState(button, previousState);
      showMessage(error.message, 'error');
    } finally {
      button.classList.remove('is-loading');
      button.disabled = false;
    }
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', syncInitialWishlistStates, { once: true });
  } else {
    syncInitialWishlistStates();
  }
})();
