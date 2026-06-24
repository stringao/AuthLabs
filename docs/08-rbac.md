# Role-Based Access Control (RBAC)

## O que é

RBAC (Role-Based Access Control) é um modelo de controle de acesso onde permissões são atribuídas a roles (papeis) e usuários são atribuídos a roles. O sistema verifica se o usuário pertence a uma role específica para conceder ou negar acesso.

## Como funciona

1. **Role Assignment**: Administrador atribui roles aos usuários
2. **Role-Permission Mapping**: Sistema define quais operações cada role pode executar
3. **Login**: Usuário faz login e recebe suas roles
4. **Request**: Usuário tenta acessar recurso protegido
5. **Role Check**: Sistema verifica se usuário tem a role requerida
6. **Access Decision**: Baseado na role, acesso é concedido ou negado

## Diagrama de fluxo
```
┌────────┐     ┌──────────────┐     ┌─────────┐
│ Client │────►│   Server     │────►│   DB    │
└────────┘     └──────────────┘     └─────────┘
     │               │                  │
     │  1. Login     │                  │
     │  (email+pass) │                  │
     │─────────────►│                  │
     │               │                  │
     │  2. Validate  │                  │
     │  + Get Roles  │                  │
     │─────────────►│                  │
     │               │◄────────────────│
     │               │                  │
     │  3. Set Cookie│                  │
     │  with Roles   │                  │
     │◄─────────────│                  │
     │               │                  │
     │  4. Request    │                  │
     │  /api/admin   │                  │
     │─────────────►│                  │
     │               │                  │
     │  5. Check Role│                  │
     │  [Authorize]  │                  │
     │  (Roles=Admin)│                  │
     │               │                  │
     │  6. Access    │                  │
     │  Granted      │                  │
     │◄─────────────│                  │
```

## Quando usar

- Sistemas com hierarquia clara de permissões (Admin > Manager > User > Guest)
- Aplicações empresariais com departamentos fixos
- Quando roles são relativamente estáveis (não mudam frequentemente)
- Projetos de médio porte que não justificam models mais complexos
- Integração com Active Directory (AD) para sincronização de grupos

## Quando NÃO usar

- Quando permissões são muito granulares (ex: "pode editar только seus próprios posts")
- Quando同一个 usuário pode ter permissões diferentes em contextos diferentes
- Sistemas onde permissões dependem de atributos externos (departamento, nível)
- Cenários multi-tenant onde permissões são por organização
- Quando precisa de separação de duties (separation of duties)

## Alertas e caveats importantes

1. **Política de senha fraca**: Mínimo de 6 caracteres é muito curto. Prod требует 8-12+ с специальными characters.

2. **Sem lockout**: `lockoutOnFailure: false` permite ataques de força bruta.

3. **Cookie sem Secure flag**: `SecurePolicy` não está configurado como `SameSiteStrict`, permitindo HTTP.

4. **Credenciais em código**: Senhas dos usuários demo estão hardcoded no seed data.

5. **Sem caching de roles**: Roles são buscadas no banco a cada requisição, sem cache.

6. **Sem data protection**: Cookie encryption keys não têm proteção explícita.

7. **Hierarquia de roles implícita**: O sistema não suporta "Admin inclui Manager" - cada endpoint especifica role exata.

## Configuração necessária

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123"
  }
}
```

**Configuração (Program.cs):**
```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthLabs.Rbac";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.SlidingExpiration = true;
    });

services.AddAuthorization();
```

## Roles disponíveis

| Role | Descrição |
|------|-----------|
| Admin | Acesso total a todos os recursos |
| Manager | Acesso a relatórios e gerenciamento |
| User | Acesso básico a recursos do sistema |
| Guest | Acesso apenas para visualização |

## Usuários de demonstração

| Email | Senha | Role |
|-------|-------|-------|
| admin@authlabs.com | Admin123! | Admin |
| manager@authlabs.com | Manager123! | Manager |
| user@authlabs.com | User123! | User |
| guest@authlabs.com | Guest123! | Guest |

## Endpoints principais

| Método | Path | Auth | Roles | Descrição |
|--------|------|------|-------|-----------|
| POST | /api/auth/login | Não | - | Login com email/password |
| POST | /api/auth/logout | Sim | Any | Logout e destrói sessão |
| GET | /api/auth/me | Sim | Any | Info do usuário atual |
| GET | /api/protected | Sim | Any | Qualquer usuário autenticado |
| GET | /api/admin/users | Sim | Admin | Listar usuários (Admin only) |
| GET | /api/admin/dashboard | Sim | Admin | Dashboard (Admin only) |
| GET | /api/reports | Sim | Admin, Manager | Ver relatórios |
| GET | /api/reports/financial | Sim | Admin, Manager | Relatórios financeiros |

## Exemplo de uso

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}' \
  -c cookies.txt
```

