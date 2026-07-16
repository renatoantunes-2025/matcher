# MatchR

Plataforma de inteligência para corretores de imóveis de alto padrão: transforma o
briefing do cliente numa seleção curta de imóveis, ordenada por score de match, pronta
para compartilhar pelo WhatsApp.

- **Backend**: ASP.NET Core 8 Web API + Entity Framework Core + SQL Server, autenticação
  JWT, motor de match baseado em regras (sem LLM).
- **Frontend**: HTML/CSS/JS puro (ES modules, sem build step), servido como arquivos
  estáticos pelo próprio ASP.NET Core (`wwwroot/`) — um único site, uma única porta.

```
backend/
  MatchR.sln
  src/MatchR.Api/
    Controllers/      endpoints da API (/api/...)
    Models/            entidades do banco (EF Core)
    Data/               DbContext + migrations + seed do admin
    Services/          autenticação, motor de match, importação de planilhas
    Dtos/               contratos de request/response
    wwwroot/            frontend (index.html, styles.css, js/)
```

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
4. **No IIS**:
   - Crie um Application Pool com **.NET CLR Version: No Managed Code** (o ASP.NET Core
     Module cuida do runtime, não o IIS).
   - Crie um site apontando para a pasta publicada (`C:\sites\matchr`), usando esse pool.
   - Garanta que o `IIS_IUSRS` (ou a identidade do pool) tem permissão de leitura na
     pasta e permissão para acessar o SQL Server.
5. **Primeiro acesso**: o app roda as migrations e cria o admin automaticamente ao
   iniciar. Acesse o site, faça login como admin e troque a senha assim que possível
   (não há tela de "trocar senha" ainda — veja Próximos passos).

## Importação de inventário

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

## Motor de match

Regras (sem IA/LLM), pesos: localização 30%, tipo 20%, preço 15%, e os 35% restantes
divididos entre área, dormitórios, suítes, vagas e características. O texto livre do
briefing é interpretado por regex (preço, área, dormitórios, suítes, vagas e palavras-
chave de características) para preencher os filtros que o corretor deixar em branco —
ver `Services/BriefingParser.cs` e `Services/MatchingService.cs`.

## Próximos passos sugeridos

- Tela de "trocar senha" para o corretor recém-aprovado (hoje a senha temporária fica
  só no modal de aprovação do admin).
- Envio de e-mail real na aprovação/rejeição de acesso (hoje é só um registro no banco).
- Paginação nas listagens de clientes/imóveis quando o volume crescer.
