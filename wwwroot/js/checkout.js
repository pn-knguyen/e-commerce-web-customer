(function () {
  'use strict';

  /* ================================================================
     checkout.js
     - Vietnamese address cascade via provinces.open-api.vn
     - Google Maps location picker with reverse geocoding
     - Payment method toggle
  ================================================================ */

  const provinceSelect = document.getElementById('co-province');
  const districtSelect = document.getElementById('co-district');
  const wardSelect = document.getElementById('co-ward');
  const addressDetailInput = document.querySelector('[name="AddressDetail"]');
  const shippingAddressIdInput = document.getElementById('co-shipping-address-id');
  const provinceNameInput = document.getElementById('co-province-name');
  const districtNameInput = document.getElementById('co-district-name');
  const wardNameInput = document.getElementById('co-ward-name');

  const API_BASE = 'https://provinces.open-api.vn/api';
  const DEFAULT_MAP_CENTER = { lat: 10.8231, lng: 106.6297 };

  let provincesCache = [];
  const districtCache = new Map();
  const wardCache = new Map();
  let provincesPromise = null;

  function populateSelect(selectEl, items, placeholder) {
    selectEl.innerHTML = `<option value="">${placeholder}</option>`;
    items.forEach(({ code, name }) => {
      const opt = document.createElement('option');
      opt.value = code;
      opt.textContent = name;
      selectEl.appendChild(opt);
    });
    selectEl.disabled = false;
  }

  function resetSelect(selectEl, placeholder) {
    selectEl.innerHTML = `<option value="">${placeholder}</option>`;
    selectEl.disabled = true;
    selectEl.value = '';
  }

  function setLoading(selectEl) {
    selectEl.innerHTML = '<option value="">Đang tải...</option>';
    selectEl.disabled = true;
  }

  function normalizeAddressName(value) {
    return (value || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'd')
      .toLowerCase()
      .replace(/\b(thanh pho|tp|tinh|quan|q|huyen|thi xa|tx|phuong|p|xa|thi tran|tt|city|district|ward|province)\b/g, '')
      .replace(/[^a-z0-9]/g, '');
  }

  function findByName(items, query) {
    const normalizedQuery = normalizeAddressName(query);
    if (!normalizedQuery) return null;

    return items.find((item) => normalizeAddressName(item.name) === normalizedQuery)
      || items.find((item) => {
        const normalizedName = normalizeAddressName(item.name);
        return normalizedName.includes(normalizedQuery)
          || normalizedQuery.includes(normalizedName);
      })
      || null;
  }

  function setFieldValue(field, value) {
    if (!field || !value) return;
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
    field.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function clearSelectedShippingAddress() {
    if (shippingAddressIdInput) {
      shippingAddressIdInput.value = '';
    }
  }

  function syncSelectedName(selectEl) {
    const selectedName = selectEl.selectedOptions[0]?.textContent?.trim() || '';
    if (selectEl === provinceSelect && provinceNameInput) {
      provinceNameInput.value = selectEl.value ? selectedName : '';
    }
    if (selectEl === districtSelect && districtNameInput) {
      districtNameInput.value = selectEl.value ? selectedName : '';
    }
    if (selectEl === wardSelect && wardNameInput) {
      wardNameInput.value = selectEl.value ? selectedName : '';
    }
  }

  async function loadProvinces() {
    if (provincesCache.length > 0) return provincesCache;
    if (provincesPromise) return provincesPromise;

    provincesPromise = fetch(`${API_BASE}/p/`)
      .then((response) => response.json())
      .then((provinces) => {
        provincesCache = provinces.map((province) => ({
          code: String(province.code),
          name: province.name
        }));

        if (provinceSelect) {
          populateSelect(
            provinceSelect,
            provincesCache,
            '-- Chọn Tỉnh / Thành phố --'
          );
        }

        return provincesCache;
      })
      .catch(() => {
        if (provinceSelect) {
          provinceSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
          provinceSelect.disabled = false;
        }

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

  async function restoreServerAddress() {
    if (!provinceSelect || !districtSelect || !wardSelect) return;

    const provinceCode = provinceSelect.dataset.serverValue;
    const districtCode = districtSelect.dataset.serverValue;
    const wardCode = wardSelect.dataset.serverValue;

    if (!provinceCode) return;
    provinceSelect.value = provinceCode;
    syncSelectedName(provinceSelect);

    if (!districtCode) return;
    const districts = await loadDistricts(provinceCode);
    if (districts.some((district) => district.code === districtCode)) {
      districtSelect.value = districtCode;
      syncSelectedName(districtSelect);
    }

    if (!wardCode) return;
    const wards = await loadWards(districtCode);
    if (wards.some((ward) => ward.code === wardCode)) {
      wardSelect.value = wardCode;
      syncSelectedName(wardSelect);
    }
  }

  if (provinceSelect && districtSelect && wardSelect) {
    loadProvinces().then(restoreServerAddress);

    provinceSelect.addEventListener('change', async () => {
      clearSelectedShippingAddress();
      syncSelectedName(provinceSelect);
      const code = provinceSelect.value;
      resetSelect(districtSelect, '-- Chọn Quận / Huyện --');
      resetSelect(wardSelect, '-- Chọn Phường / Xã --');
      syncSelectedName(districtSelect);
      syncSelectedName(wardSelect);

      if (!code) return;

      try {
        await loadDistricts(code);
      } catch {
        districtSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
        districtSelect.disabled = false;
      }
    });

    districtSelect.addEventListener('change', async () => {
      clearSelectedShippingAddress();
      syncSelectedName(districtSelect);
      const code = districtSelect.value;
      resetSelect(wardSelect, '-- Chọn Phường / Xã --');
      syncSelectedName(wardSelect);

      if (!code) return;

      try {
        await loadWards(code);
      } catch {
        wardSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
        wardSelect.disabled = false;
      }
    });

    wardSelect.addEventListener('change', () => {
      clearSelectedShippingAddress();
      syncSelectedName(wardSelect);
    });
  }

  addressDetailInput?.addEventListener('input', clearSelectedShippingAddress);



  /* ── Mapbox location picker ─────────────────────────────── */
  const mapModal = document.getElementById('co-map-modal');
  const mapOpenBtn = document.getElementById('co-map-open');
  const mapCloseBtn = document.getElementById('co-map-close');
  const mapCanvas = document.getElementById('co-map-canvas');
  const mapStatus = document.getElementById('co-map-status');
  const mapUseBtn = document.getElementById('co-map-use');
  const mapboxConfigUrl = mapModal?.dataset.googleMapsConfigUrl || '/api/integrations/google-maps/config';

  let mapboxClientConfigPromise = null;
  let mapboxPromise = null;
  let mapboxToken = '';
  let checkoutMap = null;
  let checkoutMarker = null;
  let selectedLocation = null;

  function setMapStatus(message, tone) {
    if (!mapStatus) return;
    mapStatus.textContent = message;
    mapStatus.classList.toggle('is-success', tone === 'success');
    mapStatus.classList.toggle('is-error', tone === 'error');
  }

  function showMapCanvasError(title, description) {
    if (!mapCanvas) return;

    mapCanvas.querySelector('.co-map-error-panel')?.remove();

    const panel = document.createElement('div');
    panel.className = 'co-map-error-panel';
    panel.setAttribute('role', 'alert');
    panel.innerHTML = `
      <strong>${title}</strong>
      <span>${description}</span>
    `;

    mapCanvas.appendChild(panel);
  }

  function clearMapCanvasError() {
    mapCanvas?.querySelector('.co-map-error-panel')?.remove();
  }

  async function loadMapboxConfig() {
    if (mapboxClientConfigPromise) return mapboxClientConfigPromise;

    mapboxClientConfigPromise = fetch(mapboxConfigUrl, {
      headers: { Accept: 'application/json' }
    })
      .then(async (response) => {
        const payload = await response.json().catch(() => null);
        if (!response.ok || !payload?.isConfigured || !payload?.apiKey) {
          throw new Error('missing-api-key');
        }

        return payload;
      });

    return mapboxClientConfigPromise;
  }

  async function loadMapboxApi() {
    if (window.mapboxgl) return Promise.resolve(window.mapboxgl);
    if (mapboxPromise) return mapboxPromise;

    const clientConfig = await loadMapboxConfig();
    mapboxToken = clientConfig.apiKey;

    mapboxPromise = new Promise((resolve, reject) => {
      const link = document.createElement('link');
      link.href = 'https://api.mapbox.com/mapbox-gl-js/v3.4.0/mapbox-gl.css';
      link.rel = 'stylesheet';
      document.head.appendChild(link);

      const script = document.createElement('script');
      script.id = 'mapbox-js-api';
      script.src = 'https://api.mapbox.com/mapbox-gl-js/v3.4.0/mapbox-gl.js';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        window.mapboxgl.accessToken = mapboxToken;
        resolve(window.mapboxgl);
      };
      script.onerror = () => reject(new Error('mapbox-load-failed'));
      document.head.appendChild(script);
    });

    return mapboxPromise;
  }

  async function initCheckoutMap() {
    clearMapCanvasError();
    await loadMapboxApi();
    if (checkoutMap || !mapCanvas) return;

    const center = selectedLocation
      ? [selectedLocation.lng, selectedLocation.lat]
      : [DEFAULT_MAP_CENTER.lng, DEFAULT_MAP_CENTER.lat];

    checkoutMap = new mapboxgl.Map({
      container: mapCanvas,
      style: 'mapbox://styles/mapbox/streets-v12',
      center: center,
      zoom: 13
    });

    checkoutMarker = new mapboxgl.Marker({
      draggable: true
    })
      .setLngLat(center)
      .addTo(checkoutMap);

    checkoutMap.on('click', (event) => {
      selectMapLocation(event.lngLat);
    });

    checkoutMarker.on('dragend', () => {
      const lngLat = checkoutMarker.getLngLat();
      selectMapLocation(lngLat);
    });
  }

  function parseMapboxAddress(feature) {
    const context = feature.context || [];
    let province = '';
    let district = '';
    let ward = '';

    context.forEach(item => {
      if (item.id.startsWith('region')) {
        province = item.text;
      } else if (item.id.startsWith('district') || item.id.startsWith('locality') || item.id.startsWith('place')) {
        if (!district) district = item.text;
      } else if (item.id.startsWith('neighborhood') || item.id.startsWith('postcode') || item.id.startsWith('ward')) {
        if (!ward) ward = item.text;
      }
    });

    if (feature.place_type.includes('neighborhood')) {
      ward = feature.text;
    } else if (feature.place_type.includes('district') || feature.place_type.includes('place')) {
      district = feature.text;
    } else if (feature.place_type.includes('region')) {
      province = feature.text;
    }

    let route = '';
    let streetNumber = '';
    if (feature.place_type.includes('address')) {
      route = feature.text;
      streetNumber = feature.address || '';
    }

    return {
      province,
      district,
      ward,
      streetNumber,
      route,
      formattedAddress: feature.place_name
    };
  }

  async function fillAddressForm(parsedAddress) {
    if (!provinceSelect || !districtSelect || !wardSelect) return;

    await loadProvinces();
    const province = findByName(provincesCache, parsedAddress.province);

    if (province) {
      provinceSelect.value = province.code;
      resetSelect(districtSelect, '-- Chọn Quận / Huyện --');
      resetSelect(wardSelect, '-- Chọn Phường / Xã --');

      const districts = await loadDistricts(province.code);
      const district = findByName(districts, parsedAddress.district);

      if (district) {
        districtSelect.value = district.code;

        const wards = await loadWards(district.code);
        const ward = findByName(wards, parsedAddress.ward);
        if (ward) {
          wardSelect.value = ward.code;
        }
      }
    }

    const addressDetail = [parsedAddress.streetNumber, parsedAddress.route]
      .filter(Boolean)
      .join(' ')
      .trim() || parsedAddress.formattedAddress.split(',')[0];

    setFieldValue(addressDetailInput, addressDetail);
  }

  async function reverseGeocodeLocation(lngLat) {
    const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${lngLat.lng},${lngLat.lat}.json?access_token=${mapboxToken}&language=vi`;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error('reverse-geocode-failed');
    }
    const data = await response.json();
    if (!data.features || data.features.length === 0) {
      throw new Error('reverse-geocode-failed');
    }
    return parseMapboxAddress(data.features[0]);
  }

  async function selectMapLocation(lngLat) {
    if (!checkoutMarker) return;

    selectedLocation = lngLat;
    checkoutMarker.setLngLat(lngLat);
    checkoutMap.panTo(lngLat);
    if (mapUseBtn) mapUseBtn.disabled = true;
    setMapStatus('Đang xác định địa chỉ từ vị trí đã chọn...', null);

    try {
      const parsedAddress = await reverseGeocodeLocation(lngLat);
      await fillAddressForm(parsedAddress);
      if (mapUseBtn) mapUseBtn.disabled = false;
      setMapStatus('Đã tự động điền địa chỉ. Bạn có thể dùng vị trí này hoặc chọn lại trên bản đồ.', 'success');
    } catch (error) {
      setMapStatus('Không thể xác định địa chỉ từ Mapbox. Vui lòng thử lại hoặc nhập thủ công.', 'error');
    }
  }

  function openMap() {
    if (!mapModal) return;

    mapModal.classList.add('is-open');
    document.body.style.overflow = 'hidden';
    mapCloseBtn?.focus();
    setMapStatus('Đang tải bản đồ Mapbox...', null);

    initCheckoutMap()
      .then(() => {
        window.setTimeout(() => {
          checkoutMap.resize();

          // Tự động định vị vị trí người dùng (chỉ hoạt động trên HTTPS hoặc localhost)
          if (navigator.geolocation && !selectedLocation) {
            setMapStatus('Đang xác định vị trí hiện tại của bạn...', null);
            navigator.geolocation.getCurrentPosition(
              (position) => {
                const userLngLat = {
                  lng: position.coords.longitude,
                  lat: position.coords.latitude
                };
                selectMapLocation(userLngLat);
              },
              (error) => {
                // Nếu người dùng chặn quyền hoặc lỗi, định vị theo tâm mặc định
                const center = [DEFAULT_MAP_CENTER.lng, DEFAULT_MAP_CENTER.lat];
                checkoutMap.setCenter(center);
                setMapStatus('Kéo ghim hoặc click trên bản đồ để chọn vị trí giao hàng.', null);
              },
              { enableHighAccuracy: true, timeout: 6000 }
            );
          } else {
            const center = selectedLocation
              ? [selectedLocation.lng, selectedLocation.lat]
              : [DEFAULT_MAP_CENTER.lng, DEFAULT_MAP_CENTER.lat];
            checkoutMap.setCenter(center);
            setMapStatus('Kéo ghim hoặc click trên bản đồ để chọn vị trí giao hàng.', null);
          }
        }, 200);
      })
      .catch((error) => {
        setMapStatus('Không thể tải bản đồ Mapbox. Vui lòng kiểm tra cấu hình.', 'error');
        showMapCanvasError('Lỗi tải bản đồ', 'Hãy kiểm tra lại Mapbox Access Token.');
      });
  }

  function closeMap() {
    if (!mapModal) return;
    mapModal.classList.remove('is-open');
    document.body.style.overflow = '';
    mapOpenBtn?.focus();
  }

  mapOpenBtn?.addEventListener('click', openMap);
  mapCloseBtn?.addEventListener('click', closeMap);
  mapUseBtn?.addEventListener('click', closeMap);
  mapModal?.addEventListener('click', (event) => {
    if (event.target === mapModal) closeMap();
  });
  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && mapModal?.classList.contains('is-open')) {
      closeMap();
    }
  });

  /* ── Payment method switching ────────────────────────────────── */
  const paymentRadios = document.querySelectorAll('[name="PaymentMethodId"]');
  const codInfoBlock = document.getElementById('co-cod-info');

  function syncPaymentInfo() {
    const selected = document.querySelector('[name="PaymentMethodId"]:checked');
    if (!codInfoBlock) return;
    codInfoBlock.hidden = selected?.dataset.paymentKind !== 'cod';
  }

  paymentRadios.forEach((radio) => radio.addEventListener('change', syncPaymentInfo));
  syncPaymentInfo();
})();
