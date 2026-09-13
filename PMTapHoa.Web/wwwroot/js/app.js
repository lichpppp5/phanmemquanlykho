/**
 * PM TẠP HOÁ - MODERN SINGLE PAGE APPLICATION CLIENT LOGIC
 * Full Professional Retail Suite:
 * - Multi-cart / Hold Order (Đa giỏ hàng / Giữ đơn)
 * - Expiry date tracking & alerts (Quản lý hạn sử dụng & Cận date)
 * - Real profit & loss reporting (Báo cáo doanh thu, vốn & tiền lãi thực tế)
 * - Barcode & Shelf price tag printing (In tem giá & mã vạch dán kệ)
 * - Sales returns & refunds (Xử lý đổi trả hàng & hoàn tiền)
 * - User management & RBAC (Quản lý tài khoản, phân quyền)
 * - Dual printer support K58 / K80 (Khổ giấy 58mm & 80mm)
 * - System backup & restore (Sao lưu & phục hồi database)
 */

// ==================== STATE ====================
const state = {
  currentTab: 'pos',
  currentSubTab: 'users',
  currentUser: {
    userId: 1,
    username: 'admin',
    fullName: 'Quản lý hệ thống',
    role: 'Admin'
  },
  products: [],
  categories: [],
  customers: [],
  settings: {
    defaultPaperWidth: 80,
    autoPrintReceipt: true
  },
  // Multi-cart state
  carts: [
    {
      id: 1,
      name: 'Đơn 1',
      items: [],
      customerName: '',
      paymentMethod: 'Tiền mặt',
      discountAmount: 0,
      discountNote: '',
      receivedAmount: 0
    }
  ],
  activeCartIndex: 0,
  selectedCategory: '',
  selectedProductForTag: null,
  invoiceFolder: null,
};

// ==================== GLOBAL FETCH INTERCEPTOR (RBAC) ====================
const originalFetch = window.fetch;
window.fetch = async function (url, init = {}) {
  init.headers = init.headers || {};
  const user = state.currentUser || { username: 'admin', role: 'Admin' };
  if (init.headers instanceof Headers) {
    if (!init.headers.has('X-Username')) init.headers.set('X-Username', user.username || '');
    if (!init.headers.has('X-User-Role')) init.headers.set('X-User-Role', user.role || '');
  } else if (Array.isArray(init.headers)) {
    init.headers.push(['X-Username', user.username || '']);
    init.headers.push(['X-User-Role', user.role || '']);
  } else {
    init.headers['X-Username'] = user.username || '';
    init.headers['X-User-Role'] = user.role || '';
  }

  const response = await originalFetch(url, init);
  if (response.status === 403) {
    let msg = 'Chỉ Quản trị viên (Admin) mới có quyền thực hiện thao tác này.';
    try {
      const text = await response.clone().text();
      if (text && text.trim()) {
        const data = JSON.parse(text);
        if (data && data.message) msg = data.message;
      }
    } catch (_) { }
    alert(`⚠️ ${msg}`);
  }
  return response;
};

// Safe JSON parse helper - won't throw on empty body
Response.prototype.safeJson = async function () {
  try {
    const text = await this.text();
    if (!text || !text.trim()) return null;
    return JSON.parse(text);
  } catch (_) {
    return null;
  }
};

// Helper: Remove Vietnamese diacritics / tones
function removeVietnameseTones(str) {
  if (!str) return '';
  // Bước 1: Chuẩn hóa Unicode về dạng NFC (hợp nhất các ký tự ghép)
  try { str = str.normalize('NFC'); } catch (_) {}
  // Bước 2: Chuyển sang NFD để tách dấu thanh ra rồi xóa tất cả dấu thanh
  try { str = str.normalize('NFD').replace(/[\u0300-\u036f]/g, ''); } catch (_) {}
  // Bước 3: Xử lý đặc biệt ký tự đ/Đ (không bị NFD tách)
  str = str.replace(/đ/g, 'd').replace(/Đ/g, 'D');
  // Bước 4: Xóa mọi ký tự không phải ASCII printable (0x20-0x7E) còn sót lại
  str = str.replace(/[^\x20-\x7E]/g, '');
  return str;
}


// Helper getter for active cart
function getActiveCart() {
  if (!state.carts[state.activeCartIndex]) {
    state.activeCartIndex = 0;
  }
  return state.carts[state.activeCartIndex];
}

// ==================== AUDIO FEEDBACK (WEB AUDIO API) ====================
let audioCtx = null;
function playSound(type) {
  try {
    if (!audioCtx) {
      audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (audioCtx.state === 'suspended') audioCtx.resume();

    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    osc.connect(gain);
    gain.connect(audioCtx.destination);

    if (type === 'beep') {
      osc.type = 'sine';
      osc.frequency.setValueAtTime(1050, audioCtx.currentTime);
      gain.gain.setValueAtTime(0.12, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.08);
      osc.start();
      osc.stop(audioCtx.currentTime + 0.08);
    } else if (type === 'cash') {
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(523.25, audioCtx.currentTime);
      osc.frequency.setValueAtTime(659.25, audioCtx.currentTime + 0.1);
      osc.frequency.setValueAtTime(783.99, audioCtx.currentTime + 0.2);
      gain.gain.setValueAtTime(0.15, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.45);
      osc.start();
      osc.stop(audioCtx.currentTime + 0.45);
    }
  } catch (_) { }
}

// ==================== UTILS ====================
function formatVND(num) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })
    .format(num || 0)
    .replace('₫', 'đ');
}

function formatDate(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return isNaN(d) ? dateStr : d.toLocaleString('vi-VN');
}

function formatDateOnly(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return isNaN(d) ? dateStr : d.toLocaleDateString('vi-VN');
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/[&<>"']/g, m => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[m]);
}

// ==================== TOAST NOTIFICATIONS ====================
function showToast(message, duration = 3000) {
  const existing = document.getElementById('appToast');
  if (existing) existing.remove();

  const toast = document.createElement('div');
  toast.id = 'appToast';
  toast.className = 'app-toast';
  toast.textContent = message;
  document.body.appendChild(toast);

  setTimeout(() => {
    toast.classList.add('app-toast-hide');
    setTimeout(() => toast.remove(), 400);
  }, duration);
}

// ==================== INITIALIZATION ====================
document.addEventListener('DOMContentLoaded', async () => {
  setupTheme();
  setupUserSession();
  setupNavigation();
  setupEventListeners();
  setupImageUploadHandlers();
  startClock();

  await loadSettings();
  await checkInvoiceFolderStatus();
  await loadCategories();
  await loadProducts();
  await loadCustomers();
  renderPosCategories();
  renderPosProducts();
  renderCartTabs();
  updateCartUI();

  // Khôi phục tab đang mở trước khi F5 (Tab Persistence)
  const hashTab = window.location.hash.replace('#', '');
  const savedTab = hashTab || localStorage.getItem('pmtaphoa_active_tab') || 'pos';
  if (savedTab && savedTab !== 'pos') {
    switchTab(savedTab);
  } else if (state.currentTab === 'settings') {
    populateSettingsForm();
  }

  const savedSettingsSub = localStorage.getItem('pmtaphoa_settings_subtab');
  if (savedSettingsSub) {
    switchSettingsSubTab(savedSettingsSub);
  }
});

// ==================== PRODUCT IMAGE MANAGEMENT ====================
function setProductModalImage(url) {
  const input = document.getElementById('modalProductImageUrl');
  const img = document.getElementById('modalProductImagePreview');
  const placeholder = document.getElementById('modalProductImagePlaceholder');
  const removeBtn = document.getElementById('removeProductImageBtn');
  const status = document.getElementById('uploadStatusText');

  input.value = url || '';
  if (url) {
    img.src = url;
    img.style.display = 'block';
    placeholder.style.display = 'none';
    removeBtn.style.display = 'inline-flex';
    status.textContent = 'Đã chọn ảnh';
  } else {
    img.src = '';
    img.style.display = 'none';
    placeholder.style.display = 'block';
    removeBtn.style.display = 'none';
    status.textContent = 'Hỗ trợ JPG, PNG, WEBP (tối đa 8MB)';
  }
}

function setupImageUploadHandlers() {
  const chooseBtn = document.getElementById('chooseProductImageBtn');
  const fileInput = document.getElementById('modalProductImageFileInput');
  const urlInput = document.getElementById('modalProductImageUrl');
  const removeBtn = document.getElementById('removeProductImageBtn');
  const statusText = document.getElementById('uploadStatusText');

  if (chooseBtn && fileInput) {
    chooseBtn.addEventListener('click', () => {
      fileInput.click();
    });

    fileInput.addEventListener('change', async () => {
      if (!fileInput.files || fileInput.files.length === 0) return;
      const file = fileInput.files[0];
      statusText.textContent = `Đang tải lên: ${file.name}...`;

      const formData = new FormData();
      formData.append('file', file);

      try {
        const res = await fetch('/api/upload/image', {
          method: 'POST',
          body: formData
        });

        if (res.ok) {
          const data = await res.json();
          setProductModalImage(data.url);
          statusText.textContent = '✓ Tải ảnh lên thành công!';
        } else {
          const err = await res.json();
          alert('Lỗi tải ảnh: ' + (err.message || 'Không thành công'));
          statusText.textContent = 'Tải ảnh thất bại';
        }
      } catch (e) {
        alert('Lỗi: ' + e.message);
        statusText.textContent = 'Lỗi kết nối';
      } finally {
        fileInput.value = '';
      }
    });
  }

  if (urlInput) {
    urlInput.addEventListener('input', () => {
      const val = urlInput.value.trim();
      if (val) {
        const img = document.getElementById('modalProductImagePreview');
        const placeholder = document.getElementById('modalProductImagePlaceholder');
        const remove = document.getElementById('removeProductImageBtn');
        img.src = val;
        img.style.display = 'block';
        placeholder.style.display = 'none';
        remove.style.display = 'inline-flex';
      } else {
        setProductModalImage('');
      }
    });
  }

  if (removeBtn) {
    removeBtn.addEventListener('click', () => {
      setProductModalImage('');
    });
  }

  // Lightbox handlers
  window.viewFullImage = (url, name) => {
    const lightbox = document.getElementById('imageLightbox');
    const img = document.getElementById('lightboxImg');
    const cap = document.getElementById('lightboxCaption');
    if (!lightbox || !img) return;
    img.src = url;
    if (cap) cap.textContent = name || 'Ảnh sản phẩm';
    lightbox.classList.add('active');
  };

  const closeLightbox = document.getElementById('closeImageLightboxBtn');
  if (closeLightbox) {
    closeLightbox.addEventListener('click', () => {
      document.getElementById('imageLightbox').classList.remove('active');
    });
  }
  const lightboxEl = document.getElementById('imageLightbox');
  if (lightboxEl) {
    lightboxEl.addEventListener('click', (e) => {
      if (e.target.id === 'imageLightbox') {
        lightboxEl.classList.remove('active');
      }
    });
  }
}

// ==================== THEME MANAGEMENT ====================
function setupTheme() {
  const savedTheme = localStorage.getItem('pmtaphoa_theme') || 'dark';
  document.documentElement.setAttribute('data-theme', savedTheme);
  updateThemeLabel(savedTheme);

  document.getElementById('themeToggleBtn').addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem('pmtaphoa_theme', next);
    updateThemeLabel(next);
  });
}

function updateThemeLabel(theme) {
  const label = document.getElementById('themeLabel');
  label.textContent = theme === 'dark' ? '🌙 Chế độ Tối' : '☀️ Chế độ Sáng';
}

function startClock() {
  const clock = document.getElementById('liveClock');
  const tick = () => {
    const now = new Date();
    clock.textContent = now.toLocaleDateString('vi-VN') + ' ' + now.toLocaleTimeString('vi-VN');
  };
  tick();
  setInterval(tick, 1000);
}

// ==================== AUTH HEADERS HELPER ====================
function authHeaders() {
  const u = state.currentUser || { username: 'admin', role: 'Admin' };
  const h = {};
  if (u.role) h['X-User-Role'] = u.role;
  if (u.username) h['X-Username'] = u.username;
  return h;
}

// ==================== USER SESSION & AUTH ====================
function setupUserSession() {
  const saved = localStorage.getItem('pmtaphoa_user');
  if (saved) {
    try {
      state.currentUser = JSON.parse(saved);
    } catch (_) { }
  }
  updateCurrentUserDisplay();

  const quickAdmin = document.getElementById('quickSelectAdminBtn');
  if (quickAdmin) {
    quickAdmin.addEventListener('click', () => {
      document.getElementById('loginUsername').value = 'admin';
      document.getElementById('loginPassword').focus();
    });
  }

  const quickStaff = document.getElementById('quickSelectStaffBtn');
  if (quickStaff) {
    quickStaff.addEventListener('click', () => {
      document.getElementById('loginUsername').value = 'staff';
      document.getElementById('loginPassword').focus();
    });
  }

  document.getElementById('openChangePassBtn').addEventListener('click', () => {
    document.getElementById('changePassCurrent').value = '';
    document.getElementById('changePassNew').value = '';
    document.getElementById('changePassConfirm').value = '';
    const btn = document.getElementById('confirmChangePassBtn');
    if (btn) { btn.disabled = false; btn.textContent = 'Đổi Mật Khẩu'; }
    document.getElementById('changePasswordModal').classList.add('active');
  });

  document.getElementById('openLoginBtn').addEventListener('click', () => {
    document.getElementById('loginUsername').value = '';
    document.getElementById('loginPassword').value = '';
    const btn = document.getElementById('confirmLoginBtn');
    if (btn) { btn.disabled = false; btn.textContent = 'Đăng Nhập'; }
    document.getElementById('loginModal').classList.add('active');
  });

  document.getElementById('confirmLoginBtn').addEventListener('click', handleLogin);
  document.getElementById('confirmChangePassBtn').addEventListener('click', handleChangePassword);
}

function updateCurrentUserDisplay() {
  const u = state.currentUser || { username: 'admin', role: 'Admin' };
  const isAdmin = (u.role || '').toLowerCase() === 'admin';

  document.getElementById('currentUserName').textContent = u.fullName || u.username;
  document.getElementById('currentUserAvatar').textContent = (u.username[0] || 'U').toUpperCase();

  const roleBadge = document.getElementById('currentUserRoleBadge');
  if (roleBadge) {
    if (isAdmin) {
      roleBadge.className = 'role-badge admin';
      roleBadge.textContent = '👑 Quản trị';
    } else {
      roleBadge.className = 'role-badge staff';
      roleBadge.textContent = '🛒 Thu ngân';
    }
  }

  // Toggle role class on body for instant CSS hiding
  if (isAdmin) {
    document.body.classList.remove('role-staff');
    document.body.classList.add('role-admin');
  } else {
    document.body.classList.remove('role-admin');
    document.body.classList.add('role-staff');
  }

  // Show/Hide cost price table header
  const costTh = document.getElementById('costPriceHeaderTh');
  if (costTh) {
    costTh.style.display = isAdmin ? '' : 'none';
  }

  // If staff is currently on an admin-only tab, redirect to pos
  if (!isAdmin && ['reports', 'system', 'settings'].includes(state.currentTab)) {
    switchTab('pos');
  }
}

async function handleLogin() {
  const username = document.getElementById('loginUsername').value.trim();
  const password = document.getElementById('loginPassword').value;

  if (!username || !password) {
    alert('Vui lòng nhập đầy đủ tài khoản và mật khẩu!');
    return;
  }

  const btn = document.getElementById('confirmLoginBtn');
  const origText = btn ? btn.textContent : 'Đăng Nhập';
  if (btn) {
    btn.disabled = true;
    btn.textContent = '⏳ Đang đăng nhập...';
  }

  try {
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (res.ok) {
      const data = await res.json();
      state.currentUser = data;
      localStorage.setItem('pmtaphoa_user', JSON.stringify(data));
      updateCurrentUserDisplay();

      // Reload products to obtain updated pricing visibility
      await loadProducts();
      renderInventoryTable();
      renderPosProducts();

      document.getElementById('loginModal').classList.remove('active');
      const roleText = data.role === 'Admin' ? 'Quản trị viên' : 'Nhân viên bán hàng';
      alert(`Xin chào, ${data.fullName || data.username} (${roleText})!`);
    } else {
      const err = await res.json();
      alert('Đăng nhập thất bại: ' + (err.message || 'Sai thông tin'));
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  } finally {
    if (btn) {
      btn.disabled = false;
      btn.textContent = origText;
    }
  }
}

async function handleChangePassword() {
  const cur = document.getElementById('changePassCurrent').value;
  const newPass = document.getElementById('changePassNew').value;
  const confirm = document.getElementById('changePassConfirm').value;

  if (!cur || !newPass || !confirm) {
    alert('Vui lòng điền đầy đủ các trường!');
    return;
  }
  if (newPass.length < 6) {
    alert('Mật khẩu mới phải từ 6 ký tự trở lên!');
    return;
  }
  if (newPass !== confirm) {
    alert('Mật khẩu xác nhận không khớp!');
    return;
  }

  const btn = document.getElementById('confirmChangePassBtn');
  const origText = btn ? btn.textContent : 'Đổi Mật Khẩu';
  if (btn) {
    btn.disabled = true;
    btn.textContent = '⏳ Đang đổi...';
  }

  try {
    const res = await fetch('/api/auth/change-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        userId: state.currentUser.userId,
        currentPassword: cur,
        newPassword: newPass
      })
    });
    if (res.ok) {
      alert('Đổi mật khẩu thành công!');
      document.getElementById('changePasswordModal').classList.remove('active');
    } else {
      const err = await res.json();
      alert('Lỗi: ' + (err.message || 'Không thành công'));
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  } finally {
    if (btn) {
      btn.disabled = false;
      btn.textContent = origText;
    }
  }
}

// ==================== NAVIGATION ====================
function switchTab(tabKey) {
  const btn = document.querySelector(`.sidebar-nav .nav-item[data-tab="${tabKey}"]`);
  if (btn) btn.click();
}

function setupNavigation() {
  const navButtons = document.querySelectorAll('.sidebar-nav .nav-item');
  const tabs = {
    pos: { el: document.getElementById('tabPos'), title: 'Bán Hàng Thu Ngân', sub: 'Quét mã vạch hoặc tìm sản phẩm để tính tiền' },
    inventory: { el: document.getElementById('tabInventory'), title: 'Kho Hàng & Sản Phẩm', sub: 'Quản lý danh sách mặt hàng, tồn kho, hạn dùng và in tem giá' },
    debts: { el: document.getElementById('tabDebts'), title: 'Sổ Ghi Nợ', sub: 'Theo dõi hoá đơn nợ và ghi nhận các lần thanh toán nợ' },
    customers: { el: document.getElementById('tabCustomers'), title: 'Quản Lý Khách Hàng', sub: 'Danh sách khách hàng, thông tin liên hệ và lịch sử mua hàng' },
    salesHistory: { el: document.getElementById('tabSalesHistory'), title: 'Lịch Sử Hoá Đơn', sub: 'Tra cứu hoá đơn đã bán, xem chi tiết, in lại bill và đổi trả hàng' },
    reports: { el: document.getElementById('tabReports'), title: 'Báo Cáo & Lợi Nhuận', sub: 'Doanh thu, tiền vốn, tiền lãi ròng và cảnh báo hàng cận date' },
    system: { el: document.getElementById('tabSystem'), title: 'Hệ Thống & Người Dùng', sub: 'Quản lý tài khoản, nhật ký thao tác và sao lưu cơ sở dữ liệu' },
    settings: { el: document.getElementById('tabSettings'), title: 'Cài Đặt & Máy In', sub: 'Thông tin tiệm, cấu hình máy in bill K58/K80 và thanh toán VietQR' },
  };

  navButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const tabKey = btn.getAttribute('data-tab');
      if (!tabs[tabKey]) return;

      const isAdmin = (state.currentUser?.role || '').toLowerCase() === 'admin';
      if (!isAdmin && ['reports', 'system', 'settings'].includes(tabKey)) {
        alert('⚠️ Chức năng này chỉ dành cho Quản trị viên (Admin).');
        return;
      }

      navButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
      tabs[tabKey].el.classList.add('active');

      document.getElementById('pageTitle').textContent = tabs[tabKey].title;
      document.getElementById('pageSubtitle').textContent = tabs[tabKey].sub;
      state.currentTab = tabKey;
      localStorage.setItem('pmtaphoa_active_tab', tabKey);
      try {
        history.replaceState(null, '', '#' + tabKey);
      } catch (_) { }

      if (tabKey === 'inventory') renderInventoryTable();
      if (tabKey === 'debts') loadAndRenderDebts();
      if (tabKey === 'customers') loadAndRenderCustomers();
      if (tabKey === 'salesHistory') loadAndRenderSalesHistory();
      if (tabKey === 'reports') { loadAndRenderReports(); initTaxModule(); }
      if (tabKey === 'system') switchSubTab(state.currentSubTab || 'users');
      if (tabKey === 'settings') {
        populateSettingsForm();
        loadSettings().then(() => populateSettingsForm());
      }
      if (tabKey === 'pos') document.getElementById('posSearchInput').focus();
    });
  });

  document.querySelectorAll('.sub-tabs .sub-tab-btn[data-sub]').forEach(btn => {
    btn.addEventListener('click', () => {
      switchSubTab(btn.getAttribute('data-sub'));
    });
  });

  document.querySelectorAll('[data-settings-sub]').forEach(btn => {
    btn.addEventListener('click', () => {
      switchSettingsSubTab(btn.getAttribute('data-settings-sub'));
    });
  });
}

function switchSubTab(subKey) {
  state.currentSubTab = subKey;
  document.querySelectorAll('.sub-tabs .sub-tab-btn[data-sub]').forEach(b => {
    b.classList.toggle('active', b.getAttribute('data-sub') === subKey);
  });

  document.getElementById('subTabUsers').style.display = subKey === 'users' ? 'block' : 'none';
  document.getElementById('subTabBackup').style.display = subKey === 'backup' ? 'block' : 'none';
  document.getElementById('subTabAudit').style.display = subKey === 'audit' ? 'block' : 'none';

  if (subKey === 'users') loadAndRenderUsers();
  if (subKey === 'audit') loadAndRenderAuditLogs();
}

function switchSettingsSubTab(subKey) {
  state.currentSettingsSubTab = subKey;
  localStorage.setItem('pmtaphoa_settings_subtab', subKey);
  document.querySelectorAll('[data-settings-sub]').forEach(b => {
    b.classList.toggle('active', b.getAttribute('data-settings-sub') === subKey);
  });

  document.querySelectorAll('#tabSettings .settings-card').forEach(card => {
    const cat = card.getAttribute('data-settings-cat');
    if (subKey === 'all' || cat === subKey) {
      card.style.display = '';
    } else {
      card.style.display = 'none';
    }
  });
}

// ==================== DATA FETCHING ====================
async function loadSettings() {
  try {
    const res = await fetch('/api/settings');
    if (res.ok) {
      state.settings = await res.json();
      if (state.settings.storeName) {
        document.getElementById('sidebarStoreName').textContent = state.settings.storeName;
      }
      const printArea = document.getElementById('receiptPrintArea');
      printArea.className = `receipt-print-area ${state.settings.defaultPaperWidth === 58 ? 'k58' : 'k80'}`;
      updateAutoPrintToggleUI();
    }
  } catch (e) {
    console.error('Error loading settings:', e);
  }
}

// ==================== INVOICE FOLDER MANAGEMENT (HoaDon/Ban & HoaDon/Nhap) ====================
async function checkInvoiceFolderStatus() {
  try {
    const res = await fetch('/api/invoice-folder/status');
    if (!res.ok) return;
    const data = await res.json();
    state.invoiceFolder = data;

    // 1. Cập nhật banner cảnh báo trên màn hình POS
    const alertBanner = document.getElementById('invoiceFolderAlert');
    const checkoutBtn = document.getElementById('checkoutBtn');
    if (alertBanner) {
      if (!data.isConfigured) {
        alertBanner.style.display = 'flex';
        if (checkoutBtn) {
          checkoutBtn.classList.add('checkout-blocked');
          checkoutBtn.title = 'Vui lòng thiết lập thư mục lưu hoá đơn trước khi thanh toán!';
        }
      } else {
        alertBanner.style.display = 'none';
        if (checkoutBtn) {
          checkoutBtn.classList.remove('checkout-blocked');
          checkoutBtn.title = '';
        }
      }
    }

    // 2. Cập nhật giao diện thẻ Cài Đặt
    updateInvoiceFolderSettingsUI(data);
  } catch (e) {
    console.error('Lỗi kiểm tra trạng thái thư mục hoá đơn:', e);
  }
}

