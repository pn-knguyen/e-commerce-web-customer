(function () {
  'use strict';

  const widgets = [];
  let supportUnreadCount = 0;

  const supportWidget = createChatWidget({
    kind: 'support',
    channel: 'support',
    rootId: 'support-chat',
    panelId: 'support-chat-panel',
    toggleId: 'support-chat-toggle',
    closeId: 'support-chat-close',
    formId: 'support-chat-form',
    inputId: 'support-chat-input',
    messagesId: 'support-chat-messages',
    statusId: 'support-chat-status',
    requiresRealtimeToSend: false,
    greeting: 'Nhắn trực tiếp với admin TechStore. Tin của bạn sẽ vào hàng chờ để nhân viên phản hồi.',
    loginTitle: 'Đăng nhập để nhắn admin',
    loginText: 'Admin cần tài khoản của bạn để theo dõi và trả lời đúng hội thoại.',
    connectedStatus: 'Đã kết nối',
    disconnectedStatus: 'Chưa kết nối realtime'
  });

  const aiWidget = createChatWidget({
    kind: 'ai',
    channel: 'ai',
    rootId: 'ai-assistant',
    panelId: 'ai-assistant-panel',
    toggleId: 'ai-assistant-toggle',
    closeId: 'ai-assistant-close',
    formId: 'ai-assistant-form',
    inputId: 'ai-assistant-input',
    messagesId: 'ai-assistant-messages',
    loadingId: 'ai-assistant-loading',
    statusId: 'ai-assistant-status',
    requiresRealtimeToSend: false,
    greeting: 'Xin chào! Mình là trợ lý AI của TechStore, có thể tư vấn và gợi ý sản phẩm ngay lập tức.',
    connectedStatus: 'AI sẵn sàng',
    disconnectedStatus: 'AI sẵn sàng, chưa lưu lịch sử'
  });

  if (supportWidget) widgets.push(supportWidget);
  if (aiWidget) widgets.push(aiWidget);

  widgets.forEach(widget => widget.bootstrap());

  function switchToWidget(kind) {
    const target = widgets.find(widget => widget.kind === kind);
    target?.open();
  }

  function updateChannelSwitchState(activeKind = '') {
    document.querySelectorAll('[data-chat-channel-switch]').forEach(button => {
      const isActive = button.dataset.chatChannelSwitch === activeKind;
      button.setAttribute('aria-pressed', String(isActive));
    });
  }

  function setSupportUnreadCount(value) {
    supportUnreadCount = Math.max(0, Number(value) || 0);
    const text = supportUnreadCount > 9 ? '9+' : String(supportUnreadCount);

    document.querySelectorAll('[data-support-unread]').forEach(badge => {
      badge.textContent = text;
      badge.hidden = supportUnreadCount === 0;
    });

    const toggle = document.getElementById('support-chat-toggle');
    if (toggle) {
      toggle.setAttribute(
        'aria-label',
        supportUnreadCount > 0
          ? `Mở chat admin, ${supportUnreadCount} tin nhắn mới`
          : 'Mở chat admin');
    }
  }

  function setSupportPresence(isOnline) {
    document.querySelectorAll('[data-support-presence]').forEach(indicator => {
      indicator.classList.toggle('is-online', Boolean(isOnline));
    });
  }

  function createChatWidget(options) {
    const root = document.getElementById(options.rootId);
    if (!root) return null;

    const panel = document.getElementById(options.panelId);
    const toggle = document.getElementById(options.toggleId);
    const close = document.getElementById(options.closeId);
    const form = document.getElementById(options.formId);
    const input = document.getElementById(options.inputId);
    const messages = document.getElementById(options.messagesId);
    const loading = options.loadingId ? document.getElementById(options.loadingId) : null;
    const status = document.getElementById(options.statusId);
    const submit = form.querySelector('button[type="submit"]');
    const dock = root.closest('.chat-dock');
    const greetingBubble = options.kind === 'ai'
      ? root.querySelector('[data-ai-greeting]')
      : null;
    let greetingTimer = null;

    const state = {
      isLoggedIn: false,
      userId: null,
      displayName: '',
      conversationId: null,
      hubUrl: '',
      aiProvider: 'Gemini',
      aiModel: 'gemini-2.5-flash',
      realtimeAccessToken: '',
      realtimeAccessTokenExpiresAt: '',
      connection: null,
      joinedConversationId: null,
      messageIds: new Set(),
      pendingMessages: new Map(),
      chatLog: []
    };

    toggle.addEventListener('click', () => setOpen(!root.classList.contains('is-open')));
    close.addEventListener('click', () => setOpen(false));
    root.querySelectorAll('[data-chat-channel-switch]').forEach(button => {
      button.addEventListener('click', () => switchToWidget(button.dataset.chatChannelSwitch));
    });
    form.addEventListener('submit', event =>
      options.kind === 'ai' ? handleAiSubmit(event) : handleSupportSubmit(event));
    input.addEventListener('keydown', handleInputKeydown);
    input.addEventListener('input', resizeInput);

    return {
      kind: options.kind,
      root,
      bootstrap,
      close: () => setOpen(false),
      open: () => setOpen(true)
    };

    async function bootstrap() {
      setStatus('Đang kết nối');
      setFormEnabled(false);

      try {
        const response = await fetch(`/api/customer-messages/bootstrap?channel=${encodeURIComponent(options.channel)}`, {
          cache: 'no-store',
          credentials: 'same-origin',
          headers: { Accept: 'application/json' }
        });

        if (!response.ok) {
          throw new Error('Không thể tải hội thoại.');
        }

        const data = await response.json();
        state.isLoggedIn = Boolean(data.isLoggedIn);
        state.userId = data.userId;
        state.displayName = data.displayName || '';
        state.conversationId = data.conversationId || null;
        state.hubUrl = data.hubUrl || '';
        state.aiProvider = data.aiProvider || 'Gemini';
        state.aiModel = data.aiModel || 'gemini-2.5-flash';
        state.realtimeAccessToken = data.realtimeAccessToken || '';
        state.realtimeAccessTokenExpiresAt = data.realtimeAccessTokenExpiresAt || '';

        showAiGreeting();
        messages.replaceChildren();

        if (options.kind === 'support' && (!state.isLoggedIn || !state.userId)) {
          renderLoginPrompt();
          setStatus('Cần đăng nhập');
          return;
        }

        renderMessages(Array.isArray(data.messages) ? data.messages : []);

        const connected = state.isLoggedIn && state.userId
          ? await connectRealtime()
          : false;

        if (options.kind === 'support') {
          setFormEnabled(true);
          return;
        }

        setFormEnabled(true);
        setStatus(connected ? options.connectedStatus : options.disconnectedStatus);
        if (!state.isLoggedIn) {
          renderSystemMessage('Đăng nhập để lưu lịch sử tư vấn AI cùng tài khoản của bạn.', false, 'ai-login-save');
        }
      } catch (error) {
        renderSystemMessage(error.message || 'Chatbox đang gặp lỗi. Vui lòng thử lại sau.', true, 'bootstrap');
        setStatus('Mất kết nối');
        setFormEnabled(options.kind === 'ai');
      } finally {
        resizeInput();
      }
    }

    async function connectRealtime() {
      if (!window.signalR || !state.hubUrl || !state.userId) {
        setStatus(options.disconnectedStatus);
        if (options.kind === 'support') {
          renderSystemMessage('Chat realtime chưa sẵn sàng. Tin nhắn vẫn sẽ được gửi qua kết nối dự phòng.', false, 'realtime');
        }
        return false;
      }

      state.connection = new signalR.HubConnectionBuilder()
        .withUrl(state.hubUrl, {
          withCredentials: false,
          accessTokenFactory: getRealtimeAccessToken
        })
        .withAutomaticReconnect()
        .build();

      if (options.kind === 'support') {
        state.connection.on('MessageReceived', handleRealtimeMessage);
        state.connection.on('ConversationChanged', handleConversationChanged);
        state.connection.on('ConversationStatusChanged', handleConversationChanged);
      }

      state.connection.onreconnecting(() => {
        setStatus('Đang nối lại');
        if (options.kind === 'support') setFormEnabled(true);
      });

      state.connection.onreconnected(async () => {
        setStatus(options.connectedStatus);
        if (options.kind === 'support') setFormEnabled(true);
        state.joinedConversationId = null;
        await joinCurrentConversation();
      });

      state.connection.onclose(() => {
        setStatus(options.disconnectedStatus);
        if (options.kind === 'support') {
          setFormEnabled(true);
          renderSystemMessage('Kết nối realtime bị ngắt. Tin nhắn vẫn sẽ được gửi qua kết nối dự phòng.', false, 'realtime');
        }
      });

      try {
        await state.connection.start();
        setStatus(options.connectedStatus);
        await joinCurrentConversation();
        removeSystemMessage('realtime');
        return true;
      } catch (error) {
        console.error(`${options.kind} chat realtime connection failed:`, error);
        setStatus(options.disconnectedStatus);
        if (options.kind === 'support') {
          renderSystemMessage('Chưa kết nối được realtime. Tin nhắn vẫn sẽ được gửi qua kết nối dự phòng.', false, 'realtime');
        }
        return false;
      }
    }

    async function getRealtimeAccessToken() {
      const expiresAt = Date.parse(state.realtimeAccessTokenExpiresAt || '');
      if (state.realtimeAccessToken && Number.isFinite(expiresAt) && expiresAt - Date.now() > 60000) {
        return state.realtimeAccessToken;
      }

      const response = await fetch('/api/customer-messages/access-token', {
        cache: 'no-store',
        credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      });
      const result = await response.json().catch(() => null);
      if (!response.ok || !result?.accessToken) {
        throw new Error(result?.message || 'Không thể làm mới phiên chat realtime.');
      }

      state.realtimeAccessToken = result.accessToken;
      state.realtimeAccessTokenExpiresAt = result.expiresAt || '';
      return state.realtimeAccessToken;
    }

    async function joinCurrentConversation() {
      if (!isRealtimeConnected() || !state.conversationId) return;
      if (Number(state.joinedConversationId) === Number(state.conversationId)) return;

      const result = await state.connection.invoke('JoinConversation', Number(state.conversationId));
      if (result?.succeeded) {
        state.joinedConversationId = Number(state.conversationId);
      }
    }

    async function handleSupportSubmit(event) {
      event.preventDefault();

      if (!state.isLoggedIn) {
        window.location.href = buildLoginUrl();
        return;
      }

      const body = input.value.trim();
      if (!body || submit.disabled) return;

      const tempId = createTempId('support');
      const clientMessageId = createClientMessageId();
      appendMessage({
        id: tempId,
        conversationId: state.conversationId || 0,
        sender: 'Customer',
        senderName: state.displayName || 'Bạn',
        body,
        createdAtText: 'Đang gửi',
        createdAtIso: new Date().toISOString()
      }, { pending: true });

      input.value = '';
      resizeInput();
      setFormEnabled(false);

      try {
        const payload = {
          conversationId: state.conversationId,
          subject: null,
          clientMessageId,
          body
        };

        let result = null;
        if (isRealtimeConnected()) {
          try {
            result = await state.connection.invoke('SendCustomerMessage', payload);
          } catch (error) {
            console.warn('Support realtime send failed; falling back to HTTP.', error);
          }
        }

        if (!result) {
          result = await sendSupportMessageViaHttp(payload);
        }

        if (!result?.succeeded || !result.conversationId) {
          throw new Error(result?.message || 'Không thể gửi tin nhắn.');
        }

        state.conversationId = Number(result.conversationId);
        reconcilePending(tempId, result.messageId, state.conversationId);
        await joinCurrentConversation();
      } catch (error) {
        markPendingFailed(tempId);
        renderSystemMessage(error.message || 'Không thể gửi tin nhắn. Vui lòng thử lại.', true, 'send');
      } finally {
        setFormEnabled(true);
        input.focus();
      }
    }

    async function handleAiSubmit(event) {
      event.preventDefault();

      const question = input.value.trim();
      if (!question || submit.disabled) return;

      const previousHistory = buildAiHistory();
      const tempId = createTempId('ai-user');
      appendMessage({
        id: tempId,
        conversationId: state.conversationId || 0,
        sender: 'Customer',
        senderName: 'Bạn',
        body: question,
        createdAtText: 'Vừa hỏi',
        createdAtIso: new Date().toISOString()
      }, { pending: false });

      input.value = '';
      resizeInput();
      setLoading(true);

      try {
        const aiResult = await requestAiReply(question, previousHistory);
        appendMessage({
          id: createTempId('ai-reply'),
          conversationId: state.conversationId || 0,
          sender: 'Ai',
          senderName: state.aiModel ? `AI (${state.aiModel})` : 'AI',
          body: aiResult.reply,
          aiMetadataJson: aiResult.persistenceMetadataJson || null,
          createdAtIso: new Date().toISOString(),
          createdAtText: 'Vừa xong'
        });

        await saveAiExchange(question, aiResult);
      } catch (error) {
        renderSystemMessage(error.message || 'AI chưa thể trả lời lúc này.', true, 'ai-error');
      } finally {
        setLoading(false);
        input.focus();
      }
    }

    async function requestAiReply(question, history) {
      const response = await fetch('/api/chat', {
        method: 'POST',
        cache: 'no-store',
        credentials: 'same-origin',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ message: question, history })
      });

      const result = await response.json().catch(() => null);
      if (!response.ok || !result?.success) {
        throw new Error(result?.message || 'AI chưa thể trả lời lúc này.');
      }

      return result;
    }

    async function saveAiExchange(question, aiResult) {
      if (!state.isLoggedIn || !state.userId) {
        renderSystemMessage('Đăng nhập để lưu lịch sử tư vấn AI cùng tài khoản của bạn.', false, 'ai-login-save');
        return;
      }

      if (!aiResult.persistenceReceipt || !aiResult.persistenceMetadataJson) {
        renderSystemMessage('AI đã trả lời nhưng phản hồi chưa có chứng thực để lưu lịch sử.', true, 'ai-save');
        return;
      }

      const payload = {
        conversationId: state.conversationId,
        question,
        reply: aiResult.reply,
        aiMetadataJson: aiResult.persistenceMetadataJson,
        receipt: aiResult.persistenceReceipt
      };

      let result = null;
      if (isRealtimeConnected()) {
        try {
          result = await state.connection.invoke('RecordCustomerAiExchange', payload);
        } catch (error) {
          console.warn('AI exchange realtime persistence failed; falling back to HTTP.', error);
        }
      }

      if (!result) {
        try {
          result = await persistAiExchangeViaHttp(payload);
        } catch (error) {
          renderSystemMessage(error.message || 'AI đã trả lời nhưng chưa lưu được lịch sử.', true, 'ai-save');
          return;
        }
      }

      if (!result?.succeeded || !result.conversationId) {
        renderSystemMessage(result?.message || 'AI đã trả lời nhưng chưa lưu được lịch sử.', true, 'ai-save');
        return;
      }

      state.conversationId = Number(result.conversationId);
      removeSystemMessage('ai-save');
      await joinCurrentConversation();
    }

    async function sendSupportMessageViaHttp(payload) {
      const accessToken = await getRealtimeAccessToken();
      const response = await fetch(buildAdminApiUrl('/api/customer-messages/support-messages'), {
        method: 'POST',
        cache: 'no-store',
        credentials: 'omit',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      const result = await response.json().catch(() => null);
      if (!response.ok) {
        return result || {
          succeeded: false,
          message: 'Không thể gửi tin nhắn. Vui lòng thử lại.'
        };
      }

      return result;
    }

    async function persistAiExchangeViaHttp(payload) {
      const accessToken = await getRealtimeAccessToken();
      const response = await fetch(buildAdminApiUrl('/api/customer-messages/ai-exchanges'), {
        method: 'POST',
        cache: 'no-store',
        credentials: 'omit',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      const result = await response.json().catch(() => null);
      if (!response.ok) {
        return result || {
          succeeded: false,
          message: 'AI đã trả lời nhưng chưa lưu được lịch sử.'
        };
      }

      return result;
    }

    function buildAdminApiUrl(path) {
      if (!state.hubUrl) {
        throw new Error('Chưa cấu hình địa chỉ hệ thống tin nhắn.');
      }

      const url = new URL(state.hubUrl, window.location.href);
      url.pathname = path;
      url.search = '';
      url.hash = '';
      return url.toString();
    }

    function handleRealtimeMessage(payload) {
      if (!payload?.id) return;
      if (!matchesCurrentChannel(payload.conversation?.channel, payload.conversationId)) return;

      if (state.conversationId && Number(payload.conversationId) !== Number(state.conversationId)) {
        return;
      }

      state.conversationId = Number(payload.conversationId || state.conversationId || 0) || state.conversationId;
      if (state.messageIds.has(String(payload.id))) return;
      if (replaceMatchingPending(payload)) return;

      if (options.kind === 'support' &&
          payload.sender !== 'Customer' &&
          !root.classList.contains('is-open')) {
        setSupportUnreadCount(supportUnreadCount + 1);
      }

      appendMessage(payload);
    }

    function handleConversationChanged(payload) {
      if (!payload?.id) return;
      if (!matchesCurrentChannel(payload.channel, payload.id)) return;

      if (!state.conversationId || Number(payload.id) === Number(state.conversationId)) {
        state.conversationId = Number(payload.id);
        joinCurrentConversation();
      }
    }

    function matchesCurrentChannel(channel, conversationId) {
      if (typeof channel === 'string' && channel.trim()) {
        return channel.toLowerCase() === options.channel.toLowerCase();
      }

      return Boolean(state.conversationId) &&
        Number(conversationId) === Number(state.conversationId);
    }

    function renderMessages(items) {
      state.chatLog = [];
      state.messageIds.clear();
      messages.replaceChildren();

      if (items.length === 0) {
        renderSystemMessage(options.greeting);
        return;
      }

      items.forEach(item => appendMessage(item, { silent: true }));
      scrollToLatest();
    }

    function appendMessage(payload, appendOptions = {}) {
      const id = String(payload.id || createTempId('message'));
      const isPending = Boolean(appendOptions.pending);

      if (!isPending && state.messageIds.has(id)) return;

      messages
        .querySelectorAll('.chat-widget__system:not([data-system-message="ai-login-save"])')
        .forEach(element => element.remove());

      const element = createMessageElement({ ...payload, id }, isPending);
      messages.appendChild(element);

      if (isPending) {
        state.pendingMessages.set(id, {
          sender: payload.sender,
          body: payload.body
        });
      } else {
        state.messageIds.add(id);
      }

      upsertChatLog({ ...payload, id });
      if (!appendOptions.silent) scrollToLatest();
    }

    function createMessageElement(payload, isPending) {
      const sender = payload.sender || 'Ai';
      const isCustomer = sender === 'Customer';
      const element = document.createElement('article');
      element.className = `chat-widget__message chat-widget__message--${isCustomer ? 'user' : 'assistant'} chat-widget__message--${sender.toLowerCase()}`;
      element.dataset.messageId = String(payload.id || '');
      element.dataset.sender = sender;
      if (isPending) element.classList.add('is-pending');

      const meta = document.createElement('span');
      meta.className = 'chat-widget__message-meta';
      meta.textContent = buildMetaText(payload, isCustomer);
      element.appendChild(meta);

      const body = document.createElement('span');
      body.className = 'chat-widget__message-body';
      body.textContent = payload.body || '';
      element.appendChild(body);

      const products = resolveProducts(payload);
      if (sender === 'Ai' && products.length > 0) {
        element.appendChild(createProductCards(products));
      }

      return element;
    }

    function buildMetaText(payload, isCustomer) {
      const name = isCustomer ? 'Bạn' : (payload.senderName || payload.senderLabel || 'TechStore');
      const time = payload.createdAtText || '';
      return time ? `${name} · ${time}` : name;
    }

    function reconcilePending(tempId, messageId, conversationId) {
      const element = getMessageElement(tempId);
      if (!element) return;

      const realId = String(messageId || tempId);
      element.dataset.messageId = realId;
      element.classList.remove('is-pending', 'is-error');

      const meta = element.querySelector('.chat-widget__message-meta');
      if (meta) meta.textContent = 'Bạn · Vừa xong';

      state.pendingMessages.delete(tempId);
      state.messageIds.add(realId);
      state.chatLog = state.chatLog.map(item =>
        String(item.id) === String(tempId)
          ? { ...item, id: realId, conversationId }
          : item);
    }

    function replaceMatchingPending(payload) {
      const match = Array.from(state.pendingMessages.entries())
        .find(([, item]) => item.sender === payload.sender && item.body === payload.body);

      if (!match) return false;

      const [tempId] = match;
      const element = getMessageElement(tempId);
      if (!element) {
        state.pendingMessages.delete(tempId);
        return false;
      }

      const replacement = createMessageElement(payload, false);
      element.replaceWith(replacement);
      state.pendingMessages.delete(tempId);
      state.messageIds.add(String(payload.id));
      state.chatLog = state.chatLog.map(item =>
        String(item.id) === String(tempId) ? payload : item);
      scrollToLatest();
      return true;
    }

    function markPendingFailed(tempId) {
      const element = getMessageElement(tempId);
      if (!element) return;

      element.classList.remove('is-pending');
      element.classList.add('is-error');
      const meta = element.querySelector('.chat-widget__message-meta');
      if (meta) meta.textContent = 'Bạn · Chưa gửi được';
      state.pendingMessages.delete(tempId);
    }

    function getMessageElement(id) {
      return Array.from(messages.children)
        .find(element => element.dataset.messageId === String(id));
    }

    function renderLoginPrompt() {
      messages.replaceChildren();

      const wrap = document.createElement('div');
      wrap.className = 'chat-widget__login';

      const title = document.createElement('strong');
      title.textContent = options.loginTitle || 'Đăng nhập để tiếp tục';
      wrap.appendChild(title);

      const text = document.createElement('span');
      text.textContent = options.loginText || 'Bạn cần đăng nhập để dùng tính năng này.';
      wrap.appendChild(text);

      const link = document.createElement('a');
      link.href = buildLoginUrl();
      link.textContent = 'Đăng nhập';
      wrap.appendChild(link);

      messages.appendChild(wrap);
      setFormEnabled(false);
    }

    function renderSystemMessage(message, isError = false, key = '') {
      if (key) removeSystemMessage(key);

      const element = document.createElement('div');
      element.className = 'chat-widget__system';
      if (key) element.dataset.systemMessage = key;
      if (isError) element.classList.add('is-error');
      element.textContent = message;
      messages.appendChild(element);
      scrollToLatest();
    }

    function removeSystemMessage(key) {
      if (!key) return;
      messages
        .querySelectorAll(`[data-system-message="${key}"]`)
        .forEach(element => element.remove());
    }

    function buildAiHistory() {
      return state.chatLog
        .filter(item => ['Customer', 'Ai', 'Staff'].includes(item.sender) && item.body)
        .slice(-8)
        .map(item => ({
          role: item.sender === 'Customer' ? 'user' : 'assistant',
          message: item.body
        }));
    }

    function upsertChatLog(payload) {
      const id = String(payload.id || '');
      if (id && state.chatLog.some(item => String(item.id) === id)) {
        state.chatLog = state.chatLog.map(item =>
          String(item.id) === id ? { ...item, ...payload } : item);
        return;
      }

      state.chatLog.push({
        id: payload.id,
        sender: payload.sender,
        body: payload.body,
        conversationId: payload.conversationId,
        aiMetadataJson: payload.aiMetadataJson
      });
      state.chatLog = state.chatLog.slice(-40);
    }

    function resolveProducts(payload) {
      const metadata = parseJson(payload.aiMetadataJson);
      return Array.isArray(metadata?.products) ? metadata.products : [];
    }

    function parseJson(value) {
      if (!value || typeof value !== 'string') return null;
      try {
        return JSON.parse(value);
      } catch {
        return null;
      }
    }

    function createProductCards(products) {
      const list = document.createElement('div');
      list.className = 'chat-widget__products';
      list.setAttribute('aria-label', 'Sản phẩm được gợi ý');

      products.forEach(product => {
        if (typeof product?.detailUrl !== 'string' || !product.detailUrl.startsWith('/product/')) return;

        const card = document.createElement('a');
        card.className = 'chat-widget__product-card';
        card.href = product.detailUrl;

        if (product.imageUrl) {
          const image = document.createElement('img');
          image.src = product.imageUrl;
          image.alt = product.name || 'Sản phẩm';
          image.loading = 'lazy';
          card.appendChild(image);
        }

        const content = document.createElement('span');
        content.className = 'chat-widget__product-content';

        if (product.categoryName) {
          const category = document.createElement('small');
          category.textContent = product.categoryName;
          content.appendChild(category);
        }

        const name = document.createElement('strong');
        name.textContent = product.name || 'Sản phẩm';
        content.appendChild(name);

        const footer = document.createElement('span');
        footer.className = 'chat-widget__product-footer';

        const price = document.createElement('b');
        price.textContent = formatPrice(product.price);
        footer.appendChild(price);

        const detail = document.createElement('span');
        detail.className = 'chat-widget__product-link';
        detail.textContent = 'Xem chi tiết';
        footer.appendChild(detail);

        content.appendChild(footer);
        card.appendChild(content);
        list.appendChild(card);
      });

      return list;
    }

    function setOpen(open) {
      if (open) {
        widgets.forEach(widget => {
          if (widget.root !== root) widget.close();
        });
      }

      root.classList.toggle('is-open', open);
      panel.setAttribute('aria-hidden', String(!open));
      toggle.setAttribute('aria-expanded', String(open));
      if (open) {
        dock?.setAttribute('data-open-widget', options.kind);
        updateChannelSwitchState(options.kind);
        if (options.kind === 'support') setSupportUnreadCount(0);
        root.classList.remove('is-greeting');
        scrollToLatest();
        input.focus();
      } else if (dock?.dataset.openWidget === options.kind) {
        dock.removeAttribute('data-open-widget');
        updateChannelSwitchState();
      }
    }

    function showAiGreeting() {
      if (!greetingBubble) return;

      const name = state.displayName && state.displayName.trim()
        ? ` ${state.displayName.trim()}`
        : '';
      greetingBubble.textContent = name
        ? `Xin chào${name}, cần mình tư vấn gì?`
        : 'Xin chào, mình là AI TechStore';

      root.classList.add('is-greeting');
      if (greetingTimer) window.clearTimeout(greetingTimer);
      greetingTimer = window.setTimeout(() => {
        root.classList.remove('is-greeting');
      }, 6500);
    }

    function handleInputKeydown(event) {
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        form.requestSubmit();
      }
    }

    function resizeInput() {
      input.style.height = 'auto';
      input.style.height = `${Math.min(Math.max(input.scrollHeight, 42), 110)}px`;
    }

    function setLoading(isLoading) {
      if (loading) loading.hidden = !isLoading;
      submit.disabled = isLoading || (options.requiresRealtimeToSend && !isRealtimeConnected());
      input.disabled = isLoading || (options.requiresRealtimeToSend && !isRealtimeConnected());
      if (isLoading) scrollToLatest();
    }

    function setFormEnabled(enabled) {
      submit.disabled = !enabled;
      input.disabled = !enabled;
    }

    function setStatus(value) {
      if (status) status.textContent = value;
      if (options.kind === 'support') {
        setSupportPresence(value === options.connectedStatus);
      }
    }

    function isRealtimeConnected() {
      return state.connection &&
        window.signalR &&
        state.connection.state === signalR.HubConnectionState.Connected;
    }

    function scrollToLatest() {
      requestAnimationFrame(() => {
        messages.scrollTop = messages.scrollHeight;
      });
    }

    function createTempId(prefix) {
      return `${prefix}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    }

    function createClientMessageId() {
      if (window.crypto?.randomUUID) {
        return window.crypto.randomUUID();
      }

      return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
    }

    function formatPrice(value) {
      const price = Number(value);
      return Number.isFinite(price)
        ? `${new Intl.NumberFormat('vi-VN').format(price)}đ`
        : 'Liên hệ';
    }

    function buildLoginUrl() {
      const returnUrl = `${window.location.pathname}${window.location.search}`;
      return `/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`;
    }
  }
})();
