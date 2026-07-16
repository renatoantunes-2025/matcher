import { icon } from '../icons.js';
import { $, $$ } from '../dom.js';
import { openModal } from '../components/modal.js';
import { api } from '../api.js';
import { toast } from '../components/toast.js';

const heroSamples = [
  { price: 'R$ 4.850.000', bairro: 'Itaim Bibi, São Paulo', img: 'https://images.unsplash.com/photo-1600607687920-4e2a09cf159d?auto=format&fit=crop&w=1000&q=82' },
  { price: 'R$ 6.900.000', bairro: 'Alphaville, Barueri', img: 'https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?auto=format&fit=crop&w=1000&q=82' },
];

export function landing() {
  return `<div class="landing"><header class="landing-header"><div class="container landing-nav"><img class="logo-full" src="assets/logo-matchr-full.png" alt="MatchR"><nav class="landing-links"><a href="#como-funciona">Como funciona</a><a href="#beneficios">Benefícios</a><a href="#solicitar">Solicitar acesso</a><a class="btn btn-dark btn-sm" href="#/login">Entrar</a></nav></div></header><section class="hero"><div class="container hero-grid"><div><div class="kicker">Inteligência imobiliária para corretores</div><h1>Encontre o imóvel certo em <span>poucos minutos.</span></h1><p>Transforme o briefing do seu cliente em uma seleção curta e priorizada de imóveis de alto padrão, reunindo diferentes imobiliárias em uma única experiência.</p><div class="hero-actions"><a href="#solicitar" class="btn btn-primary">Solicitar acesso ${icon('arrow')}</a><a href="#como-funciona" class="btn btn-secondary">Conhecer a plataforma</a></div><div class="hero-note"><span>${icon('check')} Busca inteligente</span><span>${icon('check')} Score de compatibilidade</span><span>${icon('check')} Compartilhamento rápido</span></div></div><div class="hero-visual"><div class="search-mock"><div class="float-score">96% Match</div><div class="mock-top"><strong>Resultados para Mariana</strong><span class="pill brand">12 imóveis</span></div><div class="mock-search">${icon('search')} Apartamento no Itaim, 4 dormitórios, varanda gourmet...</div><div class="mock-results">${heroSamples
    .map((p) => `<div class="mock-property"><img src="${p.img}" alt=""><div class="body"><strong>${p.price}</strong><div class="muted" style="font-size:11px;margin-top:4px">${p.bairro}</div></div></div>`)
    .join('')}</div></div></div></div></section><section class="trust-strip" id="beneficios"><div class="container trust-card"><div><strong>1 só busca</strong><span class="muted">para múltiplas imobiliárias</span></div><div><strong>Score claro</strong><span class="muted">resultados por aderência</span></div><div><strong>Menos cliques</strong><span class="muted">mais tempo para vender</span></div><div><strong>WhatsApp</strong><span class="muted">seleção pronta para enviar</span></div></div></section><section class="feature-section" id="como-funciona"><div class="container"><div class="feature-head"><div><div class="kicker">Como funciona</div><h2 class="section-title">Do briefing ao cliente em três passos.</h2></div><p>Uma experiência simples como um buscador, com a inteligência e a organização que o corretor precisa para trabalhar melhor.</p></div><div class="grid feature-grid"><article class="feature-card"><div class="feature-number">01 – CONTEXTO</div><h3>Selecione o cliente</h3><p>Cadastre ou escolha o cliente para manter buscas, favoritos, compartilhamentos e histórico organizados.</p></article><article class="feature-card"><div class="feature-number">02 – INTELIGÊNCIA</div><h3>Descreva o imóvel ideal</h3><p>Use linguagem natural, filtros estruturados ou os dois. O MatchR interpreta localização, perfil, preço e características.</p></article><article class="feature-card"><div class="feature-number">03 – AÇÃO</div><h3>Revise e compartilhe</h3><p>Compare os resultados ordenados por score, selecione os melhores imóveis e envie pelo WhatsApp.</p></article></div></div></section><section class="landing-form-section" id="solicitar"><div class="container access-grid"><div class="access-copy"><div class="kicker">Solicitar acesso</div><h2 class="section-title">Pronto para pesquisar menos e atender melhor?</h2><p class="muted" style="line-height:1.7">Solicite acesso ao MVP. O cadastro passará por uma validação rápida antes da liberação.</p></div><form class="card access-form" id="accessForm"><div class="field-row"><div class="form-group"><label>Nome completo</label><input class="input" name="name" required placeholder="Seu nome"></div><div class="form-group"><label>CRECI</label><input class="input" name="creci" required placeholder="CRECI-SP 123456"></div></div><div class="form-group" style="margin-top:14px"><label>E-mail profissional</label><input class="input" name="email" type="email" required placeholder="voce@imobiliaria.com.br"></div><div class="form-group" style="margin-top:14px"><label>Telefone</label><input class="input" name="phone" placeholder="(11) 99999-9999"></div><button class="btn btn-primary" style="width:100%;margin-top:20px">Solicitar acesso</button><p class="muted" style="font-size:11px;text-align:center">Ao enviar, você concorda com os termos e política de privacidade.</p></form></div></section><footer class="footer"><div class="container footer-row"><img class="logo-full" src="assets/logo-matchr-full.png" alt="MatchR"><span>© 2026 MatchR. Protótipo funcional do MVP.</span></div></footer></div>`;
}

function accessSuccessModal() {
  return `<div class="modal-backdrop access-success-backdrop" id="modalBackdrop"><div class="modal access-success-modal"><button class="icon-btn access-success-close" id="closeModal" aria-label="Fechar">${icon('close')}</button><div class="access-success-icon">${icon('check')}</div><h3>Solicitação enviada!</h3><p>Você receberá um e-mail de confirmação assim que o seu cadastro for analisado.</p><button class="btn btn-primary" id="cancelModal">Entendi</button></div></div>`;
}

export function bindLanding() {
  $$('.landing a[href^="#"]:not([href^="#/"])').forEach((a) =>
    a.addEventListener('click', (e) => {
      const target = $(a.getAttribute('href'));
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        history.replaceState(null, '', a.getAttribute('href'));
      }
    })
  );

  $('#accessForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    const payload = {
      name: form.name.value,
      creci: form.creci.value,
      email: form.email.value,
      phone: form.phone.value || null,
    };
    try {
      await api.auth.requestAccess(payload);
      openModal(accessSuccessModal());
      form.reset();
    } catch (err) {
      toast(err.message);
    }
  });
}
