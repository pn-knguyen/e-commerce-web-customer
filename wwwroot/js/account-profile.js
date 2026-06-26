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
