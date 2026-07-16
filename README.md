# MatchR

Plataforma de inteligência para corretores de imóveis de alto padrão: transforma o
briefing do cliente numa seleção curta de imóveis, ordenada por score de match, pronta
para compartilhar pelo WhatsApp.

**Em produção**: `https://matcher2.tesla.com.br` (IIS + SQL Server).

## Histórico do projeto

O projeto começou como um **protótipo estático** (HTML + CSS + um único `app.js` com
dados mockados em memória, sem backend, sem persistência). Depois virou uma SPA (JS
gerando o HTML de cada tela via template strings, consumindo uma API JSON). Na versão
atual, o frontend é **renderizado no servidor** (ASP.NET Core MVC + Razor Views) — sem
HTML montado em JavaScript.

1. Protótipo HTML/CSS/JS navegável (dados fake em arrays no `app.js`, sem login real).
2. Escolha de stack: **ASP.NET Core + SQL Server + IIS**, por já existir servidor IIS e
   SQL Server disponíveis na empresa — é a combinação mais nativa do ecossistema
   Microsoft para esse cenário (módulo do IIS mantido pela própria Microsoft).
3. Backend construído do zero (models, EF Core, JWT, motor de match, importação de
   planilha) — ver seção **Arquitetura**.
4. Primeira versão do frontend: SPA em ES modules consumindo a API JSON via `fetch`.
5. Deploy no IIS + SQL Server do zero, com todos os problemas reais de infraestrutura
   resolvidos no processo — ver **Problemas encontrados no deploy** abaixo, útil caso
   apareçam de novo num novo ambiente.
6. **Migração da SPA para MVC/Razor Views**: cada tela virou um `Controller` (em
   `Controllers/Web/`) que consulta o banco direto e retorna `View()`, com o HTML em
   arquivos `.cshtml` — não em JS. O `wwwroot/js/site.js` que sobrou só faz pequenas
   interações no DOM já renderizado (slider de preço, menu mobile, auto-submit de
   checkbox) — nunca constrói HTML. Os controllers de API (`/api/...`) foram mantidos
   como uma segunda forma de acessar o backend, autenticados por JWT. **As rotas mudaram**
   de hash (`#/dashboard`, `#/clientes`) para caminhos reais (`/dashboard`, `/clientes`) —
   links antigos salvos como favorito não funcionam mais.
7. **CSS migrado para SASS** (`Styles/*.scss`), compilado automaticamente pelo
   `AspNetCore.SassCompiler` — ver seção **Estilos (SASS)**.

## Stack

- **Backend**: ASP.NET Core 8 (MVC + Web API) + Entity Framework Core + SQL Server,
  motor de match baseado em regras (**sem LLM/IA** — decisão consciente para o MVP, ver
  seção **Motor de match**).
- **Frontend**: **Razor Views renderizadas no servidor** (`.cshtml`), sem framework JS.
  JavaScript (`wwwroot/js/site.js`) é usado só para pequenas interações (nunca para
  montar HTML). Servido pelo próprio ASP.NET Core — **um único site, uma única porta**,
  sem CORS para configurar.
- **CSS em SASS**, compilado automaticamente pelo próprio `dotnet build`/`publish` (pacote
  `AspNetCore.SassCompiler`, sem depender de Node.js) — ver seção **Estilos (SASS)**.
- **Duas formas de autenticação, lado a lado**:
  - **Cookie** para as páginas MVC (navegação normal do navegador, formulários com
    `[ValidateAntiForgeryToken]`).
  - **JWT Bearer** para o `/api` (pensado para integrações externas — app mobile, outro
    sistema, etc.), inalterado desde a versão anterior.

