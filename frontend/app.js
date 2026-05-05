// ═══════════ CONFIG ═══════════
const API = {
  product: 'http://localhost:5001',
  employee: 'http://localhost:5002',
  order: 'http://localhost:5003'
};
let token = '';

// ═══════════ HELPERS ═══════════
function toast(msg, type = 'info') {
  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.textContent = msg;
  document.getElementById('toasts').appendChild(el);
  setTimeout(() => el.remove(), 3500);
}

async function api(base, path, method = 'GET', body = null) {
  const opts = { method, headers: { 'Content-Type': 'application/json' } };
  if (token) opts.headers['Authorization'] = `Bearer ${token}`;
  if (body) opts.body = JSON.stringify(body);
  const res = await fetch(`${base}${path}`, opts);
  return res.json();
}

async function getToken() {
  const data = await api(API.product, '/api/auth/token', 'POST', { username: 'admin', password: 'admin123' });
  if (data.token) { token = data.token; return true; }
  return false;
}

function fmt(d) { return d ? new Date(d).toLocaleString() : '-'; }
function statusPill(s) { return `<span class="status-pill ${s.toLowerCase()}">${s}</span>`; }

// ═══════════ TABS ═══════════
function switchTab(name) {
  document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));
  document.querySelectorAll('.nav-tab').forEach(t => t.classList.remove('active'));
  document.getElementById(`tab-${name}`).classList.add('active');
  event.currentTarget.classList.add('active');
  if (name === 'dashboard') loadDashboard();
  if (name === 'products') loadProducts();
  if (name === 'employees') loadEmployees();
  if (name === 'orders') { loadOrders(); loadProductDropdown(); }
}

// ═══════════ HEALTH CHECKS ═══════════
async function checkHealth() {
  for (const [name, url] of Object.entries(API)) {
    try {
      const res = await fetch(`${url}/health`);
      const badge = document.getElementById(`badge-${name}`);
      badge.className = res.ok ? 'badge online' : 'badge offline';
    } catch { document.getElementById(`badge-${name}`).className = 'badge offline'; }
  }
}

// ═══════════ DASHBOARD ═══════════
async function loadDashboard() {
  try {
    const [products, employees, orders] = await Promise.all([
      api(API.product, '/api/products'),
      api(API.employee, '/api/employees'),
      api(API.order, '/api/orders')
    ]);
    document.getElementById('stat-products').textContent = products.data?.length ?? 0;
    document.getElementById('stat-employees').textContent = employees.data?.length ?? 0;
    document.getElementById('stat-orders').textContent = orders.data?.length ?? 0;
    document.getElementById('stat-available').textContent = employees.data?.filter(e => e.isAvailable).length ?? 0;

    const tbody = document.getElementById('dashboard-orders');
    tbody.innerHTML = (orders.data || []).slice(0, 5).map(o => `
      <tr>
        <td>#${o.id}</td><td>${o.productName || '-'}</td><td>${o.quantity}</td>
        <td>$${o.totalAmount?.toFixed(2)}</td><td>${o.employeeName || '-'}</td>
        <td>${statusPill(o.status)}</td><td>${statusPill(o.paymentStatus)}</td>
      </tr>`).join('') || '<tr><td colspan="7" style="text-align:center;color:var(--text-muted)">No orders yet</td></tr>';
  } catch (e) { console.error(e); }
}

// ═══════════ PRODUCTS ═══════════
async function loadProducts() {
  const data = await api(API.product, '/api/products');
  document.getElementById('products-table').innerHTML = (data.data || []).map(p => `
    <tr><td>#${p.id}</td><td>${p.name}</td><td>$${p.price.toFixed(2)}</td><td>${p.stock}</td><td>${fmt(p.createdAt)}</td>
    <td><div class="action-btns">
      <button class="btn btn-sm btn-edit" onclick="editProduct(${p.id},'${p.name.replace(/'/g,"\\'")}',${p.price},${p.stock})">✏️ Edit</button>
      <button class="btn btn-sm btn-del" onclick="deleteProduct(${p.id},'${p.name.replace(/'/g,"\\'")}')">🗑️ Del</button>
    </div></td></tr>
  `).join('') || '<tr><td colspan="6" style="text-align:center;color:var(--text-muted)">No products</td></tr>';
}

