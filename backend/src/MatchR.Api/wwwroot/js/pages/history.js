import { api } from '../api.js';
import { shell } from '../shell.js';
import { toast } from '../components/toast.js';

export async function historyPage() {
  let events = [];
  try {
    events = await api.history.list();
  } catch (err) {
    toast(err.message);
  }

  const c = `<div class="page-head"><div><h1>Histórico</h1><p class="muted">Buscas e compartilhamentos registrados por cliente.</p></div></div><div class="card table-wrap"><table class="data-table"><thead><tr><th>Data</th><th>Cliente</th><th>Ação</th><th>Resumo</th><th>Resultado</th></tr></thead><tbody>${
    events.length
      ? events
          .map(
            (e) =>
              `<tr><td>${formatDate(e.createdAt)}</td><td>${e.clientName}</td><td><span class="pill ${e.type === 'WhatsAppShare' ? 'success' : 'brand'}">${e.type === 'WhatsAppShare' ? 'WhatsApp' : 'Busca'}</span></td><td>${e.summary}</td><td>${
                e.searchRequestId ? `<a href="#/resultados/${e.searchRequestId}"><strong>${e.resultCount} resultados</strong></a>` : `${e.resultCount} imóveis enviados`
              }</td></tr>`
          )
          .join('')
      : '<tr><td colspan="5" class="empty">Nenhum evento registrado ainda.</td></tr>'
  }</tbody></table></div>`;

  return shell('historico', c, 'Histórico');
}

function formatDate(iso) {
  const d = new Date(iso);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) + ', ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}
