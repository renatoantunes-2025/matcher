import { icon } from '../icons.js';
import { $, $$ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { openModal, confirmModal } from '../components/modal.js';
import { toast } from '../components/toast.js';

export async function clientsPage() {
  let clients = [];
  try {
    clients = await api.clients.list();
  } catch (err) {
    toast(err.message);
  }

  const c = `<div class="page-head"><div><h1>Clientes</h1><p class="muted">Organize briefings, buscas e oportunidades em um só lugar.</p></div><button class="btn btn-primary" id="newClientBtn">${icon('plus')} Novo cliente</button></div><div class="toolbar"><div class="search-field">${icon('search')}<input class="input" id="clientSearch" placeholder="Buscar por nome, telefone ou e-mail"></div><select class="select" style="width:auto" id="statusFilter"><option value="">Todos os tipos</option><option value="Lead">Lead</option><option value="Cliente">Cliente</option><option value="Parceiro">Parceiro</option></select></div><div class="card table-wrap"><table class="data-table"><thead><tr><th>Cliente</th><th>Contato</th><th>Tipo</th><th>Buscas</th><th>Última atividade</th><th></th></tr></thead><tbody id="clientRows">${
    clients.length ? clients.map(clientRow).join('') : `<tr><td colspan="6" class="empty">Nenhum cliente cadastrado ainda.</td></tr>`
  }</tbody></table></div>`;

  return shell('clientes', c, 'Clientes');
}

function clientRow(x) {
  return `<tr data-id="${x.id}" data-name="${(x.name + (x.phone || '') + (x.email || '')).toLowerCase()}" data-status="${x.status}"><td><a href="#/cliente/${x.id}" style="display:flex;align-items:center;gap:10px"><div class="avatar">${initials(x.name)}</div><strong>${x.name}</strong></a></td><td><div>${x.phone || '—'}</div><small class="muted">${x.email || ''}</small></td><td><span class="status ${x.status.toLowerCase()}">${x.status}</span></td><td>${x.searchCount}</td><td>${formatDate(x.lastActivityAt)}</td><td><a class="icon-btn" href="#/cliente/${x.id}">${icon('arrow')}</a></td></tr>`;
}

export function bindClients() {
  $('#clientSearch')?.addEventListener('input', filterRows);
  $('#statusFilter')?.addEventListener('change', filterRows);
  $('#newClientBtn')?.addEventListener('click', () => openClientModal());
}

function filterRows() {
  const term = ($('#clientSearch')?.value || '').toLowerCase();
  const status = $('#statusFilter')?.value || '';
  $$('#clientRows tr').forEach((row) => {
    const matchesTerm = row.dataset.name?.includes(term);
    const matchesStatus = !status || row.dataset.status === status;
    row.style.display = matchesTerm && matchesStatus ? '' : 'none';
  });
}

export function clientModalHtml(client) {
  const edit = !!client;
  return `<div class="modal-backdrop" id="modalBackdrop"><div class="modal"><div class="modal-head"><h3>${edit ? 'Editar cliente' : 'Novo cliente'}</h3><button class="icon-btn" id="closeModal">${icon('close')}</button></div><form id="clientForm"><div class="modal-body"><div class="field-row"><div class="form-group"><label>Nome *</label><input class="input" name="name" required value="${client?.name || ''}"></div><div class="form-group"><label>Tipo</label><select class="select" name="status"><option value="Lead" ${client?.status === 'Lead' ? 'selected' : ''}>Lead</option><option value="Cliente" ${client?.status === 'Cliente' ? 'selected' : ''}>Cliente</option><option value="Parceiro" ${client?.status === 'Parceiro' ? 'selected' : ''}>Parceiro</option></select></div></div><div class="field-row" style="margin-top:14px"><div class="form-group"><label>Telefone</label><input class="input" name="phone" value="${client?.phone || ''}"></div><div class="form-group"><label>E-mail</label><input class="input" name="email" type="email" value="${client?.email || ''}"></div></div><div class="form-group" style="margin-top:14px"><label>Observações</label><textarea class="textarea" name="preferences" placeholder="Preferências e contexto do cliente">${client?.preferences || ''}</textarea></div></div><div class="modal-actions"><button type="button" class="btn btn-secondary" id="cancelModal">Cancelar</button><button class="btn btn-primary">Salvar cliente</button></div></form></div></div>`;
}

export function openClientModal(client, onSaved) {
  openModal(clientModalHtml(client), {
    onMount: (close) => {
      $('#clientForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const form = e.target;
        const payload = {
          name: form.name.value,
          phone: form.phone.value || null,
          email: form.email.value || null,
          status: form.status.value,
          preferences: form.preferences.value || null,
        };
        try {
          if (client) {
            await api.clients.update(client.id, payload);
          } else {
            await api.clients.create(payload);
          }
          close();
          toast('Cliente salvo com sucesso.');
          onSaved ? onSaved() : location.reload();
        } catch (err) {
          toast(err.message);
        }
      });
    },
  });
}

