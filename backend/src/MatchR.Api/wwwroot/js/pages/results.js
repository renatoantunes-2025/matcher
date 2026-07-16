import { icon } from '../icons.js';
import { $, $$ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { toast } from '../components/toast.js';
import { openModal } from '../components/modal.js';

let currentSearch = null;
let favoriteIds = new Set();

export async function resultsPage(searchId) {
  try {
    const [search, favorites] = await Promise.all([api.searches.get(searchId), api.favorites.list()]);
    currentSearch = search;
    favoriteIds = new Set(favorites.map((f) => f.id));
  } catch (err) {
    toast(err.message);
    return shell('busca', '<div class="empty">Busca não encontrada.</div>', 'Resultados');
  }

  const c = `<div class="results-header"><div><h1 style="font-family:Manrope;margin:0">${currentSearch.results.length} imóveis encontrados</h1><p class="muted">Ordenados por compatibilidade com o briefing de ${currentSearch.clientName}.</p></div></div><div class="results-layout"><section class="result-list" id="resultList">${
    currentSearch.results.length ? currentSearch.results.map(resultCard).join('') : '<div class="card empty">Nenhum imóvel ativo corresponde a este briefing ainda.</div>'
  }</section><aside class="card selection-panel" id="selectionPanel">${selectionPanelHtml()}</aside></div>`;

  return shell('busca', c, 'Resultados');
}

function resultCard(r) {
  const p = r.property;
  const isFav = favoriteIds.has(p.id);
  return `<article class="property-card"><div class="property-image"><img src="${p.imageUrl || ''}" alt="${p.title}"><span class="score-badge">${r.score}% Match</span><div class="property-actions-top"><button class="icon-btn favorite ${isFav ? 'active' : ''}" data-fav="${p.id}" aria-label="Favoritar">${icon('heart')}</button></div></div><div class="property-body"><div class="property-kicker">${p.agency} · ${p.neighborhood}</div><h3>${p.title}</h3><div class="property-price">${formatPrice(p.price)}</div><div class="property-facts"><span>${p.areaM2} m²</span><span>${p.bedrooms} dorm.</span><span>${p.suites} suítes</span><span>${p.parkingSpots} vagas</span></div><div class="match-reasons">${r.reasons.map((reason) => `<span class="pill brand">${reason}</span>`).join('')}</div><div class="property-footer"><label class="select-check"><input type="checkbox" data-select="${p.id}" ${r.selected ? 'checked' : ''}> Selecionar</label>${
    p.sourceUrl ? `<a class="btn btn-secondary btn-sm" href="${p.sourceUrl}" target="_blank">Ver ficha na origem ${icon('external')}</a>` : ''
  }</div></div></article>`;
}

function selectionPanelHtml() {
  const selected = currentSearch.results.filter((r) => r.selected);
  return `<div class="panel-head"><h3>Seleção para envio</h3><span class="pill brand">${selected.length}</span></div><p class="muted" style="font-size:13px">Revise os imóveis antes de abrir o WhatsApp.</p><div class="selection-list">${
    selected.length
      ? selected.map((r) => `<div class="selection-item"><img src="${r.property.imageUrl || ''}" alt=""><div class="meta"><strong>${r.property.title}</strong><small>${formatPrice(r.property.price)}</small></div><button class="icon-btn btn-sm" data-remove="${r.propertyId}">${icon('close')}</button></div>`).join('')
      : '<div class="empty">Nenhum imóvel selecionado.</div>'
  }</div><button class="btn btn-primary" id="shareBtn" style="width:100%" ${selected.length ? '' : 'disabled'}>${icon('whatsapp')} Compartilhar seleção</button><button class="btn btn-ghost btn-sm" style="width:100%;margin-top:8px" id="clearSelection">Limpar seleção</button>`;
}

export function bindResults() {
  if (!currentSearch) return;

  $$('[data-fav]').forEach((b) =>
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.fav);
      try {
        if (favoriteIds.has(id)) {
          await api.favorites.remove(id);
          favoriteIds.delete(id);
          toast('Imóvel removido dos favoritos.');
        } else {
          await api.favorites.add(id);
          favoriteIds.add(id);
          toast('Imóvel adicionado aos favoritos.');
        }
        b.classList.toggle('active');
      } catch (err) {
        toast(err.message);
      }
    })
  );

  $$('[data-select]').forEach((ch) =>
    ch.addEventListener('change', async () => {
      const id = Number(ch.dataset.select);
      try {
        currentSearch = await api.searches.setSelection(currentSearch.searchId, id, ch.checked);
        renderSelectionPanel();
      } catch (err) {
        toast(err.message);
        ch.checked = !ch.checked;
      }
    })
  );

  bindSelectionPanel();
}

function renderSelectionPanel() {
  $('#selectionPanel').innerHTML = selectionPanelHtml();
  bindSelectionPanel();
}

function bindSelectionPanel() {
  $$('[data-remove]').forEach((b) =>
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.remove);
      try {
        currentSearch = await api.searches.setSelection(currentSearch.searchId, id, false);
        document.querySelector(`[data-select="${id}"]`)?.removeAttribute('checked');
        renderSelectionPanel();
      } catch (err) {
        toast(err.message);
      }
    })
  );

  $('#clearSelection')?.addEventListener('click', async () => {
    const selectedIds = currentSearch.results.filter((r) => r.selected).map((r) => r.propertyId);
    for (const id of selectedIds) {
      currentSearch = await api.searches.setSelection(currentSearch.searchId, id, false);
    }
    $$('[data-select]').forEach((x) => (x.checked = false));
    renderSelectionPanel();
  });

  $('#shareBtn')?.addEventListener('click', () => openShareModal());
}

function openShareModal() {
  const selected = currentSearch.results.filter((r) => r.selected);
  const html = `<div class="modal-backdrop" id="modalBackdrop"><div class="modal"><div class="modal-head"><h3>Compartilhar pelo WhatsApp</h3><button class="icon-btn" id="closeModal">${icon('close')}</button></div><div class="modal-body"><p>Você selecionou <strong>${selected.length} imóveis</strong> para ${currentSearch.clientName}.</p><div class="form-group"><label>Mensagem opcional</label><textarea class="textarea" id="waMessage">Olá, ${currentSearch.clientName}! Separei estes imóveis com maior compatibilidade com o seu perfil. Veja as opções abaixo:</textarea></div><div class="selection-list">${selected
    .map((r) => `<div class="selection-item"><img src="${r.property.imageUrl || ''}" alt=""><div class="meta"><strong>${r.property.title}</strong><small>${formatPrice(r.property.price)}</small></div></div>`)
    .join('')}</div></div><div class="modal-actions"><button class="btn btn-secondary" id="cancelModal">Voltar</button><button class="btn btn-primary" id="openWa">${icon('whatsapp')} Abrir WhatsApp</button></div></div></div>`;

  openModal(html, {
    onMount: (close) => {
      $('#openWa').addEventListener('click', async () => {
        const message = $('#waMessage').value + '\n\n' + selected.map((r) => `${r.property.title} – ${formatPrice(r.property.price)}${r.property.sourceUrl ? `\n${r.property.sourceUrl}` : ''}`).join('\n\n');
        window.open('https://wa.me/?text=' + encodeURIComponent(message), '_blank');
        try {
          await api.searches.share(currentSearch.searchId, $('#waMessage').value);
          toast('Compartilhamento registrado no histórico.');
        } catch (err) {
          toast(err.message);
        }
        close();
      });
    },
  });
}

function formatPrice(value) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value);
}