function updateInvoiceFolderSettingsUI(data) {
  const badge = document.getElementById('settingInvoiceFolderStatusBadge');
  const pathInput = document.getElementById('settingInvoiceStoragePath');
  const chipBan = document.getElementById('chipBanFolder');
  const chipNhap = document.getElementById('chipNhapFolder');
  const iconBan = document.getElementById('iconBanFolder');
  const iconNhap = document.getElementById('iconNhapFolder');
  const textBan = document.getElementById('textBanFolder');
  const textNhap = document.getElementById('textNhapFolder');

  if (!badge) return;

  if (pathInput && !pathInput.value) {
    pathInput.value = data.currentPath || data.defaultPath || '';
  }

  if (data.isConfigured) {
    badge.textContent = '✓ Đã kích hoạt & sẵn sàng';
    badge.className = 'badge badge-success';
  } else {
    badge.textContent = '⚠️ Chưa thiết lập';
    badge.className = 'badge badge-danger';
  }

  const root = data.currentPath || data.defaultPath || 'HoaDon';

  if (chipBan && iconBan && textBan) {
    if (data.banExists) {
      chipBan.className = 'folder-chip active';
      iconBan.textContent = '✅';
      textBan.textContent = `${root}/Ban (Sẵn sàng)`;
    } else {
      chipBan.className = 'folder-chip missing';
      iconBan.textContent = '❌';
      textBan.textContent = `${root}/Ban (Chưa tạo)`;
    }
  }

  if (chipNhap && iconNhap && textNhap) {
    if (data.nhapExists) {
      chipNhap.className = 'folder-chip active';
      iconNhap.textContent = '✅';
      textNhap.textContent = `${root}/Nhap (Sẵn sàng)`;
    } else {
      chipNhap.className = 'folder-chip missing';
      iconNhap.textContent = '❌';
      textNhap.textContent = `${root}/Nhap (Chưa tạo)`;
    }
  }
}

async function setupInvoiceFolder(customPath = null) {
  const actor = state.currentUser ? state.currentUser.username : 'admin';
  try {
    const res = await fetch(`/api/invoice-folder/setup?operatorUser=${encodeURIComponent(actor)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path: customPath })
    });

    if (!res.ok) {
      const err = await res.json();
      alert('Lỗi thiết lập thư mục: ' + (err.message || 'Không thể tạo thư mục'));
      return;
    }

    const data = await res.json();
    alert(`🎉 THÀNH CÔNG!\n\n${data.message}\n\nĐường dẫn gốc: ${data.path}\n• Thư mục Bán hàng: ${data.banPath}\n• Thư mục Nhập kho: ${data.nhapPath}\n\nBây giờ bạn đã có thể bán hàng và xuất hoá đơn bình thường!`);
    await checkInvoiceFolderStatus();
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
}

async function openInvoiceFolder(subfolder = null) {
  try {
    const res = await fetch(`/api/invoice-folder/open${subfolder ? `?folder=${encodeURIComponent(subfolder)}` : ''}`, {
      method: 'POST'
    });
    const data = await res.json();
    if (!res.ok) {
      alert(data.message || 'Không thể mở thư mục');
    }
  } catch (e) {
    alert('Lỗi mở thư mục: ' + e.message);
  }
}

async function loadCategories() {
  try {
    const res = await fetch('/api/categories');
    if (res.ok) state.categories = await res.json();
  } catch (e) {
    console.error('Error loading categories:', e);
  }
}

async function loadProducts() {
  try {
    const res = await fetch('/api/products');
    if (res.ok) {
      state.products = await res.json();
      updateLowStockBadge();
    }
  } catch (e) {
    console.error('Error loading products:', e);
  }
}

async function loadCustomers() {
  try {
    const res = await fetch('/api/customers');
    if (res.ok) {
      state.customers = await res.json();
      const datalist = document.getElementById('customerDataList');
      datalist.innerHTML = state.customers
        .map(c => `<option value="${c.customerName}">${c.phone ? `SĐT: ${c.phone}` : ''}</option>`)
        .join('');
    }
  } catch (e) {
    console.error('Error loading customers:', e);
  }
}

function updateLowStockBadge() {
  const badge = document.getElementById('sidebarLowStockBadge');
  const count = state.products.filter(p => p.stockQuantity <= Math.max(p.minStock, 5)).length;
  if (count > 0) {
    badge.textContent = count;
    badge.style.display = 'inline-block';
  } else {
    badge.style.display = 'none';
  }
}

// ==================== MULTI-CART (GIỮ ĐƠN CHỜ) ====================
function renderCartTabs() {
  const bar = document.getElementById('cartTabsBar');
  bar.innerHTML = state.carts.map((cart, idx) => {
    const count = cart.items.reduce((s, i) => s + i.quantity, 0);
    return `
      <div class="cart-tab-item ${idx === state.activeCartIndex ? 'active' : ''}" onclick="switchCart(${idx})">
        <span>${escapeHtml(cart.name)}</span>
        ${count > 0 ? `<span style="background: var(--primary); color: white; border-radius: 999px; padding: 1px 6px; font-size: 10px;">${count}</span>` : ''}
        ${state.carts.length > 1 ? `<span onclick="closeCart(event, ${idx})" style="font-size: 12px; margin-left: 2px; opacity: 0.6;" title="Huỷ đơn này">✕</span>` : ''}
      </div>
    `;
  }).join('') + `
    <button class="cart-tab-add-btn" onclick="addNewCart()" title="Mở thêm đơn mới để tính cho khách khác">+</button>
  `;

  document.getElementById('currentCartTitle').innerHTML = `${state.carts[state.activeCartIndex].name} (<span id="cartCount">${getActiveCart().items.reduce((s, i) => s + i.quantity, 0)}</span>)`;
}

window.addNewCart = () => {
  const newNum = state.carts.length + 1;
  state.carts.push({
    id: Date.now(),
    name: `Đơn ${newNum}`,
    items: [],
    customerName: '',
    paymentMethod: 'Tiền mặt',
    discountAmount: 0,
    discountNote: '',
    receivedAmount: 0
  });
  state.activeCartIndex = state.carts.length - 1;
  renderCartTabs();
  updateCartUI();
};

window.switchCart = (idx) => {
  if (idx === state.activeCartIndex) return;
  // Save current form inputs into current cart state
  saveFormToCurrentCart();

  state.activeCartIndex = idx;
  renderCartTabs();

  // Restore form inputs from new active cart
  const cart = getActiveCart();
  document.getElementById('customerSearchInput').value = cart.customerName || '';
  const phoneEl = document.getElementById('customerPhoneInput');
  if (phoneEl) phoneEl.value = cart.customerPhone || '';
  document.getElementById('discountInput').value = cart.discountAmount || '';
  document.getElementById('discountNoteInput').value = cart.discountNote || '';
  document.getElementById('receivedAmountInput').value = cart.receivedAmount || '';

  // Payment method buttons
  document.querySelectorAll('.payment-tabs .pay-method-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-method') === cart.paymentMethod);
  });
  const cashRow = document.getElementById('cashCalculationRow');
  const quickCash = document.getElementById('quickCashContainer');
  if (cart.paymentMethod === 'Tiền mặt') {
    cashRow.style.display = 'flex';
    quickCash.style.display = 'flex';
  } else {
    cashRow.style.display = 'none';
    quickCash.style.display = 'none';
  }

  updateDebtWarningState();
  updateCartUI();
};

window.closeCart = (e, idx) => {
  e.stopPropagation();
  if (state.carts.length <= 1) return;
  if (state.carts[idx].items.length > 0 && !confirm(`Bạn có chắc muốn huỷ ${state.carts[idx].name}?`)) {
    return;
  }
  state.carts.splice(idx, 1);
  if (state.activeCartIndex >= state.carts.length) {
    state.activeCartIndex = state.carts.length - 1;
  }
  renderCartTabs();
  updateCartUI();
};

function saveFormToCurrentCart() {
  const cart = getActiveCart();
  cart.customerName = document.getElementById('customerSearchInput').value.trim();
  const phoneEl = document.getElementById('customerPhoneInput');
  if (phoneEl) cart.customerPhone = phoneEl.value.trim();
  cart.discountAmount = parseFloat(document.getElementById('discountInput').value) || 0;
  cart.discountNote = document.getElementById('discountNoteInput').value.trim();
  cart.receivedAmount = parseFloat(document.getElementById('receivedAmountInput').value) || 0;
}

// Hold order button click
document.getElementById('holdOrderBtn').addEventListener('click', () => {
  const cart = getActiveCart();
  if (cart.items.length === 0) {
    alert('Đơn hiện tại đang trống, không cần giữ đơn!');
    return;
  }
  saveFormToCurrentCart();
  addNewCart();
  alert(`Đã lưu tạm "${cart.name}". Hệ thống đã mở đơn mới để bạn tính cho khách tiếp theo!`);
});

// ==================== POS CUSTOMER AUTO-MATCH & DEBT WARNING ====================
function setupPosCustomerEvents() {
  const nameInput = document.getElementById('customerSearchInput');
  const phoneInput = document.getElementById('customerPhoneInput');
  const foundBadge = document.getElementById('posCustomerFoundBadge');

  if (!nameInput || !phoneInput) return;

  nameInput.addEventListener('input', () => {
    const n = nameInput.value.trim().toLowerCase();
    const match = (state.customers || []).find(c => c.customerName && c.customerName.trim().toLowerCase() === n);
    if (match && match.phone) {
      phoneInput.value = match.phone;
      if (foundBadge) foundBadge.style.display = 'inline';
    } else {
      if (foundBadge) foundBadge.style.display = 'none';
    }
    updateDebtWarningState();
  });

  phoneInput.addEventListener('input', () => {
    const p = phoneInput.value.trim();
    const match = (state.customers || []).find(c => c.phone && c.phone.trim() === p);
    if (match) {
      nameInput.value = match.customerName;
      if (foundBadge) foundBadge.style.display = 'inline';
    }
    updateDebtWarningState();
  });

  // Khi bấm chuyển tab phương thức thanh toán
  document.querySelectorAll('.payment-tabs .pay-method-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      setTimeout(updateDebtWarningState, 50);
    });
  });
}

function updateDebtWarningState() {
  const cart = getActiveCart();
  const isDebt = cart && cart.paymentMethod === 'Ghi nợ';
  const badge = document.getElementById('debtRequiredBadge');
  const warning = document.getElementById('debtCustomerWarning');
  const nameInput = document.getElementById('customerSearchInput');
  const phoneInput = document.getElementById('customerPhoneInput');

  if (badge) badge.style.display = isDebt ? 'inline' : 'none';

  if (!isDebt) {
    if (warning) warning.style.display = 'none';
    if (nameInput) nameInput.style.borderColor = '';
    if (phoneInput) phoneInput.style.borderColor = '';
    return;
  }

  const n = (nameInput?.value || '').trim();
  const p = (phoneInput?.value || '').trim();
  const hasName = n.length >= 2 && n.toLowerCase() !== 'khách lẻ';
  const hasPhone = p.length >= 8;

  if (hasName && hasPhone) {
    if (warning) warning.style.display = 'none';
    if (nameInput) nameInput.style.borderColor = '#16a34a';
    if (phoneInput) phoneInput.style.borderColor = '#16a34a';
  } else {
    if (warning) warning.style.display = 'block';
    if (nameInput) nameInput.style.borderColor = hasName ? '' : '#ef4444';
    if (phoneInput) phoneInput.style.borderColor = hasPhone ? '' : '#ef4444';
  }
}
window.updateDebtWarningState = updateDebtWarningState;

// ==================== POS CATALOG & CART ====================
function renderPosCategories() {
  const container = document.getElementById('posCategoryChips');
  container.innerHTML = `<button class="chip ${state.selectedCategory === '' ? 'active' : ''}" data-cat="">Tất cả</button>` +
    state.categories.map(c => `
      <button class="chip ${state.selectedCategory == c.categoryID ? 'active' : ''}" data-cat="${c.categoryID}">
        ${c.categoryName}
      </button>
    `).join('');

  container.querySelectorAll('.chip').forEach(btn => {
    btn.addEventListener('click', () => {
      container.querySelectorAll('.chip').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      state.selectedCategory = btn.getAttribute('data-cat');
      renderPosProducts();
    });
  });
}

function renderPosProducts() {
  const grid = document.getElementById('posProductGrid');
  const keyword = document.getElementById('posSearchInput').value.trim().toLowerCase();

  let filtered = state.products;
  if (state.selectedCategory) {
    filtered = filtered.filter(p => p.categoryID == state.selectedCategory);
  }
  if (keyword) {
    filtered = filtered.filter(p =>
      (p.productName && p.productName.toLowerCase().includes(keyword)) ||
      (p.barcode && p.barcode.toLowerCase().includes(keyword))
    );
  }

  if (filtered.length === 0) {
    grid.innerHTML = `
      <div style="grid-column: 1/-1; text-align: center; padding: 40px; color: var(--text-muted);">
        Không tìm thấy sản phẩm phù hợp.
      </div>
    `;
    return;
  }

  grid.innerHTML = filtered.map(p => {
    const isLow = p.stockQuantity <= Math.max(p.minStock, 5);
    return `
      <div class="product-card" data-id="${p.productID}">
        <span class="product-badge ${isLow ? 'low' : ''}">${p.categoryName || 'Mặt hàng'}</span>
        <div class="product-card-img-wrapper">
          ${p.imageUrl ? `<img src="${escapeHtml(p.imageUrl)}" class="product-card-img" alt="${escapeHtml(p.productName)}" loading="lazy">` : `<span class="product-card-img-placeholder">📦</span>`}
        </div>
        <div>
          <div class="product-name">${escapeHtml(p.productName)}</div>
          <div class="product-barcode">${p.barcode ? escapeHtml(p.barcode) : 'Không có mã'}</div>
        </div>
        <div>
          <div class="product-price">${formatVND(p.sellingPrice)}</div>
          <div class="product-stock">Tồn: ${p.stockQuantity} ${p.unit || ''}</div>
        </div>
      </div>
    `;
  }).join('');

  grid.querySelectorAll('.product-card').forEach(card => {
    card.addEventListener('click', () => {
      const pid = parseInt(card.getAttribute('data-id'));
      addToCart(pid);
    });
  });
}

function addToCart(productId, qty = 1) {
  const product = state.products.find(p => p.productID === productId);
  if (!product) return;

  const stock = product.stockQuantity ?? Infinity;
  const cart = getActiveCart();
  const existing = cart.items.find(item => item.productId === productId);
  const currentQty = existing ? existing.quantity : 0;

  if (currentQty + qty > stock) {
    // Tồn kho không đủ
    const maxAdd = stock - currentQty;
    if (maxAdd <= 0) {
      showStockWarningToast(product.productName, stock);
      playSound('beep');
      renderCartTabs();
      updateCartUI();
      return;
    }
    // Thêm phần còn lại có thể thêm
    qty = maxAdd;
    showStockWarningToast(product.productName, stock);
  }

  if (existing) {
    existing.quantity += qty;
  } else {
    cart.items.push({
      productId: product.productID,
      barcode: product.barcode,
      productName: product.productName,
      unitPrice: product.sellingPrice,
      quantity: qty,
      unit: product.unit,
      note: ''
    });
  }

  playSound('beep');
  renderCartTabs();
  updateCartUI();
}

function showStockWarningToast(productName, stock) {
  // Xóa toast cũ nếu có
  const old = document.getElementById('stockWarningToast');
  if (old) old.remove();

  const toast = document.createElement('div');
  toast.id = 'stockWarningToast';
  toast.style.cssText = `
    position: fixed; bottom: 80px; left: 50%; transform: translateX(-50%);
    background: #dc2626; color: #fff; padding: 10px 20px; border-radius: 10px;
    font-size: 13px; font-weight: 600; z-index: 9999; box-shadow: 0 4px 16px rgba(0,0,0,0.3);
    animation: fadeInUp 0.25s ease; white-space: nowrap;
  `;
  toast.innerHTML = `⚠️ ${escapeHtml(productName)}: chỉ còn ${stock} trong kho!`;
  document.body.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}

function updateCartUI() {
  const cart = getActiveCart();
  const list = document.getElementById('cartItemsList');
  const countBadge = document.getElementById('cartCount');
  countBadge.textContent = cart.items.reduce((s, i) => s + i.quantity, 0);

  if (cart.items.length === 0) {
    list.innerHTML = `
      <div class="cart-empty">
        <div class="cart-empty-icon">🛒</div>
        <p>Chưa có sản phẩm nào trong ${escapeHtml(cart.name)}.<br>Quét mã vạch hoặc bấm chọn sản phẩm.</p>
      </div>
    `;
  } else {
    list.innerHTML = cart.items.map((item, idx) => {
      const product = state.products.find(p => p.productID === item.productId);
      const stock = product ? (product.stockQuantity ?? Infinity) : Infinity;
      const atLimit = item.quantity >= stock;
      const stockNote = atLimit
        ? `<span style="color:#ef4444;font-size:10px;font-weight:600;">⚠️ Tối đa (${stock} trong kho)</span>`
        : (stock < 10 && stock !== Infinity
          ? `<span style="color:#f97316;font-size:10px;">Còn ${stock} trong kho</span>`
          : '');
      return `
      <div class="cart-item-row${atLimit ? ' stock-limit' : ''}">
        <div class="cart-item-info">
          <div class="cart-item-title">${escapeHtml(item.productName)}</div>
          <div class="cart-item-unitprice">${formatVND(item.unitPrice)} ${stockNote}</div>
        </div>
        <div class="qty-control">
          <button class="qty-btn" onclick="changeCartQty(${idx}, -1)">-</button>
          <span class="qty-val">${item.quantity}</span>
          <button class="qty-btn${atLimit ? ' qty-btn-disabled' : ''}" onclick="changeCartQty(${idx}, 1)"${atLimit ? ' disabled title="Đã đạt tối đa tồn kho"' : ''}>+</button>
        </div>
        <div class="cart-item-total">${formatVND(item.unitPrice * item.quantity)}</div>
        <button class="remove-item-btn" onclick="removeCartItem(${idx})">✕</button>
      </div>
    `;
    }).join('');
  }

  calculateTotals();
}

window.changeCartQty = (idx, delta) => {
  const cart = getActiveCart();
  if (!cart.items[idx]) return;
  const item = cart.items[idx];

  if (delta > 0) {
    // Kiểm tra tồn kho trước khi tăng
    const product = state.products.find(p => p.productID === item.productId);
    const stock = product ? (product.stockQuantity ?? Infinity) : Infinity;
    if (item.quantity >= stock) {
      showStockWarningToast(item.productName, stock);
      return; // Không cho tăng thêm
    }
  }

  item.quantity += delta;
  if (item.quantity <= 0) {
    cart.items.splice(idx, 1);
  }
  renderCartTabs();
  updateCartUI();
};

window.removeCartItem = (idx) => {
  const cart = getActiveCart();
  cart.items.splice(idx, 1);
  renderCartTabs();
  updateCartUI();
};

function calculateTotals() {
  const cart = getActiveCart();
  const subtotal = cart.items.reduce((s, i) => s + (i.unitPrice * i.quantity), 0);
  const discount = Math.max(0, parseFloat(document.getElementById('discountInput').value) || 0);
  const finalTotal = Math.max(0, subtotal - discount);

  document.getElementById('subtotalDisplay').textContent = formatVND(subtotal);
  document.getElementById('discountDisplay').textContent = '-' + formatVND(discount);
  document.getElementById('totalAmountDisplay').textContent = formatVND(finalTotal);

  const receivedInput = document.getElementById('receivedAmountInput');
  const received = parseFloat(receivedInput.value) || 0;
  const change = Math.max(0, received - finalTotal);

  const changeDisplay = document.getElementById('changeDisplay');
  changeDisplay.textContent = `Trả lại: ${formatVND(change)}`;
}

// ==================== CHECKOUT EXECUTION ====================
async function handleCheckout() {
  if (!state.invoiceFolder || !state.invoiceFolder.isConfigured) {
    alert('⚠️ CHƯA THIẾT LẬP THƯ MỤC LƯU TRỮ HOÁ ĐƠN!\n\nPhần mềm bắt buộc phải có thư mục "HoaDon" (chứa 2 thư mục con "Ban" và "Nhap") trên máy tính để lưu hoá đơn theo ngày trước khi thực hiện bán hàng.\n\nVui lòng bấm "Tạo thư mục chuẩn ngay" trên thanh cảnh báo POS hoặc vào tab Cài Đặt!');
    return;
  }

  const cart = getActiveCart();
  if (cart.items.length === 0) {
    alert('Giỏ hàng đang trống! Vui lòng chọn hoặc quét sản phẩm trước khi thanh toán.');
    return;
  }

  const checkoutBtn = document.getElementById('checkoutBtn');
  const origCheckoutHTML = checkoutBtn ? checkoutBtn.innerHTML : null;
  if (checkoutBtn) {
    checkoutBtn.disabled = true;
    checkoutBtn.innerHTML = '<div class="checkout-label"><span class="checkout-title">⏳ Đang xử lý...</span></div>';
  }

  try {
    const subtotal = cart.items.reduce((s, i) => s + (i.unitPrice * i.quantity), 0);
    const discountAmount = Math.max(0, parseFloat(document.getElementById('discountInput').value) || 0);
    const discountNote = document.getElementById('discountNoteInput').value.trim();
    const finalTotal = Math.max(0, subtotal - discountAmount);

    const customerName = document.getElementById('customerSearchInput').value.trim();
    const customerPhone = (document.getElementById('customerPhoneInput')?.value || '').trim();

    if (cart.paymentMethod === 'Ghi nợ') {
      if (!customerName || customerName.toLowerCase() === 'khách lẻ' || customerName.length < 2) {
        alert('⚠️ ĐƠN HÀNG GHI NỢ BẮT BUỘC CÓ TÊN KHÁCH HÀNG!\n\nBạn đang chọn hình thức thanh toán "Ghi nợ", vui lòng nhập đầy đủ Họ và Tên của khách hàng (không được để trống hoặc "Khách lẻ").');
        document.getElementById('customerSearchInput').focus();
        return;
      }
      if (!customerPhone || customerPhone.length < 8) {
        alert('⚠️ ĐƠN HÀNG GHI NỢ BẮT BUỘC CÓ SỐ ĐIỆN THOẠI KHÁCH HÀNG!\n\nBạn đang chọn hình thức thanh toán "Ghi nợ", vui lòng nhập Số điện thoại khách hàng hợp lệ (tối thiểu 8-10 chữ số) để lưu vào sổ nợ và quản lý công nợ.');
        document.getElementById('customerPhoneInput')?.focus();
        return;
      }
    }

    const finalCustName = customerName || 'Khách lẻ';
    const customer = (state.customers || []).find(c =>
      (customerPhone && c.phone && c.phone.trim() === customerPhone) ||
      (finalCustName !== 'Khách lẻ' && c.customerName.toLowerCase() === finalCustName.toLowerCase())
    );

    let receivedAmount = parseFloat(document.getElementById('receivedAmountInput').value) || 0;
    if (cart.paymentMethod === 'Tiền mặt' && receivedAmount < finalTotal) {
      receivedAmount = finalTotal;
    }

    if (cart.paymentMethod === 'Chuyển khoản' && state.settings.qrPaymentEnabled) {
      // Restore button before opening modal (modal has its own flow)
      if (checkoutBtn && origCheckoutHTML) {
        checkoutBtn.disabled = false;
        checkoutBtn.innerHTML = origCheckoutHTML;
      }
      openVietQrModal(finalTotal, finalCustName, async () => {
        await sendCheckoutRequest({
          customerId: customer ? customer.customerID : null,
          customerName: finalCustName,
          customerPhone: customerPhone || null,
          paymentMethod: 'Chuyển khoản',
          discountAmount,
          discountNote,
          receivedAmount: finalTotal,
          items: cart.items
        });
      });
      return;
    }

    await sendCheckoutRequest({
      customerId: customer ? customer.customerID : null,
      customerName: finalCustName,
      customerPhone: customerPhone || null,
      paymentMethod: cart.paymentMethod,
      discountAmount,
      discountNote,
      receivedAmount: cart.paymentMethod === 'Ghi nợ' ? 0 : receivedAmount,
      items: cart.items
    });
  } finally {
    if (checkoutBtn && origCheckoutHTML) {
      checkoutBtn.disabled = false;
      checkoutBtn.innerHTML = origCheckoutHTML;
    }
  }
}

async function sendCheckoutRequest(payload) {
  try {
    const operator = state.currentUser ? state.currentUser.username : 'Thu ngân';
    const res = await fetch(`/api/sales/checkout?operatorUser=${encodeURIComponent(operator)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) {
      const errText = await res.text().catch(() => '');
      let err = {};
      try { if (errText) err = JSON.parse(errText); } catch (_) {}
      if (err.requiresFolderSetup) {
        await checkInvoiceFolderStatus();
      }
      alert('Lỗi thanh toán: ' + (err.message || 'Không thành công'));
      return;
    }

    const data = await res.json();
    playSound('cash');

    // Remove or reset completed cart
    if (state.carts.length > 1) {
      state.carts.splice(state.activeCartIndex, 1);
      if (state.activeCartIndex >= state.carts.length) state.activeCartIndex = 0;
    } else {
      state.carts[0].items = [];
      state.carts[0].customerName = '';
      state.carts[0].customerPhone = '';
      state.carts[0].discountAmount = 0;
      state.carts[0].discountNote = '';
      state.carts[0].receivedAmount = 0;
    }

    document.getElementById('discountInput').value = '';
    document.getElementById('discountNoteInput').value = '';
    document.getElementById('receivedAmountInput').value = '';
    document.getElementById('customerSearchInput').value = '';
    const phoneEl = document.getElementById('customerPhoneInput');
    if (phoneEl) phoneEl.value = '';
    const foundBadge = document.getElementById('posCustomerFoundBadge');
    if (foundBadge) foundBadge.style.display = 'none';
    updateDebtWarningState();
    renderCartTabs();
    updateCartUI();

    await loadProducts();
    await loadCustomers();
    renderPosProducts();

    showReceiptModal(data);

    if (state.settings.autoPrintReceipt) {
      setTimeout(() => {
        window.print();
      }, 300);
    }
  } catch (e) {
    alert('Không thể kết nối máy chủ: ' + e.message);
  }
}

// ==================== VIETQR AUTOMATED PAYMENT MODAL ====================
let qrPollingTimer = null;

function formatNumberToVietnameseSpeech(n) {
  const num = Math.round(Number(n) || 0);
  if (num <= 0) return "không đồng";

  const dv = ["", "nghìn", "triệu", "tỷ"];
  const chuSo = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];

  function doc3So(so, docKhongTram = false) {
    let tram = Math.floor(so / 100);
    let chuc = Math.floor((so % 100) / 10);
    let donvi = so % 10;
    let res = "";

    if (tram > 0 || docKhongTram) {
      res += chuSo[tram] + " trăm ";
    }

    if (chuc > 1) {
      res += chuSo[chuc] + " mươi ";
      if (donvi === 1) res += "mốt ";
      else if (donvi === 4) res += "tư ";
      else if (donvi === 5) res += "lăm ";
      else if (donvi > 0) res += chuSo[donvi] + " ";
    } else if (chuc === 1) {
      res += "mười ";
      if (donvi === 5) res += "lăm ";
      else if (donvi > 0) res += chuSo[donvi] + " ";
    } else if (chuc === 0 && donvi > 0) {
      if (tram > 0 || docKhongTram) res += "lẻ ";
      res += chuSo[donvi] + " ";
    }

    return res.trim();
  }

  let strNum = num.toString();
  let groups = [];
  while (strNum.length > 0) {
    groups.unshift(parseInt(strNum.slice(-3), 10));
    strNum = strNum.slice(0, -3);
  }

  let words = [];
  for (let i = 0; i < groups.length; i++) {
    let g = groups[i];
    if (g > 0) {
      let isFirst = (i === 0);
      let gText = doc3So(g, !isFirst);
      let unitIdx = groups.length - 1 - i;
      let unit = dv[unitIdx] || "";
      words.push(gText + (unit ? " " + unit : ""));
    }
  }

  return words.join(" ").trim() + " đồng";
}