export async function clientDetailPage(id) {
  let client;
  let events = [];
  try {
    [client, events] = await Promise.all([api.clients.get(id), api.history.list(id)]);
  } catch (err) {
    toast(err.message);
    return shell('clientes', `<div class="empty">Cliente não encontrado.</div>`, 'Clientes');
  }

  const c = `<div class="page-head"><div><div class="muted" style="font-size:13px"><a href="#/clientes">Clientes</a> / ${client.name}</div><h1>${client.name}</h1></div><div style="display:flex;gap:10px"><a class="btn btn-secondary" href="#/busca" id="newSearchForClient">Nova busca</a><button class="btn btn-primary" id="editClientBtn">Editar cliente</button></div></div><div class="grid profile-grid"><aside class="card profile-card"><div class="profile-top"><div class="avatar">${initials(client.name)}</div><h2>${client.name}</h2><span class="status ${client.status.toLowerCase()}">${client.status}</span></div><div class="profile-info"><div class="info-line">${icon('whatsapp')}<div><strong>${client.phone || '—'}</strong><div class="muted">Telefone</div></div></div><div class="info-line">${icon('file')}<div><strong>${client.email || '—'}</strong><div class="muted">E-mail</div></div></div><div class="divider"></div><div><strong>Preferências gerais</strong><p class="muted" style="font-size:13px;line-height:1.55">${client.preferences || 'Nenhuma preferência registrada ainda.'}</p></div></div></aside><section class="card timeline"><div class="panel-head"><h3>Histórico do cliente</h3></div>${
    events.length
      ? events
          .map(
            (e) =>
              `<div class="timeline-item"><div class="timeline-bullet">${icon(e.type === 'WhatsAppShare' ? 'whatsapp' : 'search')}</div><div><h4>${e.type === 'WhatsAppShare' ? 'Seleção compartilhada' : 'Busca realizada'}</h4><p>${e.summary} · ${formatDate(e.createdAt)}</p>${
                e.searchRequestId ? `<a href="#/resultados/${e.searchRequestId}" class="btn btn-secondary btn-sm" style="margin-top:10px">Ver ${e.resultCount} resultados</a>` : ''
              }</div></div>`
          )
          .join('')
      : '<div class="empty">Nenhum evento registrado ainda.</div>'
  }</section></div>`;

  return { html: shell('clientes', c, client.name), client };
}

export function bindClientDetail(client) {
  $('#editClientBtn')?.addEventListener('click', () => openClientModal(client, () => location.reload()));
  $('#newSearchForClient')?.addEventListener('click', () => {
    import('../state.js').then(({ searchDraft }) => {
      searchDraft.clientId = String(client.id);
    });
  });
}

export async function deleteClient(id) {
  const ok = await confirmModal('Remover cliente', 'Tem certeza que deseja remover este cliente? Essa ação não pode ser desfeita.', {
    confirmLabel: 'Remover',
  });
  if (!ok) return false;
  await api.clients.remove(id);
  toast('Cliente removido.');
  return true;
}

function initials(name) {
  return name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase();
}

function formatDate(iso) {
  const d = new Date(iso);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) + ', ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}