```
backend/
  MatchR.sln
  src/MatchR.Api/
    Controllers/
      *.cs                  API JSON (/api/...), autenticada por JWT
      Web/                   MVC — uma classe por área de tela, retornam View()
        HomeController.cs     landing pública, login, logout, solicitação de acesso
        DashboardController.cs
        ClientsController.cs  lista, detalhe, criar/editar/excluir
        SearchController.cs   formulário de busca (roda o motor de match)
        ResultsController.cs  resultados, seleção, favoritar, compartilhar
        FavoritesController.cs, HistoryController.cs
        AdminController.cs, ImportController.cs   (só role Admin)
        WebControllerBase.cs  base com BrokerId/BrokerName/IsAdmin a partir do cookie
    Views/
      Shared/_Layout.cshtml        shell (sidebar/topbar) das telas internas
      Shared/_PublicLayout.cshtml  layout enxuto (landing/login)
      Home/, Dashboard/, Clients/, Search/, Results/, Favorites/, History/, Admin/,
      Import/                      um .cshtml por tela, mesmo nome da action
    ViewModels/               modelos usados pelas Views (não confundir com Dtos, que
                               são só da API JSON)
    Models/                   entidades do banco (EF Core)
    Data/
      MatchRDbContext.cs       configuração do EF Core (relações, conversões, índices)
      DbInitializer.cs         aplica migrations + cria o Admin inicial no startup
      Migrations/               migrations do EF Core
    Services/
      AuthService.cs            hash de senha (BCrypt) + geração/validação de JWT
      MatchingService.cs         motor de match por regras
      BriefingParser.cs          extrai filtros do texto livre do briefing (regex)
      ImportService.cs           parser de planilha .xlsx/.csv (ClosedXML)
    Dtos/                     contratos de request/response só da API JSON
    Icons.cs, Helpers.cs      SVGs inline e formatação (data, preço, iniciais) usados
                               pelas Views via @Html.Raw / chamadas diretas
    Styles/                   fonte SASS (.scss) — ver seção Estilos (SASS)
      styles.scss               entry point, @use de todos os partials abaixo
      _variables.scss            tokens de design como CSS custom properties
      _base.scss, _common.scss   reset e componentes compartilhados (botões, cards, forms)
      _landing.scss, _auth.scss, _shell.scss, _dashboard.scss, _clients.scss,
      _search.scss, _results.scss, _favorites.scss, _admin.scss, _modal.scss
      _responsive.scss           as 3 media queries do layout, por último
    wwwroot/
      styles.css                 gerado a partir de Styles/ — não versionado, não edite
      assets/                   logos (logo-matchr-icon.png, logo-matchr-full.png)
      js/site.js                 interações pontuais no DOM, nunca gera HTML
```

## Arquitetura do backend

### Entidades (`Models/`)
`Broker` (corretor/admin) · `Client` · `Agency` (imobiliária) · `Property` (imóvel) ·
`SearchRequest` (busca) · `SearchResult` (resultado com score, vinculado a uma busca) ·
`Favorite` · `ShareEvent` (histórico de buscas e compartilhamentos) · `ImportBatch`
(log de importações) · `AccessRequest` (solicitação de acesso da landing page).

### Páginas (MVC, cookie de sessão, `Controllers/Web/`)
- `GET /` landing pública · `GET/POST /entrar` login · `POST /sair` logout ·
  `POST /solicitar-acesso` (público, formulário da landing)
- `GET /dashboard`
- `GET /clientes`, `GET /clientes/{id}`, `GET+POST /clientes/novo`,
  `GET+POST /clientes/{id}/editar`, `POST /clientes/{id}/excluir`
- `GET+POST /busca` (roda o motor de match e redireciona pro resultado)
- `GET /resultados/{id}`, `POST /resultados/{id}/selecionar|favoritar|compartilhar`
- `GET /favoritos`, `POST /favoritos/{propertyId}/remover`
- `GET /historico`
- `GET /admin` (só role `Admin`), `POST /admin/solicitacoes/{id}/aprovar|rejeitar`
- `GET+POST /importacao` (só role `Admin`)

### API JSON (`Controllers/*.cs`, todos sob `/api`, JWT Bearer exceto login/access-request)
- `POST /api/auth/login`, `POST /api/auth/access-requests` (públicos), `GET /api/auth/me`
- `GET/POST/PUT/DELETE /api/clients`
- `GET/POST/PUT/DELETE /api/properties`, `GET /api/agencies`
- `POST /api/searches`, `GET /api/searches/{id}`, `PATCH /api/searches/{id}/selection`,
  `POST /api/searches/{id}/share`
- `GET/POST/DELETE /api/favorites`
- `GET /api/history` (aceita `?clientId=`)
- `GET /api/dashboard/stats|recent-clients|recent-activity`
- `GET /api/admin/access-requests`, `POST /api/admin/access-requests/{id}/approve|reject`,
  `GET /api/admin/brokers`, `GET /api/admin/inventory-summary` (só role `Admin`)
- `POST /api/import`, `GET /api/import`

### Autenticação
Dois esquemas registrados em paralelo (`Program.cs`): **Cookie** (padrão, usado pelas
páginas MVC) e **JWT Bearer** (exigido explicitamente pelos controllers de API via
`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`, herdado de
`ApiControllerBase`). Login pelas páginas MVC (`/entrar`) grava um cookie; login pela API
(`POST /api/auth/login`) devolve um JWT — ambos validam a mesma senha (BCrypt) contra a
tabela `Brokers`.

