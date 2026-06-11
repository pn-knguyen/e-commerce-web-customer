(function () {
  'use strict';

  document.addEventListener('click', (event) => {
    const button = event.target.closest('[data-product-wishlist]');

    if (!button) {
      return;
    }

    const isWishlisted = button.classList.toggle('is-active');
    const productId = button.dataset.productId;
    const productName = button.dataset.productName || '';
    const visibleLabel = isWishlisted
      ? button.dataset.activeLabel
      : button.dataset.inactiveLabel;
    const label = button.querySelector('span');

    button.setAttribute('aria-pressed', String(isWishlisted));
    button.setAttribute(
      'aria-label',
      isWishlisted
        ? `Xóa ${productName} khỏi danh sách yêu thích`
        : `Thêm ${productName} vào danh sách yêu thích`
    );

    if (label && visibleLabel) {
      label.textContent = visibleLabel;
    }

    document.dispatchEvent(new CustomEvent('product:wishlist-change', {
      detail: {
        productId,
        isWishlisted
      }
    }));
  });
})();