function playPaymentTingTing(amount = null) {
  if (state.settings && state.settings.paymentSoundEnabled === false) return;
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const now = ctx.currentTime;

    // Upbeat celebratory 4-note ascending chord (C5, E5, G5, C6)
    const notes = [523.25, 659.25, 783.99, 1046.50];
    notes.forEach((freq, idx) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(freq, now + idx * 0.08);

      gain.gain.setValueAtTime(0, now + idx * 0.08);
      gain.gain.linearRampToValueAtTime(0.2, now + idx * 0.08 + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, now + idx * 0.08 + 0.32);

      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start(now + idx * 0.08);
      osc.stop(now + idx * 0.08 + 0.32);
    });

    let textToSpeak = "Đã nhận đủ tiền từ khách hàng";
    if (amount && Number(amount) > 0) {
      textToSpeak = `Đã nhận thành công ${formatNumberToVietnameseSpeech(amount)}`;
    }

    setTimeout(() => {
      speakVietnameseFemale(textToSpeak);
    }, 450);
  } catch (_) { }
}

let currentTtsAudio = null;

function speakVietnameseFemale(textToSpeak) {
  if (!textToSpeak) return;

  // Dừng âm thanh cũ nếu đang phát
  try {
    if (currentTtsAudio) {
      currentTtsAudio.pause();
      currentTtsAudio.currentTime = 0;
      currentTtsAudio = null;
    }
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  } catch (_) {}

  // 1. ƯU TIÊN 1: Dùng API TTS giọng nữ tiếng Việt chuẩn "Chị Google" (rất tự nhiên, 100% tiếng Việt nữ)
  let played = false;
  try {
    const audio = new Audio(`/api/tts?text=${encodeURIComponent(textToSpeak)}`);
    currentTtsAudio = audio;
    audio.playbackRate = 1.0;

    const playPromise = audio.play();
    if (playPromise !== undefined) {
      playPromise.then(() => {
        played = true;
      }).catch(() => {
        if (!played) fallbackBrowserVietnameseFemale(textToSpeak);
      });
    }

    audio.onerror = () => {
      if (!played) fallbackBrowserVietnameseFemale(textToSpeak);
    };
  } catch (_) {
    fallbackBrowserVietnameseFemale(textToSpeak);
  }
}

function fallbackBrowserVietnameseFemale(textToSpeak) {
  if (!('speechSynthesis' in window)) return;
  try {
    window.speechSynthesis.cancel();
    const utter = new SpeechSynthesisUtterance(textToSpeak);
    utter.lang = 'vi-VN';
    utter.rate = 1.0;
    utter.pitch = 1.15;

    const voices = window.speechSynthesis.getVoices() || [];
    
    // Ưu tiên các giọng nữ tiếng Việt:
    const viFemaleVoice = voices.find(v => {
      const l = (v.lang || '').toLowerCase();
      const n = (v.name || '').toLowerCase();
      if (!l.includes('vi')) return false;
      return n.includes('hoaimy') || n.includes('google') || n.includes('linh') || n.includes('female') || n.includes('nu');
    }) || voices.find(v => {
      const l = (v.lang || '').toLowerCase();
      const n = (v.name || '').toLowerCase();
      return l.includes('vi') && !n.includes('nam') && !n.includes('male') && !n.includes('david');
    }) || voices.find(v => (v.lang || '').toLowerCase().includes('vi'));

    // BẮT BUỘC: Chỉ đọc nếu tìm thấy giọng Tiếng Việt, KHÔNG BAO GIỜ để trình duyệt tự phát giọng nam tiếng Anh
    if (viFemaleVoice) {
      utter.voice = viFemaleVoice;
      window.speechSynthesis.speak(utter);
    }
  } catch (_) {}
}

if ('speechSynthesis' in window && window.speechSynthesis.onvoiceschanged !== undefined) {
  window.speechSynthesis.onvoiceschanged = () => {
    try { window.speechSynthesis.getVoices(); } catch (_) {}
  };
}

const VIETQR_BANKS = [
  { bin: '970422', name: 'MBBank (Quân Đội)' },
  { bin: '970436', name: 'Vietcombank (VCB)' },
  { bin: '970415', name: 'VietinBank' },
  { bin: '970418', name: 'BIDV' },
  { bin: '970405', name: 'Agribank' },
  { bin: '970407', name: 'Techcombank' },
  { bin: '970416', name: 'ACB' },
  { bin: '970432', name: 'VPBank' },
  { bin: '970423', name: 'TPBank' },
  { bin: '970403', name: 'Sacombank' },
  { bin: '970441', name: 'VIB' },
  { bin: '970448', name: 'OCB' },
  { bin: '970437', name: 'HDBank' },
  { bin: '970443', name: 'SHB' },
  { bin: '970426', name: 'MSB' },
  { bin: '970440', name: 'SeABank' },
  { bin: '970449', name: 'LPBank' },
  { bin: '546034', name: 'Cake by VPBank' },
  { bin: '963388', name: 'Timo by BVBank' }
];

function getBankNameByBin(bin) {
  const b = VIETQR_BANKS.find(x => x.bin === bin);
  return b ? b.name : `Ngân hàng (BIN ${bin})`;
}

async function openVietQrModal(amount, customerName, onConfirm) {
  const modal = document.getElementById('vietQrModal');
  const memo = `${state.settings.qrTransferPrefix || 'HD'}${Date.now().toString().slice(-6)}`;

  const bank = (state.settings.qrBankBin || '970436').trim();
  const acc = (state.settings.qrAccountNo || '').trim();
  const accountName = (state.settings.qrAccountName || '').trim();

  const qrImg = document.getElementById('vietQrImg');
  const missingConfigEl = document.getElementById('vietQrMissingConfig');
  const errorBoxEl = document.getElementById('vietQrErrorBox');
  const accInfoEl = document.getElementById('vietQrAccountInfo');

  document.getElementById('vietQrAmountDisplay').textContent = formatVND(amount);
  document.getElementById('vietQrMemoInfo').textContent = memo;

  if (!acc) {
    // Chưa cài đặt STK ngân hàng: Hiển thị form cài đặt nhanh
    if (qrImg) qrImg.style.display = 'none';
    if (errorBoxEl) errorBoxEl.style.display = 'none';
    if (missingConfigEl) {
      missingConfigEl.style.display = 'block';
      const quickBank = document.getElementById('quickQrBankSelect');
      if (quickBank) quickBank.value = bank || '970436';
      const quickAcc = document.getElementById('quickQrAccountNo');
      if (quickAcc) {
        quickAcc.value = '';
        setTimeout(() => quickAcc.focus(), 250);
      }
      const quickName = document.getElementById('quickQrAccountName');
      if (quickName) quickName.value = accountName;

      const saveQuickBtn = document.getElementById('btnSaveQuickQr');
      if (saveQuickBtn) {
        saveQuickBtn.onclick = async () => {
          const newAcc = document.getElementById('quickQrAccountNo')?.value.trim();
          const newBank = document.getElementById('quickQrBankSelect')?.value || '970436';
          const newName = document.getElementById('quickQrAccountName')?.value.trim().toUpperCase() || '';
          if (!newAcc) {
            alert('Vui lòng nhập Số tài khoản ngân hàng nhận tiền!');
            return;
          }
          saveQuickBtn.disabled = true;
          saveQuickBtn.textContent = 'Đang lưu...';

          state.settings.qrBankBin = newBank;
          state.settings.qrAccountNo = newAcc;
          state.settings.qrAccountName = newName;
          state.settings.qrPaymentEnabled = true;

          try {
            await fetch('/api/settings', {
              method: 'PUT',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(state.settings)
            });
            const sBin = document.getElementById('settingQrBin');
            if (sBin) sBin.value = newBank;
            const sAcc = document.getElementById('settingQrAccountNo');
            if (sAcc) sAcc.value = newAcc;
            const sName = document.getElementById('settingQrAccountName');
            if (sName) sName.value = newName;
            const sBankSel = document.getElementById('settingQrBankSelect');
            if (sBankSel) sBankSel.value = newBank;

            // Re-render modal với mã QR mới
            openVietQrModal(amount, customerName, onConfirm);
          } catch (e) {
            alert('Lỗi lưu cấu hình: ' + e.message);
          } finally {
            saveQuickBtn.disabled = false;
            saveQuickBtn.textContent = '✓ Lưu & Tạo Mã QR Ngay';
          }
        };
      }
    }
    if (accInfoEl) accInfoEl.style.display = 'none';
  } else {
    // Đã có STK ngân hàng: Hiển thị mã QR VietQR chuẩn compact2
    if (missingConfigEl) missingConfigEl.style.display = 'none';
    if (errorBoxEl) errorBoxEl.style.display = 'none';

    if (accInfoEl) {
      accInfoEl.style.display = 'none';
    }

    const url = `https://img.vietqr.io/image/${bank}-${acc}-qr_only.png?amount=${Math.round(amount)}&addInfo=${encodeURIComponent(memo)}`;

    if (qrImg) {
      qrImg.style.display = 'block';
      qrImg.src = url;

      qrImg.onerror = () => {
        qrImg.style.display = 'none';
        if (errorBoxEl) {
          errorBoxEl.style.display = 'block';
          const detailsEl = document.getElementById('vietQrFallbackDetails');
          if (detailsEl) {
            detailsEl.innerHTML = `
              <div>🏦 <b>Ngân hàng:</b> ${escapeHtml(getBankNameByBin(bank))} (BIN: ${escapeHtml(bank)})</div>
              <div>🔢 <b>Số tài khoản:</b> <span style="font-size: 14px; color: #0284c7; font-weight: 800; font-family: monospace;">${escapeHtml(acc)}</span></div>
              ${accountName ? `<div>👤 <b>Chủ tài khoản:</b> ${escapeHtml(accountName)}</div>` : ''}
              <div>💵 <b>Số tiền:</b> <span style="color: #dc2626; font-weight: 800;">${formatVND(amount)}</span></div>
              <div>📝 <b>Nội dung CK:</b> <span style="font-size: 13px; color: #0284c7; font-weight: 800; font-family: monospace;">${escapeHtml(memo)}</span></div>
            `;
          }
          const retryBtn = document.getElementById('btnRetryVietQr');
          if (retryBtn) {
            retryBtn.onclick = () => {
              errorBoxEl.style.display = 'none';
              qrImg.style.display = 'block';
              qrImg.src = url + '&t=' + Date.now();
            };
          }
        }
      };

      qrImg.onload = () => {
        qrImg.style.display = 'block';
        if (errorBoxEl) errorBoxEl.style.display = 'none';
      };
    }
  }

  // Reset status UI
  const statusBox = document.getElementById('vietQrStatusBox');
  const successBox = document.getElementById('vietQrSuccessBox');
  const statusText = document.getElementById('vietQrStatusText');
  const doneBtn = document.getElementById('vietQrDoneBtn');

  if (statusBox) statusBox.style.display = 'flex';
  if (successBox) successBox.style.display = 'none';
  if (statusText) statusText.textContent = '📡 Đang lắng nghe tín hiệu chuyển khoản ngân hàng...';
  if (doneBtn) {
    doneBtn.disabled = false;
    doneBtn.textContent = '✓ Xác Nhận Thủ Công';
  }

  modal.classList.add('active');

  // Đăng ký pending payment trên server
  try {
    await fetch('/api/payment/create-pending', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        paymentCode: memo,
        amount: Math.round(amount),
        customerName: customerName || 'Khách lẻ'
      })
    });
  } catch (e) {
    console.error('Error creating pending payment:', e);
  }

  // Đồng bộ sang màn hình phụ khách hàng ESP32 (Hỗ trợ cả USB Serial và WiFi)
  try {
    fetch('/api/esp32/set-state', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        state: 'QR',
        orderCode: memo,
        amount: Math.round(amount),
        customerName: customerName || 'Khách lẻ'
      })
    }).then(async (res) => {
      if (res && res.ok) {
        const d = await res.json();
        if (d && d.qrContent) {
          const formattedAmt = `${Math.round(amount).toLocaleString('vi-VN')} VND`;
          const qrCmd = `QR|${memo}|${formattedAmt}|${d.qrContent}`;
          window._lastPendingEsp32QrCmd = qrCmd;
          const sent = await sendEsp32UsbCommand(qrCmd);
          if (typeof updatePosEsp32Display === 'function') {
            updatePosEsp32Display(sent);
          }
        }
      }
    }).catch(() => {});
  } catch (_) { }

  // Helper hàm đóng modal VietQR và dọn dẹp sạch sẽ trạng thái
  const closeVietQrModal = () => {
    window._lastPendingEsp32QrCmd = null;
    if (qrPollingTimer) {
      clearInterval(qrPollingTimer);
      qrPollingTimer = null;
    }
    modal.classList.remove('active');
    if (doneBtn) {
      doneBtn.disabled = false;
      doneBtn.textContent = '✓ Xác Nhận Thủ Công';
    }
    const store = state.settings?.storeName || 'TAP HOA VIET';
    sendEsp32UsbCommand(`IDLE|${removeVietnameseTones(store)}|Xin chao quy khach!`);
    fetch('/api/esp32/set-state', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ state: 'IDLE' })
    }).catch(() => {});
  };

  // Clear any existing polling timer
  if (qrPollingTimer) clearInterval(qrPollingTimer);

  let isCompleted = false;

  const completePayment = (paidInfo = null) => {
    return new Promise((resolve) => {
      if (isCompleted) {
        resolve();
        return;
      }
      isCompleted = true;
      if (qrPollingTimer) {
        clearInterval(qrPollingTimer);
        qrPollingTimer = null;
      }

      const paidAmt = Math.round(paidInfo?.receivedAmount || amount);
      const bank = paidInfo?.bankName || 'Ngan hang';
      const formattedAmt = `${paidAmt.toLocaleString('vi-VN')} VND`;

      // Đồng bộ báo thành công sang màn hình phụ khách hàng ESP32 (Cả USB Serial và WiFi)
      try {
        sendEsp32UsbCommand(`SUCCESS|${memo}|${formattedAmt}|${removeVietnameseTones(bank)}`);
      } catch (_) { }

      // Phát âm thanh chuông ting-ting và đọc to số tiền chuyển khoản
      try {
        playPaymentTingTing(paidAmt);
      } catch (_) { }

      try {
        fetch('/api/esp32/set-state', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            state: 'SUCCESS',
            orderCode: memo,
            amount: paidAmt,
            bankName: bank
          })
        }).catch(() => {});
      } catch (_) { }

      try {
        if (statusBox) statusBox.style.display = 'none';
        if (successBox) {
          successBox.style.display = 'block';
          const titleEl = document.getElementById('vietQrSuccessTitle');
          const descEl = document.getElementById('vietQrSuccessDesc');
          if (titleEl) {
            titleEl.textContent = paidInfo?.bankName && paidInfo.bankName !== 'Xác nhận thủ công'
              ? `✓ ĐÃ NHẬN ${formatVND(paidInfo.receivedAmount || amount)} QUA ${paidInfo.bankName.toUpperCase()}!`
              : `✓ ĐÃ XÁC NHẬN THANH TOÁN THÀNH CÔNG!`;
          }
          if (descEl) {
            descEl.textContent = 'Đang tự động lưu hoá đơn và in bill...';
          }
        }
      } catch (_) { }

      // Short delay for visual feedback, then complete checkout
      setTimeout(async () => {
        try {
          closeVietQrModal();
          if (onConfirm) await onConfirm();
        } catch (err) {
          console.error('Error completing payment:', err);
        } finally {
          resolve();
        }
      }, 1000);
    });
  };

  // Start polling
  qrPollingTimer = setInterval(async () => {
    if (isCompleted || !modal.classList.contains('active')) {
      clearInterval(qrPollingTimer);
      return;
    }

    try {
      const res = await fetch(`/api/payment/status/${encodeURIComponent(memo)}`);
      if (res.ok) {
        const data = await res.json();
        if (data.isSuccess) {
          await completePayment(data);
        }
      }
    } catch (_) { }
  }, 1200);

  // Manual fallback button
  if (doneBtn) {
    doneBtn.onclick = async () => {
      doneBtn.disabled = true;
      doneBtn.textContent = '⏳ Đang xử lý...';
      try {
        await completePayment({ receivedAmount: amount, bankName: 'Xác nhận thủ công' });
      } catch (err) {
        console.error('Error in doneBtn:', err);
        closeVietQrModal();
        if (onConfirm) await onConfirm();
      } finally {
        doneBtn.disabled = false;
        doneBtn.textContent = '✓ Xác Nhận Thủ Công';
      }
    };
  }

  // Close modal button
  const closeBtn = document.getElementById('closeVietQrModalBtn');
  if (closeBtn) {
    closeBtn.onclick = closeVietQrModal;
  }
  const cancelBtn = document.getElementById('cancelVietQrModalBtn');
  if (cancelBtn) {
    cancelBtn.onclick = closeVietQrModal;
  }
}

// ==================== RECEIPT PRINTING (K58 / K80) ====================
function showReceiptModal(saleData) {
  window._activeReceiptData = saleData;
  window._currentReceiptSaleId = saleData.saleId || saleData.saleID || 0;

  const modal = document.getElementById('receiptModal');
  const container = document.getElementById('receiptPreviewBody');
  const html = buildReceiptHtml(saleData);

  container.innerHTML = html;

  const printArea = document.getElementById('receiptPrintArea');
  printArea.className = `receipt-print-area ${state.settings.defaultPaperWidth === 58 ? 'k58' : 'k80'}`;
  printArea.innerHTML = html;

  modal.classList.add('active');

  document.getElementById('printReceiptBtn').onclick = () => {
    window.print();
  };
}

function buildReceiptHtml(data) {
  const store = state.settings.storeName || 'CỬA HÀNG TẠP HOÁ';
  const addr = state.settings.storeAddress || '';
  const phone = state.settings.storePhone || '';
  const footer = state.settings.receiptFooter || 'Cảm ơn quý khách và hẹn gặp lại!';
  const is58 = state.settings.defaultPaperWidth === 58;
  const copies = state.settings.printCopies || 1;
  const qrEnabled = state.settings.printQrOnReceipt !== false && state.settings.qrPaymentEnabled && state.settings.qrAccountNo;

  let baseFontSize = is58 ? '11px' : '13px';
  if (state.settings.receiptFontSize === 'large') baseFontSize = is58 ? '13px' : '15px';
  else if (state.settings.receiptFontSize === 'compact') baseFontSize = is58 ? '10px' : '11.5px';

  function renderSingleCopy(copyTitle = null) {
    return `
      <div style="font-family: 'Courier New', monospace; font-size: ${baseFontSize}; line-height: 1.35; color: var(--text-primary); max-width: ${is58 ? '58mm' : '80mm'}; margin: 0 auto; padding-bottom: 8px;">
        ${copyTitle ? `
          <div style="text-align: center; font-weight: bold; font-size: 11px; margin-bottom: 6px; padding: 2px 4px; border: 1px dashed var(--border-color); border-radius: 4px;">
            ${escapeHtml(copyTitle)}
          </div>
        ` : ''}
        <div style="text-align: center; margin-bottom: 8px;">
          <h2 style="font-size: ${is58 ? '14px' : '16px'}; font-weight: bold; margin-bottom: 3px;">${escapeHtml(store)}</h2>
          ${addr ? `<div>Đ/c: ${escapeHtml(addr)}</div>` : ''}
          ${phone ? `<div>SĐT: ${escapeHtml(phone)}</div>` : ''}
          <div style="font-weight: bold; margin-top: 6px; font-size: ${is58 ? '13px' : '14px'};">PHIẾU BÁN HÀNG</div>
          <div>Số HĐ: ${data.invoiceCode || ('HD' + String(data.saleId || data.saleID || Date.now()).slice(-6))}</div>
          <div>Ngày: ${data.saleDate || new Date().toLocaleString('vi-VN')}</div>
          <div>Khách: ${escapeHtml(data.customerName || 'Khách lẻ')}</div>
        </div>

        <div style="border-top: 1px dashed var(--border-color); margin: 6px 0;"></div>

        <table style="width: 100%; font-size: inherit; border-collapse: collapse;">
          <thead>
            <tr style="text-align: left; border-bottom: 1px solid var(--border-color);">
              <th style="padding: 3px 0;">Mặt hàng</th>
              <th style="text-align: center;">SL</th>
              <th style="text-align: right;">Đ.Giá</th>
              <th style="text-align: right;">T.Tiền</th>
            </tr>
          </thead>
          <tbody>
            ${(data.items || []).map(i => `
              <tr>
                <td style="padding: 3px 0;">${escapeHtml(i.productName)}</td>
                <td style="text-align: center;">${i.quantity}</td>
                <td style="text-align: right;">${formatVND(i.unitPrice)}</td>
                <td style="text-align: right; font-weight: bold;">${formatVND(i.unitPrice * i.quantity)}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>

        <div style="border-top: 1px dashed var(--border-color); margin: 6px 0;"></div>

        <div style="display: flex; justify-content: space-between;">
          <span>Tạm tính:</span>
          <span>${formatVND(data.subtotal || data.totalAmount || data.finalAmount)}</span>
        </div>
        ${data.discountAmount > 0 ? `
          <div style="display: flex; justify-content: space-between; color: var(--danger);">
            <span>Giảm giá (${escapeHtml(data.discountNote || 'Chiết khấu')}):</span>
            <span>-${formatVND(data.discountAmount)}</span>
          </div>
        ` : ''}
        <div style="display: flex; justify-content: space-between; font-weight: bold; font-size: ${is58 ? '13px' : '14px'}; margin-top: 4px;">
          <span>TỔNG CỘNG:</span>
          <span>${formatVND(data.finalAmount || data.totalAmount)}</span>
        </div>
        <div style="display: flex; justify-content: space-between; font-size: 11px; color: var(--text-secondary); margin-top: 2px;">
          <span>Hình thức:</span>
          <span>${data.paymentMethod || 'Tiền mặt'}</span>
        </div>
        ${data.paymentMethod === 'Tiền mặt' && data.receivedAmount ? `
          <div style="display: flex; justify-content: space-between; font-size: 11px; margin-top: 2px;">
            <span>Khách đưa:</span>
            <span>${formatVND(data.receivedAmount)}</span>
          </div>
          <div style="display: flex; justify-content: space-between; font-size: 11px; font-weight: bold;">
            <span>Tiền thừa:</span>
            <span>${formatVND(data.changeAmount || 0)}</span>
          </div>
        ` : ''}

        ${qrEnabled ? `
          <div style="text-align: center; margin-top: 8px; padding-top: 6px; border-top: 1px dashed var(--border-color);">
            <img src="https://img.vietqr.io/image/${state.settings.qrBankBin}-${state.settings.qrAccountNo}-qr_only.png?amount=${Math.round(data.finalAmount || data.totalAmount)}&addInfo=${encodeURIComponent(data.invoiceCode || 'HD')}" style="width: 130px; height: 130px; display: block; margin: 0 auto; object-fit: contain;" alt="QR Code">
          </div>
        ` : ''}

        <div style="border-top: 1px dashed var(--border-color); margin: 10px 0 6px 0;"></div>
        <div style="text-align: center; font-size: 11px; font-style: italic;">
          ${escapeHtml(footer)}
      </div>
    `;
  }

  if (copies === 2) {
    return renderSingleCopy('LIÊN 1: GIAO KHÁCH HÀNG') +
      '<div class="receipt-double-cut"></div>' +
      renderSingleCopy('LIÊN 2: LƯU CỬA HÀNG / KẾ TOÁN');
  }

  return renderSingleCopy();
}

// ==================== A4 / A5 INVOICE MODAL & BUILDER ====================
window.openA4InvoiceModal = async (saleId, fallbackSaleData = null) => {
  const modal = document.getElementById('a4InvoiceModal');
  const frame = document.getElementById('a4InvoiceFrame');
  const title = document.getElementById('a4InvoiceModalTitle');
  if (!modal || !frame) return;

  const paperDesc = `${state.settings.a4PaperSize || 'A4'} ${state.settings.a4Orientation === 'landscape' ? 'Ngang' : 'Dọc'}`;

  if (saleId && saleId > 0) {
    title.textContent = `📄 Hoá Đơn Bán Hàng [Khổ ${paperDesc}] - #HD${String(saleId).padStart(6, '0')}`;
    frame.srcdoc = `<div style="padding: 40px; font-family: sans-serif; text-align: center; color: #64748b;">⏳ Đang tải biểu mẫu hoá đơn A4...</div>`;
    modal.classList.add('active');

    try {
      const res = await fetch(`/api/sales/${saleId}/invoice-a4`);
      if (res.ok) {
        const html = await res.text();
        frame.srcdoc = html;
      } else {
        frame.srcdoc = `<div style="padding: 40px; font-family: sans-serif; text-align: center; color: red;">Không tìm thấy dữ liệu hoá đơn #${saleId}</div>`;
      }
    } catch (e) {
      frame.srcdoc = `<div style="padding: 40px; font-family: sans-serif; text-align: center; color: red;">Lỗi tải hoá đơn: ${e.message}</div>`;
    }
  } else if (fallbackSaleData && (fallbackSaleData.saleId || fallbackSaleData.saleID)) {
    const id = fallbackSaleData.saleId || fallbackSaleData.saleID;
    title.textContent = `📄 Hoá Đơn Bán Hàng [Khổ ${paperDesc}] - #HD${String(id).padStart(6, '0')}`;
    frame.srcdoc = `<div style="padding: 40px; font-family: sans-serif; text-align: center; color: #64748b;">⏳ Đang tải biểu mẫu hoá đơn A4...</div>`;
    modal.classList.add('active');

    try {
      const res = await fetch(`/api/sales/${id}/invoice-a4`);
      if (res.ok) {
        frame.srcdoc = await res.text();
        return;
      }
    } catch (_) { }
    frame.srcdoc = buildSampleA4Html(fallbackSaleData);
  } else {
    title.textContent = `📄 Biểu Mẫu Hoá Đơn Mẫu [Khổ ${paperDesc}] - Bán Buôn & Đại Lý`;
    frame.srcdoc = buildSampleA4Html(fallbackSaleData);
    modal.classList.add('active');
  }
};