No primeiro start, `DbInitializer` cria um **Admin inicial** (email/senha de
`appsettings.json` → seção `Admin`). Esse admin aprova as solicitações de acesso da
landing page pela tela **Administração**, o que cria a conta do corretor com uma **senha
temporária** (mostrada uma única vez via toast na aprovação — ainda não existe tela de
"trocar senha" no primeiro login, ver **Próximos passos**).

## Estilos (SASS)

O CSS é escrito em **SASS** (`Styles/*.scss`) e compilado automaticamente para
`wwwroot/styles.css` pelo pacote `AspNetCore.SassCompiler` — sem Node.js, sem passo de
build separado: `dotnet build`/`dotnet run`/`dotnet publish` já compilam. O
`wwwroot/styles.css` **não é versionado** (está no `.gitignore`, é sempre regenerado) —
**edite sempre os arquivos em `Styles/`**, nunca o `.css` final.

- Tokens de design (cores, raio, sombra) ficam em `_variables.scss` como **CSS custom
  properties** (`var(--brand)` etc.), não como variáveis SASS — assim continuam
  inspecionáveis/alteráveis em runtime pelo devtools do navegador.
- Um partial por área de tela (`_landing.scss`, `_dashboard.scss`, `_search.scss` etc.),
  espelhando a mesma divisão das Views.
- Cada seletor é definido **uma única vez** — a versão anterior do CSS (herdada do
  protótipo) tinha uma seção "requested refinements" no final que reescrevia regras já
  declaradas antes (ex.: `.landing-nav` aparecia duas vezes); isso foi consolidado.
- Em desenvolvimento (`dotnet run` em build Debug), um watcher recompila o SCSS a cada
  salvamento (`builder.Services.AddSassCompiler()` em `Program.cs`, só ativo em `#if
  DEBUG`). Em produção o SCSS é compilado uma vez, minificado, durante o `dotnet publish`.

## Motor de match

Regras (sem IA/LLM), pesos: **localização 30%, tipo 20%, preço 15%**, e os **35%**
restantes divididos entre área, dormitórios, suítes, vagas e características. O texto
livre do briefing é interpretado por regex (preço, área, dormitórios, suítes, vagas e
palavras-chave de características) para preencher os filtros que o corretor deixar em
branco — ver `Services/BriefingParser.cs` e `Services/MatchingService.cs`.

## Importação de inventário

Hoje esse é o **único jeito de cadastrar imóveis** (não existe tela de cadastro manual
no frontend, embora a API já suporte `POST/PUT/DELETE /properties`).

Tela **Administração → Importar inventário** aceita `.xlsx` ou `.csv` com as colunas:

```
Titulo, Bairro, Cidade, Preco, AreaM2, Dormitorios, Suites, Vagas, Tipo, Finalidade,
Imobiliaria, ImagemUrl, LinkOrigem, Caracteristicas
```

- `Tipo`: `Casa`, `CasaEmCondominio`, `Apartamento`, `Cobertura`, `CasaDeVila` ou `Duplex`.
- `Finalidade`: `Compra` ou `Locacao`.
- `Caracteristicas`: itens separados por `;` (ex.: `Piscina;Varanda gourmet;Academia`).
- O upsert é feito por `Titulo` + `Bairro`: reimportar a mesma planilha atualiza os
  imóveis existentes em vez de duplicar.

## Rodando localmente

Pré-requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download) e um SQL Server
acessível (local, LocalDB no Windows, ou um servidor de desenvolvimento).

1. Ajuste a connection string em `backend/src/MatchR.Api/appsettings.Development.json`
   (`ConnectionStrings:Default`) apontando para o seu SQL Server.
2. Rode:
   ```bash
   cd backend/src/MatchR.Api
   dotnet run
   ```
3. Acesse `http://localhost:5177`. Na primeira execução, o app aplica as migrations
   automaticamente e cria o usuário **Admin inicial** definido em `Admin:Email` /
   `Admin:Password` (padrão em dev: `admin@matchr.com.br` / `admin123`).
4. Faça login com o admin e aprove os corretores que solicitarem acesso pela landing
   page — isso cria a conta de cada corretor com uma senha temporária.

## Publicando no IIS + SQL Server (produção)