async function createProduct() {
  if (!token) await getToken();
  const name = document.getElementById('p-name').value;
  const price = parseFloat(document.getElementById('p-price').value);
  const stock = parseInt(document.getElementById('p-stock').value);
  if (!name || !price || !stock) { toast('Fill all fields', 'error'); return; }
  const data = await api(API.product, '/api/products', 'POST', { name, price, stock });
  if (data.success) { toast(`Product "${name}" created!`, 'success'); loadProducts(); document.getElementById('p-name').value=''; document.getElementById('p-price').value=''; document.getElementById('p-stock').value=''; }
  else toast(data.message || 'Failed', 'error');
}

function editProduct(id, name, price, stock) {
  document.getElementById('modal-title').textContent = `Edit Product #${id}`;
  document.getElementById('modal-body').innerHTML = `
    <div class="form-group"><label>Product Name</label><input id="edit-p-name" value="${name}"></div>
    <div class="form-group"><label>Price ($)</label><input id="edit-p-price" type="number" step="0.01" value="${price}"></div>
    <div class="form-group"><label>Stock</label><input id="edit-p-stock" type="number" value="${stock}"></div>`;
  currentEdit = { type: 'product', id };
  document.getElementById('editModal').style.display = 'flex';
}

async function deleteProduct(id, name) {
  if (!confirm(`Delete product "${name}"?`)) return;
  if (!token) await getToken();
  const res = await fetch(`${API.product}/api/products/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
  if (res.ok) { toast(`Product "${name}" deleted`, 'success'); loadProducts(); }
  else toast('Delete failed', 'error');
}

// ═══════════ EMPLOYEES ═══════════
async function loadEmployees() {
  const data = await api(API.employee, '/api/employees');
  document.getElementById('employees-table').innerHTML = (data.data || []).map(e => `
    <tr>
      <td>#${e.id}</td><td>${e.name}</td><td>${e.phone}</td>
      <td>${e.isAvailable ? statusPill('Available') : statusPill('Busy')}</td>
      <td><div class="action-btns">
        <button class="btn btn-sm ${e.isAvailable ? 'btn-warning' : 'btn-success'}" onclick="toggleAvail(${e.id}, ${!e.isAvailable})">${e.isAvailable ? 'Busy' : 'Free'}</button>
        <button class="btn btn-sm btn-edit" onclick="editEmployee(${e.id},'${e.name.replace(/'/g,"\\'")}','${e.phone.replace(/'/g,"\\'")}')">✏️</button>
        <button class="btn btn-sm btn-del" onclick="deleteEmployee(${e.id},'${e.name.replace(/'/g,"\\'")}')">🗑️</button>
      </div></td>
    </tr>`).join('') || '<tr><td colspan="5" style="text-align:center;color:var(--text-muted)">No employees</td></tr>';
}

async function createEmployee() {
  if (!token) await getToken();
  const name = document.getElementById('e-name').value;
  const phone = document.getElementById('e-phone').value;
  if (!name || !phone) { toast('Fill all fields', 'error'); return; }
  const data = await api(API.employee, '/api/employees', 'POST', { name, phone });
  if (data.success) { toast(`Employee "${name}" added!`, 'success'); loadEmployees(); document.getElementById('e-name').value=''; document.getElementById('e-phone').value=''; }
  else toast(data.message || 'Failed', 'error');
}

async function toggleAvail(id, isAvailable) {
  await api(API.employee, `/api/employees/${id}/availability`, 'PUT', { isAvailable });
  toast(`Employee ${isAvailable ? 'available' : 'busy'}`, 'info');
  loadEmployees();
}

function editEmployee(id, name, phone) {
  document.getElementById('modal-title').textContent = `Edit Employee #${id}`;
  document.getElementById('modal-body').innerHTML = `
    <div class="form-group"><label>Employee Name</label><input id="edit-e-name" value="${name}"></div>
    <div class="form-group"><label>Phone</label><input id="edit-e-phone" value="${phone}"></div>`;
  currentEdit = { type: 'employee', id };
  document.getElementById('editModal').style.display = 'flex';
}

