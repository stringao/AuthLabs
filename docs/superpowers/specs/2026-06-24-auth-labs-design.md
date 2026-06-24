# AuthLabs - Laboratório de Autenticação e Autorização .NET 10

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:brainstorming before writing any code.

**Goal:** Criar um laboratório completo com 8 padrões de autenticação/autorização em .NET 10, implementados com TDD e documentação extensiva para estudo.

**Architecture:** Solution única com projetos isolados por padrão de autenticação. Cada padrão tem seu próprio projeto de API e projeto de testes. Um projeto compartilhado (`AuthLabs.Shared`) fornece helpers, extensions e base classes reutilizáveis. Docker Compose provê PostgreSQL para todos os projetos.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, FluentAssertions, PostgreSQL, Docker Compose, Entity Framework Core.

---

## 1. Estrutura de Projetos

```
auth-labs/
├── docker-compose.yml
├── AuthLabs.sln
├── src/
│   ├── AuthLabs.Shared/                    # Helpers, extensions, base classes
│   ├── AuthLabs.Cookie/                    # Cookie Authentication
│   │   ├── AuthLabs.Cookie/                # API + lógica
│   │   └── AuthLabs.Cookie.Tests/          # Testes isolados
│   ├── AuthLabs.Jwt/                       # JWT Authentication
│   │   ├── AuthLabs.Jwt/
│   │   └── AuthLabs.Jwt.Tests/
│   ├── AuthLabs.OAuth/                     # OAuth 2.0 + OIDC
│   │   ├── AuthLabs.OAuth/
│   │   └── AuthLabs.OAuth.Tests/
│   ├── AuthLabs.Windows/                   # Windows Authentication
│   │   ├── AuthLabs.Windows/
│   │   └── AuthLabs.Windows.Tests/
│   ├── AuthLabs.ApiKey/                    # API Keys
│   │   ├── AuthLabs.ApiKey/
│   │   └── AuthLabs.ApiKey.Tests/
│   ├── AuthLabs.Claims/                    # Claims-based Authorization
│   │   ├── AuthLabs.Claims/
│   │   └── AuthLabs.Claims.Tests/
│   ├── AuthLabs.Resource/                  # Resource-based Authorization
│   │   ├── AuthLabs.Resource/
│   │   └── AuthLabs.Resource.Tests/
│   └── AuthLabs.Rbac/                      # Role-based Authorization
│       ├── AuthLabs.Rbac/
│       └── AuthLabs.Rbac.Tests/
└── docs/
    ├── 01-cookie-authentication.md
    ├── 02-jwt.md
    ├── 03-oauth-oidc.md
    ├── 04-windows-authentication.md
    ├── 05-api-keys.md
    ├── 06-claims-based-authorization.md
    ├── 07-resource-based-authorization.md
    └── 08-rbac.md
```

---

## 2. Docker Compose

- **PostgreSQL 16** exposto na porta 5432
- Database: `authlabs`
- Usuário: `postgres`
- Senha: `postgres123`
- Volume persistente para dados

---

## 3. Os 8 Padrões de Autenticação/Autorização

| # | Projeto | Descrição |
|---|---------|-----------|
| 1 | `AuthLabs.Cookie` | Cookie Authentication - autenticação tradicional com cookies |
| 2 | `AuthLabs.Jwt` | JWT - autenticação stateless com JSON Web Tokens |
| 3 | `AuthLabs.OAuth` | OAuth 2.0 + OpenID Connect - delegação de autenticação |
| 4 | `AuthLabs.Windows` | Windows Authentication (Kerberos/NTLM) - autenticação integrada Windows |
| 5 | `AuthLabs.ApiKey` | API Keys - autenticação por chave de API |
| 6 | `AuthLabs.Claims` | Claims-based Authorization - autorização granular por claims |
| 7 | `AuthLabs.Resource` | Resource-based Authorization - autorização baseada em recursos |
| 8 | `AuthLabs.Rbac` | Role-based Access Control (RBAC) - autorização por papéis |

---

## 4. Estrutura de Cada Projeto (Padrão)

Cada projeto de autenticação/autorização segue a estrutura:

```
AuthLabs.<Pattern>/
├── AuthLabs.<Pattern>/
│   ├── Program.cs                          # Configuração da aplicação
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── AuthController.cs               # Endpoints de autenticação
│   │   └── ProtectedController.cs          # Endpoints protegidos
│   ├── Data/
│   │   └── AppDbContext.cs                 # EF Core DbContext
│   ├── Models/
│   │   ├── User.cs
│   │   └── ...
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   └── AuthService.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # Extensions para DI
└── AuthLabs.<Pattern>.Tests/
    ├── AuthLabs.<Pattern>.Tests.csproj
    ├── Services/
    │   └── AuthServiceTests.cs
    └── Controllers/
        └── AuthControllerTests.cs
```

---

## 5. Documentação por Padrão

Cada padrão terá:

- **README.md** no projeto com visão geral
- **docs/XX-pattern-name.md** com documentação completa:
  - O que é / How it works
  - Diagrama de fluxo
  - Quando usar
  - Quando NÃO usar
  - Alertas e caveats importantes
  - Exemplos de código
  - Configuração necessária

---

## 6. Abordagem TDD

Para cada padrão:

1. **Escrever testes primeiro** (vermelho)
2. **Implementar código mínimo** para passar (verde)
3. **Refatorar** se necessário
4. **Documentar** o padrão

---

## 7. Ordem de Implementação

1. `AuthLabs.Shared` - infraestrutura compartilhada
2. `docker-compose.yml` - PostgreSQL
3. `AuthLabs.Jwt` - JWT (mais comum, base para outros)
4. `AuthLabs.Cookie` - Cookie Authentication
5. `AuthLabs.ApiKey` - API Keys (simples, bom primeiro)
6. `AuthLabs.Claims` - Claims-based Authorization
7. `AuthLabs.Rbac` - Role-based Authorization
8. `AuthLabs.Resource` - Resource-based Authorization
9. `AuthLabs.OAuth` - OAuth 2.0 + OIDC (mais complexo)
10. `AuthLabs.Windows` - Windows Authentication (requer ambiente Windows)

---

## 8. Decisões de Design

- **DbContext compartilhado** via `AuthLabs.Shared`
- **JWT:access token** (15min) + **refresh token** (7 dias)
- **Testes com in-memory database** para isolamento
- **FluentAssertions** para assertions legíveis
- **Comments XML** em todos os métodos públicos para documentação