const TOKEN_KEY = 'matchr_token';
const USER_KEY = 'matchr_user';

export const auth = {
  getToken: () => localStorage.getItem(TOKEN_KEY),
  getUser: () => JSON.parse(localStorage.getItem(USER_KEY) || 'null'),
  setSession(token, user) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
  isLoggedIn: () => !!localStorage.getItem(TOKEN_KEY),
};

async function request(method, path, body, { isForm = false } = {}) {
  const headers = {};
  const token = auth.getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (!isForm && body !== undefined) headers['Content-Type'] = 'application/json';

  const res = await fetch(`/api${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : (isForm ? body : JSON.stringify(body)),
  });

  if (res.status === 401) {
    auth.clearSession();
    location.hash = '#/login';
    throw new Error('Sessão expirada. Faça login novamente.');
  }

  if (!res.ok) {
    let message = `Erro ${res.status}`;
    try {
      const data = await res.json();
      message = data.message || message;
    } catch { /* body not json */ }
    throw new Error(message);
  }

  if (res.status === 204) return null;
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

const get = (path) => request('GET', path);
const post = (path, body, opts) => request('POST', path, body, opts);
const put = (path, body) => request('PUT', path, body);
const patch = (path, body) => request('PATCH', path, body);
const del = (path) => request('DELETE', path);

export const api = {
  auth: {
    login: (email, password) => post('/auth/login', { email, password }),
    requestAccess: (payload) => post('/auth/access-requests', payload),
  },
  dashboard: {
    stats: () => get('/dashboard/stats'),
    recentClients: () => get('/dashboard/recent-clients'),
    recentActivity: () => get('/dashboard/recent-activity'),
  },
  clients: {
    list: (search) => get(`/clients${search ? `?search=${encodeURIComponent(search)}` : ''}`),
    get: (id) => get(`/clients/${id}`),
    create: (payload) => post('/clients', payload),
    update: (id, payload) => put(`/clients/${id}`, payload),
    remove: (id) => del(`/clients/${id}`),
  },
  properties: {
    list: () => get('/properties'),
  },
  agencies: {
    list: () => get('/agencies'),
  },
  searches: {
    create: (payload) => post('/searches', payload),
    get: (id) => get(`/searches/${id}`),
    setSelection: (id, propertyId, selected) => patch(`/searches/${id}/selection`, { propertyId, selected }),
    share: (id, message) => post(`/searches/${id}/share`, { message }),
  },
  favorites: {
    list: () => get('/favorites'),
    add: (propertyId) => post(`/favorites/${propertyId}`),
    remove: (propertyId) => del(`/favorites/${propertyId}`),
  },
  history: {
    list: (clientId) => get(`/history${clientId ? `?clientId=${clientId}` : ''}`),
  },
  admin: {
    accessRequests: () => get('/admin/access-requests'),
    approve: (id) => post(`/admin/access-requests/${id}/approve`),
    reject: (id) => post(`/admin/access-requests/${id}/reject`),
    brokers: () => get('/admin/brokers'),
    inventorySummary: () => get('/admin/inventory-summary'),
  },
  import: {
    upload: (file) => {
      const form = new FormData();
      form.append('file', file);
      return post('/import', form, { isForm: true });
    },
    history: () => get('/import'),
  },
};