async function deleteEmployee(id, name) {
  if (!confirm(`Delete employee "${name}"?`)) return;
  if (!token) await getToken();
  const res = await fetch(`${API.employee}/api/employees/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
  if (res.ok) { toast(`Employee "${name}" deleted`, 'success'); loadEmployees(); }
  else toast('Delete failed', 'error');
}

// ═══════════ ORDERS ═══════════
async function loadProductDropdown() {
  const data = await api(API.product, '/api/products');
  const sel = document.getElementById('o-product');
  sel.innerHTML = '<option value="">Select Product...</option>' + (data.data || []).map(p => `<option value="${p.id}">${p.name} — $${p.price.toFixed(2)} (Stock: ${p.stock})</option>`).join('');
}

async function loadOrders() {
  const data = await api(API.order, '/api/orders');
  document.getElementById('orders-table').innerHTML = (data.data || []).map(o => {
    let actions = '';
    if (o.status === 'Created') actions = `<button class="btn btn-sm btn-success" onclick="deliverOrder(${o.id})">🚚 Deliver</button>`;
    if (o.status === 'Delivered' && o.paymentStatus === 'Pending') actions = `<button class="btn btn-sm btn-warning" onclick="payOrder(${o.id})">💰 COD Pay</button>`;
    if (o.paymentStatus === 'Paid') actions = '<span style="color:var(--success);font-size:0.75rem">✅ Complete</span>';
    return `<tr>
      <td>#${o.id}</td><td>${o.productName || '-'}</td><td>${o.quantity}</td>
      <td>$${o.totalAmount?.toFixed(2)}</td><td>${o.employeeName || '-'}</td>
      <td>${statusPill(o.status)}</td><td>${statusPill(o.paymentStatus)}</td><td>${actions}</td>
    </tr>`;
  }).join('') || '<tr><td colspan="8" style="text-align:center;color:var(--text-muted)">No orders</td></tr>';
}

async function createOrder() {
  if (!token) await getToken();
  const productId = parseInt(document.getElementById('o-product').value);
  const quantity = parseInt(document.getElementById('o-qty').value);
  if (!productId) { toast('Select a product', 'error'); return; }
  const data = await api(API.order, '/api/orders', 'POST', { productId, quantity });
  if (data.success) { toast(`Order #${data.data.id} created! Agent: ${data.data.employeeName}`, 'success'); loadOrders(); }
  else toast(data.message || 'Failed', 'error');
}

async function deliverOrder(id) {
  if (!token) await getToken();
  const data = await api(API.order, `/api/orders/${id}/deliver`, 'PUT');
  if (data.success) { toast(`Order #${id} delivered!`, 'success'); loadOrders(); }
  else toast(data.message || 'Failed', 'error');
}

async function payOrder(id) {
  if (!token) await getToken();
  const data = await api(API.order, `/api/orders/${id}/pay`, 'PUT');
  if (data.success) { toast(`Order #${id} — COD Payment collected!`, 'success'); loadOrders(); }
  else toast(data.message || 'Failed', 'error');
}

// ═══════════ COD FLOW DEMO ═══════════
function flowLog(msg, cls = 'log-info') {
  const box = document.getElementById('flow-log');
  const time = new Date().toLocaleTimeString();
  box.innerHTML += `<div class="log-entry"><span class="log-time">[${time}]</span> <span class="${cls}">${msg}</span></div>`;
  box.scrollTop = box.scrollHeight;
}

function stepUpdate(n, status, cls) {
  const el = document.getElementById(`fs-${n}`);
  document.getElementById(`fs-${n}-s`).textContent = status;
  el.className = `flow-step ${cls}`;
}

async function runCodFlow() {
  const btn = document.getElementById('btn-flow');
  btn.disabled = true; btn.textContent = '⏳ Running...';
  document.getElementById('flow-log').innerHTML = '';
  for (let i = 1; i <= 6; i++) stepUpdate(i, 'Waiting', '');

  try {
    // Step 1: Auth
    stepUpdate(1, 'Running...', 'active');
    flowLog('POST /api/auth/token — Authenticating...');
    await getToken();
    flowLog('✅ JWT Token obtained (24h expiry)', 'log-ok');
    stepUpdate(1, '✅ Done', 'done');
    await new Promise(r => setTimeout(r, 500));

    // Step 2: Create Product
    stepUpdate(2, 'Running...', 'active');
    flowLog('POST /api/products — Creating "MacBook Pro M3"...');
    const prod = await api(API.product, '/api/products', 'POST', { name: 'MacBook Pro M3', price: 2499.99, stock: 10 });
    flowLog(`✅ Product created: ${prod.data.name} (ID: ${prod.data.id}, $${prod.data.price})`, 'log-ok');
    stepUpdate(2, '✅ Done', 'done');
    await new Promise(r => setTimeout(r, 500));

    // Step 3: Create Employee
    stepUpdate(3, 'Running...', 'active');
    flowLog('POST /api/employees — Creating "Amit Kumar"...');
    const emp = await api(API.employee, '/api/employees', 'POST', { name: 'Amit Kumar', phone: '+91-9988776655' });
    flowLog(`✅ Employee created: ${emp.data.name} (Available: ${emp.data.isAvailable})`, 'log-ok');
    stepUpdate(3, '✅ Done', 'done');
    await new Promise(r => setTimeout(r, 500));

    // Step 4: Create Order
    stepUpdate(4, 'Running...', 'active');
    flowLog(`POST /api/orders — Creating order for Product ${prod.data.id}...`);
    flowLog('  → Order Service calling Product Service to validate...', 'log-info');
    flowLog('  → Order Service calling Employee Service for assignment...', 'log-info');
    const order = await api(API.order, '/api/orders', 'POST', { productId: prod.data.id, quantity: 1 });
    if (!order.success) throw new Error(order.message);
    flowLog(`✅ Order #${order.data.id} created!`, 'log-ok');
    flowLog(`   Product: ${order.data.productName} | Agent: ${order.data.employeeName} | Total: $${order.data.totalAmount}`, 'log-ok');
    flowLog(`   Status: ${order.data.status} | Payment: ${order.data.paymentStatus}`, 'log-ok');
    stepUpdate(4, '✅ Done', 'done');
    await new Promise(r => setTimeout(r, 700));

    // Step 5: Deliver
    stepUpdate(5, 'Running...', 'active');
    flowLog(`PUT /api/orders/${order.data.id}/deliver — Marking as delivered...`);
    const del = await api(API.order, `/api/orders/${order.data.id}/deliver`, 'PUT');
    flowLog(`✅ Order delivered at ${fmt(del.data.deliveredAt)}`, 'log-ok');
    flowLog(`   Employee ${order.data.employeeName} released back to available`, 'log-ok');
    stepUpdate(5, '✅ Done', 'done');
    await new Promise(r => setTimeout(r, 700));

    // Step 6: COD Pay
    stepUpdate(6, 'Running...', 'active');
    flowLog(`PUT /api/orders/${order.data.id}/pay — Collecting COD payment...`);
    const pay = await api(API.order, `/api/orders/${order.data.id}/pay`, 'PUT');
    flowLog(`✅ Payment collected! Paid at ${fmt(pay.data.paidAt)}`, 'log-ok');
    flowLog('', 'log-ok');
    flowLog('🎉 COMPLETE COD FLOW FINISHED SUCCESSFULLY!', 'log-ok');
    stepUpdate(6, '✅ Done', 'done');

    toast('COD Flow completed successfully!', 'success');
  } catch (e) {
    flowLog(`❌ Error: ${e.message}`, 'log-err');
    toast(`Flow failed: ${e.message}`, 'error');
  }

  btn.disabled = false; btn.textContent = '▶️ Run Complete COD Flow';
}

// ═══════════ MODAL ═══════════
let currentEdit = null;

function closeModal() {
  document.getElementById('editModal').style.display = 'none';
  currentEdit = null;
}

async function saveEdit() {
  if (!currentEdit) return;
  if (!token) await getToken();

  if (currentEdit.type === 'product') {
    const name = document.getElementById('edit-p-name').value;
    const price = parseFloat(document.getElementById('edit-p-price').value);
    const stock = parseInt(document.getElementById('edit-p-stock').value);
    if (!name || !price) { toast('Fill all fields', 'error'); return; }
    const data = await api(API.product, `/api/products/${currentEdit.id}`, 'PUT', { name, price, stock });
    if (data.success) { toast(`Product updated!`, 'success'); loadProducts(); }
    else toast(data.message || 'Update failed', 'error');
  }

  if (currentEdit.type === 'employee') {
    const name = document.getElementById('edit-e-name').value;
    const phone = document.getElementById('edit-e-phone').value;
    if (!name || !phone) { toast('Fill all fields', 'error'); return; }
    const data = await api(API.employee, `/api/employees/${currentEdit.id}`, 'PUT', { name, phone });
    if (data.success) { toast(`Employee updated!`, 'success'); loadEmployees(); }
    else toast(data.message || 'Update failed', 'error');
  }

  closeModal();
}

// Close modal on overlay click
document.addEventListener('click', (e) => {
  if (e.target.classList.contains('modal-overlay')) closeModal();
});

// ═══════════ INIT ═══════════
checkHealth(); setInterval(checkHealth, 10000);
getToken().then(() => loadDashboard());
