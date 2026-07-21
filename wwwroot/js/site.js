/**
 * site.js - Global site scripts
 * Handles: back-to-top, toast notifications and cart badge sync
 */

(function () {
  'use strict';

  // ============================================================
  // BACK TO TOP
  // ============================================================
  const backToTopBtn = document.getElementById('back-to-top');

  if (backToTopBtn) {
    const topSentinel = document.getElementById('page-top-sentinel');

    if (topSentinel && 'IntersectionObserver' in window) {
      const observer = new IntersectionObserver((entries) => {
        backToTopBtn.classList.toggle('visible', !entries[0].isIntersecting);
      }, {
        rootMargin: '400px 0px 0px 0px'
      });

      observer.observe(topSentinel);
    } else {
      window.addEventListener('scroll', () => {
        if (window.scrollY > 400) {
          backToTopBtn.classList.add('visible');
        } else {
          backToTopBtn.classList.remove('visible');
        }
      }, { passive: true });
    }

    backToTopBtn.addEventListener('click', () => {
      const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

      window.scrollTo({
        top: 0,
        behavior: reducedMotion ? 'auto' : 'smooth'
      });
    });
  }

  // ============================================================
  // CART BADGE UPDATE
  // Backend should call window.updateCartCount(n) after cart mutations
  // ============================================================
  window.updateCartCount = function (count) {
    const badges = document.querySelectorAll('[id^="cart-item-count"]');
    badges.forEach((badge) => {
      if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.hidden = false;
        badge.setAttribute('aria-label', `${count} sản phẩm trong giỏ`);
      } else {
        badge.hidden = true;
      }
    });
  };

  window.refreshCartCount = async function () {
    try {
      const response = await fetch('/Cart/Count', {
        headers: { Accept: 'application/json' },
        credentials: 'same-origin',
        cache: 'no-store',
      });

      if (!response.ok) return;

      const result = await response.json();
      const count = Number(result.count);

      if (Number.isFinite(count)) {
        window.updateCartCount(count);
      }
    } catch {
      // Keep the existing badge state if the lightweight refresh fails.
    }
  };

  const scheduleCartRefresh = () => {
    window.refreshCartCount();
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', scheduleCartRefresh, { once: true });
  } else {
    scheduleCartRefresh();
  }

  window.addEventListener('pageshow', scheduleCartRefresh);

  // ============================================================
  // IMAGE FALLBACK
  // ============================================================
  const fallbackImageUrl = '/images/logo-techstore-icon.svg';

  document.addEventListener('error', (event) => {
    const image = event.target instanceof HTMLImageElement
      ? event.target
      : null;

    if (!image || image.dataset.fallbackApplied === 'true') {
      return;
    }

    const currentSource = image.getAttribute('src') || '';
    if (currentSource.includes('logo-techstore-icon.svg')) {
      return;
    }

    image.dataset.fallbackApplied = 'true';
    image.src = image.dataset.fallbackSrc || fallbackImageUrl;

    if (!image.getAttribute('alt')) {
      image.alt = 'TechStore';
    }
  }, true);

  // ============================================================
  // TOAST NOTIFICATIONS
  // Usage: window.showToast('Đã thêm vào giỏ hàng', 'success')
  // ============================================================
  window.showToast = function (message, type = 'default', duration = 3000) {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="none" viewBox="0 0 24 24" aria-hidden="true">
        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"/>
      </svg>
      ${escapeHtml(message)}
    `;
    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateY(8px)';
      toast.style.transition = 'all 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, duration);
  };

  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const trigger = target?.closest('[data-profile-coming-soon]');
    if (!trigger) return;

    event.preventDefault();
    const message = trigger.getAttribute('data-coming-soon-message') || 'Chức năng này sẽ được triển khai sau.';
    if (typeof window.showToast === 'function') {
      window.showToast(message, 'default');
      return;
    }

    window.alert(message);
  });

  // ============================================================
  // HELPER: Escape HTML for user input
  // ============================================================
  function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
  }

})();
