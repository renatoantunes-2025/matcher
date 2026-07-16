import { icon } from '../icons.js';
import { $$ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { toast } from '../components/toast.js';

let favorites = [];

export async function favoritesPage() {
  try {
    favorites = await api.favorites.list();
  } catch (err) {
    toast(err.message);
    favorites = [];
  }

  const c = `<div class="page-head"><div><h1>Favoritos</h1><p class="muted">Imóveis salvos para consulta rápida.</p></div></div><div class="grid property-grid" id="favoriteGrid">${
    favorites.length
      ? favorites.map(tile).join('')
      : '<div class="card empty">Nenhum imóvel favoritado ainda. Favorite direto na tela de resultados.</div>'
  }</div>`;

  return shell('favoritos', c, 'Favoritos');
}

function tile(p) {
  return `<article class="card property-tile" data-id="${p.id}"><div class="tile-image"><img src="${p.imageUrl || ''}" alt="${p.title}"><button class="icon-btn favorite active" data-fav="${p.id}" style="position:absolute;right:12px;top:12px">${icon('heart')}</button></div><div class="tile-body"><div class="property-kicker">${p.neighborhood}</div><h3>${p.title}</h3><div class="property-price">${formatPrice(p.price)}</div>${
    p.sourceUrl ? `<a class="btn btn-secondary btn-sm" href="${p.sourceUrl}" target="_blank">Ver detalhes</a>` : ''
  }</div></article>`;
}

export function bindFavorites() {
  $$('[data-fav]').forEach((b) =>
    b.addEventListener('click', async () => {
      const id = Number(b.dataset.fav);
      try {
        await api.favorites.remove(id);
        toast('Imóvel removido dos favoritos.');
        document.querySelector(`.property-tile[data-id="${id}"]`)?.remove();
      } catch (err) {
        toast(err.message);
      }
    })
  );
}

function formatPrice(value) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value);
}
