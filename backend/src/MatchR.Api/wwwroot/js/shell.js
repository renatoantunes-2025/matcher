import { icon } from './icons.js';
import { auth } from './api.js';

const nav = [
  ['dashboard', 'home', 'Visão geral'],
  ['clientes', 'users', 'Clientes'],
  ['busca', 'search', 'Nova busca'],
  ['favoritos', 'heart', 'Favoritos'],
  ['historico', 'clock', 'Histórico'],
];
const adminNav = [
  ['admin', 'settings', 'Administração'],
  ['importacao', 'upload', 'Importar inventário'],
];

export function shell(route, content, title) {
  const user = auth.getUser();
  const isAdmin = user?.role === 'Admin';
  const initials = (user?.name || 'US').split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase();

  return `<div class="app-shell"><aside class="sidebar" id="sidebar"><div class="sidebar-logo"><img src="assets/logo-matchr-full.png" alt="MatchR"></div><nav class="nav-group">${nav
    .map((n) => `<a class="nav-item ${route === n[0] ? 'active' : ''}" href="#/${n[0]}">${icon(n[1])}${n[2]}</a>`)
    .join('')}${
    isAdmin
      ? `<div class="nav-label">Gestão</div>${adminNav
          .map((n) => `<a class="nav-item ${route === n[0] ? 'active' : ''}" href="#/${n[0]}">${icon(n[1])}${n[2]}</a>`)
          .join('')}`
      : ''
  }</nav><div class="sidebar-bottom"><div class="user-mini"><div class="avatar">${initials}</div><div><strong style="font-size:13px">${user?.name || ''}</strong><div class="muted" style="font-size:11px">${isAdmin ? 'Administrador' : 'Corretor'}</div></div></div><button class="btn btn-ghost btn-sm" id="logoutBtn" style="width:100%;margin-top:10px">Sair</button></div></aside><div class="overlay" id="overlay"></div><main class="main"><header class="topbar"><div class="topbar-left"><button class="icon-btn mobile-menu" id="menuBtn">${icon('menu')}</button><div class="page-title">${title}</div></div><div class="top-actions"><button class="icon-btn">${icon('bell')}</button><a href="#/busca" class="btn btn-primary btn-sm">${icon('plus')}<span>Nova busca</span></a></div></header><div class="page">${content}</div></main></div>`;
}
