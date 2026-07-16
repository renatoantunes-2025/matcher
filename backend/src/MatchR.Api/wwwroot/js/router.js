import { $ } from './dom.js';
import { auth } from './api.js';
import { landing, bindLanding } from './pages/landing.js';
import { login, bindLogin } from './pages/login.js';
import { dashboard, bindDashboard } from './pages/dashboard.js';
import { clientsPage, bindClients, clientDetailPage, bindClientDetail } from './pages/clients.js';
import { searchPage, bindSearch } from './pages/search.js';
import { resultsPage, bindResults } from './pages/results.js';
import { favoritesPage, bindFavorites } from './pages/favorites.js';
import { historyPage } from './pages/history.js';
import { adminPage, bindAdmin } from './pages/admin.js';
import { importPage, bindImport } from './pages/importInventory.js';

const PUBLIC_ROUTES = new Set(['', 'login']);

export async function route() {
  const rawHash = location.hash;
  const isAppRoute = rawHash.startsWith('#/');

  if (rawHash && !isAppRoute) {
    if (!$('.landing')) {
      $('#app').innerHTML = landing();
      bindLanding();
      bindShellGlobals();
    }
    requestAnimationFrame(() => document.querySelector(rawHash)?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    return;
  }

  const hash = rawHash.replace('#/', '') || '';
  const [r, id] = hash.split('/');

  if (!PUBLIC_ROUTES.has(r) && !auth.isLoggedIn()) {
    location.hash = '#/login';
    return;
  }

  let html = '';
  let afterRender = () => {};

  switch (r) {
    case '':
      html = landing();
      afterRender = bindLanding;
      break;
    case 'login':
      if (auth.isLoggedIn()) {
        location.hash = '#/dashboard';
        return;
      }
      html = login();
      afterRender = bindLogin;
      break;
    case 'dashboard':
      html = await dashboard();
      afterRender = bindDashboard;
      break;
    case 'clientes':
      html = await clientsPage();
      afterRender = bindClients;
      break;
    case 'cliente': {
      const { html: detailHtml, client } = await clientDetailPage(id);
      html = detailHtml;
      afterRender = () => bindClientDetail(client);
      break;
    }
    case 'busca':
      html = await searchPage();
      afterRender = bindSearch;
      break;
    case 'resultados':
      html = await resultsPage(id);
      afterRender = bindResults;
      break;
    case 'favoritos':
      html = await favoritesPage();
      afterRender = bindFavorites;
      break;
    case 'historico':
      html = await historyPage();
      break;
    case 'admin':
      html = await adminPage();
      afterRender = bindAdmin;
      break;
    case 'importacao':
      html = await importPage();
      afterRender = bindImport;
      break;
    default:
      html = landing();
      afterRender = bindLanding;
  }

  $('#app').innerHTML = html;
  bindShellGlobals();
  afterRender();
  window.scrollTo(0, 0);
}

function bindShellGlobals() {
  $('#menuBtn')?.addEventListener('click', () => {
    $('#sidebar').classList.add('open');
    $('#overlay').classList.add('show');
  });
  $('#overlay')?.addEventListener('click', () => {
    $('#sidebar').classList.remove('open');
    $('#overlay').classList.remove('show');
  });
  $('#logoutBtn')?.addEventListener('click', () => {
    auth.clearSession();
    location.hash = '#/login';
  });
}
