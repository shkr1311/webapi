// ═══════════════════════════════════════════════════════
// API Configuration
// ═══════════════════════════════════════════════════════
// After deploying to Render, replace these URLs with your
// Render service URLs. Example:
//   https://cod-product-service.onrender.com
//
// For local development, comment out this file or use localhost.
// ═══════════════════════════════════════════════════════

window.API_CONFIG = {
  product:  'http://localhost:5001',
  employee: 'http://localhost:5002',
  order:    'http://localhost:5003'
};

// ── AFTER RENDER DEPLOYMENT, use these instead: ──
// window.API_CONFIG = {
//   product:  'https://cod-product-service.onrender.com',
//   employee: 'https://cod-employee-service.onrender.com',
//   order:    'https://cod-order-service.onrender.com'
// };
