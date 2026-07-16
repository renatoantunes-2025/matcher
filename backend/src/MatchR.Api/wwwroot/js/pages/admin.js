import { icon } from '../icons.js';
import { $$ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { toast } from '../components/toast.js';
import { confirmModal } from '../components/modal.js';

let pendingRequests = [];

export async function adminPage() {
  let inventory = { totalActive: 0, agencyCount: 0 };
  try {
    [pendingRequests, inventory] = await Promise.all([api.admin.accessRequests(), api.admin.inventorySummary()]);
  } catch (err) {
    toast(err.message);
    pendingRequests = [];
  }

  const c = `<div class="page-head"><div><h1>Administração</h1><p class="muted">Gerencie usuários, inventário e indicadores básicos do MVP.</p></div></div><div class="grid admin-grid"><article class="card admin-card"><div class="stat-icon">${icon('users')}</div><h3>Aprovação de corretores</h3><p>${pendingRequests.length} solicitações aguardando análise de dados e CRECI.</p></article><article class="card admin-card"><div class="stat-icon">${icon('home')}</div><h3>Inventário</h3><p>${inventory.totalActive} imóveis ativos de ${inventory.agencyCount} imobiliárias parceiras.</p><a class="btn btn-secondary btn-sm" href="#/importacao">Gerenciar base</a></article><article class="card admin-card"><div class="stat-icon">${icon('settings')}</div><h3>Regras do Match</h3><p>Pesos atuais: localização 30%, tipo 20%, preço 15% e demais critérios.</p></article></div><div class="card panel" style="margin-top:20px"><div class="panel-head"><h3>Solicitações pendentes</h3><span class="pill brand">${pendingRequests.length} pendentes</span></div><div class="table-wrap"><table class="data-table"><thead><tr><th>Corretor</th><th>CRECI</th><th>Solicitado em</th><th>Ação</th></tr></thead><tbody id="requestRows">${
    pendingRequests.length
      ? pendingRequests
          .map(
            (r) =>
              `<tr data-id="${r.id}"><td>${r.name}</td><td>${r.creci}</td><td>${formatDate(r.createdAt)}</td><td style="display:flex;gap:8px"><button class="btn btn-primary btn-sm" data-approve="${r.id}">Aprovar</button><button class="btn btn-secondary btn-sm" data-reject="${r.id}">Rejeitar</button></td></tr>`
          )
          .join('')
      : '<tr><td colspan="4" class="empty">Nenhuma solicitação pendente.</td></tr>'
  }</tbody></table></div></div>`;

  return shell('admin', c, 'Administração');
}

export function bindAdmin() {
  $$('[data-approve]').forEach((b) =>
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.approve);
      try {
        const res = await api.admin.approve(id);
        await confirmModal('Corretor aprovado', `Senha temporária gerada: <strong>${res.temporaryPassword}</strong><br><br>Compartilhe com segurança — o corretor deve trocá-la no primeiro acesso.`, {
          confirmLabel: 'Entendi',
        });
        document.querySelector(`tr[data-id="${id}"]`)?.remove();
      } catch (err) {
        toast(err.message);
      }
    })
  );

  $$('[data-reject]').forEach((b) =>
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.reject);
      const ok = await confirmModal('Rejeitar solicitação', 'Tem certeza que deseja rejeitar este cadastro?', { confirmLabel: 'Rejeitar' });
      if (!ok) return;
      try {
        await api.admin.reject(id);
        toast('Solicitação rejeitada.');
        document.querySelector(`tr[data-id="${id}"]`)?.remove();
      } catch (err) {
        toast(err.message);
      }
    })
  );
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' });
}
