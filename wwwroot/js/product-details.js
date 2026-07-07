(function () {
  'use strict';

  const page = document.querySelector('[data-product-detail]');
  if (!page) return;

  const mainImage = page.querySelector('[data-product-main-image]');
  const stickyOrder = page.querySelector('[data-sticky-order]');
  const stickySentinel = page.querySelector('[data-sticky-sentinel]');
  const stickyImage = page.querySelector('[data-sticky-product-image]');
  const stickyProductName = page.querySelector('[data-sticky-product-name]');
  const stickyProductPrice = page.querySelector('[data-sticky-product-price]');
  const stickyPrice = page.querySelector('[data-sticky-price]');
  const stockStatus = page.querySelector('[data-stock-status]');
  const stockStatusText = page.querySelector('[data-stock-status-text]');
  const stockStatusNote = page.querySelector('[data-stock-status-note]');
  const specModal = page.querySelector('[data-spec-modal]');
  const specModalContent = specModal?.querySelector('[data-spec-modal-content]');
  const reviewModal = page.querySelector('[data-review-modal]');
  const galleryModal = page.querySelector('[data-gallery-modal]');
  const upsellModal = page.querySelector('[data-upsell-modal]');
  const upsellModalProduct = upsellModal?.querySelector('[data-upsell-modal-product]');
  const upsellModalVariants = upsellModal?.querySelector('[data-upsell-modal-variants]');
  const upsellConfirmButton = upsellModal?.querySelector('[data-upsell-confirm]');
  const galleryModalImage = galleryModal?.querySelector('[data-gallery-modal-image]');
  const extraVersions = Array.from(page.querySelectorAll('[data-extra-version="true"]'));
  const thumbs = Array.from(page.querySelectorAll('[data-gallery-thumb]'));
  const cartActions = Array.from(page.querySelectorAll('[data-add-to-cart], [data-buy-now]'));
  const specSections = specModalContent
    ? Array.from(specModalContent.querySelectorAll('[data-spec-section]'))
    : [];
  let modalLockCount = 0;
  let activeGalleryIndex = thumbs.findIndex((thumb) => thumb.classList.contains('is-active'));
  let activeUpsellButton = null;
  let specScrollFrame = 0;

  if (activeGalleryIndex < 0) {
    activeGalleryIndex = 0;
  }

  page.addEventListener('click', async (event) => {
    if (event.target === specModal) {
      closeModal(specModal);
      return;
    }

    if (event.target === reviewModal) {
      closeModal(reviewModal);
      return;
    }

    if (event.target === galleryModal) {
      closeModal(galleryModal);
      return;
    }

    if (event.target === upsellModal) {
      closeModal(upsellModal);
      return;
    }

    const sectionLink = event.target.closest('a[href^="#"]');
    if (sectionLink && sectionLink.hash.length > 1) {
      const target = page.querySelector(sectionLink.hash);

      if (target) {
        event.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        window.history.replaceState(null, '', sectionLink.hash);
        return;
      }
    }

    if (event.target.closest('[data-spec-open]')) {
      openSpecModal();
      return;
    }

    if (event.target.closest('[data-spec-close]')) {
      closeModal(specModal);
      return;
    }

    const specTab = event.target.closest('[data-spec-tab]');
    if (specTab) {
      scrollToSpecSection(specTab.dataset.target);
      return;
    }

    const relatedTab = event.target.closest('[data-related-tab]');
    if (relatedTab) {
      setRelatedGroup(relatedTab);
      return;
    }

    const reviewShowAll = event.target.closest('[data-review-show-all]');
    if (reviewShowAll) {
      revealItems('[data-review-item]', reviewShowAll);
      return;
    }

    const qaShowMore = event.target.closest('[data-qa-show-more]');
    if (qaShowMore) {
      revealItems('[data-qa-thread]', qaShowMore);
      return;
    }

    if (event.target.closest('[data-review-open]')) {
      openModal(reviewModal);
      return;
    }

    if (event.target.closest('[data-review-close]')) {
      closeModal(reviewModal);
      return;
    }

    const versionToggle = event.target.closest('[data-version-toggle]');
    if (versionToggle) {
      toggleExtraVersions(versionToggle);
      return;
    }

    const thumb = event.target.closest('[data-gallery-thumb]');
    if (thumb) {
      showGalleryItem(Number(thumb.dataset.galleryIndex) || 0);
      return;
    }

    if (event.target.closest('[data-gallery-prev]')) {
      showGalleryItem(activeGalleryIndex - 1);
      return;
    }

    if (event.target.closest('[data-gallery-next]')) {
      showGalleryItem(activeGalleryIndex + 1);
      return;
    }

    if (event.target.closest('[data-gallery-open]')) {
      openGalleryModal();
      return;
    }

    if (event.target.closest('[data-gallery-close]')) {
      closeModal(galleryModal);
      return;
    }

    const upsellAddButton = event.target.closest('[data-upsell-add]');
    if (upsellAddButton) {
      activeUpsellButton = upsellAddButton;
      const variantTemplate = upsellAddButton
        .closest('.upsell-item')
        ?.querySelector('[data-upsell-variant-template]');
      const variants = variantTemplate
        ? Array.from(variantTemplate.content.querySelectorAll('[data-upsell-variant]'))
        : [];

      if (variants.length === 0) {
        window.showToast?.('Phụ kiện này hiện không còn phiên bản có thể mua.', 'info');
        return;
      }

      if (variants.length === 1) {
        const added = await submitCartAction(
          variants[0],
          '/Cart/AddItem',
          { busyAction: upsellAddButton }
        );
        if (added) markUpsellAdded(upsellAddButton);
        return;
      }

      if (upsellModalProduct) {
        upsellModalProduct.textContent = upsellAddButton.dataset.productName || 'Phụ kiện';
      }

      if (upsellModalVariants) {
        upsellModalVariants.replaceChildren(variantTemplate.content.cloneNode(true));
      }

      const firstVariant = upsellModalVariants?.querySelector('[data-upsell-variant]');
      setActiveUpsellVariant(firstVariant);
      if (upsellConfirmButton) upsellConfirmButton.disabled = !firstVariant;
      openModal(upsellModal);
      window.setTimeout(() => firstVariant?.focus(), 50);
      return;
    }

    if (event.target.closest('[data-upsell-close]')) {
      closeModal(upsellModal);
      activeUpsellButton?.focus();
      return;
    }

    const upsellVariant = event.target.closest('[data-upsell-variant]');
    if (upsellVariant) {
      setActiveUpsellVariant(upsellVariant);
      return;
    }

    const upsellConfirm = event.target.closest('[data-upsell-confirm]');
    if (upsellConfirm) {
      const selectedVariant = upsellModalVariants
        ?.querySelector('[data-upsell-variant].is-active');
      if (!selectedVariant || !activeUpsellButton) return;

      const added = await submitCartAction(
        selectedVariant,
        '/Cart/AddItem',
        { busyAction: upsellConfirm }
      );
      if (!added) return;

      markUpsellAdded(activeUpsellButton);
      closeModal(upsellModal);
      activeUpsellButton.focus();
      return;
    }

    if (event.target.closest('[data-gallery-modal-prev]')) {
      showGalleryItem(activeGalleryIndex - 1);
      return;
    }

    if (event.target.closest('[data-gallery-modal-next]')) {
      showGalleryItem(activeGalleryIndex + 1);
      return;
    }

    const colorOption = event.target.closest('[data-color-option]');
    if (colorOption) {
      setActiveButton('[data-color-option]', colorOption);
      updateMainImage(colorOption.dataset.image, colorOption.dataset.alt);
      activateGalleryThumbByImage(colorOption.dataset.image);
      updateStickyProduct(colorOption.dataset.image, colorOption.dataset.colorName, colorOption.dataset.priceText);
      syncCartActionsFromColor(colorOption);
      syncStockState(colorOption);
      syncVariantUrl(colorOption.dataset.detailUrl);
      return;
    }

    const buyNowButton = event.target.closest('[data-buy-now]');
    if (buyNowButton) {
      event.preventDefault();
      await submitCartAction(buyNowButton, '/Cart/BuyNow', { redirect: true });
      return;
    }

    const addToCartButton = event.target.closest('[data-add-to-cart]');
    if (addToCartButton) {
      await submitCartAction(addToCartButton, '/Cart/AddItem');
    }
  });

  document.addEventListener('keydown', (event) => {
    if (galleryModal?.classList.contains('is-open')) {
      if (event.key === 'ArrowLeft') {
        showGalleryItem(activeGalleryIndex - 1);
        return;
      }

      if (event.key === 'ArrowRight') {
        showGalleryItem(activeGalleryIndex + 1);
        return;
      }
    }

    if (event.key !== 'Escape') return;

    closeModal(specModal);
    closeModal(reviewModal);
    closeModal(galleryModal);
    closeModal(upsellModal);
  });

  specModalContent?.addEventListener('scroll', () => {
    if (specScrollFrame) return;

    specScrollFrame = window.requestAnimationFrame(() => {
      specScrollFrame = 0;
      updateActiveSpecTabFromScroll();
    });
  }, { passive: true });

  setupStickyOrder();
  setupUpsellSwiper();
  syncStockState(page.querySelector('[data-color-option].is-active'));

  function setupUpsellSwiper() {
    const swiperElement = page.querySelector('[data-upsell-swiper]');
    if (!swiperElement || typeof window.Swiper !== 'function') return;

    new window.Swiper(swiperElement, {
      slidesPerView: 1,
      slidesPerGroup: 1,
      spaceBetween: 10,
      grid: {
        rows: 2,
        fill: 'row'
      },
      navigation: {
        nextEl: '.pd-upsell-next',
        prevEl: '.pd-upsell-prev'
      },
      pagination: {
        el: '.pd-upsell-pagination',
        clickable: true
      },
      breakpoints: {
        540: {
          slidesPerView: 2,
          slidesPerGroup: 2
        }
      }
    });
  }

  function setActiveButton(selector, activeButton) {
    page.querySelectorAll(selector).forEach((button) => {
      const isActive = button === activeButton;
      button.classList.toggle('is-active', isActive);
      button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });
  }

  function setActiveUpsellVariant(activeButton) {
    if (!activeButton) return;

    upsellModal?.querySelectorAll('[data-upsell-variant]').forEach((button) => {
      const isActive = button === activeButton;
      button.classList.toggle('is-active', isActive);
      button.setAttribute('aria-checked', isActive ? 'true' : 'false');
    });
  }

  function markUpsellAdded(button) {
    button.classList.add('is-added');
    const label = button.querySelector('span');
    const icon = button.querySelector('b');
    if (label) label.textContent = 'Đã thêm';
    if (icon) icon.textContent = '✓';
    button.setAttribute(
      'aria-label',
      `${button.dataset.productName || 'Phụ kiện'} đã được thêm vào giỏ`
    );
  }

  function showGalleryItem(index) {
    if (thumbs.length === 0) return;

    activeGalleryIndex = (index + thumbs.length) % thumbs.length;
    const thumb = thumbs[activeGalleryIndex];

    setActiveButton('[data-gallery-thumb]', thumb);
    updateMainImage(thumb.dataset.image, thumb.dataset.alt);
    thumb.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  }

  function openGalleryModal() {
    syncGalleryModalImage(
      mainImage?.getAttribute('src') || '',
      mainImage?.getAttribute('alt') || ''
    );
    openModal(galleryModal);
  }

  function activateGalleryThumbByImage(src) {
    if (!src) return;

    const thumb = thumbs.find((item) => item.dataset.image === src);
    if (!thumb) return;

    activeGalleryIndex = Number(thumb.dataset.galleryIndex) || 0;
    setActiveButton('[data-gallery-thumb]', thumb);
    thumb.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
  }

  function toggleExtraVersions(button) {
    const isExpanded = button.getAttribute('aria-expanded') === 'true';
    const nextExpanded = !isExpanded;
    const text = button.querySelector('[data-version-toggle-text]');

    extraVersions.forEach((version) => {
      version.hidden = !nextExpanded;
    });

    button.classList.toggle('is-expanded', nextExpanded);
    button.setAttribute('aria-expanded', nextExpanded ? 'true' : 'false');

    if (text) {
      text.textContent = nextExpanded ? 'Thu gọn phiên bản' : 'Xem thêm phiên bản khác';
    }
  }

  function openSpecModal() {
    openModal(specModal);

    if (specModalContent) {
      specModalContent.scrollTop = 0;
      setActiveSpecTab(specSections[0]?.id);
    }
  }

  function openModal(modal) {
    if (!modal || modal.classList.contains('is-open')) return;

    modal.hidden = false;
    lockBodyScroll();

    window.requestAnimationFrame(() => {
      modal.classList.add('is-open');
    });
  }

  function closeModal(modal) {
    if (!modal || modal.hidden || !modal.classList.contains('is-open')) return;

    modal.classList.remove('is-open');
    unlockBodyScroll();

    window.setTimeout(() => {
      if (!modal.classList.contains('is-open')) {
        modal.hidden = true;
      }
    }, 300);
  }

  function lockBodyScroll() {
    modalLockCount += 1;

    if (modalLockCount === 1) {
      document.body.dataset.productDetailPreviousOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
    }
  }

  function unlockBodyScroll() {
    modalLockCount = Math.max(0, modalLockCount - 1);

    if (modalLockCount === 0) {
      document.body.style.overflow = document.body.dataset.productDetailPreviousOverflow || '';
      delete document.body.dataset.productDetailPreviousOverflow;
    }
  }

  function scrollToSpecSection(targetId) {
    if (!targetId) return;

    const section = specModal?.querySelector(`#${targetId}`);
    if (!section) return;

    setActiveSpecTab(targetId);
    section.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  function updateActiveSpecTabFromScroll() {
    if (!specModalContent || specSections.length === 0) return;

    const rootTop = specModalContent.getBoundingClientRect().top;
    let activeId = specSections[0].id;

    specSections.forEach((section) => {
      const offset = section.getBoundingClientRect().top - rootTop;

      if (offset <= 90) {
        activeId = section.id;
      }
    });

    setActiveSpecTab(activeId);
  }

  function setActiveSpecTab(activeId) {
    if (!activeId) return;

    specModal?.querySelectorAll('[data-spec-tab]').forEach((tab) => {
      const isActive = tab.dataset.target === activeId;
      tab.classList.toggle('is-active', isActive);
      tab.setAttribute('aria-current', isActive ? 'true' : 'false');

      if (isActive) {
        tab.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
      }
    });
  }

  function setRelatedGroup(activeTab) {
    const activeGroup = activeTab.dataset.relatedTab;
    if (!activeGroup) return;

    page.querySelectorAll('[data-related-tab]').forEach((tab) => {
      const isActive = tab === activeTab;
      tab.classList.toggle('is-active', isActive);
      tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });

    page.querySelectorAll('[data-related-panel]').forEach((panel) => {
      panel.hidden = panel.dataset.relatedPanel !== activeGroup;
    });
  }

  function revealItems(selector, trigger) {
    page.querySelectorAll(selector).forEach((item) => {
      item.hidden = false;
    });

    trigger.closest('.pd-review-more, .pd-qa-more')?.setAttribute('hidden', '');
  }

  function setupStickyOrder() {
    if (!stickyOrder || !stickySentinel || !('IntersectionObserver' in window)) return;

    const observer = new IntersectionObserver((entries) => {
      const entry = entries[0];
      const shouldShow = !entry.isIntersecting && entry.boundingClientRect.top < 0;
      setStickyVisible(shouldShow);
    }, {
      threshold: 0,
      rootMargin: '0px 0px -8px 0px'
    });

    observer.observe(stickySentinel);
  }

  function setStickyVisible(isVisible) {
    stickyOrder.classList.toggle('show', isVisible);
    stickyOrder.setAttribute('aria-hidden', isVisible ? 'false' : 'true');
  }

  function syncCartActionsFromColor(colorOption) {
    cartActions.forEach((action) => {
      action.dataset.cartId = colorOption.dataset.variantKey || action.dataset.cartId || '';
      action.dataset.cartImage = colorOption.dataset.image || action.dataset.cartImage || '';
      action.dataset.cartAlt = colorOption.dataset.alt || action.dataset.cartAlt || '';
      action.dataset.cartVariant = colorOption.dataset.variantLabel
        || colorOption.dataset.colorName
        || action.dataset.cartVariant
        || '';
      action.dataset.cartPrice = colorOption.dataset.price || action.dataset.cartPrice || '';
      action.dataset.cartUrl = colorOption.dataset.detailUrl || action.dataset.cartUrl || '';
      action.dataset.cartAvailable = colorOption.dataset.isAvailable || action.dataset.cartAvailable || 'true';
      action.dataset.cartStockStatus = colorOption.dataset.stockStatus || action.dataset.cartStockStatus || '';
    });
  }

  function syncStockState(colorOption) {
    if (!colorOption) return;

    const isAvailable = colorOption.dataset.isAvailable !== 'false';
    const status = colorOption.dataset.stockStatus || (isAvailable ? 'Còn hàng' : 'Hết hàng');
    const variant = colorOption.dataset.variantLabel || colorOption.dataset.colorName || '';

    page.classList.toggle('has-unavailable-variant', !isAvailable);
    stockStatus?.classList.toggle('is-unavailable', !isAvailable);

    if (stockStatusText) {
      stockStatusText.textContent = isAvailable
        ? status
        : (variant ? `Hết hàng: ${variant}` : 'Hết hàng');
    }

    if (stockStatusNote) {
      stockStatusNote.textContent = isAvailable
        ? 'Sẵn sàng giao nhanh hoặc nhận tại cửa hàng.'
        : 'Bạn vẫn có thể chọn biến thể này để xem ảnh và thông tin chi tiết.';
    }

    cartActions.forEach((action) => {
      action.dataset.cartAvailable = isAvailable ? 'true' : 'false';
      action.dataset.cartStockStatus = status;
      action.classList.toggle('is-unavailable', !isAvailable);
      action.setAttribute('aria-disabled', isAvailable ? 'false' : 'true');
    });
  }

  function syncVariantUrl(detailUrl) {
    if (!detailUrl) return;

    window.history.replaceState(null, '', detailUrl);
  }

  async function submitCartAction(action, endpoint, options = {}) {
    const busyAction = options.busyAction || action;
    if (busyAction.dataset.pending === 'true') return false;

    if (action.dataset.cartAvailable === 'false') {
      const variant = action.dataset.cartVariant || 'Biến thể này';
      window.showToast?.(`${variant} hiện đã hết hàng.`, 'info');
      return false;
    }

    const payload = buildCartPayload(action);
    setActionBusy(busyAction, true);

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': readCsrfToken(),
        },
        body: JSON.stringify(payload),
      });

      const result = await response.json().catch(() => ({}));

      if (response.status === 401 && result.redirectUrl) {
        window.showToast?.(
          result.error || 'Vui lòng đăng nhập để sử dụng giỏ hàng.',
          'info'
        );
        window.location.href = result.redirectUrl;
        return false;
      }

      if (!response.ok) {
        throw new Error(
          result.error || `Cart request failed with status ${response.status}`
        );
      }

      if (Number.isFinite(Number(result.count))) {
        window.updateCartCount?.(Number(result.count));
      }

      if (options.redirect && result.redirectUrl) {
        window.location.href = result.redirectUrl;
        return true;
      }

      window.showToast?.('Đã thêm sản phẩm vào giỏ hàng', 'success');
      return true;
    } catch (error) {
      console.error(error);
      window.showToast?.('Không thể cập nhật giỏ hàng. Vui lòng thử lại.', 'error');
      return false;
    } finally {
      setActionBusy(busyAction, false);
    }
  }

  function buildCartPayload(action) {
    const price = Number(action.dataset.cartPrice);

    return {
      id: action.dataset.cartId || '',
      name: action.dataset.cartName || '',
      productUrl: action.dataset.cartUrl || window.location.pathname,
      imageUrl: action.dataset.cartImage || '',
      imageAlt: action.dataset.cartAlt || '',
      variant: action.dataset.cartVariant || '',
      unitPrice: Number.isFinite(price) ? price : 0,
      quantity: 1,
    };
  }

  function readCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  function setActionBusy(action, isBusy) {
    action.dataset.pending = isBusy ? 'true' : 'false';
    action.setAttribute('aria-busy', isBusy ? 'true' : 'false');

    if ('disabled' in action) {
      action.disabled = isBusy;
    }
  }

  function updateStickyProduct(src, colorName, priceText) {
    if (stickyImage && src) {
      stickyImage.src = src;
    }

    if (stickyProductName && colorName) {
      stickyProductName.textContent = `${stickyProductName.dataset.baseName} - ${colorName}`;
    }

    if (priceText) {
      if (stickyProductPrice) stickyProductPrice.textContent = priceText;
      if (stickyPrice) stickyPrice.textContent = priceText;
    }
  }

  function syncGalleryModalImage(src, alt) {
    if (!galleryModalImage || !src) return;

    galleryModalImage.src = src;

    if (alt) {
      galleryModalImage.alt = alt;
    }
  }

  function updateMainImage(src, alt) {
    if (!mainImage || !src) return;

    mainImage.src = src;
    updateStickyProduct(src);
    syncGalleryModalImage(src, alt);

    if (alt) {
      mainImage.alt = alt;
    }
  }
})();