1. **Instale no servidor Windows**: [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
   (necessário para o IIS conseguir hospedar ASP.NET Core via o módulo `AspNetCoreModuleV2`).
   Reinicie o IIS depois (`net stop was /y && net start w3svc`).
2. **Configure `appsettings.json`** (ou `appsettings.Production.json`, ou variáveis de
   ambiente no IIS — recomendado para segredos) com:
   - `ConnectionStrings:Default`: string de conexão do seu SQL Server real.
   - `Jwt:Secret`: um segredo aleatório de pelo menos 32 caracteres (`CHANGE_ME_...` no
     arquivo é só placeholder — troque antes de publicar).
   - `Admin:Email` / `Admin:Password`: credenciais do primeiro administrador.
3. **Publique**:
   ```bash
   cd backend/src/MatchR.Api
   dotnet publish -c Release -o C:\sites\matchr
   ```
   ⚠️ `dotnet publish` sobrescreve `appsettings.json` com os valores do repositório
   (placeholders). Depois de publicar de novo, **reaplique** a connection string, o
   `Jwt:Secret` e o `Admin` reais no arquivo publicado.
4. **No IIS**:
   - Crie um Application Pool com **.NET CLR Version: No Managed Code** (o ASP.NET Core
     Module cuida do runtime, não o IIS).
   - Crie um site apontando para a pasta publicada (`C:\sites\matchr`), usando esse pool.
   - **Authentication** do site → **Anonymous Authentication: Enabled** (senão dá 401 do
     próprio IIS antes de chegar na aplicação).
   - Garanta que `IIS_IUSRS` (ou a identidade do pool) tem permissão de **leitura** na
     pasta publicada.
5. **No SQL Server**:
   - Crie o banco vazio: `CREATE DATABASE MatchR;`
   - Crie o login/usuário usado na connection string com `db_owner` **dentro do banco
     `MatchR`** (não basta `CREATE LOGIN` no `master` — precisa `CREATE USER` +
     `ALTER ROLE db_owner ADD MEMBER` rodando com `USE MatchR;`).
   - Confirme que esse usuário **não** está em `db_denydatareader` /
     `db_denydatawriter` — essas roles negam acesso mesmo com `db_owner` (DENY sempre
     vence GRANT no SQL Server). Alguns ambientes adicionam essas roles por padrão a
     todo usuário novo por política de segurança.
6. **Primeiro acesso**: o app roda as migrations e cria o admin automaticamente ao
   iniciar. Acesse o site, faça login como admin e troque a senha assim que possível
   (não há tela de "trocar senha" ainda — veja **Próximos passos**).

### Problemas encontrados no deploy (e como foram resolvidos)

Documentado aqui porque são erros genéricos de infraestrutura IIS/SQL Server que podem
se repetir num novo ambiente:

| Sintoma | Causa | Correção |
|---|---|---|
| `401 Unauthorized` do próprio IIS (antes de chegar na app) | Anonymous Authentication desabilitada no site, ou `IIS_IUSRS` sem permissão de leitura na pasta | Habilitar Anonymous Authentication + dar permissão NTFS de leitura à pasta pro `IIS_IUSRS` |
| `HTTP 500.30 - ASP.NET Core app failed to start` | Genérico — várias causas possíveis, só investigável habilitando o stdout log (`web.config`, `stdoutLogEnabled="true"`) ou olhando o Visualizador de Eventos → Aplicativo → origem **.NET Runtime** | Ver as duas linhas abaixo, que foram as causas reais encontradas |
| `SqlException: The SELECT permission was denied on the object '__EFMigrationsHistory'` | O usuário SQL usado na connection string tinha `db_owner` mas também estava em `db_denydatareader`/`db_denydatawriter` — DENY sempre sobrepõe GRANT | `ALTER ROLE db_denydatareader DROP MEMBER <usuario>;` (idem para `db_denydatawriter`) |
| `SqlException 1785: Introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths` ao criar a tabela `ShareEvents` | `ShareEvent` tinha cascade de `Client` (delete direto) **e** de `SearchRequest` (que também cascade de `Client`) — dois caminhos de cascata pra mesma tabela, que o SQL Server proíbe | FK de `ShareEvent → SearchRequest` mudada de `SetNull` para `ClientSetNull` (gera `ON DELETE NO ACTION` no banco, mantendo o comportamento em memória do EF Core) — ver `Data/MatchRDbContext.cs` |
| Logos (`assets/logo-matchr-*.png`) retornando 404 no site publicado | Os arquivos nunca foram commitados no repositório (só existiam como imagens coladas numa conversa, sem arquivo real em disco) | Localizados em `~/Downloads/matchr-prototype/assets/`, copiados pra `wwwroot/assets/` e commitados |

## Próximos passos sugeridos

- Tela de "trocar senha" para o corretor recém-aprovado (hoje a senha temporária fica
  só no modal de aprovação do admin).
- Envio de e-mail real na aprovação/rejeição de acesso (hoje é só um registro no banco).
- Tela de cadastro/edição manual de imóvel no frontend (a API já suporta).
- Paginação nas listagens de clientes/imóveis quando o volume crescer.
- Automatizar a reaplicação de `appsettings.json` no publish (hoje é manual, ver aviso
  na seção de deploy).
