# MatchR

Plataforma de inteligência para corretores de imóveis de alto padrão: transforma o
briefing do cliente numa seleção curta de imóveis, ordenada por score de match, pronta
para compartilhar pelo WhatsApp.

**Em produção**: `https://matcher2.tesla.com.br` (IIS + SQL Server).

## Histórico do projeto

O projeto começou como um **protótipo estático** (HTML + CSS + um único `app.js` com
dados mockados em memória, sem backend, sem persistência). A partir dele foi construído
um backend completo e o frontend foi modularizado para consumir a API real:

1. Protótipo HTML/CSS/JS navegável (dados fake em arrays no `app.js`, sem login real).
2. Escolha de stack: **ASP.NET Core + SQL Server + IIS**, por já existir servidor IIS e
   SQL Server disponíveis na empresa — é a combinação mais nativa do ecossistema
   Microsoft para esse cenário (módulo do IIS mantido pela própria Microsoft).
3. Backend construído do zero (models, EF Core, JWT, motor de match, importação de
   planilha) — ver seção **Arquitetura**.
4. Frontend original (`app.js` monolítico) quebrado em **ES modules** (sem build step,
   pra manter o deploy simples: só copiar arquivos estáticos pro IIS) e conectado à API.
5. Deploy no IIS + SQL Server do zero, com todos os problemas reais de infraestrutura
   resolvidos no processo — ver **Problemas encontrados no deploy** abaixo, útil caso
   apareçam de novo num novo ambiente.

## Stack

- **Backend**: ASP.NET Core 8 Web API + Entity Framework Core + SQL Server, autenticação
  JWT (BCrypt para hash de senha), motor de match baseado em regras (**sem LLM/IA** —
  decisão consciente para o MVP, ver seção **Motor de match**).
- **Frontend**: HTML/CSS/JS puro (ES modules nativos do navegador, sem framework, sem
  build step), servido como arquivos estáticos pelo próprio ASP.NET Core (`wwwroot/`) —
  **um único site, uma única porta**, sem CORS para configurar.

```
backend/
  MatchR.sln
  src/MatchR.Api/
    Controllers/         endpoints da API (/api/...)
    Models/               entidades do banco (EF Core)
    Data/
      MatchRDbContext.cs   configuração do EF Core (relações, conversões, índices)
      DbInitializer.cs     aplica migrations + cria o Admin inicial no startup
      Migrations/          migrations do EF Core
    Services/
      AuthService.cs        hash de senha (BCrypt) + geração/validação de JWT
      MatchingService.cs     motor de match por regras
      BriefingParser.cs      extrai filtros do texto livre do briefing (regex)
      ImportService.cs       parser de planilha .xlsx/.csv (ClosedXML)
    Dtos/                 contratos de request/response
    wwwroot/               frontend publicado
      index.html
      styles.css
      assets/               logos (logo-matchr-icon.png, logo-matchr-full.png)
      js/
        api.js               cliente HTTP (fetch + JWT), um método por endpoint
        router.js            roteamento por hash (#/rota), guarda de autenticação
        shell.js              layout (sidebar/topbar) usado pelas páginas internas
        icons.js, dom.js, state.js   utilitários
        components/           modal.js, toast.js
        pages/                 uma página por tela (dashboard, clients, search, results,
                                favorites, history, admin, importInventory, login, landing)
```

## Arquitetura do backend

### Entidades (`Models/`)
`Broker` (corretor/admin) · `Client` · `Agency` (imobiliária) · `Property` (imóvel) ·
`SearchRequest` (busca) · `SearchResult` (resultado com score, vinculado a uma busca) ·
`Favorite` · `ShareEvent` (histórico de buscas e compartilhamentos) · `ImportBatch`
(log de importações) · `AccessRequest` (solicitação de acesso da landing page).

### Endpoints principais (`Controllers/`, todos sob `/api`, protegidos por JWT exceto login/access-request)
- `POST /auth/login`, `POST /auth/access-requests` (públicos), `GET /auth/me`
- `GET/POST/PUT/DELETE /clients`
- `GET/POST/PUT/DELETE /properties`, `GET /agencies`
- `POST /searches` (roda o motor de match), `GET /searches/{id}`,
  `PATCH /searches/{id}/selection`, `POST /searches/{id}/share`
- `GET/POST/DELETE /favorites`
- `GET /history` (aceita `?clientId=`)
- `GET /dashboard/stats|recent-clients|recent-activity`
- `GET /admin/access-requests`, `POST /admin/access-requests/{id}/approve|reject`,
  `GET /admin/brokers`, `GET /admin/inventory-summary` (só role `Admin`)
- `POST /import` (upload de planilha), `GET /import` (histórico de importações)

### Autenticação
JWT stateless. No primeiro start, `DbInitializer` cria um **Admin inicial** (email/senha
de `appsettings.json` → seção `Admin`). Esse admin aprova as solicitações de acesso da
landing page pela tela **Administração**, o que cria a conta do corretor com uma **senha
temporária** (gerada e mostrada uma única vez no modal de aprovação — ainda não existe
tela de "trocar senha" no primeiro login, ver **Próximos passos**).

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
