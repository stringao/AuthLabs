# Role-Based Access Control (RBAC)

## O que e

RBAC (Role-Based Access Control) e um modelo de controle de acesso onde permissoes sao atribuidas a roles (papeis) e usuarios sao atribuidos a roles. O sistema verifica se o usuario pertence a uma role especifica para conceder ou negar acesso.

RBAC e o modelo mais simples e comum de controle de acesso, ideal para sistemas onde:
- Papéis sao relativamente fixos
- Hierarquia de permissoes e clara
- Nao ha necessidade de granularidade por recurso

## Conceitos Fundamentais

### Componentes do RBAC

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              RBAC MODEL                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   USERS              ASSIGNED TO              ROLES              HAS PERM    │
│   ──────             ──────────              ─────              ─────────  │
│                                                                              │
│   Alice     ─────────────────────►      Admin      ─────────►   Full Access │
│                                                                              │
│   Bob       ─────────────────────►      Manager    ─────────►   Read/Write  │
│                                               │                             │
│   Carol     ─────────────────────►      User      ─────────►   Read Only   │
│                                               │                             │
│   Dave      ─────────────────────►      Guest     ─────────►   Read Only   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Hierarquia de Roles

| Hierarquia | Roles | Permissoes |
|-------------|-------|------------|
| Top | Admin | Todas as operacoes |
| Middle | Manager | Operacoes de gerenciamento |
| Base | User | Operacoes basicas |
| Lowest | Guest | Apenas leitura |

**NOTA**: Esta hierarquia e conceitual. O ASP.NET Core com `[Authorize(Roles = "...")]` NAO implementa heranca automaticamente.

## Como funciona

1. **Role Assignment**: Administrador atribui roles aos usuarios
2. **Role-Permission Mapping**: Sistema define quais operacoes cada role pode executar
3. **Login**: Usuario faz login e recebe suas roles
4. **Request**: Usuario tenta acessar recurso protegido
5. **Role Check**: Sistema verifica se usuario tem a role requerida
6. **Access Decision**: Baseado na role, acesso e concedido ou negado

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           RBAC AUTHENTICATION FLOW                             │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                         SERVER                          DATABASE
    ──────                         ──────                          ────────
       │                              │                               │
       │  1. POST /login             │                               │
       │     {email, password}       │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  2. Validate credentials      │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  3. Lookup user + roles       │
       │                              │     SELECT role FROM users    │
       │                              │     WHERE email = ?          │
       │                              │◄──────────────────────────────│
       │                              │                               │
       │                              │  4. Role found: Admin        │
       │                              │                               │
       │                              │  5. Create Identity           │
       │                              │     with ClaimTypes.Role      │
       │                              │     = "Admin"                │
       │                              │                               │
       │  6. Set-Cookie              │                               │
       │     with Role claim         │                               │
       │◄────────────────────────────│                               │
       │                              │                               │
       │  7. GET /api/admin/users   │                               │
       │     Cookie: session         │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  8. Authorization middleware │
       │                              │     [Authorize(Roles="Admin")]│
       │                              │                               │
       │                              │  9. Check: User.IsInRole?    │
       │                              │     "Admin" ∈ user.roles?    │
       │                              │     YES → Allow              │
       │                              │     NO  → Deny               │
       │                              │                               │
       │  10. 200 OK                 │                               │
       │     [User list]             │                               │
       │◄────────────────────────────│                               │


┌─────────────────────────────────────────────────────────────────────────────┐
│                        ROLE CHECK EXAMPLES                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [Authorize(Roles = "Admin")]                                                │
│  ├── User has Role = "Admin"    → 200 OK                                   │
│  ├── User has Role = "User"     → 403 Forbidden                            │
│  └── User has no Role            → 401 Unauthorized                         │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  [Authorize(Roles = "Admin,Manager")]  // OR semantics                      │
│  ├── User has Role = "Admin"     → 200 OK                                  │
│  ├── User has Role = "Manager"   → 200 OK                                  │
│  └── User has Role = "User"      → 403 Forbidden                           │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  Multiple [Authorize] attributes  // AND semantics                           │
│                                                                              │
│  [Authorize(Roles = "Admin")]                                                │
│  [Authorize(Roles = "SuperUser")]                                            │
│  └── User must have BOTH Admin AND SuperUser → 200 OK                      │
│      User with only Admin → 403 Forbidden                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- Sistemas com hierarquia clara de permissoes (Admin > Manager > User > Guest)
- Aplicacoes empresariais com departamentos fixos
- Quando roles sao relativamente estaveis (nao mudam frequentemente)
- Projetos de medio porte que nao justificam models mais complexos
- Integracao com Active Directory (AD) para sincronizacao de grupos
- Quando permissoes sao binary (pode ou nao pode acessar)