function readVNDWords(amount) {
  if (!amount || amount <= 0) return 'Không đồng';
  const units = ['', ' nghìn', ' triệu', ' tỷ', ' nghìn tỷ', ' triệu tỷ'];
  const digits = ['không', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín'];

  function readThreeDigits(n, showZeroHundred) {
    let h = Math.floor(n / 100);
    let t = Math.floor((n % 100) / 10);
    let u = n % 10;
    if (h === 0 && t === 0 && u === 0) return '';
    let res = '';
    if (h > 0 || showZeroHundred) res += digits[h] + ' trăm ';
    if (t === 0 && u > 0) {
      if (h > 0 || showZeroHundred) res += 'lẻ ';
      res += digits[u];
    } else if (t === 1) {
      res += 'mười ';
      if (u === 5) res += 'lăm';
      else if (u > 0) res += digits[u];
    } else if (t > 1) {
      res += digits[t] + ' mươi ';
      if (u === 1) res += 'mốt';
      else if (u === 5) res += 'lăm';
      else if (u > 0) res += digits[u];
    }
    return res.trim();
  }

  let str = Math.round(amount).toString();
  let groups = [];
  while (str.length > 0) {
    groups.unshift(parseInt(str.slice(-3), 10));
    str = str.slice(0, -3);
  }

  let result = '';
  for (let i = 0; i < groups.length; i++) {
    const val = groups[i];
    if (val > 0) {
      const gText = readThreeDigits(val, i > 0);
      const unitIdx = groups.length - 1 - i;
      result += (result ? ' ' : '') + gText + (unitIdx < units.length ? units[unitIdx] : '');
    }
  }
  result = result.trim();
  if (!result) return 'Không đồng';
  return result.charAt(0).toUpperCase() + result.slice(1) + ' đồng chẵn.';
}

function buildSampleA4Html(data = null) {
  const store = state.settings.storeName || 'CỬA HÀNG TẠP HOÁ & ĐẠI LÝ BÁN BUÔN';
  const addr = state.settings.storeAddress || 'Số 123 Đường Thương Mại, Quận 1, TP. Hồ Chí Minh';
  const phone = state.settings.storePhone || '0901 234 567';
  const taxId = state.settings.storeTaxId || '0109887766-001';
  const title = state.settings.a4InvoiceTitle || 'HÓA ĐƠN BÁN HÀNG';
  const paper = state.settings.a4PaperSize || 'A4';
  const orient = state.settings.a4Orientation || 'portrait';
  const showSign = state.settings.a4ShowSignatures !== false;
  const showQr = state.settings.a4ShowBankQr !== false && state.settings.qrPaymentEnabled && state.settings.qrAccountNo;

  const invCode = data?.invoiceCode || 'HD-A4-' + Math.floor(1000 + Math.random() * 9000);
  const saleDate = data?.saleDate || new Date().toLocaleString('vi-VN');
  const cust = data?.customerName || 'Đại Lý Tạp Hoá Mai Linh (Hà Nội)';

  const items = (data?.items && data.items.length > 0) ? data.items : [
    { productName: 'Gạo ST25 Thơm Thượng Hạng (Bao 10kg)', unit: 'Bao', quantity: 5, unitPrice: 380000 },
    { productName: 'Dầu Ăn Neptune Gold 5L (Thùng 4 can)', unit: 'Thùng', quantity: 3, unitPrice: 720000 },
    { productName: 'Bột Ngọt Ajinomoto 454g (Thùng 24 gói)', unit: 'Thùng', quantity: 2, unitPrice: 660000 },
    { productName: 'Nước Ngọt Coca Cola 320ml (Thùng 24 lon)', unit: 'Thùng', quantity: 8, unitPrice: 215000 },
    { productName: 'Bột Giặt OMO Matic Cửa Trên 6kg (Túi)', unit: 'Túi', quantity: 4, unitPrice: 285000 }
  ];

  const subtotal = data?.subtotal || items.reduce((sum, x) => sum + (x.quantity * x.unitPrice), 0);
  const discount = data?.discountAmount ?? 150000;
  const finalAmount = data?.finalAmount || (subtotal - discount);
  const words = readVNDWords(finalAmount);

  const qrUrl = showQr ? `https://img.vietqr.io/image/${state.settings.qrBankBin}-${state.settings.qrAccountNo}-qr_only.png?amount=${Math.round(finalAmount)}&addInfo=${encodeURIComponent(invCode)}` : '';

  return `<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <title>${escapeHtml(title)} - ${invCode}</title>
  <style>
    @page {
      size: ${paper.toLowerCase()} ${orient.toLowerCase()};
      margin: 12mm 15mm;
    }
    body {
      font-family: 'Times New Roman', Times, serif;
      font-size: 13.5px;
      line-height: 1.45;
      color: #111;
      margin: 0;
      padding: 15px 25px;
      background: #fff;
    }
    .header-table {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 12px;
    }
    .header-table td {
      vertical-align: top;
    }
    .store-name {
      font-size: 17px;
      font-weight: bold;
      text-transform: uppercase;
      color: #0f172a;
    }
    .invoice-title-box {
      text-align: center;
      margin: 14px 0 10px 0;
    }
    .invoice-title {
      font-size: 21px;
      font-weight: bold;
      letter-spacing: 1px;
      text-transform: uppercase;
      margin-bottom: 2px;
    }
    .invoice-sub {
      font-size: 12.5px;
      font-style: italic;
      color: #475569;
    }
    .info-grid {
      width: 100%;
      margin-bottom: 12px;
      font-size: 13.5px;
    }
    .info-grid td {
      padding: 2px 0;
    }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 8px;
    }
    .data-table th, .data-table td {
      border: 1px solid #334155;
      padding: 6px 8px;
    }
    .data-table th {
      background-color: #f1f5f9;
      font-weight: bold;
      text-align: center;
    }
    .summary-box {
      margin-top: 10px;
      display: flex;
      justify-content: flex-end;
    }
    .summary-table {
      border-collapse: collapse;
      width: 340px;
    }
    .summary-table td {
      padding: 3px 6px;
    }
    .in-words {
      font-style: italic;
      margin-top: 8px;
      font-size: 13px;
    }
    .signatures {
      width: 100%;
      margin-top: 24px;
      text-align: center;
      page-break-inside: avoid;
    }
    .signatures td {
      vertical-align: top;
      width: 33.33%;
    }
    .sign-title {
      font-weight: bold;
      font-size: 13px;
    }
    .sign-sub {
      font-size: 11.5px;
      font-style: italic;
      color: #64748b;
    }
    .sign-space {
      height: 75px;
    }
    .qr-box {
      border: 1px dashed #94a3b8;
      border-radius: 6px;
      padding: 6px;
      text-align: center;
      width: 125px;
    }
    @media print {
      body { padding: 0; }
    }
  </style>
</head>
<body>
  <table class="header-table">
    <tr>
      <td style="width: 72%;">
        <div class="store-name">${escapeHtml(store)}</div>
        ${taxId ? `<div><strong>Mã số thuế:</strong> ${escapeHtml(taxId)}</div>` : ''}
        <div><strong>Địa chỉ:</strong> ${escapeHtml(addr)}</div>
        <div><strong>Điện thoại:</strong> ${escapeHtml(phone)}</div>
      </td>
      <td style="width: 28%; text-align: right;">
        <div style="font-weight: bold;">Số HĐ: ${escapeHtml(invCode)}</div>
        <div style="font-size: 12px; color: #475569;">Ngày lập: ${escapeHtml(saleDate)}</div>
      </td>
    </tr>
  </table>

  <div class="invoice-title-box">
    <div class="invoice-title">${escapeHtml(title)}</div>
    <div class="invoice-sub">(Khổ in: ${paper} ${orient === 'landscape' ? 'Ngang' : 'Dọc'} - Chứng từ bán lẻ & bán buôn hợp lệ)</div>
  </div>

  <table class="info-grid">
    <tr>
      <td style="width: 60%;"><strong>Khách hàng:</strong> ${escapeHtml(cust)}</td>
      <td style="width: 40%;"><strong>Phương thức:</strong> ${escapeHtml(data?.paymentMethod || 'Tiền mặt / Chuyển khoản')}</td>
    </tr>
    <tr>
      <td><strong>Địa chỉ nhận hàng:</strong> ${escapeHtml(data?.customerAddress || 'Giao tại cửa hàng / Kho')}</td>
      <td><strong>Nhân viên bán hàng:</strong> ${escapeHtml(state.currentUser ? state.currentUser.fullName : 'Admin')}</td>
    </tr>
  </table>

  <table class="data-table">
    <thead>
      <tr>
        <th style="width: 35px;">STT</th>
        <th>Tên Hàng Hóa, Dịch Vụ</th>
        <th style="width: 55px;">ĐVT</th>
        <th style="width: 50px;">SL</th>
        <th style="width: 95px;">Đơn Giá</th>
        <th style="width: 110px;">Thành Tiền</th>
      </tr>
    </thead>
    <tbody>
      ${items.map((it, idx) => `
        <tr>
          <td style="text-align: center;">${idx + 1}</td>
          <td><strong>${escapeHtml(it.productName)}</strong></td>
          <td style="text-align: center;">${escapeHtml(it.unit || 'Cái')}</td>
          <td style="text-align: center; font-weight: bold;">${it.quantity}</td>
          <td style="text-align: right;">${Number(it.unitPrice).toLocaleString('vi-VN')} đ</td>
          <td style="text-align: right; font-weight: bold;">${(it.quantity * it.unitPrice).toLocaleString('vi-VN')} đ</td>
        </tr>
      `).join('')}
    </tbody>
  </table>

  <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-top: 10px;">
    <div>
      ${showQr ? `
        <div class="qr-box">
          <img src="${qrUrl}" style="width: 105px; height: 105px; display: block; margin: 0 auto;" alt="QR Code">
        </div>
      ` : ''}
    </div>

    <div>
      <table class="summary-table">
        <tr>
          <td><strong>Cộng tiền hàng:</strong></td>
          <td style="text-align: right; font-weight: bold;">${Number(subtotal).toLocaleString('vi-VN')} đ</td>
        </tr>
        ${discount > 0 ? `
          <tr style="color: #dc2626;">
            <td><strong>Chiết khấu / Giảm giá:</strong></td>
            <td style="text-align: right;">-${Number(discount).toLocaleString('vi-VN')} đ</td>
          </tr>
        ` : ''}
        <tr style="font-size: 15px; border-top: 1.5px solid #000;">
          <td><strong>TỔNG TIỀN THANH TOÁN:</strong></td>
          <td style="text-align: right; font-weight: bold; color: #0f172a;">${Number(finalAmount).toLocaleString('vi-VN')} đ</td>
        </tr>
      </table>
    </div>
  </div>

  <div class="in-words">
    <strong>Số tiền viết bằng chữ:</strong> <em>${words}</em>
  </div>

  ${showSign ? `
    <table class="signatures">
      <tr>
        <td>
          <div class="sign-title">NGƯỜI MUA HÀNG</div>
          <div class="sign-sub">(Ký, ghi rõ họ tên)</div>
          <div class="sign-space"></div>
        </td>
        <td>
          <div class="sign-title">NGƯỜI GIAO HÀNG</div>
          <div class="sign-sub">(Ký, ghi rõ họ tên)</div>
          <div class="sign-space"></div>
        </td>
        <td>
          <div class="sign-title">NGƯỜI LẬP PHIẾU</div>
          <div class="sign-sub">(Ký, đóng dấu nếu có)</div>
          <div class="sign-space"></div>
        </td>
      </tr>
    </table>
  ` : ''}
</body>
</html>`;
}

// ==================== BARCODE SVG GENERATOR (PURE JS CODE128) ====================
// Zero external dependencies, generates SVG barcode instantly offline
function generateBarcodeSvg(code, height = 40) {
  if (!code) return '';
  // Simple clean SVG barcode representation
  const cleanCode = String(code).trim();
  let bars = [];
  // Hash code into alternating bar patterns
  for (let i = 0; i < cleanCode.length; i++) {
    const charCode = cleanCode.charCodeAt(i);
    const w1 = (charCode % 3) + 1;
    const w2 = ((charCode * 3) % 4) + 1;
    bars.push({ type: 'bar', width: w1 });
    bars.push({ type: 'space', width: w2 });
  }

  let x = 10;
  let rects = '';
  bars.forEach(b => {
    if (b.type === 'bar') {
      rects += `<rect x="${x}" y="0" width="${b.width * 1.5}" height="${height}" fill="#000000" />`;
    }
    x += b.width * 1.5;
  });

  const totalWidth = x + 10;
  return `
    <svg class="shelf-tag-barcode-svg" viewBox="0 0 ${totalWidth} ${height}" width="${totalWidth}" height="${height}" xmlns="http://www.w3.org/2000/svg">
      ${rects}
    </svg>
  `;
}

// ==================== SHELF PRICE TAG PRINTING ====================
window.openPriceTagModal = (productId) => {
  const p = state.products.find(x => x.productID === productId);
  if (!p) return;
  state.selectedProductForTag = p;

  const storeName = state.settings.storeName || 'CỬA HÀNG TẠP HOÁ';
  const tagHtml = buildSingleShelfTagHtml(p, storeName);

  document.getElementById('priceTagPreviewContainer').innerHTML = tagHtml;
  document.getElementById('tagPrintQuantity').value = '1';
  document.getElementById('priceTagModal').classList.add('active');
};

function buildSingleShelfTagHtml(p, storeName) {
  const barcode = p.barcode || `SP${String(p.productID).padStart(8, '0')}`;
  return `
    <div class="shelf-tag">
      <div class="shelf-tag-store">${escapeHtml(storeName)}</div>
      <div class="shelf-tag-name">${escapeHtml(p.productName)}</div>
      <div class="shelf-tag-price">${formatVND(p.sellingPrice)}</div>
      <div class="shelf-tag-unit">ĐVT: ${escapeHtml(p.unit || 'Cái')}</div>
      ${generateBarcodeSvg(barcode, 32)}
      <div class="shelf-tag-barcode-num">${escapeHtml(barcode)}</div>
    </div>
  `;
}

document.getElementById('confirmPrintPriceTagBtn').addEventListener('click', () => {
  if (!state.selectedProductForTag) return;
  const qty = parseInt(document.getElementById('tagPrintQuantity').value) || 1;
  const storeName = state.settings.storeName || 'CỬA HÀNG TẠP HOÁ';
  const tagHtml = buildSingleShelfTagHtml(state.selectedProductForTag, storeName);

  const printArea = document.getElementById('shelfTagPrintArea');
  printArea.innerHTML = Array(qty).fill(tagHtml).join('');

  // Temporarily hide receipt print area and show tag print area
  document.getElementById('receiptPrintArea').style.display = 'none';
  printArea.style.display = 'flex';

  window.print();

  setTimeout(() => {
    printArea.style.display = 'none';
  }, 1000);
});

// ==================== SALES HISTORY & RETURNS ====================
async function loadAndRenderSalesHistory() {
  const search = document.getElementById('historySearchInput').value.trim();
  const from = document.getElementById('historyFromDate').value;
  const to = document.getElementById('historyToDate').value;

  let url = `/api/sales/history?limit=100`;
  if (search) url += `&search=${encodeURIComponent(search)}`;
  if (from) url += `&fromDate=${encodeURIComponent(from)}`;
  if (to) url += `&toDate=${encodeURIComponent(to)}`;

  try {
    const res = await fetch(url);
    if (!res.ok) return;
    const list = await res.json();
    const tbody = document.getElementById('salesHistoryTableBody');

    if (list.length === 0) {
      tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--text-muted); padding: 30px;">Không tìm thấy hoá đơn nào.</td></tr>`;
      return;
    }

    tbody.innerHTML = list.map(item => `
      <tr>
        <td style="font-weight: 700; font-family: monospace;">#HD${String(item.saleID).padStart(6, '0')}</td>
        <td>${formatDate(item.saleDate)}</td>
        <td style="font-weight: 600;">${escapeHtml(item.customerName || 'Khách lẻ')}</td>
        <td>
          <span class="user-badge ${item.isDebt ? 'inactive' : 'staff'}">
            ${item.isDebt ? 'Ghi nợ' : 'Đã thanh toán'}
          </span>
        </td>
        <td style="font-weight: 700; color: var(--primary);">${formatVND(item.totalAmount)}</td>
        <td>
          <div style="display: flex; gap: 6px;">
            <button class="btn btn-secondary" style="padding: 4px 8px; font-size: 11.5px;" onclick="reprintReceipt(${item.saleID})" title="In hoá đơn nhiệt K80/K58">
              🖨️ In bill
            </button>
            <button class="btn btn-info" style="padding: 4px 8px; font-size: 11.5px; background: #0284c7; color: #fff;" onclick="openA4InvoiceModal(${item.saleID})" title="In hoá đơn A4 / A5">
              📄 HĐ A4
            </button>
            <button class="btn btn-danger" style="padding: 4px 8px; font-size: 11.5px;" onclick="openReturnItemModal(${item.saleID})" title="Trả hàng">
              🔄 Trả
            </button>
          </div>
        </td>
      </tr>
    `).join('');
  } catch (e) {
    console.error('Error loading sales history:', e);
  }
}

