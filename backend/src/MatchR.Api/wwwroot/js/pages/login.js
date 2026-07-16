import { $ } from '../dom.js';
import { api, auth } from '../api.js';
import { toast } from '../components/toast.js';

export function login() {
  return `<div class="auth-shell"><section class="auth-art"><div class="auth-logo-wrap"><img class="logo-full" src="assets/logo-matchr-full.png" alt="MatchR"></div><div class="auth-quote"><div class="kicker" style="color:#fff">Menos pesquisa. Mais relacionamento.</div><h2>Inteligência para transformar briefings em oportunidades.</h2></div><small>MatchR · MVP 2026</small></section><section class="auth-panel"><form class="auth-box" id="loginForm"><img src="assets/logo-matchr-icon.png" alt="" style="width:72px"><h1>Bem-vindo de volta</h1><p class="muted">Entre para acessar seus clientes e buscas.</p><div class="auth-form"><div class="form-group"><label>E-mail</label><input class="input" name="email" type="email" required></div><div class="form-group"><label>Senha</label><input class="input" name="password" type="password" required></div><div class="auth-actions"><label><input type="checkbox" checked> Lembrar acesso</label><a href="#" style="color:var(--brand);font-weight:700">Esqueci minha senha</a></div><button class="btn btn-primary" type="submit" style="width:100%">Entrar</button><a class="btn btn-secondary" href="#/" style="width:100%">Voltar para o site</a></div></form></section></div>`;
}

export function bindLogin() {
  $('#loginForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    const submitBtn = form.querySelector('button[type="submit"]');
    submitBtn.disabled = true;
    try {
      const { token, name, email, role } = await api.auth.login(form.email.value, form.password.value);
      auth.setSession(token, { name, email, role });
      location.hash = '#/dashboard';
    } catch (err) {
      toast(err.message || 'Não foi possível entrar.');
    } finally {
      submitBtn.disabled = false;
    }
  });
}
