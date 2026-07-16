import { icon } from '../icons.js';
import { $ } from '../dom.js';
import { api, auth } from '../api.js';
import { shell } from '../shell.js';
import { searchDraft } from '../state.js';
import { toast } from '../components/toast.js';

export async function dashboard() {
  const user = auth.getUser();
  let clients = [];
  let stats = { activeClients: 0, searchesThisMonth: 0, favoritedProperties: 0, sharesSent: 0 };
  let recentClients = [];
  let activity = [];

  try {
    [clients, stats, recentClients, activity] = await Promise.all([
      api.clients.list(),
      api.dashboard.stats(),
      api.dashboard.recentClients(),
      api.dashboard.recentActivity(),
    ]);
  } catch (err) {
    toast(err.message);
  }

  const c = `<div class="card welcome"><span class="pill brand">${icon('spark')} MatchR Intelligence</span><h2>Olá, ${(user?.name || '').split(' ')[0] || ''}. Qual cliente vamos atender hoje?</h2><p class="muted">Selecione o cliente e descreva rapidamente o que procura ou abra uma nova busca completa.</p><div class="quick-client"><select class="select" id="quickClient" aria-label="Selecionar cliente"><option value="">Selecione o cliente</option>${clients
    .map((x) => `<option value="${x.id}">${x.name}</option>`)
    .join('')}</select></div><div class="quick-search"><input class="input" id="quick" placeholder="Ex.: cobertura no Itaim, até R$ 6 milhões, 4 dormitórios..."><button class="btn btn-primary quick-search-btn" id="quickBtn">Buscar imóveis</button></div></div><div class="grid stats-grid"><div class="card stat-card"><div class="stat-icon">${icon('users')}</div><strong>${stats.activeClients}</strong><span>Clientes ativos</span></div><div class="card stat-card"><div class="stat-icon">${icon('search')}</div><strong>${stats.searchesThisMonth}</strong><span>Buscas no mês</span></div><div class="card stat-card"><div class="stat-icon">${icon('heart')}</div><strong>${stats.favoritedProperties}</strong><span>Imóveis favoritados</span></div><div class="card stat-card"><div class="stat-icon">${icon('whatsapp')}</div><strong>${stats.sharesSent}</strong><span>Seleções enviadas</span></div></div><div class="grid dashboard-grid"><section class="card panel"><div class="panel-head"><h3>Clientes recentes</h3><a href="#/clientes" class="btn btn-ghost btn-sm">Ver todos</a></div><div class="client-list">${
    recentClients.length
      ? recentClients
          .map(
            (x) =>
              `<a class="client-row" href="#/cliente/${x.id}"><div class="avatar">${initials(x.name)}</div><div class="meta"><strong>${x.name}</strong><small>${x.searchCount} buscas · último acesso ${formatDate(x.lastActivityAt)}</small></div><span class="status ${x.status.toLowerCase()}">${x.status}</span>${icon('arrow')}</a>`
          )
          .join('')
      : '<div class="empty">Nenhum cliente cadastrado ainda.</div>'
  }</div></section><section class="card panel"><div class="panel-head"><h3>Atividade recente</h3></div><div class="activity-list">${
    activity.length
      ? activity
          .map(
            (a) =>
              `<div class="activity"><div class="activity-dot"></div><div><p>${a.description}</p><time>${formatDate(a.createdAt)}</time></div></div>`
          )
          .join('')
      : '<div class="empty">Nenhuma atividade recente.</div>'
  }</div></section></div>`;

  return shell('dashboard', c, 'Visão geral');
}

export function bindDashboard() {
  $('#quickBtn')?.addEventListener('click', () => {
    searchDraft.clientId = $('#quickClient')?.value || null;
    searchDraft.briefing = $('#quick')?.value || '';
    location.hash = '#/busca';
  });
}

function initials(name) {
  return name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase();
}

function formatDate(iso) {
  const d = new Date(iso);
  const today = new Date();
  const isToday = d.toDateString() === today.toDateString();
  const time = d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  if (isToday) return `Hoje, ${time}`;
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' });
}
