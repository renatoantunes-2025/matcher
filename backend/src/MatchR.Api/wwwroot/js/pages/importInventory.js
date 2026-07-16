import { icon } from '../icons.js';
import { $ } from '../dom.js';
import { api } from '../api.js';
import { shell } from '../shell.js';
import { toast } from '../components/toast.js';

export async function importPage() {
  let history = [];
  try {
    history = await api.import.history();
  } catch (err) {
    toast(err.message);
  }

  const c = `<div class="page-head"><div><h1>Importar inventário</h1><p class="muted">Envie uma planilha Excel ou CSV com as colunas Titulo, Bairro, Cidade, Preco, AreaM2, Dormitorios, Suites, Vagas, Tipo, Finalidade, Imobiliaria, ImagemUrl, LinkOrigem e Caracteristicas.</p></div></div><section class="card panel"><div class="upload-zone" id="uploadZone"><div class="stat-icon" style="margin:0 auto 14px">${icon('upload')}</div><h3>Arraste a planilha para cá</h3><p class="muted">ou selecione um arquivo .xlsx ou .csv</p><input id="fileInput" type="file" accept=".xlsx,.csv" hidden><button class="btn btn-primary" id="chooseFile">Selecionar arquivo</button></div></section><section class="card panel" style="margin-top:20px"><div class="panel-head"><h3>Últimas importações</h3></div><div class="table-wrap"><table class="data-table"><thead><tr><th>Arquivo</th><th>Data</th><th>Registros</th><th>Status</th></tr></thead><tbody id="importRows">${
    history.length
      ? history.map(row).join('')
      : '<tr><td colspan="4" class="empty">Nenhuma importação registrada ainda.</td></tr>'
  }</tbody></table></div></section>`;

  return shell('importacao', c, 'Importação');
}

function row(b) {
  const statusClass = b.status === 'Completed' ? 'ativo' : b.status === 'Failed' ? 'lead' : 'proposta';
  const statusLabel = b.status === 'Completed' ? 'Concluído' : b.status === 'Failed' ? 'Falhou' : 'Processando';
  return `<tr><td>${b.fileName}</td><td>${formatDate(b.createdAt)}</td><td>${b.recordCount}</td><td><span class="status ${statusClass}">${statusLabel}</span></td></tr>`;
}

export function bindImport() {
  const choose = $('#chooseFile'),
    input = $('#fileInput'),
    zone = $('#uploadZone');

  choose?.addEventListener('click', () => input.click());

  input?.addEventListener('change', async () => {
    const file = input.files[0];
    if (!file) return;

    zone.innerHTML = `<div class="stat-icon" style="margin:0 auto 14px">${icon('file')}</div><h3>${file.name}</h3><p class="muted">Validando estrutura e registros...</p><div class="progress"><span></span></div>`;

    try {
      const result = await api.import.upload(file);
      toast(
        result.status === 'Failed'
          ? `Falha na importação: ${result.errorMessage || 'erro desconhecido'}`
          : `Planilha validada com sucesso. ${result.recordCount} registros importados.`
      );
    } catch (err) {
      toast(err.message);
    } finally {
      location.reload();
    }
  });

  ['dragover', 'dragleave', 'drop'].forEach((evt) =>
    zone?.addEventListener(evt, (e) => {
      e.preventDefault();
      zone.classList.toggle('drag', evt === 'dragover');
      if (evt === 'drop' && e.dataTransfer.files[0]) {
        input.files = e.dataTransfer.files;
        input.dispatchEvent(new Event('change'));
      }
    })
  );
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) + ', ' + new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}
