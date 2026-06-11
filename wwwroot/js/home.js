(function () {
  'use strict';

  const slidesTrack = document.getElementById('hero-slides-track');

  if (!slidesTrack) {
    return;
  }

  const slides = slidesTrack.querySelectorAll('.hero-slide');
  const dots = document.querySelectorAll('#hero-dots .slider-dot');
  const previousButton = document.getElementById('hero-prev');
  const nextButton = document.getElementById('hero-next');
  const slider = slidesTrack.closest('.hero-slider');
  const autoplayDelay = 5000;
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  let currentSlide = 0;
  let autoplayTimer = null;
  let touchStartX = 0;

  if (!slides.length) {
    return;
  }

  function updateSlideAccessibility() {
    slides.forEach((slide, slideIndex) => {
      const isActive = slideIndex === currentSlide;

      slide.setAttribute('aria-hidden', String(!isActive));
      slide.querySelectorAll('a, button, input, select, textarea, [tabindex]').forEach((element) => {
        if (isActive) {
          element.removeAttribute('tabindex');
        } else {
          element.setAttribute('tabindex', '-1');
        }
      });
    });
  }

  function showSlide(index) {
    currentSlide = (index + slides.length) % slides.length;
    slidesTrack.style.transform = `translateX(-${currentSlide * 100}%)`;

    dots.forEach((dot, dotIndex) => {
      const isActive = dotIndex === currentSlide;

      dot.classList.toggle('active', isActive);
      dot.setAttribute('aria-selected', String(isActive));
      dot.tabIndex = isActive ? 0 : -1;
    });

    updateSlideAccessibility();
  }

  function stopAutoplay() {
    window.clearInterval(autoplayTimer);
    autoplayTimer = null;
  }

  function startAutoplay() {
    if (reducedMotion || slides.length < 2) {
      return;
    }

    stopAutoplay();
    autoplayTimer = window.setInterval(() => showSlide(currentSlide + 1), autoplayDelay);
  }

  function changeSlide(offset) {
    showSlide(currentSlide + offset);
    startAutoplay();
  }

  previousButton?.addEventListener('click', () => changeSlide(-1));
  nextButton?.addEventListener('click', () => changeSlide(1));

  dots.forEach((dot, index) => {
    dot.addEventListener('click', () => {
      showSlide(index);
      startAutoplay();
    });
  });

  slider?.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      changeSlide(-1);
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      changeSlide(1);
    }

    if (event.key === 'Home') {
      event.preventDefault();
      showSlide(0);
      startAutoplay();
    }

    if (event.key === 'End') {
      event.preventDefault();
      showSlide(slides.length - 1);
      startAutoplay();
    }
  });

  slider?.addEventListener('mouseenter', stopAutoplay);
  slider?.addEventListener('mouseleave', startAutoplay);
  slider?.addEventListener('focusin', stopAutoplay);
  slider?.addEventListener('focusout', startAutoplay);

  slidesTrack.addEventListener('touchstart', (event) => {
    touchStartX = event.touches[0].clientX;
  }, { passive: true });

  slidesTrack.addEventListener('touchend', (event) => {
    const distance = touchStartX - event.changedTouches[0].clientX;

    if (Math.abs(distance) > 50) {
      changeSlide(distance > 0 ? 1 : -1);
    }
  }, { passive: true });

  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      stopAutoplay();
    } else {
      startAutoplay();
    }
  });

  const hasMultipleSlides = slides.length > 1;

  if (previousButton) {
    previousButton.hidden = !hasMultipleSlides;
  }

  if (nextButton) {
    nextButton.hidden = !hasMultipleSlides;
  }

  document.getElementById('hero-dots')?.toggleAttribute('hidden', !hasMultipleSlides);
  showSlide(0);
  startAutoplay();
})();
