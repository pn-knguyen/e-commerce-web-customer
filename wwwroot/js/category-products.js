(function () {
  'use strict';

  const sections = document.querySelectorAll('[data-category-products]');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

  sections.forEach((section) => {
    const rows = Number.parseInt(section.dataset.categoryRows || '2', 10);
    const tabs = Array.from(section.querySelectorAll('[data-category-tab]'));
    const panels = Array.from(section.querySelectorAll('[data-category-panel]'));
    const bannerPanels = Array.from(
      section.querySelectorAll('[data-category-banner-panel]')
    );
    const swipers = new WeakMap();

    const initializeSwiper = (panel) => {
      if (!panel || swipers.has(panel) || typeof window.Swiper !== 'function') {
        return;
      }

      const swiperElement = panel.querySelector('.category-products-swiper');
      if (!swiperElement) {
        return;
      }

      const pagination = panel.querySelector('.category-products-pagination');
      const swiperOptions = {
        slidesPerView: 1.25,
        slidesPerGroup: 1,
        spaceBetween: 8,
        speed: reducedMotion.matches ? 0 : 350,
        grabCursor: true,
        grid: {
          rows: 1,
          fill: 'row'
        },
        keyboard: {
          enabled: true,
          onlyInViewport: true
        },
        navigation: {
          nextEl: panel.querySelector('.category-carousel-button--next'),
          prevEl: panel.querySelector('.category-carousel-button--prev')
        },
        observer: true,
        observeParents: true,
        watchOverflow: true,
        breakpoints: {
          480: {
            slidesPerView: 2.1,
            slidesPerGroup: 2,
            grid: {
              rows: 1,
              fill: 'row'
            }
          },
          768: {
            slidesPerView: 3,
            slidesPerGroup: 3,
            grid: {
              rows: 1,
              fill: 'row'
            }
          },
          1024: {
            slidesPerView: 4,
            slidesPerGroup: 4,
            grid: {
              rows,
              fill: 'row'
            }
          }
        }
      };

      if (pagination) {
        swiperOptions.pagination = {
          el: pagination,
          clickable: true,
          bulletClass: 'category-pagination-bullet',
          bulletActiveClass: 'is-active',
          renderBullet(index, className) {
            return `<button class="${className}" type="button" aria-label="Xem nhóm sản phẩm ${index + 1}"></button>`;
          }
        };
      }

      const swiper = new window.Swiper(swiperElement, swiperOptions);

      swipers.set(panel, swiper);
    };

    const activatePanel = (tab, focusTab, scrollTab) => {
      const panelId = tab.dataset.categoryPanelId;
      const bannerPanelId = tab.dataset.categoryBannerPanelId;

      tabs.forEach((item) => {
        const isActive = item === tab;
        item.classList.toggle('is-active', isActive);
        item.setAttribute('aria-selected', String(isActive));
        item.tabIndex = isActive ? 0 : -1;
      });

      panels.forEach((panel) => {
        const isActive = panel.id === panelId;
        panel.classList.toggle('is-active', isActive);
        panel.hidden = !isActive;
        panel.setAttribute('aria-hidden', String(!isActive));

        if (isActive) {
          initializeSwiper(panel);
          requestAnimationFrame(() => swipers.get(panel)?.update());
        }
      });

      let activePanelHasBanners = false;

      bannerPanels.forEach((bannerPanel) => {
        const isActive = bannerPanel.id === bannerPanelId;
        bannerPanel.classList.toggle('is-active', isActive);
        bannerPanel.hidden = !isActive;
        bannerPanel.setAttribute('aria-hidden', String(!isActive));

        if (isActive) {
          activePanelHasBanners = bannerPanel.childElementCount > 0;
        }
      });

      section.classList.toggle('has-active-banners', activePanelHasBanners);

      if (scrollTab) {
        tab.scrollIntoView({
          behavior: reducedMotion.matches ? 'auto' : 'smooth',
          block: 'nearest',
          inline: 'center'
        });
      }

      if (focusTab) {
        tab.focus();
      }
    };

    if (tabs.length) {
      tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => activatePanel(tab, false, true));
        tab.addEventListener('keydown', (event) => {
          let nextIndex = index;

          if (event.key === 'ArrowRight') {
            nextIndex = (index + 1) % tabs.length;
          } else if (event.key === 'ArrowLeft') {
            nextIndex = (index - 1 + tabs.length) % tabs.length;
          } else if (event.key === 'Home') {
            nextIndex = 0;
          } else if (event.key === 'End') {
            nextIndex = tabs.length - 1;
          } else {
            return;
          }

          event.preventDefault();
          activatePanel(tabs[nextIndex], true, true);
        });
      });

      const selectedTab =
        tabs.find((tab) => tab.getAttribute('aria-selected') === 'true') || tabs[0];
      activatePanel(selectedTab, false, false);
    } else {
      panels.forEach((panel) => {
        if (panel.classList.contains('is-active')) {
          initializeSwiper(panel);
        }
      });
    }
  });
})();
