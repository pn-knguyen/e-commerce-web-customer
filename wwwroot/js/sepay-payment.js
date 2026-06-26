(() => {
  'use strict';

  const payment = document.getElementById('sepay-payment');
  const status = document.getElementById('sepay-status');
  const statusTitle = document.getElementById('sepay-status-title');
  const statusDetail = document.getElementById('sepay-status-detail');
  const checkButton = document.getElementById('sepay-check-now');
  const successOverlay = document.getElementById('sepay-success-overlay');
  if (!payment || !status || !statusTitle || !statusDetail) return;

  const statusUrl = payment.dataset.statusUrl;
  const successUrl = payment.dataset.successUrl;
  const expiresAt = Number(payment.dataset.expiresAt || 0);
  let completed = false;
  let requestInFlight = false;
  let pollTimer = null;
  let consecutiveFailures = 0;

  document.querySelectorAll('[data-copy]').forEach((button) => {
    button.addEventListener('click', async () => {
      const original = button.textContent;
      try {
        await navigator.clipboard.writeText(button.dataset.copy || '');
        button.textContent = 'Đã chép ✓';
      } catch {
        button.textContent = 'Hãy sao chép thủ công';
      }
      setTimeout(() => { button.textContent = original; }, 1400);
    });
  });

  function renderWaiting() {
    if (completed || consecutiveFailures >= 3) return;

    const remaining = expiresAt - Date.now();
    if (remaining <= 0) {
      status.dataset.state = 'delayed';
      statusTitle.textContent = 'Vẫn đang kiểm tra giao dịch';
      statusDetail.textContent = 'Đã quá thời gian dự kiến, hệ thống vẫn tự động đối soát.';
      return;
    }

    const minutes = String(Math.floor(remaining / 60000)).padStart(2, '0');
    const seconds = String(Math.floor((remaining % 60000) / 1000)).padStart(2, '0');
    status.dataset.state = 'waiting';
    statusTitle.textContent = 'Đang chờ thanh toán';
    statusDetail.textContent = `Thời gian dự kiến còn lại: ${minutes}:${seconds}`;
  }

  function renderConnectionIssue(message) {
    status.dataset.state = 'error';
    statusTitle.textContent = 'Chưa thể kiểm tra trạng thái';
    statusDetail.textContent = message;
  }

  function showPaid() {
    if (completed) return;
    completed = true;
    if (pollTimer) clearTimeout(pollTimer);

    status.dataset.state = 'paid';
    statusTitle.textContent = 'Thanh toán thành công';
    statusDetail.textContent = 'Giao dịch đã được xác nhận. Đang hoàn tất đơn hàng...';
    if (checkButton) checkButton.disabled = true;
    if (successOverlay) successOverlay.hidden = false;

    setTimeout(() => { window.location.assign(successUrl); }, 1800);
  }

  function showUnavailable(paymentStatus) {
    completed = true;
    if (pollTimer) clearTimeout(pollTimer);

    status.dataset.state = 'error';
    statusTitle.textContent = paymentStatus === 'refunded'
      ? 'Giao dịch đã được hoàn tiền'
      : 'Không thể tiếp tục thanh toán';
    statusDetail.textContent = 'Trạng thái đơn hàng không còn cho phép nhận thanh toán.';
    if (checkButton) checkButton.disabled = true;
  }

  async function checkStatus({ manual = false } = {}) {
    if (completed || requestInFlight || !statusUrl) return;
    requestInFlight = true;
    if (manual && checkButton) {
      checkButton.disabled = true;
      checkButton.textContent = 'Đang kiểm tra...';
    }

    try {
      const url = new URL(statusUrl, window.location.origin);
      url.searchParams.set('_', Date.now().toString());
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 8000);

      let response;
      try {
        response = await fetch(url, {
          method: 'GET',
          headers: {
            Accept: 'application/json',
            'Cache-Control': 'no-cache'
          },
          credentials: 'same-origin',
          cache: 'no-store',
          signal: controller.signal
        });
      } finally {
        clearTimeout(timeout);
      }

      if (response.status === 401) {
        consecutiveFailures = 3;
        renderConnectionIssue('Phiên đăng nhập đã hết hạn. Hãy tải lại trang để tiếp tục.');
        return;
      }

      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      const result = await response.json();
      consecutiveFailures = 0;
      if (result.status === 'paid') {
        showPaid();
        return;
      }
      if (result.status === 'failed' || result.status === 'refunded') {
        showUnavailable(result.status);
        return;
      }
      renderWaiting();
    } catch {
      consecutiveFailures += 1;
      if (consecutiveFailures >= 3) {
        renderConnectionIssue('Kết nối đang gián đoạn. Hệ thống sẽ tự thử lại.');
      }
    } finally {
      requestInFlight = false;
      if (manual && checkButton && !completed) {
        checkButton.disabled = false;
        checkButton.textContent = 'Tôi đã chuyển khoản';
      }
    }
  }

  function schedulePoll(delay = 2000) {
    if (completed) return;
    if (pollTimer) clearTimeout(pollTimer);
    pollTimer = setTimeout(async () => {
      await checkStatus();
      schedulePoll(2000);
    }, delay);
  }

  checkButton?.addEventListener('click', () => checkStatus({ manual: true }));

  window.addEventListener('focus', () => checkStatus());
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') checkStatus();
  });
  window.addEventListener('pageshow', () => checkStatus());

  setInterval(renderWaiting, 1000);

  if (payment.dataset.initialStatus === 'paid') {
    showPaid();
  } else if (
    payment.dataset.initialStatus === 'failed'
    || payment.dataset.initialStatus === 'refunded'
  ) {
    showUnavailable(payment.dataset.initialStatus);
  } else {
    renderWaiting();
    checkStatus();
    schedulePoll();
  }
})();
