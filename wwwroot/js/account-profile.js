(function () {
  'use strict';

  const historySelector = '[data-account-order-history]';
  let activeRequest = null;

  if (!document.querySelector(historySelector)) return;

  function buildEndpointUrl(profileUrl) {
    const endpointUrl = new URL('/account/profile/order-history', window.location.origin);
    ['status', 'from', 'to'].forEach((key) => {
      const value = profileUrl.searchParams.get(key);
      if (value) endpointUrl.searchParams.set(key, value);
    });
    return endpointUrl;
  }

  function setOptimisticStatus(panel, profileUrl) {
    const nextStatus = profileUrl.searchParams.get('status') || 'all';
    panel.querySelectorAll('.ap-order-tabs [data-order-status-filter]').forEach((link) => {
      const linkUrl = new URL(link.href, window.location.origin);
      const linkStatus = linkUrl.searchParams.get('status') || 'all';
      const isActive = linkStatus === nextStatus;
      link.classList.toggle('is-active', isActive);
      if (isActive) {
        link.setAttribute('aria-current', 'page');
      } else {
        link.removeAttribute('aria-current');
      }
    });
  }

  async function loadOrderHistory(profileUrl, updateBrowserHistory) {
    const currentPanel = document.querySelector(historySelector);
    if (!currentPanel) return;

    activeRequest?.abort();
    activeRequest = new AbortController();
    setOptimisticStatus(currentPanel, profileUrl);
    currentPanel.classList.add('is-loading');
    currentPanel.setAttribute('aria-busy', 'true');

    try {
      const response = await fetch(buildEndpointUrl(profileUrl), {
        headers: {
          Accept: 'text/html',
          'X-Requested-With': 'XMLHttpRequest'
        },
        cache: 'no-store',
        signal: activeRequest.signal
      });

      if (response.status === 401) {
        const returnUrl = profileUrl.pathname + profileUrl.search;
        window.location.assign(`/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`);
        return;
      }

      if (!response.ok) {
        throw new Error('Không thể tải lịch sử đơn hàng.');
      }

      const template = document.createElement('template');
      template.innerHTML = await response.text();
      const nextPanel = template.content.querySelector(historySelector);
      if (!nextPanel) {
        throw new Error('Dữ liệu lịch sử đơn hàng không hợp lệ.');
      }

      currentPanel.replaceWith(nextPanel);
      if (updateBrowserHistory) {
        window.history.pushState({}, '', profileUrl);
      }
    } catch (error) {
      if (error.name === 'AbortError') return;

      currentPanel.classList.remove('is-loading');
      currentPanel.removeAttribute('aria-busy');
      if (typeof window.showToast === 'function') {
        window.showToast(error.message, 'error');
      } else {
        window.alert(error.message);
      }
    }
  }

  document.addEventListener('click', (event) => {
    const link = event.target.closest('[data-order-status-filter]');
    if (!link || !link.closest(historySelector)) return;

    event.preventDefault();
    loadOrderHistory(new URL(link.href, window.location.origin), true);
  });

  document.addEventListener('submit', (event) => {
    const form = event.target.closest('[data-order-history-date-form]');
    if (!form || !form.closest(historySelector)) return;

    event.preventDefault();
    const profileUrl = new URL(form.action, window.location.origin);
    profileUrl.search = new URLSearchParams(new FormData(form)).toString();
    loadOrderHistory(profileUrl, true);
  });

  window.addEventListener('popstate', () => {
    const profileUrl = new URL(window.location.href);
    if (profileUrl.searchParams.get('tab') === 'history') {
      loadOrderHistory(profileUrl, false);
    }
  });
})();