**Resposta:**
```json
{
  "email": "admin@authlabs.com",
  "role": "Admin"
}
```

### Ver usuário atual
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -b cookies.txt
```

**Resposta:**
```json
{
  "email": "admin@authlabs.com",
  "role": "Admin"
}
```

### Listar usuários (Admin)
```bash
curl -X GET http://localhost:5000/api/admin/users \
  -b cookies.txt
```

**Resposta:**
```json
[
  { "id": 1, "email": "admin@authlabs.com", "role": "Admin" },
  { "id": 2, "email": "manager@authlabs.com", "role": "Manager" },
  { "id": 3, "email": "user@authlabs.com", "role": "User" },
  { "id": 4, "email": "guest@authlabs.com", "role": "Guest" }
]
```

### Dashboard (Admin)
```bash
curl -X GET http://localhost:5000/api/admin/dashboard \
  -b cookies.txt
```

### Relatórios financeiros (Manager ou Admin)
```bash
curl -X GET http://localhost:5000/api/reports/financial \
  -b cookies.txt
```

### Tentar acessar Admin como User
```bash
# Login como user
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@authlabs.com","password":"User123!"}' \
  -c user_cookies.txt

# Tentar acessar admin endpoint
curl -X GET http://localhost:5000/api/admin/users \
  -b user_cookies.txt
```

**Resposta:**
```
403 Forbidden
```

### Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -b cookies.txt
```

## Controller com Roles

```csharp
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    // Qualquer usuário autenticado
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUsers()
    {
        return Ok(_userService.GetAll());
    }

    // Apenas Admin
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public IActionResult Dashboard()
    {
        return Ok(new { message = "Admin Dashboard" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    // Admin ou Manager
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult GetReports()
    {
        return Ok(_reportsService.GetAll());
    }

    // Admin ou Manager
    [HttpGet("financial")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult FinancialReports()
    {
        return Ok(_reportsService.GetFinancial());
    }
}
```

## Hierarquia de Roles

```
Admin
├── Manager
│   ├── User
│   │   └── Guest
```

**NOTA**: Esta hierarquia é conceitual. O sistema ASP.NET Core Identity com `[Authorize(Roles = "...")]` não implementa herança automaticamente. Cada endpoint deve especificar todas as roles permitidas.

Para implementar hierarquia real:

```csharp
// ClaimsTransformation (adiciona roles herdadas)
services.AddScoped<IClaimsTransformation, RoleHierarchyTransformer>();

public class RoleHierarchyTransformer : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity;
        var roles = identity.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // Admin herda todas as roles
        if (roles.Contains("Admin"))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Manager"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Guest"));
        }

        // Manager herda User e Guest
        if (roles.Contains("Manager"))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Guest"));
        }

        return principal;
    }
}
```

## RBAC vs Claims vs Resource

| Aspecto | RBAC | Claims | Resource |
|---------|------|--------|----------|
| Granularidade | Role | Atributo | Instância |
| Complexidade | Baixa | Média | Alta |
| Flexibilidade | Baixa | Média | Alta |
| Performance | Alta | Média | Baixa |
| Melhor para | Apps fixas | Multi-tenant | Docs/Permissões |
| Manutenção | Simples | Média | Complexa |

## Referências

- [NIST RBAC](https://csrc.nist.gov/projects/role-based-access-control)
- [Microsoft Docs - Role-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/roles)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
- [RBAC vs ABAC](https://www.cyberark.com/what-is/role-based-access-control-vs-attribute-based-access-control/)