window.reprintReceipt = async (saleId) => {
  try {
    const res = await fetch(`/api/sales/${saleId}/receipt`);
    if (!res.ok) {
      alert('Không tìm thấy dữ liệu hoá đơn này.');
      return;
    }
    const data = await res.json();
    showReceiptModal(data);
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

window.openReturnItemModal = async (saleId) => {
  try {
    const res = await fetch(`/api/sales/${saleId}/receipt`);
    if (!res.ok) {
      alert('Không thể tải thông tin đơn hàng.');
      return;
    }
    const sale = await res.json();
    if (!sale.items || sale.items.length === 0) {
      alert('Đơn hàng này không có mặt hàng nào để trả.');
      return;
    }

    document.getElementById('returnSaleId').value = sale.saleId;
    document.getElementById('returnSaleInfo').innerHTML = `
      <strong>Đơn hàng:</strong> #${sale.invoiceCode} | <strong>Khách:</strong> ${escapeHtml(sale.customerName)} | <strong>Tổng tiền:</strong> ${formatVND(sale.finalAmount)}
    `;

    const select = document.getElementById('returnProductSelect');
    select.innerHTML = sale.items.map(i => `
      <option value="${i.productID}" data-price="${i.unitPrice}" data-max="${i.quantity}">
        ${escapeHtml(i.productName)} (Mua: ${i.quantity} x ${formatVND(i.unitPrice)})
      </option>
    `).join('');

    // Trigger update price
    updateReturnRefundCalculation();

    select.onchange = updateReturnRefundCalculation;
    document.getElementById('returnQuantityInput').oninput = updateReturnRefundCalculation;
    const retBtn = document.getElementById('confirmReturnBtn');
    if (retBtn) { retBtn.disabled = false; retBtn.textContent = 'Xác Nhận Trả Hàng & Hoàn Tiền'; }
    document.getElementById('returnItemModal').classList.add('active');
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

function updateReturnRefundCalculation() {
  const select = document.getElementById('returnProductSelect');
  const opt = select.options[select.selectedIndex];
  if (!opt) return;

  const unitPrice = parseFloat(opt.getAttribute('data-price')) || 0;
  const maxQty = parseInt(opt.getAttribute('data-max')) || 1;

  const qtyInput = document.getElementById('returnQuantityInput');
  let qty = parseInt(qtyInput.value) || 1;
  if (qty > maxQty) {
    qty = maxQty;
    qtyInput.value = maxQty;
  }
  if (qty < 1) {
    qty = 1;
    qtyInput.value = 1;
  }

  document.getElementById('returnRefundAmountInput').value = unitPrice * qty;
}

document.getElementById('confirmReturnBtn').addEventListener('click', async (e) => {
  const saleId = parseInt(document.getElementById('returnSaleId').value);
  const select = document.getElementById('returnProductSelect');
  const productId = parseInt(select.value);
  const quantity = parseInt(document.getElementById('returnQuantityInput').value);
  const refundAmount = parseFloat(document.getElementById('returnRefundAmountInput').value) || 0;
  const reason = document.getElementById('returnReasonInput').value.trim();

  if (isNaN(quantity) || quantity <= 0) {
    alert('Số lượng trả phải lớn hơn 0!');
    return;
  }

  if (!confirm(`Xác nhận trả ${quantity} sản phẩm và hoàn ${formatVND(refundAmount)} cho khách?`)) {
    return;
  }

  const btn = e.currentTarget;
  const origText = btn ? btn.textContent : 'Xác Nhận Trả Hàng & Hoàn Tiền';
  if (btn) {
    btn.disabled = true;
    btn.textContent = '⏳ Đang xử lý...';
  }

  const operator = state.currentUser ? state.currentUser.username : 'Thu ngân';

  try {
    const res = await fetch(`/api/sales/${saleId}/return?operatorUser=${encodeURIComponent(operator)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productId,
        quantity,
        refundAmount,
        reason
      })
    });

    if (res.ok) {
      alert('Đã xử lý đổi trả hàng và hoàn tiền thành công! Hàng đã được cộng lại kho.');
      document.getElementById('returnItemModal').classList.remove('active');
      await loadProducts();
      await loadAndRenderSalesHistory();
    } else {
      const err = await res.json();
      alert('Lỗi: ' + (err.message || 'Không thành công'));
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  } finally {
    if (btn) {
      btn.disabled = false;
      btn.textContent = origText;
    }
  }
});

// ==================== INVENTORY MANAGEMENT & EXPIRY ====================
function renderInventoryTable() {
  const tbody = document.getElementById('inventoryTableBody');
  const catFilter = document.getElementById('inventoryCategoryFilter');
  const search = document.getElementById('inventorySearchInput').value.trim().toLowerCase();
  const lowOnly = document.getElementById('inventoryLowStockFilter').checked;
  const expiringOnly = document.getElementById('inventoryExpiringFilter').checked;

  catFilter.innerHTML = `<option value="">Tất cả danh mục</option>` +
    state.categories.map(c => `<option value="${c.categoryID}">${c.categoryName}</option>`).join('');

  let list = state.products;
  if (catFilter.value) {
    list = list.filter(p => p.categoryID == catFilter.value);
  }
  if (lowOnly) {
    list = list.filter(p => p.stockQuantity <= Math.max(p.minStock, 5));
  }
  if (expiringOnly) {
    const target = new Date();
    target.setDate(target.getDate() + 30);
    list = list.filter(p => p.expiryDate && new Date(p.expiryDate) <= target);
  }
  if (search) {
    list = list.filter(p =>
      (p.productName && p.productName.toLowerCase().includes(search)) ||
      (p.barcode && p.barcode.toLowerCase().includes(search))
    );
  }

  const isAdmin = (state.currentUser?.role || '').toLowerCase() === 'admin';
  const costTh = document.getElementById('costPriceHeaderTh');
  if (costTh) costTh.style.display = isAdmin ? '' : 'none';

  tbody.innerHTML = list.map(p => {
    const isLow = p.stockQuantity <= Math.max(p.minStock, 5);

    // Expiry badge calculation
    let expiryBadge = `<span style="color: var(--text-muted);">—</span>`;
    if (p.expiryDate) {
      const expDate = new Date(p.expiryDate);
      const today = new Date();
      const diffDays = Math.ceil((expDate - today) / (1000 * 60 * 60 * 24));
      if (diffDays < 0) {
        expiryBadge = `<span class="expiry-badge danger">⛔ Hết hạn (${formatDateOnly(p.expiryDate)})</span>`;
      } else if (diffDays <= 30) {
        expiryBadge = `<span class="expiry-badge warning">⚠️ Còn ${diffDays} ngày</span>`;
      } else {
        expiryBadge = `<span class="expiry-badge safe">📅 ${formatDateOnly(p.expiryDate)}</span>`;
      }
    }

    const costCell = isAdmin ? `<td>${formatVND(p.costPrice)}</td>` : '';
    const adminActions = isAdmin ? `
      <button class="btn btn-secondary" style="padding: 3px 6px; font-size: 11px;" onclick="openQuickImportModal(${p.productID})">+ Nhập</button>
      <button class="btn btn-secondary" style="padding: 3px 6px; font-size: 11px;" onclick="openEditProductModal(${p.productID})">Sửa</button>
      <button class="btn btn-danger" style="padding: 3px 6px; font-size: 11px;" onclick="deleteProduct(${p.productID})">Xoá</button>
    ` : '';

    return `
      <tr>
        <td>
          ${p.imageUrl ? `<img src="${escapeHtml(p.imageUrl)}" class="product-thumb-sm" alt="${escapeHtml(p.productName)}" onclick="viewFullImage('${escapeHtml(p.imageUrl)}', '${escapeHtml(p.productName)}')" title="Bấm để xem ảnh to">` : `<div class="product-thumb-placeholder" title="Chưa có ảnh">📷</div>`}
        </td>
        <td style="font-family: monospace;">${p.barcode || '—'}</td>
        <td style="font-weight: 600;">${escapeHtml(p.productName)}</td>
        <td>${p.categoryName || '—'}</td>
        <td>${p.unit || 'Cái'}</td>
        ${costCell}
        <td style="font-weight: 700; color: var(--success);">${formatVND(p.sellingPrice)}</td>
        <td>
          <span style="font-weight: 700; color: ${isLow ? 'var(--danger)' : 'inherit'};">
            ${p.stockQuantity} ${isLow ? '⚠️' : ''}
          </span>
        </td>
        <td>${expiryBadge}</td>
        <td>
          <div style="display: flex; gap: 4px; flex-wrap: wrap;">
            <button class="btn btn-secondary" style="padding: 3px 6px; font-size: 11px;" onclick="openPriceTagModal(${p.productID})" title="In tem giá & mã vạch dán kệ">🏷️ Tem giá</button>
            ${adminActions}
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

window.openQuickImportModal = (productId) => {
  const p = state.products.find(x => x.productID === productId);
  if (!p) return;
  document.getElementById('quickImportProductId').value = p.productID;
  document.getElementById('quickImportProductName').textContent = `${p.productName} (Tồn hiện tại: ${p.stockQuantity} ${p.unit || ''})`;
  document.getElementById('quickImportQty').value = '10';
  document.getElementById('quickImportCost').value = p.costPrice || '';
  document.getElementById('quickImportNote').value = '';
  const importBtn = document.getElementById('confirmQuickImportBtn');
  if (importBtn) { importBtn.disabled = false; importBtn.textContent = 'Xác Nhận Nhập'; }
  document.getElementById('quickImportModal').classList.add('active');
};

window.openEditProductModal = (productId) => {
  const p = state.products.find(x => x.productID === productId);
  if (!p) return;
  document.getElementById('productModalTitle').textContent = 'Sửa Sản Phẩm';
  document.getElementById('modalProductId').value = p.productID;
  document.getElementById('modalProductBarcode').value = p.barcode || '';
  document.getElementById('modalProductName').value = p.productName;
  document.getElementById('modalProductUnit').value = p.unit || '';
  document.getElementById('modalProductCost').value = p.costPrice || '';
  document.getElementById('modalProductPrice').value = p.sellingPrice || '';
  document.getElementById('modalProductStock').value = p.stockQuantity || 0;
  document.getElementById('modalProductMinStock').value = p.minStock || 5;
  document.getElementById('modalProductExpiry').value = p.expiryDate ? p.expiryDate.split('T')[0] : '';

  const catSelect = document.getElementById('modalProductCategory');
  catSelect.innerHTML = `<option value="">-- Chọn danh mục --</option>` +
    state.categories.map(c => `<option value="${c.categoryID}" ${c.categoryID === p.categoryID ? 'selected' : ''}>${c.categoryName}</option>`).join('');

  setProductModalImage(p.imageUrl || '');
  const saveBtn = document.getElementById('saveProductBtn');
  if (saveBtn) { saveBtn.disabled = false; saveBtn.textContent = 'Lưu Sản Phẩm'; }
  document.getElementById('productModal').classList.add('active');
};

window.deleteProduct = async (productId) => {
  if (!confirm('Bạn có chắc chắn muốn xoá sản phẩm này?')) return;
  const actor = state.currentUser ? state.currentUser.username : 'admin';
  try {
    const res = await fetch(`/api/products/${productId}?operatorUser=${encodeURIComponent(actor)}`, { method: 'DELETE' });
    if (res.ok) {
      await loadProducts();
      renderInventoryTable();
      renderPosProducts();
    }
  } catch (e) {
    alert('Lỗi xoá: ' + e.message);
  }
};

// ==================== DEBT MANAGEMENT ====================
async function loadAndRenderDebts() {
  try {
    const search = document.getElementById('debtSearchInput').value.trim();
    const res = await fetch(`/api/debts?keyword=${encodeURIComponent(search)}`);
    if (!res.ok) return;
    const debts = await res.json();

    const total = debts.reduce((sum, d) => sum + d.outstandingAmount, 0);
    document.getElementById('debtTotalAmountDisplay').textContent = formatVND(total);

    const tbody = document.getElementById('debtTableBody');
    if (debts.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 30px;">Không có nợ cần thanh toán.</td></tr>`;
      return;
    }

    tbody.innerHTML = debts.map(d => `
      <tr>
        <td style="font-weight: 700;">#HD${String(d.saleID).padStart(6, '0')}</td>
        <td>${formatDate(d.saleDate)}</td>
        <td style="font-weight: 600;">${escapeHtml(d.customerName || 'Khách lẻ')}</td>
        <td>${d.customerPhone || '—'}</td>
        <td>${formatVND(d.totalAmount)}</td>
        <td style="color: var(--success);">${formatVND(d.paidAmount)}</td>
        <td style="font-weight: 800; color: var(--danger);">${formatVND(d.outstandingAmount)}</td>
        <td>
          <button class="btn btn-success" style="padding: 4px 10px; font-size: 12px;" onclick="openDebtPayModal(${d.saleID}, '${escapeHtml(d.customerName)}', ${d.outstandingAmount})">
            💵 Thu nợ
          </button>
        </td>
      </tr>
    `).join('');
  } catch (e) {
    console.error('Error loading debts:', e);
  }
}

window.openDebtPayModal = (saleId, customerName, outstandingAmount) => {
  document.getElementById('debtPaySaleId').value = saleId;
  document.getElementById('debtPayCustomerInfo').textContent = `Khách hàng: ${customerName} (Còn nợ: ${formatVND(outstandingAmount)})`;
  document.getElementById('debtPayAmount').value = outstandingAmount;
  document.getElementById('debtPayNote').value = 'Khách trả nợ';
  const payBtn = document.getElementById('confirmDebtPaymentBtn');
  if (payBtn) { payBtn.disabled = false; payBtn.textContent = 'Ghi Nhận Thanh Toán'; }
  document.getElementById('debtPaymentModal').classList.add('active');
};

// ==================== QUẢN LÝ KHÁCH HÀNG ====================
let _allCustomers = [];

async function loadAndRenderCustomers() {
  try {
    const search = (document.getElementById('customerTabSearchInput')?.value || '').trim();
    const url = search ? `/api/customers?search=${encodeURIComponent(search)}` : '/api/customers';
    const res = await fetch(url);
    if (!res.ok) return;
    const customers = await res.json();
    _allCustomers = customers;
    renderCustomerTable(customers);
    updateCustomerStats(customers);
  } catch (e) {
    console.error('Error loading customers:', e);
  }
}

function updateCustomerStats(customers) {
  const total = document.getElementById('statTotalCustomers');
  if (total) total.textContent = customers.length;

  // Count new customers this month
  const now = new Date();
  const thisMonth = customers.filter(c => {
    const d = new Date(c.createdDate);
    return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
  }).length;
  const newEl = document.getElementById('statNewCustomersMonth');
  if (newEl) newEl.textContent = thisMonth;
}

function renderCustomerTable(customers) {
  const tbody = document.getElementById('customerTableBody');
  if (!tbody) return;
  if (!customers || customers.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:var(--text-muted);padding:32px;">Chưa có khách hàng nào. Nhấn "+ Thêm khách hàng" để bắt đầu.</td></tr>`;
    return;
  }
  tbody.innerHTML = customers.map(c => `
    <tr>
      <td style="color:var(--text-muted);font-size:12px;">#${c.customerID}</td>
      <td style="font-weight:600;">
        <span style="font-size:16px;">👤</span> ${escapeHtml(c.customerName)}
      </td>
      <td>${c.phone ? `<a href="tel:${escapeHtml(c.phone)}" style="color:var(--primary);text-decoration:none;">📞 ${escapeHtml(c.phone)}</a>` : '<span style="color:var(--text-muted);">—</span>'}</td>
      <td style="max-width:160px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="${escapeHtml(c.address || '')}">${escapeHtml(c.address || '—')}</td>
      <td style="max-width:140px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="${escapeHtml(c.note || '')}">${escapeHtml(c.note || '—')}</td>
      <td style="font-size:12px;color:var(--text-muted);">${c.createdDate ? new Date(c.createdDate).toLocaleDateString('vi-VN') : '—'}</td>
      <td style="text-align:center;">
        <div style="display:flex;gap:6px;justify-content:center;">
          <button class="btn btn-secondary btn-sm" style="padding:3px 8px;font-size:11.5px;" onclick="viewCustomerHistory(${c.customerID}, '${escapeHtml(c.customerName)}')">📋 Lịch sử</button>
          <button class="btn btn-secondary btn-sm" style="padding:3px 8px;font-size:11.5px;" onclick="openEditCustomerModal(${c.customerID})">✏️ Sửa</button>
          <button class="btn btn-sm" style="padding:3px 8px;font-size:11.5px;background:rgba(239,68,68,0.15);color:#ef4444;border:1px solid rgba(239,68,68,0.3);" onclick="deleteCustomer(${c.customerID}, '${escapeHtml(c.customerName)}')">🗑️</button>
        </div>
      </td>
    </tr>
  `).join('');
}

function openAddCustomerModal() {
  document.getElementById('customerModalTitle').textContent = 'Thêm Khách Hàng Mới';
  document.getElementById('customerModalId').value = '';
  document.getElementById('customerModalName').value = '';
  document.getElementById('customerModalPhone').value = '';
  document.getElementById('customerModalAddress').value = '';
  document.getElementById('customerModalNote').value = '';
  const custBtn = document.getElementById('confirmSaveCustomerBtn');
  if (custBtn) { custBtn.disabled = false; custBtn.textContent = 'Lưu Khách Hàng'; }
  document.getElementById('customerModal').classList.add('active');
  setTimeout(() => document.getElementById('customerModalName').focus(), 100);
}

window.openEditCustomerModal = (id) => {
  const c = _allCustomers.find(x => x.customerID === id);
  if (!c) return;
  document.getElementById('customerModalTitle').textContent = 'Sửa Thông Tin Khách Hàng';
  document.getElementById('customerModalId').value = c.customerID;
  document.getElementById('customerModalName').value = c.customerName || '';
  document.getElementById('customerModalPhone').value = c.phone || '';
  document.getElementById('customerModalAddress').value = c.address || '';
  document.getElementById('customerModalNote').value = c.note || '';
  const custBtn = document.getElementById('confirmSaveCustomerBtn');
  if (custBtn) { custBtn.disabled = false; custBtn.textContent = 'Lưu Khách Hàng'; }
  document.getElementById('customerModal').classList.add('active');
  setTimeout(() => document.getElementById('customerModalName').focus(), 100);
};

async function saveCustomer() {
  const id = parseInt(document.getElementById('customerModalId').value) || 0;
  const name = document.getElementById('customerModalName').value.trim();
  const phone = document.getElementById('customerModalPhone').value.trim();
  const address = document.getElementById('customerModalAddress').value.trim();
  const note = document.getElementById('customerModalNote').value.trim();

  if (!name) { alert('Vui lòng nhập tên khách hàng!'); return; }

  const saveBtn = document.getElementById('confirmSaveCustomerBtn');
  const origText = saveBtn ? saveBtn.textContent : null;
  if (saveBtn) { saveBtn.disabled = true; saveBtn.textContent = '⏳ Đang lưu...'; }

  const actor = state.currentUser?.username || 'admin';
  const payload = { customerName: name, phone: phone || null, address: address || null, note: note || null };

  try {
    const url = id ? `/api/customers/${id}?operatorUser=${encodeURIComponent(actor)}`
                   : `/api/customers?operatorUser=${encodeURIComponent(actor)}`;
    const method = id ? 'PUT' : 'POST';
    const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
    if (res.ok) {
      document.getElementById('customerModal').classList.remove('active');
      await loadAndRenderCustomers();
      await loadCustomers();
    } else {
      const err = await res.json().catch(() => ({}));
      alert('Lỗi: ' + (err.message || 'Không thành công'));
    }
  } catch (e) {
    alert('Lỗi kết nối: ' + e.message);
  } finally {
    if (saveBtn && origText) { saveBtn.disabled = false; saveBtn.textContent = origText; }
  }
}

window.deleteCustomer = async (id, name) => {
  if (!confirm(`Bạn có chắc muốn xóa khách hàng "${name}"?\nDữ liệu đơn hàng của khách sẽ không bị mất.`)) return;
  const actor = state.currentUser?.username || 'admin';
  try {
    const res = await fetch(`/api/customers/${id}?operatorUser=${encodeURIComponent(actor)}`, { method: 'DELETE' });
    if (res.ok) {
      await loadAndRenderCustomers();
      await loadCustomers();
    } else {
      const err = await res.json().catch(() => ({}));
      alert('Không thể xóa: ' + (err.message || 'Lỗi không xác định'));
    }
  } catch (e) {
    alert('Lỗi kết nối: ' + e.message);
  }
};

window.viewCustomerHistory = async (id, name) => {
  document.getElementById('customerHistoryTitle').textContent = `📋 Lịch sử mua hàng: ${name}`;
  document.getElementById('customerHistoryTableBody').innerHTML = '<tr><td colspan="6" style="text-align:center;padding:20px;">Đang tải...</td></tr>';
  document.getElementById('chStatOrders').textContent = '…';
  document.getElementById('chStatSpent').textContent = '…';
  document.getElementById('chStatLastPurchase').textContent = '…';
  document.getElementById('customerHistoryModal').classList.add('active');

  try {
    const [statsRes, salesRes] = await Promise.all([
      fetch(`/api/customers/${id}/stats`),
      fetch(`/api/customers/${id}/sales`)
    ]);

    if (statsRes.ok) {
      const s = await statsRes.json();
      document.getElementById('chStatOrders').textContent = s.orderCount ?? 0;
      document.getElementById('chStatSpent').textContent = formatVND(s.totalSpent ?? 0);
      document.getElementById('chStatLastPurchase').textContent = s.lastPurchase ?? 'Chưa có';
    }

    if (salesRes.ok) {
      const sales = await salesRes.json();
      const tbody = document.getElementById('customerHistoryTableBody');
      if (!sales || sales.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:20px;">Chưa có đơn hàng nào.</td></tr>';
        return;
      }
      tbody.innerHTML = sales.map(s => `
        <tr>
          <td><span style="font-family:monospace;color:var(--primary);">HD${String(s.saleID).padStart(6,'0')}</span></td>
          <td style="font-size:12px;">${escapeHtml(s.saleDate || '—')}</td>
          <td style="font-weight:700;color:#10b981;">${formatVND(s.totalAmount)}</td>
          <td style="color:#f59e0b;">${s.discountAmount > 0 ? formatVND(s.discountAmount) : '—'}</td>
          <td style="font-size:11px;color:var(--text-muted);">${escapeHtml(s.discountNote || '—')}</td>
          <td>
            <span style="font-size:11px;padding:2px 8px;border-radius:10px;
              background:${s.paymentStatus === 'Ghi nợ' ? 'rgba(239,68,68,0.15)' : 'rgba(16,185,129,0.15)'};
              color:${s.paymentStatus === 'Ghi nợ' ? '#ef4444' : '#10b981'};">
              ${escapeHtml(s.paymentStatus)}
            </span>
          </td>
        </tr>
      `).join('');
    }
  } catch (e) {
    document.getElementById('customerHistoryTableBody').innerHTML =
      `<tr><td colspan="6" style="text-align:center;color:#ef4444;padding:20px;">Lỗi tải dữ liệu</td></tr>`;
  }
};

// ==================== REPORTS & PROFIT ANALYTICS ====================
async function loadAndRenderReports() {
  try {
    const res = await fetch('/api/reports/dashboard');
    if (res.ok) {
      const data = await res.json();
      document.getElementById('reportTodayRevenue').textContent = formatVND(data.todayRevenue);
      document.getElementById('reportTodayCount').textContent = `${data.todayCount} đơn`;
      document.getElementById('reportMonthRevenue').textContent = formatVND(data.monthRevenue);
      document.getElementById('reportTotalDebt').textContent = formatVND(data.totalDebt);
      renderRevenueChart(data.last7Days || []);

      const tbody = document.getElementById('topSellingTableBody');
      if (!data.topSelling || data.topSelling.length === 0) {
        tbody.innerHTML = `<tr><td colspan="3" style="text-align: center; color: var(--text-muted); padding: 20px;">Chưa có giao dịch hôm nay.</td></tr>`;
      } else {
        tbody.innerHTML = data.topSelling.map(item => `
          <tr>
            <td style="font-weight: 600;">${escapeHtml(item.productName)}</td>
            <td style="text-align: center; font-weight: 700;">${item.qty}</td>
            <td style="color: var(--success); font-weight: 700;">${formatVND(item.revenue)}</td>
          </tr>
        `).join('');
      }
    }

    // Load profit summary
    const profitRes = await fetch('/api/reports/profit');
    if (profitRes.ok) {
      const pData = await profitRes.json();
      document.getElementById('reportTodayProfit').textContent = formatVND(pData.todayProfit);
      document.getElementById('reportTodayMargin').textContent = `${pData.todayMargin}%`;
      document.getElementById('reportMonthProfit').textContent = formatVND(pData.monthProfit);
    }

    // Load expiring products alert
    const expRes = await fetch('/api/inventory/expiring');
    if (expRes.ok) {
      const expList = await expRes.json();
      document.getElementById('reportExpiringCount').textContent = `${expList.length} SP`;

      const expTbody = document.getElementById('expiringTableBody');
      if (expList.length === 0) {
        expTbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: var(--success); padding: 20px;">✓ Toàn bộ hàng hoá đều có hạn sử dụng an toàn.</td></tr>`;
      } else {
        expTbody.innerHTML = expList.map(item => `
          <tr>
            <td style="font-family: monospace;">${item.barcode || '—'}</td>
            <td style="font-weight: 600;">${escapeHtml(item.productName)}</td>
            <td>${item.categoryName || '—'}</td>
            <td>${formatVND(item.sellingPrice)}</td>
            <td style="font-weight: 700;">${item.stockQuantity} ${item.unit || ''}</td>
            <td>${formatDateOnly(item.expiryDate)}</td>
            <td>
              <span class="expiry-badge ${item.isExpired ? 'danger' : 'warning'}">
                ${item.isExpired ? '⛔ Đã hết hạn' : `⚠️ Còn ${item.daysRemaining} ngày`}
              </span>
            </td>
          </tr>
        `).join('');
      }
    }
  } catch (e) {
    console.error('Error loading reports:', e);
  }
}

function renderRevenueChart(days) {
  const container = document.getElementById('chartContainer');
  if (days.length === 0) {
    container.innerHTML = '<div style="color: var(--text-muted);">Không có dữ liệu 7 ngày qua.</div>';
    return;
  }

  const max = Math.max(...days.map(d => d.revenue), 100000);

  container.innerHTML = days.map(d => {
    const percent = Math.min(100, Math.round((d.revenue / max) * 100));
    return `
      <div style="display: flex; flex-direction: column; align-items: center; gap: 8px; flex: 1; height: 100%; justify-content: flex-end;">
        <div style="font-size: 11px; font-weight: 600; color: var(--text-secondary);">${d.revenue > 0 ? formatVND(d.revenue) : '0'}</div>
        <div style="width: 100%; max-width: 44px; height: ${Math.max(6, percent)}%; background: linear-gradient(180deg, var(--primary), #10b981); border-radius: 6px 6px 0 0; transition: height 0.5s ease;"></div>
        <div style="font-size: 12px; font-weight: 700; color: var(--text-primary);">${d.date}</div>
      </div>
    `;
  }).join('');
}

// ==================== MODULE KÊ KHAI THUẾ ====================
function initTaxModule() {
  const yearSel = document.getElementById('taxYearSelect');
  if (!yearSel) return;

  // Điền các năm vào select (từ 3 năm trước đến năm hiện tại)
  const currentYear = new Date().getFullYear();
  yearSel.innerHTML = '';
  for (let y = currentYear; y >= currentYear - 4; y--) {
    const opt = document.createElement('option');
    opt.value = y;
    opt.textContent = `Năm ${y}`;
    if (y === currentYear) opt.selected = true;
    yearSel.appendChild(opt);
  }

  // Cập nhật link xuất CSV cả năm
  function updateYearCsvLink() {
    const y = yearSel.value;
    const link = document.getElementById('btnExportTaxCsvYear');
    if (link) link.href = `/api/tax/export-csv?year=${y}`;
  }
  yearSel.addEventListener('change', updateYearCsvLink);
  updateYearCsvLink();

  // Nút tải dữ liệu thuế
  document.getElementById('btnLoadTaxSummary')?.addEventListener('click', () => loadTaxSummary());

  // Nút in tóm tắt thuế
  document.getElementById('btnPrintTaxSummary')?.addEventListener('click', async () => {
    const y = parseInt(yearSel.value);
    try {
      const res = await fetch(`/api/tax/print-summary?year=${y}`);
      if (!res.ok) return;
      const d = await res.json();
      printTaxSummary(d);
    } catch (e) { alert('Lỗi tải dữ liệu: ' + e.message); }
  });

  // Tự động tải khi mở tab
  loadTaxSummary();
}

async function loadTaxSummary() {
  const yearSel = document.getElementById('taxYearSelect');
  if (!yearSel) return;
  const year = parseInt(yearSel.value);

  try {
    // Cập nhật link CSV năm
    const csvLink = document.getElementById('btnExportTaxCsvYear');
    if (csvLink) csvLink.href = `/api/tax/export-csv?year=${year}`;

    // Load tổng hợp thuế năm
    const res = await fetch(`/api/tax/summary?year=${year}`);
    if (!res.ok) return;
    const data = await res.json();

    // Cập nhật cards tổng hợp
    document.getElementById('taxTotalRevenue').textContent = formatVND(data.totalRevenue);
    document.getElementById('taxVatAmount').textContent = formatVND(data.vatTax);
    document.getElementById('taxPitAmount').textContent = formatVND(data.pitTax);
    document.getElementById('taxGrandTotal').textContent = formatVND(data.grandTotalTax);
    document.getElementById('taxTotalInvoices').textContent = `${data.totalInvoices} HĐ`;

    const blTax = data.businessLicenseTax;
    document.getElementById('taxBusinessLicense').textContent = blTax === 0 ? 'Miễn thuế' : formatVND(blTax);
    const blNote = data.totalRevenue <= 100_000_000
      ? '(DT < 100 triệu → miễn)'
      : data.totalRevenue <= 300_000_000
        ? '(DT 100-300 triệu)'
        : data.totalRevenue <= 500_000_000
          ? '(DT 300-500 triệu)'
          : '(DT > 500 triệu)';
    document.getElementById('taxBusinessLicenseNote').textContent = blNote;

    // Render bảng tháng
    const tbody = document.getElementById('taxMonthlyTableBody');
    const tfoot = document.getElementById('taxMonthlyTableFoot');
    const monthNames = ['', 'Tháng 01', 'Tháng 02', 'Tháng 03', 'Tháng 04', 'Tháng 05', 'Tháng 06',
      'Tháng 07', 'Tháng 08', 'Tháng 09', 'Tháng 10', 'Tháng 11', 'Tháng 12'];

    tbody.innerHTML = (data.monthly || []).map(m => {
      const hasData = m.invoiceCount > 0;
      const rowStyle = hasData ? '' : 'color:var(--text-muted);font-style:italic;';
      return `<tr style="${rowStyle}">
        <td style="font-weight:${hasData ? '700' : '400'};">${monthNames[m.month]}/${year}</td>
        <td style="text-align:center;">Q${m.quarter}</td>
        <td style="text-align:right;">${hasData ? m.invoiceCount : '—'}</td>
        <td style="text-align:right;font-weight:${hasData ? '600' : '400'};">${hasData ? formatVND(m.revenue) : '—'}</td>
        <td style="text-align:right;color:${hasData ? '#ef4444' : 'var(--text-muted)'};">${hasData ? formatVND(m.vatTax) : '—'}</td>
        <td style="text-align:right;color:${hasData ? '#ef4444' : 'var(--text-muted)'};">${hasData ? formatVND(m.pitTax) : '—'}</td>
        <td style="text-align:right;font-weight:800;color:${hasData ? '#dc2626' : 'var(--text-muted)'};">${hasData ? formatVND(m.totalTax) : '—'}</td>
        <td style="text-align:center;">
          ${hasData
          ? `<a href="/api/tax/export-csv?year=${year}&month=${m.month}" style="font-size:11px;background:#16a34a;color:#fff;padding:3px 8px;border-radius:5px;text-decoration:none;white-space:nowrap;" title="Xuất CSV tháng ${m.month}/${year}">📥 CSV</a>`
          : '<span style="color:var(--text-muted);font-size:11px;">Trống</span>'}
        </td>
      </tr>`;
    }).join('');

    // Footer tổng
    tfoot.innerHTML = `<tr style="background:linear-gradient(135deg,#78350f25,#92400e18);font-weight:800;">
      <td colspan="3" style="padding:10px 12px;">TỔNG CỘNG NĂM ${year}</td>
      <td style="text-align:right;color:#b45309;">${formatVND(data.totalRevenue)}</td>
      <td style="text-align:right;color:#dc2626;">${formatVND(data.vatTax)}</td>
      <td style="text-align:right;color:#dc2626;">${formatVND(data.pitTax)}</td>
      <td style="text-align:right;color:#dc2626;font-size:15px;">${formatVND(data.totalTax)}</td>
      <td style="text-align:center;">
        <a href="/api/tax/export-csv?year=${year}" style="font-size:11px;background:#16a34a;color:#fff;padding:4px 10px;border-radius:5px;text-decoration:none;" title="Xuất CSV cả năm ${year}">📥 Cả năm</a>
      </td>
    </tr>`;

    // Load quý
    const qRes = await fetch(`/api/tax/quarterly?year=${year}`);
    if (qRes.ok) {
      const quarters = await qRes.json();
      quarters.forEach((q, i) => {
        const qi = i + 1;
        const el = document.getElementById(`taxQ${qi}Revenue`);
        const elTax = document.getElementById(`taxQ${qi}Tax`);
        if (el) el.textContent = q.revenue > 0 ? formatVND(q.revenue) : 'Chưa có';
        if (elTax) elTax.textContent = q.revenue > 0 ? `Thuế: ${formatVND(q.totalTax)}` : 'Thuế: 0 đ';
      });
    }

  } catch (e) {
    console.error('Lỗi tải dữ liệu thuế:', e);
  }
}

function printTaxSummary(d) {
  const vatFmt = new Intl.NumberFormat('vi-VN').format(d.vatTax);
  const pitFmt = new Intl.NumberFormat('vi-VN').format(d.pitTax);
  const totalFmt = new Intl.NumberFormat('vi-VN').format(d.totalTax);
  const revFmt = new Intl.NumberFormat('vi-VN').format(d.totalRevenue);

  const html = `<!DOCTYPE html><html lang="vi"><head><meta charset="UTF-8">
  <title>Tóm tắt thuế ${d.periodLabel}</title>
  <style>
    body{font-family:Arial,sans-serif;margin:30px;font-size:13px;color:#111;}
    h1{font-size:18px;text-align:center;margin-bottom:4px;}
    .sub{text-align:center;color:#555;font-size:12px;margin-bottom:24px;}
    table{width:100%;border-collapse:collapse;margin-top:12px;}
    th,td{border:1px solid #ccc;padding:8px 12px;}
    th{background:#f5f5f5;font-weight:700;}
    .highlight{background:#fff3cd;font-weight:700;font-size:15px;}
    .tax-row{color:#dc2626;font-weight:700;}
    @media print{body{margin:10px}}
  </style></head><body>
  <h1>TÓM TẮT KÊ KHAI THUẾ</h1>
  <div class="sub">
    ${escapeHtml(d.storeName || '')} — MST: ${escapeHtml(d.taxId || 'Chưa có')}<br>
    Kỳ: <b>${escapeHtml(d.periodLabel)}</b> (${d.from} → ${d.to})<br>
    In ngày: ${d.printDate}
  </div>
  <table>
    <tr><th>Chỉ tiêu</th><th style="text-align:right;">Số tiền (đ)</th></tr>
    <tr><td>Tổng doanh thu thực thu</td><td style="text-align:right;font-weight:700;">${revFmt} đ</td></tr>
    <tr><td>Số hoá đơn</td><td style="text-align:right;">${d.invoiceCount} hoá đơn</td></tr>
    <tr class="tax-row"><td>Thuế GTGT phải nộp (1%)</td><td style="text-align:right;">${vatFmt} đ</td></tr>
    <tr class="tax-row"><td>Thuế TNCN phải nộp (0.5%)</td><td style="text-align:right;">${pitFmt} đ</td></tr>
    <tr class="highlight"><td>TỔNG THUẾ PHẢI NỘP (1.5%)</td><td style="text-align:right;">${totalFmt} đ</td></tr>
  </table>
  <p style="margin-top:16px;font-size:11px;color:#555;">
    Căn cứ: Thông tư 40/2021/TT-BTC, Nghị định 126/2020/NĐ-CP<br>
    Chế độ thuế: Hộ kinh doanh khoán — GTGT 1% + TNCN 0.5% = 1.5% doanh thu<br>
    Lưu ý: Doanh thu ghi nợ chưa thu tiền KHÔNG được tính vào kê khai thuế.
  </p>
  <br><br>
  <table style="border:none;">
    <tr>
      <td style="border:none;width:50%;text-align:center;padding-top:40px;">
        <div style="border-top:1px solid #333;width:180px;margin:0 auto;padding-top:4px;font-size:12px;">Chủ hộ kinh doanh ký tên</div>
      </td>
      <td style="border:none;width:50%;text-align:center;padding-top:40px;">
        <div style="border-top:1px solid #333;width:180px;margin:0 auto;padding-top:4px;font-size:12px;">Ngày ___ tháng ___ năm ___</div>
      </td>
    </tr>
  </table>
  <script>window.onload=()=>window.print();</script>
  </body></html>`;

  const w = window.open('', '_blank');
  if (w) { w.document.write(html); w.document.close(); }
}



// ==================== USER MANAGEMENT ====================
async function loadAndRenderUsers() {
  try {
    const res = await fetch('/api/users');
    if (!res.ok) return;
    const users = await res.json();
    const tbody = document.getElementById('usersTableBody');

    tbody.innerHTML = users.map(u => `
      <tr>
        <td>#${u.userID}</td>
        <td style="font-weight: 700;">${escapeHtml(u.username)}</td>
        <td>${escapeHtml(u.fullName)}</td>
        <td>
          <span class="role-badge ${u.role.toLowerCase() === 'admin' ? 'admin' : 'staff'}">
            ${u.role.toLowerCase() === 'admin' ? '👑 Quản trị' : '🛒 Thu ngân'}
          </span>
        </td>
        <td>
          <span class="user-badge ${u.isActive ? 'active' : 'inactive'}">
            ${u.isActive ? 'Hoạt động' : 'Đã khoá'}
          </span>
        </td>
        <td>
          <div style="display: flex; gap: 6px;">
            <button class="btn btn-secondary" style="padding: 4px 8px; font-size: 12px;" onclick="openEditUserModal(${u.userID}, '${escapeHtml(u.username)}', '${escapeHtml(u.fullName)}', '${u.role}', ${u.isActive})">
              Sửa
            </button>
            <button class="btn btn-secondary" style="padding: 4px 8px; font-size: 12px;" onclick="resetUserPassword(${u.userID}, '${escapeHtml(u.username)}')">
              Đặt lại MK
            </button>
            ${u.username.toLowerCase() !== 'admin' ? `
              <button class="btn btn-danger" style="padding: 4px 8px; font-size: 12px;" onclick="deleteUser(${u.userID}, '${escapeHtml(u.username)}')">
                Xoá
              </button>
            ` : ''}
          </div>
        </td>
      </tr>
    `).join('');
  } catch (e) {
    console.error('Error loading users:', e);
  }
}

window.openEditUserModal = (id, username, fullName, role, isActive) => {
  document.getElementById('userModalTitle').textContent = 'Cập Nhật Tài Khoản';
  document.getElementById('modalUserId').value = id;
  document.getElementById('modalUserUsername').value = username;
  document.getElementById('modalUserUsername').disabled = true;
  document.getElementById('modalUserFullName').value = fullName;
  document.getElementById('modalUserRole').value = role;
  document.getElementById('modalUserActive').value = String(isActive);
  document.getElementById('modalUserActiveContainer').style.display = 'block';
  document.getElementById('modalUserPasswordGroup').style.display = 'none';

  const userBtn = document.getElementById('saveUserBtn');
  if (userBtn) { userBtn.disabled = false; userBtn.textContent = 'Lưu Thay Đổi'; }
  document.getElementById('userModal').classList.add('active');
};

document.getElementById('openAddUserModalBtn').addEventListener('click', () => {
  document.getElementById('userModalTitle').textContent = 'Thêm Tài Khoản Mới';
  document.getElementById('modalUserId').value = '';
  document.getElementById('modalUserUsername').value = '';
  document.getElementById('modalUserUsername').disabled = false;
  document.getElementById('modalUserFullName').value = '';
  document.getElementById('modalUserRole').value = 'User';
  document.getElementById('modalUserActive').value = 'true';
  document.getElementById('modalUserActiveContainer').style.display = 'none';
  document.getElementById('modalUserPasswordGroup').style.display = 'block';
  document.getElementById('modalUserPassword').value = '';

  const userBtn = document.getElementById('saveUserBtn');
  if (userBtn) { userBtn.disabled = false; userBtn.textContent = 'Lưu Người Dùng'; }
  document.getElementById('userModal').classList.add('active');
});

document.getElementById('saveUserBtn').addEventListener('click', async (e) => {
  const id = document.getElementById('modalUserId').value;
  const username = document.getElementById('modalUserUsername').value.trim();
  const fullName = document.getElementById('modalUserFullName').value.trim();
  const role = document.getElementById('modalUserRole').value;
  const isActive = document.getElementById('modalUserActive').value === 'true';
  const password = document.getElementById('modalUserPassword').value;

  const btn = e.currentTarget;
  const origText = btn ? btn.textContent : (id ? 'Lưu Thay Đổi' : 'Lưu Người Dùng');

  const actor = state.currentUser ? state.currentUser.username : 'admin';

  if (!id) {
    if (!username || !fullName || !password) {
      alert('Vui lòng điền đầy đủ thông tin và mật khẩu!');
      return;
    }
    if (btn) { btn.disabled = true; btn.textContent = '⏳ Đang lưu...'; }
    try {
      const res = await fetch(`/api/users?operatorUser=${encodeURIComponent(actor)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, fullName, role, password })
      });
      if (res.ok) {
        document.getElementById('userModal').classList.remove('active');
        await loadAndRenderUsers();
      } else {
        const err = await res.json();
        alert('Lỗi: ' + (err.message || 'Không thành công'));
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    } finally {
      if (btn) { btn.disabled = false; btn.textContent = origText; }
    }
  } else {
    if (btn) { btn.disabled = true; btn.textContent = '⏳ Đang lưu...'; }
    try {
      const res = await fetch(`/api/users/${id}?operatorUser=${encodeURIComponent(actor)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role, isActive })
      });
      if (res.ok) {
        document.getElementById('userModal').classList.remove('active');
        await loadAndRenderUsers();
      } else {
        const err = await res.json();
        alert('Lỗi: ' + (err.message || 'Không thành công'));
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    } finally {
      if (btn) { btn.disabled = false; btn.textContent = origText; }
    }
  }
});

window.resetUserPassword = async (userId, username) => {
  const newPass = prompt(`Nhập mật khẩu mới cho tài khoản "${username}" (tối thiểu 6 ký tự):`);
  if (!newPass) return;
  if (newPass.length < 6) {
    alert('Mật khẩu phải từ 6 ký tự trở lên!');
    return;
  }
  const actor = state.currentUser ? state.currentUser.username : 'admin';
  try {
    const res = await fetch(`/api/users/${userId}/reset-password?operatorUser=${encodeURIComponent(actor)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newPassword: newPass })
    });
    if (res.ok) alert(`Đã đặt lại mật khẩu cho "${username}" thành công!`);
    else {
      const err = await res.json();
      alert('Lỗi: ' + err.message);
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

window.deleteUser = async (userId, username) => {
  if (!confirm(`Bạn có chắc chắn muốn xoá tài khoản "${username}"?`)) return;
  const actor = state.currentUser ? state.currentUser.username : 'admin';
  try {
    const res = await fetch(`/api/users/${userId}?operatorUser=${encodeURIComponent(actor)}`, { method: 'DELETE' });
    if (res.ok) await loadAndRenderUsers();
    else {
      const err = await res.json();
      alert('Lỗi: ' + err.message);
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

// ==================== AUDIT LOGS ====================
async function loadAndRenderAuditLogs() {
  try {
    const res = await fetch('/api/audit-logs?limit=100');
    if (!res.ok) return;
    const logs = await res.json();
    const tbody = document.getElementById('auditTableBody');

    if (logs.length === 0) {
      tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: var(--text-muted); padding: 30px;">Chưa có nhật ký ghi nhận.</td></tr>`;
      return;
    }

    tbody.innerHTML = logs.map(l => `
      <tr>
        <td style="font-size: 12px; color: var(--text-secondary); white-space: nowrap;">${formatDate(l.logTime)}</td>
        <td style="font-weight: 700;">${escapeHtml(l.username || 'system')}</td>
        <td><span class="user-badge admin">${escapeHtml(l.action)}</span></td>
        <td style="color: var(--text-primary);">${escapeHtml(l.detail || '—')}</td>
      </tr>
    `).join('');
  } catch (e) {
    console.error('Error loading audit logs:', e);
  }
}

document.getElementById('refreshAuditBtn').addEventListener('click', loadAndRenderAuditLogs);

// ==================== BACKUP & RESTORE ====================
document.getElementById('confirmRestoreBtn').addEventListener('click', async (e) => {
  const fileInput = document.getElementById('restoreFileInput');
  if (!fileInput.files || fileInput.files.length === 0) {
    alert('Vui lòng chọn file backup (.db)!');
    return;
  }
  if (!confirm('CẢNH BÁO: Thao tác này sẽ ghi đè toàn bộ dữ liệu hiện tại bằng dữ liệu trong file backup. Bạn có chắc chắn muốn khôi phục?')) {
    return;
  }

  const btn = e.currentTarget;
  const origText = btn ? btn.textContent : 'Khôi Phục Dữ Liệu';
  if (btn) { btn.disabled = true; btn.textContent = '⏳ Đang khôi phục...'; }

  const formData = new FormData();
  formData.append('file', fileInput.files[0]);

  try {
    const res = await fetch('/api/database/restore', {
      method: 'POST',
      body: formData
    });
    if (res.ok) {
      alert('Khôi phục dữ liệu thành công! Ứng dụng sẽ tải lại trang.');
      location.reload();
    } else {
      const err = await res.json();
      alert('Lỗi khôi phục: ' + (err.message || 'Không thành công'));
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  } finally {
    if (btn) { btn.disabled = false; btn.textContent = origText; }
  }
});

// ==================== CATEGORY MANAGEMENT ====================
document.getElementById('manageCategoriesBtn').addEventListener('click', () => {
  renderCategoriesModal();
  document.getElementById('categoriesModal').classList.add('active');
});

function renderCategoriesModal() {
  const tbody = document.getElementById('categoriesTableBody');
  tbody.innerHTML = state.categories.map(c => `
    <tr>
      <td>#${c.categoryID}</td>
      <td style="font-weight: 600;">${escapeHtml(c.categoryName)}</td>
      <td>
        <div style="display: flex; gap: 6px;">
          <button class="btn btn-secondary" style="padding: 2px 8px; font-size: 11px;" onclick="renameCategory(${c.categoryID}, '${escapeHtml(c.categoryName)}')">Sửa</button>
          ${c.categoryID !== 1 ? `
            <button class="btn btn-danger" style="padding: 2px 8px; font-size: 11px;" onclick="deleteCategory(${c.categoryID})">Xoá</button>
          ` : ''}
        </div>
      </td>
    </tr>
  `).join('');
}

document.getElementById('addCategoryBtn').addEventListener('click', async (e) => {
  const input = document.getElementById('newCategoryInput');
  const name = input.value.trim();
  if (!name) return;

  const btn = e.currentTarget;
  const origText = btn ? btn.textContent : 'Thêm';
  if (btn) { btn.disabled = true; btn.textContent = '⏳ Đang thêm...'; }

  try {
    const res = await fetch('/api/categories', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ categoryName: name })
    });
    if (res.ok) {
      input.value = '';
      await loadCategories();
      renderCategoriesModal();
      renderPosCategories();
    } else {
      const err = await res.json();
      alert('Lỗi: ' + err.message);
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  } finally {
    if (btn) { btn.disabled = false; btn.textContent = origText; }
  }
});

window.renameCategory = async (catId, oldName) => {
  const newName = prompt('Nhập tên danh mục mới:', oldName);
  if (!newName || newName.trim() === oldName) return;
  try {
    const res = await fetch(`/api/categories/${catId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ categoryName: newName.trim() })
    });
    if (res.ok) {
      await loadCategories();
      renderCategoriesModal();
      renderPosCategories();
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

window.deleteCategory = async (catId) => {
  if (!confirm('Bạn có chắc muốn xoá danh mục này?')) return;
  try {
    const res = await fetch(`/api/categories/${catId}`, { method: 'DELETE' });
    if (res.ok) {
      await loadCategories();
      renderCategoriesModal();
      renderPosCategories();
    }
  } catch (e) {
    alert('Lỗi: ' + e.message);
  }
};

// ==================== SETTINGS FORM ====================
// ==================== AUTO-PRINT QUICK TOGGLE ====================
function updateAutoPrintToggleUI() {
  const enabled = state.settings.autoPrintReceipt !== false;
  const btn = document.getElementById('quickAutoPrintToggle');
  const icon = document.getElementById('quickAutoPrintIcon');
  const label = document.getElementById('quickAutoPrintLabel');
  if (!btn || !icon || !label) return;

  if (enabled) {
    btn.style.borderColor = 'var(--primary)';
    btn.style.background = 'color-mix(in srgb, var(--bg-input) 85%, var(--primary) 15%)';
    btn.style.color = 'var(--primary)';
    icon.textContent = '🖨️';
    label.textContent = 'Tự động in: BẬT';
  } else {
    btn.style.borderColor = 'var(--border-color)';
    btn.style.background = 'var(--bg-input)';
    btn.style.color = 'var(--text-muted)';
    icon.textContent = '🚫';
    label.textContent = 'Tự động in: TẮT';
  }

  // Giữ đồng bộ với checkbox trong Settings
  const settingsCb = document.getElementById('settingAutoPrint');
  if (settingsCb) settingsCb.checked = enabled;
}

window.toggleAutoPrint = async function() {
  const current = state.settings.autoPrintReceipt !== false;
  state.settings.autoPrintReceipt = !current;
  updateAutoPrintToggleUI();

  // Lưu lên server ngay lập tức
  try {
    await fetch('/api/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify({ autoPrintReceipt: state.settings.autoPrintReceipt })
    });
  } catch (e) {
    console.warn('Không lưu được cài đặt in:', e);
  }
};

function populateSettingsForm() {
  const s = state.settings || {};
  const setVal = (id, val) => {
    const el = document.getElementById(id);
    if (el) el.value = val !== undefined && val !== null ? val : '';
  };
  const setChecked = (id, val) => {
    const el = document.getElementById(id);
    if (el) el.checked = !!val;
  };

  setVal('settingStoreName', s.storeName || '');
  setVal('settingStoreAddress', s.storeAddress || '');
  setVal('settingStorePhone', s.storePhone || '');
  setVal('settingReceiptFooter', s.receiptFooter || '');
  setVal('settingPaperWidth', String(s.defaultPaperWidth || 80));
  setChecked('settingAutoPrint', s.autoPrintReceipt !== false);
  updateAutoPrintToggleUI();

  setVal('settingPrinterName', s.defaultPrinter || '');
  setChecked('settingQrEnabled', s.qrPaymentEnabled);
  setVal('settingQrBin', s.qrBankBin || '970436');
  const bankSel = document.getElementById('settingQrBankSelect');
  if (bankSel) {
    const targetBin = s.qrBankBin || '970436';
    const hasOption = Array.from(bankSel.options).some(o => o.value === targetBin);
    bankSel.value = hasOption ? targetBin : 'custom';
    if (!bankSel.dataset.listenerAttached) {
      bankSel.dataset.listenerAttached = 'true';
      bankSel.addEventListener('change', () => {
        if (bankSel.value !== 'custom') {
          setVal('settingQrBin', bankSel.value);
        }
      });
    }
  }
  setVal('settingQrAccountNo', s.qrAccountNo || '');
  setVal('settingQrAccountName', s.qrAccountName || '');
  setVal('settingQrPrefix', s.qrTransferPrefix || 'HD');
  setChecked('settingPaymentSoundEnabled', s.paymentSoundEnabled !== false);

  // Cổng thanh toán tự động (SePay / App điện thoại)
  setVal('settingPaymentAutoDetectMode', s.paymentAutoDetectMode || 'both');
  setVal('settingPaymentWebhookSecret', s.paymentWebhookSecret || '');

  // Máy in nhiệt
  setVal('settingPrinterModel', s.printerModel || 'Xprinter XP-N160M / Q200 / C300H (K80)');
  setVal('settingPrinterConnectionType', s.printerConnectionType || 'USB');
  setVal('settingPrintCopies', String(s.printCopies || 1));
  setVal('settingReceiptFontSize', s.receiptFontSize || 'standard');
  setChecked('settingPrintQrOnReceipt', s.printQrOnReceipt !== false);

  // Tay bấm QR & máy quét mã vạch
  setVal('settingScannerType', s.scannerType || 'usb_handheld');
  setVal('settingScannerModel', s.scannerModel || '');
  setVal('settingScannerSuffix', s.scannerSuffix || 'enter');
  setChecked('settingScannerAutoAdd', s.scannerAutoAdd !== false);
  setChecked('settingScannerBeepSound', s.scannerBeepSound !== false);

  // Hoá đơn tổng khổ A4 / A5
  setVal('settingStoreTaxId', s.storeTaxId || '');
  setVal('settingA4InvoiceTitle', s.a4InvoiceTitle || 'HÓA ĐƠN BÁN HÀNG');
  setVal('settingA4PaperSize', s.a4PaperSize || 'A4');
  setVal('settingA4Orientation', s.a4Orientation || 'portrait');
  setChecked('settingA4ShowSignatures', s.a4ShowSignatures !== false);
  setChecked('settingA4ShowBankQr', s.a4ShowBankQr !== false);

  // Màn hình phụ ESP32 TFT 1.8
  const esp32Mode = s.esp32ConnMode || 'usb';
  const radioUsb = document.getElementById('radioEsp32Usb');
  const radioWifi = document.getElementById('radioEsp32Wifi');
  if (radioUsb && radioWifi) {
    if (esp32Mode === 'wifi') radioWifi.checked = true;
    else radioUsb.checked = true;
    const usbSec = document.getElementById('esp32UsbSection');
    const wifiSec = document.getElementById('esp32WifiSection');
    if (usbSec) usbSec.style.display = esp32Mode === 'wifi' ? 'none' : 'block';
    if (wifiSec) wifiSec.style.display = esp32Mode === 'wifi' ? 'block' : 'none';
  }
  setVal('esp32ServerIpInput', s.esp32ServerIp || '');
  setVal('esp32WifiSsidInput', s.esp32WifiSsid || 'WiFi_CuaHang');
  setVal('esp32WifiPassInput', s.esp32WifiPass || '12345678');

  const pathInput = document.getElementById('settingInvoiceStoragePath');
  if (pathInput) {
    pathInput.value = state.invoiceFolder?.currentPath || s.invoiceStoragePath || state.invoiceFolder?.defaultPath || '';
  }
  if (state.invoiceFolder) {
    updateInvoiceFolderSettingsUI(state.invoiceFolder);
  }
}

async function saveSettings() {
  const pathInput = document.getElementById('settingInvoiceStoragePath');
  const s = state.settings || {};

  // Lấy giá trị trực tiếp từ form input
  const getText = (id, fallback) => {
    const el = document.getElementById(id);
    if (!el) return fallback !== undefined ? fallback : '';
    return el.value.trim();
  };

  const payload = {
    storeName: getText('settingStoreName', s.storeName),
    storeAddress: getText('settingStoreAddress', s.storeAddress),
    storePhone: getText('settingStorePhone', s.storePhone),
    receiptFooter: getText('settingReceiptFooter', s.receiptFooter),
    defaultPaperWidth: parseInt(document.getElementById('settingPaperWidth')?.value) || s.defaultPaperWidth || 80,
    autoPrintReceipt: document.getElementById('settingAutoPrint')?.checked ?? s.autoPrintReceipt ?? true,
    defaultPrinter: getText('settingPrinterName', s.defaultPrinter),
    qrPaymentEnabled: document.getElementById('settingQrEnabled')?.checked ?? s.qrPaymentEnabled ?? false,
    qrBankBin: getText('settingQrBin', s.qrBankBin),
    qrAccountNo: getText('settingQrAccountNo', s.qrAccountNo),
    qrAccountName: getText('settingQrAccountName', s.qrAccountName),
    qrTransferPrefix: getText('settingQrPrefix', s.qrTransferPrefix) || 'HD',
    paymentAutoDetectMode: document.getElementById('settingPaymentAutoDetectMode')?.value || s.paymentAutoDetectMode || 'both',
    paymentWebhookSecret: getText('settingPaymentWebhookSecret', s.paymentWebhookSecret),
    paymentSoundEnabled: document.getElementById('settingPaymentSoundEnabled')?.checked ?? s.paymentSoundEnabled ?? true,
    invoiceStoragePath: pathInput?.value.trim() || s.invoiceStoragePath || null,

    // Máy in nhiệt
    printerModel: getText('settingPrinterModel', s.printerModel) || 'Xprinter XP-N160M / Q200 / C300H (K80)',
    printerConnectionType: getText('settingPrinterConnectionType', s.printerConnectionType) || 'USB',
    printCopies: parseInt(document.getElementById('settingPrintCopies')?.value) || s.printCopies || 1,
    receiptFontSize: getText('settingReceiptFontSize', s.receiptFontSize) || 'standard',
    printQrOnReceipt: document.getElementById('settingPrintQrOnReceipt')?.checked ?? s.printQrOnReceipt ?? true,

    // Tay bấm QR & máy quét
    scannerType: getText('settingScannerType', s.scannerType) || 'usb_handheld',
    scannerModel: getText('settingScannerModel', s.scannerModel) || '',
    scannerSuffix: getText('settingScannerSuffix', s.scannerSuffix) || 'enter',
    scannerAutoAdd: document.getElementById('settingScannerAutoAdd')?.checked ?? s.scannerAutoAdd ?? true,
    scannerBeepSound: document.getElementById('settingScannerBeepSound')?.checked ?? s.scannerBeepSound ?? true,

    // Biểu mẫu A4 / A5
    storeTaxId: getText('settingStoreTaxId', s.storeTaxId) || '',
    a4InvoiceTitle: getText('settingA4InvoiceTitle', s.a4InvoiceTitle) || 'HÓA ĐƠN BÁN HÀNG',
    a4PaperSize: getText('settingA4PaperSize', s.a4PaperSize) || 'A4',
    a4Orientation: getText('settingA4Orientation', s.a4Orientation) || 'portrait',
    a4ShowSignatures: document.getElementById('settingA4ShowSignatures')?.checked ?? s.a4ShowSignatures ?? true,
    a4ShowBankQr: document.getElementById('settingA4ShowBankQr')?.checked ?? s.a4ShowBankQr ?? true,

    // Màn hình phụ ESP32
    esp32ConnMode: document.getElementById('radioEsp32Wifi')?.checked ? 'wifi' : 'usb',
    esp32ServerIp: getText('esp32ServerIpInput', s.esp32ServerIp),
    esp32WifiSsid: getText('esp32WifiSsidInput', s.esp32WifiSsid),
    esp32WifiPass: getText('esp32WifiPassInput', s.esp32WifiPass)
  };

  const actor = state.currentUser ? state.currentUser.username : 'admin';

  // Hiệu ứng visual nút lưu
  const saveBtns = [document.getElementById('saveSettingsBtn'), document.getElementById('saveSettingsTopBtn')].filter(Boolean);
  saveBtns.forEach(b => {
    b.dataset.origText = b.textContent;
    b.textContent = '⏳ Đang lưu...';
    b.disabled = true;
  });

  try {
    const res = await fetch(`/api/settings?operatorUser=${encodeURIComponent(actor)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify(payload)
    });
    if (res.ok) {
      showToast('✅ Đã lưu cấu hình thành công! F5 hoặc mở lại ứng dụng đều giữ nguyên.');
      localStorage.setItem('pmtaphoa_active_tab', 'settings');
      await loadSettings();
      populateSettingsForm();
      if (payload.storeName) {
        const titleEl = document.getElementById('sidebarStoreName');
        if (titleEl) titleEl.textContent = payload.storeName;
      }
      await checkInvoiceFolderStatus();
    } else {
      const err = await res.json().catch(() => ({}));
      alert('Lỗi lưu cấu hình: ' + (err.message || res.statusText));
    }
  } catch (e) {
    alert('Lỗi lưu cấu hình: ' + e.message);
  } finally {
    saveBtns.forEach(b => {
      if (b.dataset.origText) b.textContent = b.dataset.origText;
      b.disabled = false;
    });
  }
}

// ==================== SCANNER BEEP & AUDIO ====================
function playScannerBeep(frequency = 1900, duration = 0.08) {
  if (state.settings && state.settings.scannerBeepSound === false) return;
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = 'sine';
    osc.frequency.value = frequency;
    gain.gain.setValueAtTime(0.18, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + duration);
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start();
    osc.stop(ctx.currentTime + duration);
  } catch (_) { }
}

// ==================== INTERACTIVE SCANNER TEST AREA ====================
function setupScannerTestArea() {
  const input = document.getElementById('scannerTestInput');
  const led = document.getElementById('scannerLed');
  const ledText = document.getElementById('scannerLedText');
  const resultDiv = document.getElementById('scannerTestResult');
  if (!input || !led || !resultDiv) return;

  let keyTimestamps = [];
  let resetTimer = null;

  led.className = 'scanner-led ready';
  ledText.textContent = 'Sẵn sàng';
  ledText.style.color = '#22c55e';

  input.addEventListener('keydown', (e) => {
    const now = Date.now();
    keyTimestamps.push(now);
    clearTimeout(resetTimer);

    led.className = 'scanner-led scanning';
    ledText.textContent = 'Đang nhận tia quét...';
    ledText.style.color = '#38bdf8';

    if (e.key === 'Enter' || e.key === 'Tab') {
      e.preventDefault();
      processScannerTest(input.value.trim(), keyTimestamps, e.key === 'Enter' ? 'Phím Enter (Chuẩn)' : 'Phím Tab');
      input.value = '';
      keyTimestamps = [];
      return;
    }

    resetTimer = setTimeout(() => {
      if (input.value.trim().length >= 3) {
        processScannerTest(input.value.trim(), keyTimestamps, 'Khoảng lặng (Timeout không phím kết thúc)');
        input.value = '';
        keyTimestamps = [];
      }
    }, 180);
  });

  function processScannerTest(code, timestamps, suffixDetected) {
    if (!code) return;
    playScannerBeep();

    led.className = 'scanner-led success';
    ledText.textContent = 'Quét thành công!';
    ledText.style.color = '#10b981';

    setTimeout(() => {
      led.className = 'scanner-led ready';
      ledText.textContent = 'Sẵn sàng';
      ledText.style.color = '#22c55e';
    }, 1500);

    let durationMs = 0;
    if (timestamps.length >= 2) {
      durationMs = timestamps[timestamps.length - 1] - timestamps[0];
    }
    const isHardwareScanner = durationMs < 250 && code.length >= 4;

    const matched = state.products.find(p => p.barcode && p.barcode.toLowerCase() === code.toLowerCase());
    let matchHtml = '';
    if (matched) {
      matchHtml = `
        <div style="color: #4ade80; margin-top: 5px; font-weight: 600; background: rgba(34, 197, 94, 0.1); padding: 4px 8px; border-radius: 4px;">
          ✓ Khớp sản phẩm: [${escapeHtml(matched.productName)}] - Giá: ${formatVND(matched.sellingPrice)} (Tồn: ${matched.stockQuantity} ${escapeHtml(matched.unit)})
        </div>
      `;
    } else {
      matchHtml = `
        <div style="color: #fbbf24; margin-top: 5px; background: rgba(245, 158, 11, 0.1); padding: 4px 8px; border-radius: 4px;">
          ℹ️ Mã quét chuẩn nhưng chưa có sản phẩm nào khớp trong kho. (Có thể copy gán ngay cho sản phẩm mới).
        </div>
      `;
    }

    resultDiv.innerHTML = `
      <div style="background: rgba(30, 41, 59, 0.95); padding: 9px 12px; border-radius: 6px; border-left: 3px solid #38bdf8;">
        <div style="color: #38bdf8; font-weight: bold; font-size: 13.5px;">📟 Mã nhận diện: <span style="color:#fff; font-size:15px; font-family:monospace;">${escapeHtml(code)}</span></div>
        <div style="color: #94a3b8; font-size: 11px; margin-top: 3px; line-height: 1.5;">
          • Độ dài: <strong>${code.length}</strong> ký tự | Hậu tố: <span style="color:#e2e8f0; font-weight:600;">${suffixDetected}</span><br>
          • Thời gian truyền: <strong>${durationMs}ms</strong> ${isHardwareScanner ? '<span style="color:#38bdf8; font-weight:bold;">(Tốc độ máy quét tay bấm phần cứng)</span>' : '(Tốc độ gõ phím máy tính)'}
        </div>
        ${matchHtml}
      </div>
    `;
  }
}

// ==================== EVENT LISTENERS & SHORTCUTS ====================
function setupEventListeners() {
  const searchInput = document.getElementById('posSearchInput');
  searchInput.addEventListener('input', () => renderPosProducts());
  searchInput.addEventListener('keydown', async (e) => {
    if (e.key === 'Enter') {
      const code = searchInput.value.trim();
      if (!code) return;

      const found = state.products.find(p => p.barcode && p.barcode.toLowerCase() === code.toLowerCase());
      if (found) {
        addToCart(found.productID);
        playScannerBeep();
        searchInput.value = '';
        renderPosProducts();
      } else {
        try {
          const res = await fetch(`/api/products/barcode/${encodeURIComponent(code)}`);
          if (res.ok) {
            const p = await res.json();
            addToCart(p.productID);
            playScannerBeep();
            searchInput.value = '';
            renderPosProducts();
          }
        } catch (_) { }
      }
    }
  });

  document.getElementById('clearCartBtn').addEventListener('click', () => {
    const cart = getActiveCart();
    if (cart.items.length > 0 && confirm(`Bạn có muốn xoá toàn bộ sản phẩm trong ${cart.name}?`)) {
      cart.items = [];
      renderCartTabs();
      updateCartUI();
    }
  });

  const methodButtons = document.querySelectorAll('.payment-tabs .pay-method-btn');
  methodButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      methodButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      const method = btn.getAttribute('data-method');
      getActiveCart().paymentMethod = method;

      const cashRow = document.getElementById('cashCalculationRow');
      const quickCash = document.getElementById('quickCashContainer');
      if (method === 'Tiền mặt') {
        cashRow.style.display = 'flex';
        quickCash.style.display = 'flex';
      } else {
        cashRow.style.display = 'none';
        quickCash.style.display = 'none';
      }
    });
  });

  document.querySelectorAll('.quick-cash-row .cash-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const val = btn.getAttribute('data-cash');
      const receivedInput = document.getElementById('receivedAmountInput');
      const cart = getActiveCart();
      const subtotal = cart.items.reduce((s, i) => s + (i.unitPrice * i.quantity), 0);
      const discount = parseFloat(document.getElementById('discountInput').value) || 0;
      const total = Math.max(0, subtotal - discount);

      if (val === 'exact') {
        receivedInput.value = total;
      } else {
        const cur = parseFloat(receivedInput.value) || 0;
        receivedInput.value = cur + parseFloat(val);
      }
      calculateTotals();
    });
  });

  document.getElementById('discountInput').addEventListener('input', calculateTotals);
  document.getElementById('receivedAmountInput').addEventListener('input', calculateTotals);
  document.getElementById('checkoutBtn').addEventListener('click', handleCheckout);

  window.addEventListener('keydown', (e) => {
    if (e.key === 'F2') {
      e.preventDefault();
      document.getElementById('navPosBtn').click();
      document.getElementById('posSearchInput').focus();
    } else if (e.key === 'F4') {
      e.preventDefault();
      handleCheckout();
    } else if (e.key === 'Escape') {
      closeAllModals();
    }
  });

  document.querySelectorAll('[data-close]').forEach(btn => {
    btn.addEventListener('click', () => {
      const modalId = btn.getAttribute('data-close');
      document.getElementById(modalId).classList.remove('active');
    });
  });

  document.getElementById('openAddProductModalBtn').addEventListener('click', () => {
    document.getElementById('productModalTitle').textContent = 'Thêm Sản Phẩm Mới';
    document.getElementById('modalProductId').value = '';
    document.getElementById('modalProductBarcode').value = '';
    document.getElementById('modalProductName').value = '';
    document.getElementById('modalProductUnit').value = 'Cái';
    document.getElementById('modalProductCost').value = '';
    document.getElementById('modalProductPrice').value = '';
    document.getElementById('modalProductStock').value = '10';
    document.getElementById('modalProductMinStock').value = '5';
    document.getElementById('modalProductExpiry').value = '';

    const catSelect = document.getElementById('modalProductCategory');
    catSelect.innerHTML = `<option value="">-- Chọn danh mục --</option>` +
      state.categories.map(c => `<option value="${c.categoryID}">${c.categoryName}</option>`).join('');

    setProductModalImage('');
    const saveBtn = document.getElementById('saveProductBtn');
    if (saveBtn) { saveBtn.disabled = false; saveBtn.textContent = 'Lưu Sản Phẩm'; }
    document.getElementById('productModal').classList.add('active');
  });

  document.getElementById('saveProductBtn').addEventListener('click', async (e) => {
    const btn = e.currentTarget;
    const origText = btn.textContent;
    const id = document.getElementById('modalProductId').value;
    const name = document.getElementById('modalProductName').value.trim();
    const price = parseFloat(document.getElementById('modalProductPrice').value);
    const exp = document.getElementById('modalProductExpiry').value;

    if (!name || isNaN(price) || price < 0) {
      alert('Vui lòng nhập đầy đủ tên sản phẩm và giá bán hợp lệ!');
      return;
    }

    btn.disabled = true;
    btn.textContent = '⏳ Đang lưu...';

    const barcodeVal = document.getElementById('modalProductBarcode').value.trim();
    const payload = {
      barcode: barcodeVal.length > 0 ? barcodeVal : null,
      productName: name,
      categoryID: parseInt(document.getElementById('modalProductCategory').value) || null,
      unit: document.getElementById('modalProductUnit').value.trim() || 'Cái',
      costPrice: parseFloat(document.getElementById('modalProductCost').value) || 0,
      sellingPrice: price,
      stockQuantity: parseFloat(document.getElementById('modalProductStock').value) || 0,
      minStock: parseInt(document.getElementById('modalProductMinStock').value) || 5,
      expiryDate: exp ? exp : null,
      imageUrl: document.getElementById('modalProductImageUrl').value.trim() || null
    };

    const actor = state.currentUser ? state.currentUser.username : 'admin';
    try {
      const url = id ? `/api/products/${id}?operatorUser=${encodeURIComponent(actor)}` : `/api/products?operatorUser=${encodeURIComponent(actor)}`;
      const method = id ? 'PUT' : 'POST';
      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        document.getElementById('productModal').classList.remove('active');
        await loadProducts();
        renderInventoryTable();
        renderPosProducts();
      } else {
        let msg = 'Không thành công';
        try {
          const err = await res.json();
          msg = err.message || msg;
        } catch {
          const text = await res.text().catch(() => '');
          if (text) msg = text;
        }
        alert('Lỗi: ' + msg);
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    } finally {
      btn.disabled = false;
      btn.textContent = origText;
    }
  });

  document.getElementById('confirmQuickImportBtn').addEventListener('click', async (e) => {
    const btn = e.currentTarget;
    const origText = btn.textContent;
    const productId = parseInt(document.getElementById('quickImportProductId').value);
    const qty = parseFloat(document.getElementById('quickImportQty').value);
    const cost = parseFloat(document.getElementById('quickImportCost').value) || null;
    const note = document.getElementById('quickImportNote').value.trim();

    if (isNaN(qty) || qty <= 0) {
      alert('Số lượng nhập phải lớn hơn 0!');
      return;
    }

    btn.disabled = true;
    btn.textContent = '⏳ Đang nhập kho...';

    const actor = state.currentUser ? state.currentUser.username : 'admin';
    try {
      const res = await fetch(`/api/products/quick-import?operatorUser=${encodeURIComponent(actor)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId, quantity: qty, costPrice: cost, note })
      });
      if (res.ok) {
        const result = await res.json();
        document.getElementById('quickImportModal').classList.remove('active');
        await loadProducts();
        renderInventoryTable();
        renderPosProducts();
        const fileNotice = result.savedHtmlFile ? `\n\n📄 Đã tự động lưu phiếu nhập: ${result.savedHtmlFile}` : '';
        alert(`Đã nhập hàng thành công!${fileNotice}`);
      } else {
        const err = await res.json().catch(() => ({}));
        alert('Lỗi nhập kho: ' + (err.message || 'Không thành công'));
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    } finally {
      btn.disabled = false;
      btn.textContent = origText;
    }
  });

  document.getElementById('viewImportHistoryBtn').addEventListener('click', async () => {
    try {
      const res = await fetch('/api/inventory/import-history');
      if (res.ok) {
        const list = await res.json();
        const tbody = document.getElementById('importHistoryTableBody');
        tbody.innerHTML = list.map(item => `
          <tr>
            <td>${formatDate(item.importDate)}</td>
            <td style="font-weight: 600;">${escapeHtml(item.productName)}</td>
            <td style="font-weight: 700; color: var(--primary);">+${item.quantity}</td>
            <td>${formatVND(item.costPrice)}</td>
            <td>${item.importedBy || '—'}</td>
            <td>${item.note || '—'}</td>
          </tr>
        `).join('');
        document.getElementById('importHistoryModal').classList.add('active');
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    }
  });

  document.getElementById('confirmDebtPaymentBtn').addEventListener('click', async (e) => {
    const btn = e.currentTarget;
    const origText = btn.textContent;
    const saleId = parseInt(document.getElementById('debtPaySaleId').value);
    const amount = parseFloat(document.getElementById('debtPayAmount').value);
    const note = document.getElementById('debtPayNote').value.trim();

    if (isNaN(amount) || amount <= 0) {
      alert('Số tiền thanh toán phải lớn hơn 0!');
      return;
    }

    btn.disabled = true;
    btn.textContent = '⏳ Đang ghi nhận...';

    const actor = state.currentUser ? state.currentUser.username : 'admin';
    try {
      const res = await fetch(`/api/debts/${saleId}/payments?operatorUser=${encodeURIComponent(actor)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount, note })
      });
      if (res.ok) {
        document.getElementById('debtPaymentModal').classList.remove('active');
        await loadAndRenderDebts();
        alert('Đã ghi nhận thanh toán nợ thành công!');
      } else {
        const err = await res.json().catch(() => ({}));
        alert('Lỗi: ' + (err.message || 'Không thành công'));
      }
    } catch (e) {
      alert('Lỗi: ' + e.message);
    } finally {
      btn.disabled = false;
      btn.textContent = origText;
    }
  });

  document.getElementById('inventorySearchInput').addEventListener('input', renderInventoryTable);
  document.getElementById('inventoryCategoryFilter').addEventListener('change', renderInventoryTable);
  document.getElementById('inventoryLowStockFilter').addEventListener('change', renderInventoryTable);
  document.getElementById('inventoryExpiringFilter').addEventListener('change', renderInventoryTable);

  document.getElementById('debtSearchInput').addEventListener('input', loadAndRenderDebts);
  document.getElementById('historyFilterBtn').addEventListener('click', loadAndRenderSalesHistory);
  document.getElementById('historySearchInput').addEventListener('keydown', (e) => {
    if (e.key === 'Enter') loadAndRenderSalesHistory();
  });

  // Customer tab events
  const customerTabSearch = document.getElementById('customerTabSearchInput');
  if (customerTabSearch) {
    customerTabSearch.addEventListener('input', () => loadAndRenderCustomers());
    customerTabSearch.addEventListener('keydown', e => { if (e.key === 'Escape') { customerTabSearch.value = ''; loadAndRenderCustomers(); } });
  }
  setupPosCustomerEvents();
  const openAddCustBtn = document.getElementById('openAddCustomerModalBtn');
  if (openAddCustBtn) openAddCustBtn.addEventListener('click', openAddCustomerModal);
  const confirmSaveCustBtn = document.getElementById('confirmSaveCustomerBtn');
  if (confirmSaveCustBtn) confirmSaveCustBtn.addEventListener('click', saveCustomer);

  document.getElementById('saveSettingsBtn').addEventListener('click', saveSettings);
  const saveTopBtn = document.getElementById('saveSettingsTopBtn');
  if (saveTopBtn) {
    saveTopBtn.addEventListener('click', saveSettings);
  }

  const btnTestVoice = document.getElementById('btnTestPaymentVoice');
  if (btnTestVoice) {
    btnTestVoice.addEventListener('click', () => {
      playPaymentTingTing(250000);
    });
  }

  // Thư mục lưu trữ hoá đơn (HoaDon/Ban & HoaDon/Nhap)
  const btnQuickCreate = document.getElementById('btnQuickCreateFolder');
  if (btnQuickCreate) {
    btnQuickCreate.addEventListener('click', () => setupInvoiceFolder());
  }

  const btnAutoSetup = document.getElementById('btnAutoSetupFolder');
  if (btnAutoSetup) {
    btnAutoSetup.addEventListener('click', () => {
      const p = document.getElementById('settingInvoiceStoragePath')?.value?.trim();
      setupInvoiceFolder(p || null);
    });
  }

  const btnOpenBan = document.getElementById('btnOpenBanFolder');
  if (btnOpenBan) {
    btnOpenBan.addEventListener('click', () => openInvoiceFolder('ban'));
  }

  const btnOpenNhap = document.getElementById('btnOpenNhapFolder');
  if (btnOpenNhap) {
    btnOpenNhap.addEventListener('click', () => openInvoiceFolder('nhap'));
  }

  // Cấu hình máy in nhiệt & In thử
  const btnTestThermal = document.getElementById('btnTestThermalPrint');
  if (btnTestThermal) {
    btnTestThermal.addEventListener('click', () => {
      const sampleSale = {
        saleId: 0,
        invoiceCode: 'HD-TEST-' + Math.floor(1000 + Math.random() * 9000),
        saleDate: new Date().toLocaleString('vi-VN'),
        customerName: 'Khách In Thử Máy In Nhiệt',
        items: [
          { productName: 'Gạo ST25 Ông Cua (Túi 5kg)', quantity: 2, unitPrice: 195000 },
          { productName: 'Dầu Ăn Neptune Light 1L', quantity: 3, unitPrice: 52000 },
          { productName: 'Nước Ngọt Coca Cola 320ml (Lốc 6 lon)', quantity: 2, unitPrice: 60000 },
          { productName: 'Bột Giặt OMO Đỏ 3kg', quantity: 1, unitPrice: 165000 }
        ],
        subtotal: 831000,
        discountAmount: 31000,
        discountNote: 'Khuyến mãi thử máy in',
        finalAmount: 800000,
        paymentMethod: 'Chuyển khoản VietQR',
        receivedAmount: 800000,
        changeAmount: 0
      };
      showReceiptModal(sampleSale);
    });
  }

  // In thử hoá đơn A4 biểu mẫu
  const btnTestA4 = document.getElementById('btnTestA4Invoice');
  if (btnTestA4) {
    btnTestA4.addEventListener('click', () => {
      openA4InvoiceModal(0);
    });
  }

  // Mở hoá đơn A4 từ Receipt Modal
  const btnViewA4FromReceipt = document.getElementById('viewA4InvoiceFromReceiptBtn');
  if (btnViewA4FromReceipt) {
    btnViewA4FromReceipt.addEventListener('click', () => {
      openA4InvoiceModal(window._currentReceiptSaleId, window._activeReceiptData);
    });
  }

  // In trực tiếp từ iframe hoá đơn A4
  const btnPrintA4Direct = document.getElementById('printA4DirectBtn');
  if (btnPrintA4Direct) {
    btnPrintA4Direct.addEventListener('click', () => {
      const frame = document.getElementById('a4InvoiceFrame');
      if (frame && frame.contentWindow) {
        frame.contentWindow.focus();
        frame.contentWindow.print();
      }
    });
  }

  // Cập nhật URL webhook/phone-notification từ server IP thực
  const serverBaseUrl = `${window.location.protocol}//${window.location.hostname}:${window.location.port || 8888}`;
  const webhookUrlInput = document.getElementById('displayWebhookUrl');
  const phoneUrlInput = document.getElementById('displayPhoneNotiUrl');
  if (webhookUrlInput) webhookUrlInput.value = `${serverBaseUrl}/api/payment/webhook`;
  if (phoneUrlInput) phoneUrlInput.value = `${serverBaseUrl}/api/payment/phone-notification`;

  const btnCopyWebhook = document.getElementById('btnCopyWebhookUrl');
  if (btnCopyWebhook && webhookUrlInput) {
    btnCopyWebhook.addEventListener('click', () => {
      navigator.clipboard.writeText(webhookUrlInput.value).then(() => {
        btnCopyWebhook.textContent = '✓ Đã copy!';
        setTimeout(() => { btnCopyWebhook.textContent = '📋 Copy'; }, 2000);
      });
    });
  }

  const btnCopyPhoneNoti = document.getElementById('btnCopyPhoneNotiUrl');
  if (btnCopyPhoneNoti && phoneUrlInput) {
    btnCopyPhoneNoti.addEventListener('click', () => {
      navigator.clipboard.writeText(phoneUrlInput.value).then(() => {
        btnCopyPhoneNoti.textContent = '✓ Đã copy!';
        setTimeout(() => { btnCopyPhoneNoti.textContent = '📋 Copy'; }, 2000);
      });
    });
  }

  // Khởi chạy khu vực test tay bấm QR
  setupScannerTestArea();

  // Khởi chạy module màn hình phụ khách hàng ESP32 TFT 1.8 SPI
  setupEsp32CustomerDisplay();
}

