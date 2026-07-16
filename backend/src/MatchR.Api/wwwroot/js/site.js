// Interações pontuais no DOM já renderizado pelo servidor (Razor Views).
// Este arquivo nunca monta HTML — só manipula elementos que já existem na página.
(function () {
  function toast(msg) {
    var t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.classList.add('show');
    setTimeout(function () { t.classList.remove('show'); }, 2500);
  }
  window.toast = toast;

  document.addEventListener('DOMContentLoaded', function () {
    if (window.__toastMessage) toast(window.__toastMessage);

    // Menu mobile
    var menuBtn = document.getElementById('menuBtn');
    var sidebar = document.getElementById('sidebar');
    var overlay = document.getElementById('overlay');
    menuBtn && menuBtn.addEventListener('click', function () {
      sidebar.classList.add('open');
      overlay.classList.add('show');
    });
    overlay && overlay.addEventListener('click', function () {
      sidebar.classList.remove('open');
      overlay.classList.remove('show');
    });

    // Âncoras da landing (scroll suave)
    document.querySelectorAll('.landing a[href^="#"]').forEach(function (a) {
      a.addEventListener('click', function (e) {
        var target = document.querySelector(a.getAttribute('href'));
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      });
    });

    // Slider de faixa de preço (tela de busca)
    var minRange = document.getElementById('priceMinRange');
    var maxRange = document.getElementById('priceMaxRange');
    var fill = document.getElementById('priceFill');
    function updatePriceRange() {
      if (!minRange || !maxRange || !fill) return;
      var minVal = Number(minRange.value), maxVal = Number(maxRange.value);
      if (minVal > maxVal - 0.5) {
        if (document.activeElement === minRange) minVal = maxVal - 0.5;
        else maxVal = minVal + 0.5;
        minRange.value = minVal;
        maxRange.value = maxVal;
      }
      var minPct = (minVal / Number(minRange.max)) * 100;
      var maxPct = (maxVal / Number(maxRange.max)) * 100;
      fill.style.left = minPct + '%';
      fill.style.right = (100 - maxPct) + '%';
      var format = function (v) { return v === 0 ? 'Sem mínimo' : v === 20 ? 'R$ 20 mi+' : 'R$ ' + String(v).replace('.', ',') + ' mi'; };
      var minLabel = document.getElementById('priceMinLabel');
      var maxLabel = document.getElementById('priceMaxLabel');
      if (minLabel) minLabel.textContent = format(minVal);
      if (maxLabel) maxLabel.textContent = format(maxVal);
    }
    if (minRange && maxRange) {
      minRange.addEventListener('input', updatePriceRange);
      maxRange.addEventListener('input', updatePriceRange);
      updatePriceRange();
    }

    // Chips de exemplo de briefing (só preenche o textarea já existente)
    document.querySelectorAll('.prompt-chip').forEach(function (chip) {
      chip.addEventListener('click', function () {
        var briefing = document.getElementById('briefing');
        if (briefing) briefing.value = chip.textContent + ' em região nobre de São Paulo, com boa iluminação e acabamento contemporâneo.';
      });
    });

    // Upload de planilha: feedback visual simples, o upload em si é um <form> normal
    var fileInput = document.getElementById('fileInput');
    var chooseBtn = document.getElementById('chooseFile');
    var uploadForm = document.getElementById('uploadForm');
    chooseBtn && chooseBtn.addEventListener('click', function () { fileInput.click(); });
    fileInput && fileInput.addEventListener('change', function () {
      if (fileInput.files[0]) {
        document.getElementById('selectedFileName').textContent = fileInput.files[0].name;
        uploadForm.submit();
      }
    });
    var dropZone = document.getElementById('uploadZone');
    if (dropZone) {
      ['dragover', 'dragleave', 'drop'].forEach(function (evt) {
        dropZone.addEventListener(evt, function (e) {
          e.preventDefault();
          dropZone.classList.toggle('drag', evt === 'dragover');
          if (evt === 'drop' && e.dataTransfer.files[0]) {
            fileInput.files = e.dataTransfer.files;
            fileInput.dispatchEvent(new Event('change'));
          }
        });
      });
    }

    // Filtro client-side da tabela de clientes (só esconde/mostra linhas já renderizadas)
    var clientSearch = document.getElementById('clientSearch');
    var statusFilter = document.getElementById('statusFilter');
    function filterClientRows() {
      var term = (clientSearch && clientSearch.value || '').toLowerCase();
      var status = (statusFilter && statusFilter.value) || '';
      document.querySelectorAll('#clientRows tr[data-name]').forEach(function (row) {
        var matchesTerm = row.dataset.name.indexOf(term) !== -1;
        var matchesStatus = !status || row.dataset.status === status;
        row.style.display = matchesTerm && matchesStatus ? '' : 'none';
      });
    }
    clientSearch && clientSearch.addEventListener('input', filterClientRows);
    statusFilter && statusFilter.addEventListener('change', filterClientRows);
  });
})();