## Quando NAO usar

- Quando permissoes sao muito granulares (ex: "pode editar apenas seus proprios posts")
- Quando o mesmo usuario pode ter permissoes diferentes em contextos diferentes
- Sistemas onde permissoes dependem de atributos externos (departamento, nivel)
- Cenarios multi-tenant onde permissoes sao por organizacao
- Quando precisa de separacao de duties (separation of duties)
- Quando voce precisa de permissoes por recurso especifico (use Resource-Based)

## Alertas e caveats importantes

1. **Politica de senha fraca**: Minimo de 6 caracteres e muito curto. Prod requer 8-12+ com caracteres especiais.

2. **Sem lockout**: `lockoutOnFailure: false` permite ataques de forca bruta.

3. **Cookie sem Secure flag**: `SecurePolicy` nao esta configurado como `SameSiteStrict`, permitindo HTTP.

4. **Credenciais em codigo**: Senhas dos usuarios demo estao hardcoded no seed data.

5. **Sem caching de roles**: Roles sao buscadas no banco a cada requisicao, sem cache.

6. **Sem data protection**: Cookie encryption keys nao tem protecao explícita.

7. **Hierarquia de roles implícita**: O sistema nao suporta "Admin inclui Manager" - cada endpoint especifica role exata.

## Configuracao necessaria

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123"
  }
}
```

**Configuracao (Program.cs):**
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

## Roles disponiveis

| Role | Descricao | Permissoes tipicas |
|------|-----------|-------------------|
| Admin | Acesso total a todos os recursos | Criar, Ler, Atualizar, Deletar |
| Manager | Acesso a relatorios e gerenciamento | Ler, Atualizar |
| User | Acesso basico a recursos do sistema | Ler, Atualizar (seus proprios) |
| Guest | Acesso apenas para visualizacao | Ler (apenas publicos) |

## Usuarios de demonstracao

| Email | Senha | Role |
|-------|-------|-------|
| admin@authlabs.com | Admin123! | Admin |
| manager@authlabs.com | Manager123! | Manager |
| user@authlabs.com | User123! | User |
| guest@authlabs.com | Guest123! | Guest |

## Endpoints principais

| Metodo | Path | Auth | Roles | Descricao |
|--------|------|------|-------|-----------|
| POST | /api/auth/login | Nao | - | Login com email/password |
| POST | /api/auth/logout | Sim | Any | Logout e destroi sessao |
| GET | /api/auth/me | Sim | Any | Info do usuario atual |
| GET | /api/protected | Sim | Any | Qualquer usuario autenticado |
| GET | /api/admin/users | Sim | Admin | Listar usuarios (Admin only) |
| GET | /api/admin/dashboard | Sim | Admin | Dashboard (Admin only) |
| GET | /api/reports | Sim | Admin, Manager | Ver relatorios |
| GET | /api/reports/financial | Sim | Admin, Manager | Relatorios financeiros |

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

### Ver usuario atual
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

### Listar usuarios (Admin)
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

### Relatorios financeiros (Manager ou Admin)
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

## Common Errors

### 1. 403 Forbidden mesmo com role correta

**Sintoma:** Usuario tem a role correta mas recebe 403.

**Causas:**
- Claims.Role nao esta configurado corretamente
- Cookie nao esta sendo enviado
- Sessao expirou

**Solucao:**
```bash
# Verificar claims do usuario
curl -X GET http://localhost:5000/api/auth/me \
  -b cookies.txt

# Verificar cookie esta sendo enviado
curl -v -X GET http://localhost:5000/api/auth/me \
  -b cookies.txt 2>&1 | grep -i cookie