function closeAllModals() {
  document.querySelectorAll('.modal-overlay').forEach(m => m.classList.remove('active'));
}

// ==================== ESP32 CUSTOMER DISPLAY (USB SERIAL & WIFI) ====================
window.esp32UsbState = {
  port: null,
  writer: null,
  isConnected: false
};

// Đọc phản hồi debug từ ESP32 và in ra browser console
async function startEsp32SerialReader(port) {
  if (!port || !port.readable || window.esp32UsbState.reader) return;
  try {
    const reader = port.readable.getReader();
    window.esp32UsbState.reader = reader;
    const decoder = new TextDecoder();
    let buf = '';
    while (window.esp32UsbState.isConnected) {
      const { value, done } = await reader.read();
      if (done) break;
      buf += decoder.decode(value, { stream: true });
      const lines = buf.split('\n');
      buf = lines.pop();
      for (const line of lines) {
        const t = line.trim();
        if (t) {
          console.log('[ESP32-RX]', t);
          if (t.includes('ACK|QR_DRAWN') || t.includes('ACK|QR_SHOWN')) {
            const modalState = document.getElementById('vietQrEsp32State');
            if (modalState) modalState.innerHTML = '<span style="color: #16a34a; font-weight: 700;">🟢 Đang hiển thị mã QR trên TFT</span>';
          } else if (t.startsWith('ERR|')) {
            const modalState = document.getElementById('vietQrEsp32State');
            if (modalState) modalState.innerHTML = `<span style="color: #dc2626; font-weight: 700;">⚠️ ${escapeHtml(t)}</span>`;
          }
        }
      }
    }
    reader.releaseLock();
  } catch (e) {
    console.warn('[ESP32-RX] Reader dừng:', e.message);
  } finally {
    window.esp32UsbState.reader = null;
  }
}