(function () {
  'use strict';

  const formPanel = document.getElementById('ap-address-form-panel');
  const formOpenButtons = document.querySelectorAll('[data-address-form-open]');
  const formCloseButton = document.querySelector('[data-address-form-close]');
  const provinceSelect = document.getElementById('ap-province');
  const districtSelect = document.getElementById('ap-district');
  const wardSelect = document.getElementById('ap-ward');
  const provinceNameInput = document.getElementById('ap-province-name');
  const districtNameInput = document.getElementById('ap-district-name');
  const wardNameInput = document.getElementById('ap-ward-name');
  const API_BASE = 'https://provinces.open-api.vn/api';

  if (!formPanel || !provinceSelect || !districtSelect || !wardSelect) return;

  let provincesCache = [];
  const districtCache = new Map();
  const wardCache = new Map();
  let provincesPromise = null;

  function openForm() {
    formPanel.hidden = false;
    formPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    const firstInput = formPanel.querySelector('input[name="ContactName"]');
    window.setTimeout(() => firstInput?.focus(), 150);
    loadProvinces();
  }

  function closeForm() {
    formPanel.hidden = true;
  }

  function populateSelect(selectEl, items, placeholder) {
    selectEl.innerHTML = `<option value="">${placeholder}</option>`;
    items.forEach(({ code, name }) => {
      const option = document.createElement('option');
      option.value = code;
      option.textContent = name;
      selectEl.appendChild(option);
    });
    selectEl.disabled = false;
    syncSelectedName(selectEl);
  }

  function resetSelect(selectEl, placeholder) {
    selectEl.innerHTML = `<option value="">${placeholder}</option>`;
    selectEl.disabled = true;
    selectEl.value = '';
    syncSelectedName(selectEl);
  }

  function setLoading(selectEl) {
    selectEl.innerHTML = '<option value="">Đang tải...</option>';
    selectEl.disabled = true;
  }

  function syncSelectedName(selectEl) {
    const selectedName = selectEl.selectedOptions[0]?.textContent?.trim() || '';
    if (selectEl === provinceSelect) provinceNameInput.value = selectEl.value ? selectedName : '';
    if (selectEl === districtSelect) districtNameInput.value = selectEl.value ? selectedName : '';
    if (selectEl === wardSelect) wardNameInput.value = selectEl.value ? selectedName : '';
  }

  async function loadProvinces() {
    if (provincesCache.length > 0) {
      populateSelect(provinceSelect, provincesCache, '-- Chọn Tỉnh / Thành phố --');
      return provincesCache;
    }

    if (provincesPromise) return provincesPromise;

    provincesPromise = fetch(`${API_BASE}/p/`)
      .then((response) => response.json())
      .then((provinces) => {
        provincesCache = provinces.map((province) => ({
          code: String(province.code),
          name: province.name
        }));
        populateSelect(provinceSelect, provincesCache, '-- Chọn Tỉnh / Thành phố --');
        return provincesCache;
      })
      .catch(() => {
        provinceSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
        provinceSelect.disabled = false;
        return [];
      });

    return provincesPromise;
  }

  async function loadDistricts(provinceCode) {
    const key = String(provinceCode || '');
    if (!key) return [];

    if (districtCache.has(key)) {
      const cached = districtCache.get(key);
      populateSelect(districtSelect, cached, '-- Chọn Quận / Huyện --');
      return cached;
    }

    setLoading(districtSelect);
    const province = await fetch(`${API_BASE}/p/${key}?depth=2`)
      .then((response) => response.json());
    const districts = (province.districts || []).map((district) => ({
      code: String(district.code),
      name: district.name
    }));

    districtCache.set(key, districts);
    populateSelect(districtSelect, districts, '-- Chọn Quận / Huyện --');
    return districts;
  }

  async function loadWards(districtCode) {
    const key = String(districtCode || '');
    if (!key) return [];

    if (wardCache.has(key)) {
      const cached = wardCache.get(key);
      populateSelect(wardSelect, cached, '-- Chọn Phường / Xã --');
      return cached;
    }

    setLoading(wardSelect);
    const district = await fetch(`${API_BASE}/d/${key}?depth=2`)
      .then((response) => response.json());
    const wards = (district.wards || []).map((ward) => ({
      code: String(ward.code),
      name: ward.name
    }));

    wardCache.set(key, wards);
    populateSelect(wardSelect, wards, '-- Chọn Phường / Xã --');
    return wards;
  }

  formOpenButtons.forEach((button) => button.addEventListener('click', openForm));
  formCloseButton?.addEventListener('click', closeForm);

  provinceSelect.addEventListener('change', async () => {
    syncSelectedName(provinceSelect);
    resetSelect(districtSelect, '-- Chọn Quận / Huyện --');
    resetSelect(wardSelect, '-- Chọn Phường / Xã --');

    if (!provinceSelect.value) return;

    try {
      await loadDistricts(provinceSelect.value);
    } catch {
      districtSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
      districtSelect.disabled = false;
    }
  });

  districtSelect.addEventListener('change', async () => {
    syncSelectedName(districtSelect);
    resetSelect(wardSelect, '-- Chọn Phường / Xã --');

    if (!districtSelect.value) return;

    try {
      await loadWards(districtSelect.value);
    } catch {
      wardSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
      wardSelect.disabled = false;
    }
  });

  wardSelect.addEventListener('change', () => syncSelectedName(wardSelect));

  loadProvinces();
})();

(function () {
  'use strict';

  const list = document.querySelector('[data-profile-favorite-list]');
  if (!list) return;

  function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  }

  function showMessage(message, type = 'default') {
    if (!message) return;
    if (typeof window.showToast === 'function') {
      window.showToast(message, type);
      return;
    }
    window.alert(message);
  }

  function renderEmptyState() {
    const empty = document.createElement('div');
    empty.className = 'ap-favorite-empty';
    empty.innerHTML = [
      '<div class="ap-empty-illustration ap-empty-illustration--small">',
      '<span class="ap-empty-bag">S</span>',
      '</div>',
      '<p>Bạn chưa có sản phẩm nào yêu thích? Hãy bắt đầu mua sắm ngay nào! <a href="/catalog">Mua sắm ngay</a></p>'
    ].join('');
    list.replaceWith(empty);
  }

  async function removeFavorite(button) {
    const productId = button.dataset.productId;
    if (!productId) {
      showMessage('Sản phẩm không hợp lệ.', 'error');
      return;
    }

    button.disabled = true;
    button.classList.add('is-loading');

    try {
      const response = await fetch('/wishlist/remove', {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          RequestVerificationToken: getAntiForgeryToken()
        },
        body: JSON.stringify({ productId })
      });

      let data = {};
      try {
        data = await response.json();
      } catch {
        data = {};
      }

      if (response.status === 401) {
        window.location.href = data.loginUrl || '/Account/Login';
        return;
      }

      if (!response.ok) {
        throw new Error(data.message || 'Không thể bỏ yêu thích sản phẩm.');
      }

      button.closest('[data-profile-favorite-item]')?.remove();
      showMessage(data.message || 'Đã bỏ sản phẩm khỏi danh sách yêu thích.', 'success');

      if (!list.querySelector('[data-profile-favorite-item]')) {
        renderEmptyState();
      }
    } catch (error) {
      showMessage(error.message, 'error');
      button.disabled = false;
      button.classList.remove('is-loading');
    }
  }

  list.addEventListener('click', (event) => {
    const button = event.target.closest('[data-profile-wishlist-remove]');
    if (!button || button.disabled || button.classList.contains('is-loading')) {
      return;
    }

    removeFavorite(button);
  });
})();
