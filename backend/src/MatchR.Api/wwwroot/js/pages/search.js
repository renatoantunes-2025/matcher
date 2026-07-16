import { icon } from '../icons.js';
import { $, $$ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { searchDraft } from '../state.js';
import { toast } from '../components/toast.js';

const propertyTypes = [
  ['Casa', 'Casa'],
  ['CasaEmCondominio', 'Casa em condomínio'],
  ['Apartamento', 'Apartamento'],
  ['Cobertura', 'Cobertura'],
  ['CasaDeVila', 'Casa de Vila'],
  ['Duplex', 'Duplex'],
];

const featureGroups = [
  ['Condomínio', ['Academia', 'Piscina aquecida', 'Lavanderia', 'Quadra de tênis']],
  ['Comodidade', ['Chuveiro a gás', 'Garden', 'Ar condicionado', 'Varanda']],
  ['Bem-estar', ['Rua silenciosa', 'Sol da manhã', 'Vista livre']],
  ['Cômodos', ['Home-office', 'Cozinha americana']],
  ['Estilo arquitetônico', ['Moderno', 'Contemporâneo', 'Neoclássico']],
];

function optionButtons(name) {
  return `<div class="option-buttons" role="group" aria-label="${name}">${['Qualquer', '1', '2', '3', '4', '5+']
    .map((v, i) => `<label class="option-button"><input type="radio" name="${name}" value="${v}" ${i === 0 ? 'checked' : ''}><span>${v}</span></label>`)
    .join('')}</div>`;
}

export async function searchPage() {
  let clients = [];
  let agencies = [];
  try {
    [clients, agencies] = await Promise.all([api.clients.list(), api.agencies.list()]);
  } catch (err) {
    toast(err.message);
  }

  const preselectedClient = searchDraft.clientId;
  const briefingValue = searchDraft.briefing || '';

  const c = `<div class="page-head"><div><h1>Busca inteligente</h1><p class="muted">Conte o que o cliente procura. O MatchR interpreta o briefing e prioriza os imóveis mais aderentes.</p></div></div><div class="search-layout"><section class="card briefing-card"><div class="form-group"><label>Cliente</label><select class="select" id="searchClient">${
    clients.length ? clients.map((x) => `<option value="${x.id}" ${String(x.id) === preselectedClient ? 'selected' : ''}>${x.name}</option>`).join('') : '<option value="">Cadastre um cliente primeiro</option>'
  }</select></div><div class="form-group" style="margin-top:18px"><label>Apelido da Oportunidade</label><input class="input" id="searchLabel" placeholder="Ex.: Apartamento Mariana – Itaim"></div><div class="form-group" style="margin-top:18px"><label>Briefing em linguagem natural</label><textarea class="textarea" id="briefing" placeholder="Descreva o que o cliente procura...">${briefingValue}</textarea></div><div class="prompt-examples"><button type="button" class="prompt-chip">Casa em condomínio com piscina</button><button type="button" class="prompt-chip">Cobertura próxima ao parque</button><button type="button" class="prompt-chip">Imóvel com home office</button></div><div class="search-bottom"><div class="ai-note">${icon('spark')} O texto livre será combinado aos filtros.</div><button class="btn btn-primary" id="runSearch">Buscar imóveis ${icon('arrow')}</button></div></section><aside class="card filters-card"><div class="panel-head"><h3>Filtros opcionais</h3><button class="btn btn-ghost btn-sm" id="clearFilters">Limpar</button></div><div class="filters-grid"><div class="form-group filter-span"><label>Localização</label><input class="input" id="fLocation" placeholder="Ex.: Itaim Bibi, Vila Nova Conceição"></div><div class="form-group"><label>Tipo</label><select class="select" id="fType"><option value="">Qualquer tipo</option>${propertyTypes
    .map(([v, label]) => `<option value="${v}">${label}</option>`)
    .join('')}</select></div><div class="form-group"><label>Finalidade</label><select class="select" id="fPurpose"><option value="Compra">Compra</option><option value="Locacao">Locação</option></select></div><div class="form-group filter-span"><label>Imobiliária</label><select class="select" id="fAgency"><option value="">Todas as imobiliárias</option>${agencies
    .map((a) => `<option value="${a.id}">${a.name}</option>`)
    .join('')}</select></div><div class="form-group filter-span"><label>Faixa de preço</label><div class="price-range" id="priceRange"><div class="range-track"><div class="range-fill" id="priceFill"></div><input type="range" id="priceMinRange" min="0" max="20" step="0.5" value="0" aria-label="Preço mínimo em milhões"><input type="range" id="priceMaxRange" min="0" max="20" step="0.5" value="20" aria-label="Preço máximo em milhões"></div><div class="range-values"><div class="range-value"><span>Mín.</span><strong id="priceMinLabel">Sem mínimo</strong></div><span class="range-separator">–</span><div class="range-value"><span>Máx.</span><strong id="priceMaxLabel">R$ 20 mi+</strong></div></div></div></div><div class="form-group filter-span"><label>Área mínima (m²)</label><input class="input" id="fArea" type="number" min="0" placeholder="Ex.: 220"></div><div class="form-group filter-span number-filter"><label>Dormitórios</label>${optionButtons(
    'dormitorios'
  )}</div><div class="form-group filter-span number-filter"><label>Suítes</label>${optionButtons('suites')}</div><div class="form-group filter-span number-filter"><label>Vagas</label>${optionButtons(
    'vagas'
  )}</div><div class="filter-span additional-features"><h4>Itens adicionais</h4>${featureGroups
    .map(
      ([category, items]) =>
        `<section class="feature-check-group"><h5>${category}</h5><div class="feature-check-grid">${items
          .map((item) => `<label class="check-option"><input type="checkbox" value="${item}"><span>${item}</span></label>`)
          .join('')}</div></section>`
    )
    .join('')}</div></div></aside></div>`;

  return shell('busca', c, 'Nova busca');
}

export function bindSearch() {
  $$('#priceMinRange, #priceMaxRange').forEach((r) => r.addEventListener('input', updatePriceRange));
  updatePriceRange();

  $$('.prompt-chip').forEach((b) =>
    b.addEventListener('click', () => {
      $('#briefing').value = b.textContent + ' em região nobre de São Paulo, com boa iluminação e acabamento contemporâneo.';
    })
  );

  $('#clearFilters')?.addEventListener('click', () => {
    $('#fLocation').value = '';
    $('#fType').value = '';
    $('#fAgency').value = '';
    $('#fArea').value = '';
    $$('.filters-card input[type="checkbox"]').forEach((i) => (i.checked = false));
    $$('.option-buttons').forEach((g) => {
      const first = g.querySelector('input');
      if (first) first.checked = true;
    });
    $('#priceMinRange').value = 0;
    $('#priceMaxRange').value = 20;
    updatePriceRange();
  });

  $('#runSearch')?.addEventListener('click', async () => {
    const clientId = $('#searchClient')?.value;
    if (!clientId) {
      toast('Selecione um cliente para continuar.');
      return;
    }

    const btn = $('#runSearch');
    btn.disabled = true;

    const payload = buildPayload(Number(clientId));

    try {
      const result = await api.searches.create(payload);
      searchDraft.clientId = null;
      searchDraft.briefing = '';
      toast('Briefing interpretado. Ordenando resultados...');
      location.hash = `#/resultados/${result.searchId}`;
    } catch (err) {
      toast(err.message);
    } finally {
      btn.disabled = false;
    }
  });
}

function buildPayload(clientId) {
  const optionValue = (name) => {
    const checked = document.querySelector(`input[name="${name}"]:checked`);
    return checked && checked.value !== 'Qualquer' ? Number(checked.value.replace('+', '')) : null;
  };

  const priceMin = Number($('#priceMinRange').value);
  const priceMax = Number($('#priceMaxRange').value);

  const features = $$('.filters-card input[type="checkbox"]:checked').map((c) => c.value);

  return {
    clientId,
    label: $('#searchLabel').value || null,
    briefingText: $('#briefing').value || '',
    location: $('#fLocation').value || null,
    type: $('#fType').value || null,
    purpose: $('#fPurpose').value || null,
    agencyId: $('#fAgency').value ? Number($('#fAgency').value) : null,
    priceMin: priceMin > 0 ? priceMin * 1_000_000 : null,
    priceMax: priceMax < 20 ? priceMax * 1_000_000 : null,
    minArea: $('#fArea').value ? Number($('#fArea').value) : null,
    bedrooms: optionValue('dormitorios'),
    suites: optionValue('suites'),
    parkingSpots: optionValue('vagas'),
    features,
  };
}

function updatePriceRange() {
  const min = $('#priceMinRange'),
    max = $('#priceMaxRange'),
    fill = $('#priceFill');
  if (!min || !max || !fill) return;
  let minVal = Number(min.value),
    maxVal = Number(max.value);
  if (minVal > maxVal - 0.5) {
    if (document.activeElement === min) minVal = maxVal - 0.5;
    else maxVal = minVal + 0.5;
    min.value = minVal;
    max.value = maxVal;
  }
  const minPct = (minVal / Number(min.max)) * 100,
    maxPct = (maxVal / Number(max.max)) * 100;
  fill.style.left = minPct + '%';
  fill.style.right = 100 - maxPct + '%';
  const format = (v) => (v === 0 ? 'Sem mínimo' : v === 20 ? 'R$ 20 mi+' : `R$ ${String(v).replace('.', ',')} mi`);
  $('#priceMinLabel').textContent = format(minVal);
  $('#priceMaxLabel').textContent = format(maxVal);
}