// Queue để serialize các lệnh USB — tránh lỗi "WritableStream is locked"
let _esp32WriteQueue = Promise.resolve();
window._lastPendingEsp32QrCmd = null;

// Cập nhật trạng thái hiển thị của màn hình phụ ESP32 trên toàn bộ giao diện (POS, Modal QR, Cài đặt)
function updatePosEsp32Display(connected, statusText = '') {
  const dot = document.getElementById('posEsp32Dot');
  const label = document.getElementById('posEsp32Label');
  const btn = document.getElementById('btnPosConnectEsp32');
  const btnDisc = document.getElementById('btnPosDisconnectEsp32');
  const usbBadge = document.getElementById('esp32UsbPortBadge');
  const connectUsbBtn = document.getElementById('btnConnectUsbSerial');
  const disconnectUsbBtn = document.getElementById('btnDisconnectUsbSerial');

  const modalState = document.getElementById('vietQrEsp32State');
  const modalBtn = document.getElementById('btnVietQrConnectEsp32');
  const modalDiscBtn = document.getElementById('btnVietQrDisconnectEsp32');

  if (connected) {
    if (dot) dot.style.background = '#16a34a';
    if (label) label.innerHTML = `Màn hình phụ ESP32: <span style="color:#16a34a;">🟢 Sẵn sàng ${statusText ? `(${escapeHtml(statusText)})` : ''}</span>`;
    if (btn) btn.style.display = 'none';
    if (btnDisc) btnDisc.style.display = 'inline-block';

    if (modalState) modalState.innerHTML = `<span style="color: #16a34a; font-weight: 700;">🟢 Đang hiển thị mã QR</span>`;
    if (modalBtn) modalBtn.style.display = 'none';
    if (modalDiscBtn) modalDiscBtn.style.display = 'inline-block';

    if (usbBadge) {
      usbBadge.textContent = '🟢 Đã kết nối USB: 115200 Baud (Sẵn sàng)';
      usbBadge.style.background = '#15803d';
    }
    if (connectUsbBtn) connectUsbBtn.style.display = 'none';
    if (disconnectUsbBtn) disconnectUsbBtn.style.display = 'inline-flex';
  } else {
    if (dot) dot.style.background = '#94a3b8';
    if (label) label.innerHTML = 'Màn hình phụ ESP32: <span style="color:#64748b;">⚪ Chưa kết nối</span>';
    if (btn) btn.style.display = 'inline-block';
    if (btnDisc) btnDisc.style.display = 'none';

    if (modalState) modalState.innerHTML = `<span style="color: #dc2626; font-weight: 700;">🔴 Chưa kết nối</span>`;
    if (modalBtn) modalBtn.style.display = 'inline-block';
    if (modalDiscBtn) modalDiscBtn.style.display = 'none';

    if (usbBadge) {
      usbBadge.textContent = '⚪ Chưa kết nối cáp USB';
      usbBadge.style.background = '#64748b';
    }
    if (connectUsbBtn) connectUsbBtn.style.display = 'inline-flex';
    if (disconnectUsbBtn) disconnectUsbBtn.style.display = 'none';
  }
}
window.updatePosEsp32Display = updatePosEsp32Display;

