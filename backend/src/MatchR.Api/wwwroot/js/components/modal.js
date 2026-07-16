import { $ } from '../dom.js';
import { icon } from '../icons.js';

export function openModal(html, { onMount } = {}) {
  document.body.insertAdjacentHTML('beforeend', html);
  const close = () => $('#modalBackdrop')?.remove();
  $('#closeModal')?.addEventListener('click', close);
  $('#cancelModal')?.addEventListener('click', close);
  $('#modalBackdrop')?.addEventListener('click', (e) => {
    if (e.target.id === 'modalBackdrop') close();
  });
  onMount?.(close);
  return close;
}

export function confirmModal(title, message, { confirmLabel = 'Confirmar' } = {}) {
  return new Promise((resolve) => {
    const html = `<div class="modal-backdrop" id="modalBackdrop"><div class="modal"><div class="modal-head"><h3>${title}</h3><button class="icon-btn" id="closeModal">${icon('close')}</button></div><div class="modal-body"><p>${message}</p></div><div class="modal-actions"><button class="btn btn-secondary" id="cancelModal">Cancelar</button><button class="btn btn-primary" id="confirmModalBtn">${confirmLabel}</button></div></div></div>`;
    const close = openModal(html, {
      onMount: (closeFn) => {
        $('#confirmModalBtn').addEventListener('click', () => {
          closeFn();
          resolve(true);
        });
      },
    });
    $('#closeModal')?.addEventListener('click', () => resolve(false));
    $('#cancelModal')?.addEventListener('click', () => resolve(false));
  });
}
