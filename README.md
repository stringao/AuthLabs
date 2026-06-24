# AuthLabs

> Laboratorio de autenticacao e autorizacao em .NET 10 com 8 padroes de implementacao

[![Build](https://github.com/stringao/AuthLabs/actions/workflows/test.yml/badge.svg)](https://github.com/stringao/AuthLabs/actions/workflows/test.yml)
[![Tests](https://img.shields.io/badge/tests-209-brightgreen)]()
[![Coverage](https://img.shields.io/badge/coverage-13%25-yellow)]()
[![License](https://img.shields.io/badge/license-MIT-blue.svg)]()

---

## Descricao do Projeto

AuthLabs e um projeto educacional desenvolvido em **.NET 10** que demonstra **8 padroes diferentes de autenticacao e autorizacao** em aplicacoes ASP.NET Core. O objetivo e fornecer exemplos praticos e completos de como implementar seguranca em APIs REST, desde autenticacao basica por cookie ate modelos complexos como OAuth 2.0 + OIDC e RBAC (Role-Based Access Control).

Cada padrao esta implementado em um projeto separado, permitindo estudo isolado de cada abordagem.

---

## Padroes Implementados

| # | Padrao | Descricao | Documentacao |
|---|--------|-----------|--------------|
| 1 | **Cookie Authentication** | Autenticacao tradicional com cookies criptograficos | [docs/01-cookie-authentication.md](docs/01-cookie-authentication.md) |
| 2 | **JWT (JSON Web Tokens)** | Autenticacao stateless com tokens assinados | [docs/02-jwt.md](docs/02-jwt.md) |
| 3 | **OAuth 2.0 + OIDC** | Login social (Google, GitHub) com federacao de identidade | [docs/03-oauth-oidc.md](docs/03-oauth-oidc.md) |
| 4 | **Windows Authentication** | Autenticacao integrada com Active Directory/Kerberos | [docs/04-windows-authentication.md](docs/04-windows-authentication.md) |
| 5 | **API Keys** | Autenticacao de servicos machine-to-machine | [docs/05-api-keys.md](docs/05-api-keys.md) |
| 6 | **Claims-Based Authorization** | Autorizacao granular baseada em claims (pares chave-valor) | [docs/06-claims-based-authorization.md](docs/06-claims-based-authorization.md) |
| 7 | **Resource-Based Authorization** | Autorizacao por recurso especifico (ex: documento) | [docs/07-resource-based-authorization.md](docs/07-resource-based-authorization.md) |
| 8 | **RBAC (Role-Based Access Control)** | Controle de acesso baseado em roles hierarquicas | [docs/08-rbac.md](docs/08-rbac.md) |

---

## Arquitetura

```
                            AuthLabs
                              |
          +-------------------+-------------------+
          |                   |                   |
    AuthLabs.Shared    AuthLabs.ApiKey      AuthLabs.Windows
    (Biblioteca         (API Keys)           (Windows Auth)
     Compartilhada)           |
     - Models            +---+---+---+---+---+---+---+
     - Data              |                       |
     - Extensions   AuthLabs.Jwt           AuthLabs.Cookie
                      (JWT Auth)              (Cookie Auth)
                            |                       |
                      +-----+-----+           +-----+-----+
                      |           |           |           |
              AuthLabs.OAuth   AuthLabs.Claims   AuthLabs.Rbac
              (OAuth/OIDC)     (Claims)          (RBAC)
                                      |
                              AuthLabs.Resource
                              (Resource-Based)
```

### Diagrama de Fluxo de Autenticacao

```
┌─────────┐     ┌──────────────────┐     ┌─────────────┐
│ Client  │────►│  ASP.NET Core    │────►│  PostgreSQL │
│         │◄────│  Authentication  │◄────│  Database   │
└─────────┘     │  Middleware       │     └─────────────┘
                    │
                    ├── Cookie ────── Browser Sessions
                    ├── JWT ────────── Stateless Tokens
                    ├── OAuth/OIDC ── Social Login
                    ├── API Keys ───── Service-to-Service
                    └── Windows ────── Kerberos/NTLM
```

---

## Quick Start

### 1. Subir infraestrutura com Docker Compose

```bash
# Navegar para o diretorio do projeto
cd /home/stringao/dev/auth-labs

# Subir PostgreSQL e pgAdmin
docker-compose up -d

# Verificar se os servicos estao rodando
docker-compose ps
```

**Servicos iniciados:**
- PostgreSQL 16: `localhost:5432` (usuario: `postgres`, senha: `postgres123`)
- pgAdmin: `http://localhost:5050` (email: `admin@authlabs.com`, senha: `admin123`)

### 2. Executar um projeto

```bash
# Exemplo: Executar o projeto JWT
cd /home/stringao/dev/auth-labs
dotnet run --project src/AuthLabs.Jwt/AuthLabs.Jwt/AuthLabs.Jwt.csproj

# O projeto iniciara em http://localhost:5000
```

### 3. Testar autenticacao

```bash
# Login com usuario demo (JWT)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}'

# Resposta esperada:
# {
#   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
#   "expiresIn": 900
# }
```

---

## Estrutura dos Projetos

| Projeto | Tipo | Descricao | Portas |
|---------|------|-----------|--------|
| `AuthLabs.slnx` | Solution | Solucao principal com todos os projetos | - |
| `src/AuthLabs.Shared/` | Class Library | Codigo compartilhado (Models, Data, Extensions) | - |
| `src/AuthLabs.ApiKey/AuthLabs.ApiKey/` | Web API | Autenticacao por API Keys | 5000 |
| `src/AuthLabs.Claims/AuthLabs.Claims/` | Web API | Autorizacao baseada em Claims | 5000 |
| `src/AuthLabs.Cookie/AuthLabs.Cookie/` | Web API | Autenticacao por Cookie | 5000 |
| `src/AuthLabs.Jwt/AuthLabs.Jwt/` | Web API | Autenticacao por JWT | 5000 |
| `src/AuthLabs.OAuth/AuthLabs.OAuth/` | Web API | OAuth 2.0 + OpenID Connect | 5000 |
| `src/AuthLabs.Rbac/AuthLabs.Rbac/` | Web API | RBAC (Role-Based Access Control) | 5000 |
| `src/AuthLabs.Resource/AuthLabs.Resource/` | Web API | Autorizacao baseada em Recursos | 5000 |
| `src/AuthLabs.Windows/AuthLabs.Windows/` | Web API | Autenticacao Windows (NTLM/Kerberos) | 5000 |

### Projetos de Teste

| Projeto | Framework | Descricao |
|---------|-----------|-----------|
| `AuthLabs.ApiKey/AuthLabs.ApiKey.Tests/` | xUnit | Testes para API Keys |
| `AuthLabs.Claims/AuthLabs.Claims.Tests/` | xUnit | Testes para Claims |
| `AuthLabs.Cookie/AuthLabs.Cookie.Tests/` | xUnit | Testes para Cookie Auth |
| `AuthLabs.Jwt/AuthLabs.Jwt.Tests/` | xUnit | Testes para JWT |
| `AuthLabs.OAuth/AuthLabs.OAuth.Tests/` | xUnit | Testes para OAuth |
| `AuthLabs.Rbac/AuthLabs.Rbac.Tests/` | xUnit | Testes para RBAC |
| `AuthLabs.Resource/AuthLabs.Resource.Tests/` | xUnit | Testes para Resource Auth |
| `AuthLabs.Windows/AuthLabs.Windows.Tests/` | xUnit | Testes para Windows Auth |

---

## Usuarios para Testes

### Usuarios Comuns (Cookie, JWT, Claims, RBAC, Resource)

| Email | Senha | Roles | Descricao |
|-------|-------|-------|-----------|
| `admin@authlabs.com` | `Admin123!` | Admin | Administrador com acesso total |
| `manager@authlabs.com` | `Manager123!` | Manager | Gerente com acessos medios |
| `user@authlabs.com` | `User123!` | User | Usuario com acesso basico |
| `guest@authlabs.com` | `Guest123!` | Guest | Convidado com acesso limitado |

### API Keys (AuthLabs.ApiKey)

| Client | API Key | Scopes | Role |
|--------|---------|--------|------|
| mobile-app | `mobile-app-key-12345678` | read, write | User |
| web-frontend | `web-frontend-key-87654321` | read | User |
| admin-panel | `admin-panel-key-11223344` | read, write, delete | Admin |
| external-partner | `external-partner-key-55667788` | read | Guest |

### OAuth 2.0 / OIDC

| Provider | Scopes | Descricao |
|----------|--------|-----------|
| Google | openid, profile, email | Login com conta Google |
| GitHub | read:user, user:email | Login com conta GitHub |

---

## Documentacao da API

Cada projeto implementa endpoints RESTful com a seguinte estrutura:

### Endpoints de Autenticacao

| Metodo | Path | Autenticacao | Descricao |
|--------|------|--------------|-----------|
| POST | `/api/auth/login` | Nao | Login com credenciais |
| POST | `/api/auth/logout` | Sim | Logout e invalida sessao |
| GET | `/api/auth/me` | Sim | Retorna usuario atual |
| POST | `/api/auth/refresh` | Nao | Refresh de token (JWT) |

### Endpoints Protegidos

| Metodo | Path | Autenticacao | Roles | Descricao |
|--------|------|--------------|-------|-----------|
| GET | `/api/protected` | Sim | Qualquer | Informacoes do usuario |
| GET | `/api/protected/admin` | Sim | Admin | Area administrativa |
| GET | `/api/protected/manager` | Sim | Manager | Area de gerenciamento |
| GET | `/api/protected/read` | Sim | User/Admin + scope:read | Leitura |
| GET | `/api/protected/write` | Sim | User/Admin + scope:write | Escrita |
| GET | `/api/protected/delete` | Sim | Admin + scope:delete | Exclusao |

### Headers de Autenticacao

| Metodo | Header | Valor |
|--------|--------|-------|
| JWT | `Authorization` | `Bearer <access_token>` |
| API Key | `X-Api-Key` | `<api_key>` |
| Cookie | `Cookie` | `AuthLabs.Cookie=<cookie_value>` |

---

## Executando os Testes

### Executar todos os testes

```bash
cd /home/stringao/dev/auth-labs
dotnet test
```

### Executar testes de um projeto especifico

```bash
# Testes de JWT
dotnet test src/AuthLabs.Jwt/AuthLabs.Jwt.Tests/

# Testes de API Keys
dotnet test src/AuthLabs.ApiKey/AuthLabs.ApiKey.Tests/

# Testes de RBAC
dotnet test src/AuthLabs.Rbac/AuthLabs.Rbac.Tests/
```

### Executar testes com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Executar testes especificos

```bash
# Filtrar por nome de teste
dotnet test --filter "FullyQualifiedName~JwtTests"
```

---

## Configuracao

### Variaveis de Ambiente

| Variavel | Default | Descricao |
|----------|---------|-----------|
| `POSTGRES_USER` | `postgres` | Usuario do banco de dados |
| `POSTGRES_PASSWORD` | `postgres123` | Senha do banco de dados |
| `POSTGRES_DB` | `authlabs` | Nome do banco de dados |
| `POSTGRES_PORT` | `5432` | Porta do PostgreSQL |
| `PGADMIN_EMAIL` | `admin@authlabs.com` | Email do pgAdmin |
| `PGADMIN_PASSWORD` | `admin123` | Senha do pgAdmin |
| `PGADMIN_PORT` | `5050` | Porta do pgAdmin |

### Configuracao de Banco de Dados

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123"
  }
}
```

---

## Contribuindo

Contribuicoes sao bem-vindas! Para contribuir:

1. **Fork** o repositorio
2. **Crie uma branch** para sua feature (`git checkout -b feature/nova-feature`)
3. **Commit** suas mudancas (`git commit -m 'Adiciona nova feature'`)
4. **Push** para a branch (`git push origin feature/nova-feature`)
5. **Abra um Pull Request**

### Diretrizes de Contribuicao

- Mantenha o codigo bem documentado em portugues
- Adicione testes unitarios para novas funcionalidades
- Siga o estilo de codificacao do projeto
- Atualize a documentacao quando necessario
- Respeite o principios SOLID e clean code

### Padroes de Commits

```
feat: adiciona novo padrao de autenticacao
fix: corrige bug no handler de autenticacao
docs: atualiza documentacao
test: adiciona testes para novo modulo
refactor: refatora codigo existente
```

---

## Licenca

Este projeto esta licenciado sob a **MIT License** - consulte o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## Referencias e Recursos

- [Documentacao ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security/)
- [Microsoft Identity Documentation](https://docs.microsoft.com/azure/active-directory/develop/)
- [RFC 7519 - JSON Web Token (JWT)](https://tools.ietf.org/html/rfc7519)
- [RFC 6749 - OAuth 2.0 Authorization Framework](https://tools.ietf.org/html/rfc6749)
- [OWASP API Security](https://owasp.org/www-project-api-security/)
- [NIST RBAC Standard](https://csrc.nist.gov/projects/role-based-access-control)

---

## Autores

Desenvolvido como projeto educacional para .NET 10.

---

<p align="center">
  Feito com .NET 10 e ASP.NET Core
</p>