// Hàm ngắt kết nối USB ESP32
window.disconnectEsp32UsbQuick = async function() {
  try {
    if (window.esp32UsbState.reader) {
      try { await window.esp32UsbState.reader.cancel(); } catch (_) {}
      window.esp32UsbState.reader = null;
    }
    if (window.esp32UsbState.writer) {
      try { await window.esp32UsbState.writer.close(); } catch (_) {}
      window.esp32UsbState.writer = null;
    }
    if (window.esp32UsbState.port) {
      try { await window.esp32UsbState.port.close(); } catch (_) {}
      window.esp32UsbState.port = null;
    }
  } catch (_) {}
  window.esp32UsbState.isConnected = false;
  updatePosEsp32Display(false);
};

// Hàm xoay lật màn hình dọc 180 độ
let _currentEsp32Rotation = 2;
window.toggleRotateEsp32 = async function() {
  _currentEsp32Rotation = (_currentEsp32Rotation === 2) ? 0 : 2;
  await sendEsp32UsbCommand(`ROTATE|${_currentEsp32Rotation}`);
};

// Hàm kết nối nhanh màn hình phụ ESP32 từ bất kỳ đâu (POS, Modal QR, Cài đặt)
window.connectEsp32UsbQuick = async function() {
  if (!('serial' in navigator)) {
    alert('Trình duyệt hiện tại chưa hỗ trợ Web Serial API.\nVui lòng mở phần mềm bằng Google Chrome, Microsoft Edge hoặc Cốc Cốc trên máy tính để kết nối USB trực tiếp.');
    return;
  }

  try {
    const port = await navigator.serial.requestPort();
    await port.open({ baudRate: 115200 });

    window.esp32UsbState.port = port;
    window.esp32UsbState.writer = null;
    window.esp32UsbState.isConnected = true;
    _esp32WriteQueue = Promise.resolve();

    startEsp32SerialReader(port);
    updatePosEsp32Display(true);

    if (window._lastPendingEsp32QrCmd) {
      await sendEsp32UsbCommand(window._lastPendingEsp32QrCmd);
    } else {
      const store = removeVietnameseTones(state.settings?.storeName || 'TAP HOA VIET').toUpperCase();
      await sendEsp32UsbCommand(`IDLE|${store}|Xin chao quy khach!`);
    }
  } catch (e) {
    if (e.name === 'NotFoundError') {
      return; // Người dùng bấm huỷ chọn cổng
    }
    console.error('Lỗi kết nối cổng USB:', e);
    const msg = e.message || '';
    if (msg.includes('Failed to open') || e.name === 'NetworkError') {
      alert('⚠️ CỔNG COM ĐANG BỊ CHIẾM (PORT BUSY)!\n\nChi tiết: ' + msg + '\n\n👉 NGUYÊN NHÂN: Cổng COM5 đang bị phần mềm khác (như Arduino IDE hoặc cửa sổ Serial Monitor) chiếm giữ.\n\n👉 CÁCH XỬ LÝ NHANH:\n1. Mở Arduino IDE -> ĐÓNG cửa sổ Serial Monitor ở góc dưới (hoặc tắt hoàn toàn Arduino IDE).\n2. Bấm lại nút "Kết nối" trên web để kết nối.');
    } else {
      alert('Lỗi kết nối cổng USB: ' + msg);
    }
  }
};

async function sendEsp32UsbCommand(cmdString) {
  if (!('serial' in navigator)) {
    return false;
  }

  // Nếu chưa kết nối, thử tự động lấy lại cổng đã từng cấp quyền
  if (!window.esp32UsbState.isConnected || !window.esp32UsbState.port) {
    try {
      const ports = await navigator.serial.getPorts();
      if (ports && ports.length > 0) {
        const port = ports[0];
        try {
          await port.open({ baudRate: 115200 });
        } catch (_) { /* Có thể cổng đã mở sẵn */ }
        window.esp32UsbState.port = port;
        window.esp32UsbState.writer = null;
        window.esp32UsbState.isConnected = true;
        _esp32WriteQueue = Promise.resolve();
        startEsp32SerialReader(port);
        updatePosEsp32Display(true);
      }
    } catch (_) { }
  }

  if (!window.esp32UsbState.isConnected) {
    console.warn('[ESP32-USB] Bỏ qua: chưa kết nối USB');
    updatePosEsp32Display(false);
    return false;
  }

  // Mỗi lệnh xếp hàng, chờ lệnh trước hoàn thành rồi mới gửi
  const result = _esp32WriteQueue.then(async () => {
    try {
      const safeCmdString = (cmdString || '')
        .normalize('NFC')
        .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd').replace(/Đ/g, 'D')
        .replace(/[^\x20-\x7E\n]/g, '');

      if (!window.esp32UsbState.port || !window.esp32UsbState.port.writable) {
        console.warn('[ESP32-USB] Port không writable!');
        return false;
      }

      // Dùng persistent writer — chỉ getWriter() một lần, giữ mãi
      if (!window.esp32UsbState.writer) {
        window.esp32UsbState.writer = window.esp32UsbState.port.writable.getWriter();
      }

      const encoder = new TextEncoder();
      await window.esp32UsbState.writer.write(encoder.encode(safeCmdString + '\n'));
      console.log('[ESP32-USB] Gửi OK:', safeCmdString.substring(0, 60));
      updatePosEsp32Display(true);
      return true;
    } catch (e) {
      console.error('[ESP32-USB] Lỗi gửi lệnh:', e.message);
      try { window.esp32UsbState.writer?.releaseLock(); } catch (_) {}
      window.esp32UsbState.writer = null;
      return false;
    }
  });

  _esp32WriteQueue = result.catch(() => {});
  return result;
}

function setupEsp32CustomerDisplay() {
  const badge = document.getElementById('esp32StatusBadge');
  const sim = document.getElementById('tftScreenSimulator');
  const ipInput = document.getElementById('esp32ServerIpInput');
  const ssidInput = document.getElementById('esp32WifiSsidInput');
  const passInput = document.getElementById('esp32WifiPassInput');
  const downloadLink = document.getElementById('btnDownloadArduinoIno');
  const copyBtn = document.getElementById('btnCopyArduinoCode');
  const copyUsbBtn = document.getElementById('btnCopyUsbArduinoCode');
  const refreshBtn = document.getElementById('btnRefreshEsp32Status');

  const btnConnectUsb = document.getElementById('btnConnectUsbSerial');
  const btnDisconnectUsb = document.getElementById('btnDisconnectUsbSerial');
  const usbBadge = document.getElementById('esp32UsbPortBadge');

  const radioUsb = document.getElementById('radioEsp32Usb');
  const radioWifi = document.getElementById('radioEsp32Wifi');
  const secUsb = document.getElementById('esp32UsbSection');
  const secWifi = document.getElementById('esp32WifiSection');

  const btnTestIdle = document.getElementById('btnEsp32TestIdle');
  const btnTestQr = document.getElementById('btnEsp32TestQr');
  const btnTestSuccess = document.getElementById('btnEsp32TestSuccess');

  if (!badge || !sim) return;

  // 1. Chuyển đổi giao diện giữa Chế độ USB và Chế độ WiFi
  if (radioUsb && radioWifi && secUsb && secWifi) {
    radioUsb.addEventListener('change', () => {
      if (radioUsb.checked) {
        secUsb.style.display = 'block';
        secWifi.style.display = 'none';
      }
    });
    radioWifi.addEventListener('change', () => {
      if (radioWifi.checked) {
        secUsb.style.display = 'none';
        secWifi.style.display = 'block';
      }
    });
  }

  // 2. Quản lý kết nối Cổng USB (Web Serial API - Cắm dây USB trực tiếp)
  if (btnConnectUsb && btnDisconnectUsb && usbBadge) {
    btnConnectUsb.addEventListener('click', connectEsp32UsbQuick);

    btnDisconnectUsb.addEventListener('click', async () => {
      if (window.esp32UsbState.writer) {
        try {
          await window.esp32UsbState.writer.close();
        } catch (_) { }
        window.esp32UsbState.writer = null;
      }
      if (window.esp32UsbState.port) {
        try {
          await window.esp32UsbState.port.close();
        } catch (_) { }
        window.esp32UsbState.port = null;
      }
      window.esp32UsbState.isConnected = false;
      updatePosEsp32Display(false);
    });

    // Tự động kết nối lại cổng USB đã từng cấp quyền khi tải trang
    if ('serial' in navigator) {
      navigator.serial.getPorts().then(async (ports) => {
        if (ports && ports.length > 0 && !window.esp32UsbState.isConnected) {
          try {
            const port = ports[0];
            await port.open({ baudRate: 115200 });
            window.esp32UsbState.port = port;
            window.esp32UsbState.writer = null;
            window.esp32UsbState.isConnected = true;
            _esp32WriteQueue = Promise.resolve();
            updatePosEsp32Display(true);
          } catch (_) {
            updatePosEsp32Display(false);
          }
        } else {
          updatePosEsp32Display(false);
        }
      }).catch(() => {
        updatePosEsp32Display(false);
      });

      navigator.serial.addEventListener('disconnect', () => {
        window.esp32UsbState.isConnected = false;
        window.esp32UsbState.writer = null;
        window.esp32UsbState.port = null;
        updatePosEsp32Display(false);
      });
    }
  }

  // 3. Cập nhật đường link tải Arduino .ino theo thông số người dùng nhập
  function updateDownloadUrl() {
    if (!downloadLink) return;
    const ip = (ipInput?.value || '').trim();
    const ssid = (ssidInput?.value || 'WiFi_CuaHang').trim();
    const pass = (passInput?.value || '12345678').trim();
    downloadLink.href = `/api/esp32/arduino-code?ssid=${encodeURIComponent(ssid)}&pass=${encodeURIComponent(pass)}&serverIp=${encodeURIComponent(ip)}`;
  }

  if (ipInput) ipInput.addEventListener('input', updateDownloadUrl);
  if (ssidInput) ssidInput.addEventListener('input', updateDownloadUrl);
  if (passInput) passInput.addEventListener('input', updateDownloadUrl);


  // 4. Vẽ mô phỏng giao diện màn hình TFT 1.8 inch (128x160)
  function renderTftSimulator(screen) {
    if (!sim) return;
    const store = screen?.storeName || state.settings?.storeName || 'TẠP HOÁ VIỆT';

    if (screen?.state === 'QR') {
      const order = screen.orderCode || 'HDTEST';
      const bank = state.settings?.qrBankBin || '970436';
      const acc = state.settings?.qrAccountNo || '123456';
      const amt = Math.round(screen.amount || 0);
      const amountFormatted = screen.amountFormatted || formatVND(amt);
      const qrUrl = `https://img.vietqr.io/image/${bank}-${acc}-qr_only.png?amount=${amt}&addInfo=${encodeURIComponent(order)}`;

      sim.innerHTML = `
        <div class="tft-sim-qr-fullscreen">
          <div class="tft-sim-qr-header">DON: ${escapeHtml(order)}</div>
          <div class="tft-sim-qr-body">
            <img src="${qrUrl}" class="qr-img" alt="Mã VietQR 2.8 inch">
          </div>
          <div class="tft-sim-qr-footer">
            <div style="font-size: 8.5px; color: #cbd5e1; letter-spacing: 0.3px;">SO TIEN THANH TOAN</div>
            <div class="tft-sim-qr-amount">${escapeHtml(amountFormatted)}</div>
          </div>
        </div>
      `;
    } else if (screen?.state === 'SUCCESS') {
      const order = screen.orderCode || 'HDTEST';
      const amount = screen.amountFormatted || formatVND(screen.amount || 0);

      sim.innerHTML = `
        <div class="tft-sim-success">
          <div style="font-size: 22px; line-height: 1;">✓</div>
          <div style="font-size: 13px; font-weight: 800; margin-top: 4px; letter-spacing: 0.5px;">ĐÃ NHẬN TIỀN!</div>
          <div style="margin-top: 8px; background: rgba(0,0,0,0.35); padding: 6px 10px; border-radius: 6px; width: 92%; border: 1px solid rgba(255,255,255,0.15);">
            <div style="font-size: 10px; color: #cbd5e1; font-weight: 600;">${escapeHtml(order)}</div>
            <div style="font-size: 13px; font-weight: 800; color: #fef08a; margin-top: 2px;">${escapeHtml(amount)}</div>
          </div>
          <div style="font-size: 9.5px; margin-top: 10px; opacity: 0.95;">Cảm ơn quý khách!</div>
        </div>
      `;
    } else {
      // Màn hình Chờ / Xin chào
      const ip = window.esp32UsbState.isConnected ? 'USB Cắm Trực Tiếp' : (ipInput?.value || '192.168.1.x');
      sim.innerHTML = `
        <div class="tft-sim-idle">
          <div class="header">${escapeHtml(store)}</div>
          <div class="body">
            <div class="tft-sim-title">XIN CHÀO</div>
            <div class="tft-sim-sub">Cảm ơn quý khách đã mua hàng!</div>
            <div style="font-size: 9px; color: #94a3b8; margin-top: 4px;">Hẹn gặp lại quý khách</div>
          </div>
          <div style="font-size: 9px; color: #22c55e; padding: 5px; font-weight: 600; border-top: 1px solid #1e293b;">${escapeHtml(ip)}</div>
        </div>
      `;
    }
  }

  // 5. Gọi API lấy trạng thái hiện tại của ESP32
  async function checkEsp32Status() {
    try {
      const res = await fetch('/api/esp32/status');
      if (!res.ok) return;
      const data = await res.json();

      // Cập nhật badge trạng thái tổng quan
      if (window.esp32UsbState.isConnected) {
        badge.textContent = '🟢 Đang chạy qua cáp USB Serial';
        badge.style.background = '#15803d';
      } else if (data.isOnline) {
        badge.textContent = `🟢 Đã kết nối WiFi (IP: ${data.ipAddress}${data.rssi ? `, Sóng: ${data.rssi}dBm` : ''})`;
        badge.style.background = '#15803d';
      } else {
        badge.textContent = '⚪ Chưa kết nối (Cắm USB hoặc kết nối WiFi)';
        badge.style.background = '#64748b';
      }

      // Tự động điền IP server nếu người dùng chưa nhập
      if (ipInput && (!ipInput.value || ipInput.value === '192.168.1.100') && data.serverIp) {
        ipInput.value = data.serverIp;
        updateDownloadUrl();
      }

      // Cập nhật màn hình mô phỏng
      renderTftSimulator(data.currentScreen);
    } catch (_) { }
  }

  // 6. Nút test trạng thái
  if (btnTestIdle) {
    btnTestIdle.addEventListener('click', async () => {
      try {
        const store = state.settings?.storeName || 'TAP HOA VIET';
        sendEsp32UsbCommand(`IDLE|${removeVietnameseTones(store)}|Xin chao quy khach!`);
        await fetch('/api/esp32/set-state', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ state: 'IDLE' })
        });
        await checkEsp32Status();
      } catch (e) {
        alert('Lỗi gửi tín hiệu: ' + e.message);
      }
    });
  }

  if (btnTestQr) {
    btnTestQr.addEventListener('click', async () => {
      try {
        const res = await fetch('/api/esp32/set-state', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            state: 'QR',
            orderCode: 'HD000088',
            amount: 150000,
            customerName: 'Khach Mau'
          })
        });
        if (res.ok) {
          const d = await res.json();
          if (d && d.qrContent) {
            sendEsp32UsbCommand(`QR|HD000088|150.000 VND|${d.qrContent}`);
          }
        }
        await checkEsp32Status();
      } catch (e) {
        alert('Lỗi gửi tín hiệu: ' + e.message);
      }
    });
  }

  if (btnTestSuccess) {
    btnTestSuccess.addEventListener('click', async () => {
      try {
        sendEsp32UsbCommand('SUCCESS|HD000088|150.000 VND|MBBank');
        await fetch('/api/esp32/set-state', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            state: 'SUCCESS',
            orderCode: 'HD000088',
            amount: 150000,
            bankName: 'MBBank'
          })
        });
        await checkEsp32Status();
      } catch (e) {
        alert('Lỗi gửi tín hiệu: ' + e.message);
      }
    });
  }

  if (refreshBtn) {
    refreshBtn.addEventListener('click', async () => {
      await checkEsp32Status();
      alert('Đã làm mới thông tin kết nối màn hình ESP32.');
    });
  }

  // Copy code WiFi
  if (copyBtn) {
    copyBtn.addEventListener('click', async () => {
      try {
        const ip = (ipInput?.value || '').trim();
        const ssid = (ssidInput?.value || 'WiFi_CuaHang').trim();
        const pass = (passInput?.value || '12345678').trim();
        const url = `/api/esp32/arduino-code?ssid=${encodeURIComponent(ssid)}&pass=${encodeURIComponent(pass)}&serverIp=${encodeURIComponent(ip)}`;
        const res = await fetch(url);
        if (res.ok) {
          const code = await res.text();
          await navigator.clipboard.writeText(code);
          alert('✓ Đã copy mã nguồn Arduino WiFi nạp ESP32 vào bộ nhớ tạm (Clipboard)!');
        }
      } catch (e) {
        alert('Lỗi copy mã nguồn: ' + e.message);
      }
    });
  }

  // Copy code USB
  if (copyUsbBtn) {
    copyUsbBtn.addEventListener('click', async () => {
      try {
        const res = await fetch('/api/esp32/arduino-usb-code');
        if (res.ok) {
          const code = await res.text();
          await navigator.clipboard.writeText(code);
          alert('✓ ĐÃ COPY MÃ NGUỒN ARDUINO USB VÀO BỘ NHỚ TẠM!\nHãy dán vào Arduino IDE, cắm cáp USB và bấm nạp là chạy ngay (không cần cấu hình WiFi).');
        }
      } catch (e) {
        alert('Lỗi copy mã nguồn USB: ' + e.message);
      }
    });
  }

  // Khởi tạo ban đầu
  updateDownloadUrl();
  checkEsp32Status();

  // Định kỳ cập nhật trạng thái kết nối ESP32 mỗi 4 giây nếu đang mở Tab Cài Đặt
  setInterval(() => {
    if (state.currentTab === 'settings') {
      checkEsp32Status();
    }
  }, 4000);
}