```

### 2. Multiple roles nao funcionam

**Sintoma:** `[Authorize(Roles = "Admin,Manager")]` so funciona com Admin.

**Causa:** ASP.NET Core interpreta como "AND" (ambas roles), nao "OR".

**Solucao:**
```csharp
// Use Policy-based authorization para OR
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.IsInRole("Manager")));
});

[Authorize(Policy = "AdminOrManager")]
public IActionResult Reports() { ... }
```

### 3. Hierarquia de roles nao funciona

**Sintoma:** Admin nao tem acesso a endpoints Manager.

**Causa:** RBAC padrao NAO implementa heranca de roles.

**Solucao:**
```csharp
// Implementar Role Hierarchy via IClaimsTransformation
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
            AddRoleIfNotPresent(identity, "Manager");
            AddRoleIfNotPresent(identity, "User");
            AddRoleIfNotPresent(identity, "Guest");
        }

        // Manager herda User e Guest
        if (roles.Contains("Manager"))
        {
            AddRoleIfNotPresent(identity, "User");
            AddRoleIfNotPresent(identity, "Guest");
        }

        return principal;
    }
    
    private void AddRoleIfNotPresent(ClaimsIdentity identity, string role)
    {
        if (!identity.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }
}
```

### 4. Login funciona mas Authorize falha

**Sintoma:** Login retorna 200 com role correta, mas proxima requisicao 401.

**Causa:** Claims nao estao sendo persistidos no cookie.

**Solucao:**
```csharp
// Ao fazer login, criar ClaimsIdentity com roles
var claims = new List<Claim>
{
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.Role, user.Role)  // IMPORTANTE: Role claim
};

var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
var principal = new ClaimsPrincipal(identity);

await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    principal,
    new AuthenticationProperties
    {
        IsPersistent = true
    });
```

## Security Considerations

### 1. Principio do Menor Privilegio

```csharp
// Comece com menos permissoes
[Authorize(Roles = "User")]  // ou Guest
public IActionResult Read() { ... }

// Adicione roles apenas quando necessario
[Authorize(Roles = "Admin")]
public IActionResult AdminOnly() { ... }
```

### 2. Protecao contra Role Manipulation

```csharp
// Nunca permitir que usuarios escolham suas proprias roles
[Authorize(Roles = "Admin")]
public async Task<IActionResult> AssignRole(string userId, string newRole)
{
    // Validar que admin nao pode se remover admin
    if (userId == GetCurrentUserId() && newRole != "Admin")
    {
        return BadRequest("Cannot remove your own admin role");
    }
    
    await _userService.SetRoleAsync(userId, newRole);
    return Ok();
}
```

### 3. Auditing de mudancas de Role

```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> SetRole(string userId, string role)
{
    var previousRole = await _userService.GetRoleAsync(userId);
    
    _auditLogger.Log(
        "RoleChange",
        GetCurrentUserId(),
        new
        {
            TargetUserId = userId,
            OldRole = previousRole,
            NewRole = role,
            Timestamp = DateTime.UtcNow
        });
        
    await _userService.SetRoleAsync(userId, role);
    return Ok();
}
```

## Controller com Roles

```csharp
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    // Qualquer usuario autenticado
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

**NOTA**: Esta hierarquia e conceitual. O sistema ASP.NET Core Identity com `[Authorize(Roles = "...")]` nao implementa heranca automaticamente. Cada endpoint deve especificar todas as roles permitidas.

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
| Granularidade | Role | Atributo | Instancia |
| Complexidade | Baixa | Media | Alta |
| Flexibilidade | Baixa | Media | Alta |
| Performance | Alta | Media | Baixa |
| Melhor para | Apps fixas | Multi-tenant | Docs/Permissoes |
| Manutencao | Simples | Media | Complexa |

## Referências

- [NIST RBAC](https://csrc.nist.gov/projects/role-based-access-control)
- [Microsoft Docs - Role-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/roles)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
- [RBAC vs ABAC](https://www.cyberark.com/what-is/role-based-access-control-vs-attribute-based-access-control/)
- [NIST SP 800-53 Access Control](https://csrc.nist.gov/publications/detail/sp/800-53/rev5/final)
