# Claims-Based Authorization

## O que e

Claims-based Authorization e um modelo de autorizacao onde permissoes sao concedidas com base em Claims (afirmacoes) - pares de chave-valor que descrevem caracteristicas do usuario como cargo, departamento, nivel de assinatura, etc. Diferente de RBAC (Role-Based), claims permitem modelagem mais granular e flexivel.

Claims sao declaracoes sobre o usuario feitas por um identity provider. Cada claim tem um **tipo** (claim type) e um **valor** (claim value). Por exemplo:
- `Department = "Sales"`
- `SubscriptionTier = "Premium"`
- `Country = "Brazil"`

## Conceitos Fundamentais

### Claim vs Role

| Aspecto | Role | Claim |
|---------|------|-------|
| Estrutura | Simples (nome) | Tipo + Valor |
| Granularidade | Gross | Fina |
| Flexibilidade | Baixa (fixa) | Alta (customizavel) |
| Exemplo | `Role=Admin` | `Department=Sales,Level=Senior` |

### Beneficios dos Claims

1. **Granularidade**: Permites expressar permissoes complexas
2. **Flexibilidade**: Claims podem ser adicionados sem mudar codigo
3. **Federacao**: Claims de provedores externos (OAuth, OIDC)
4. **Declarativo**: Policies declarativas baseadas em claims

## Como funciona

1. **Autenticacao**: Usuario faz login e recebe claims do identity provider
2. **Claims Generation**: Sistema gera claims baseados nos dados do usuario (ex: `Department=Sales`, `Subscription=Tier=Premium`)
3. **Policies Definition**: Administrador define policies que requerem claims especificos
4. **Request Evaluation**: Em cada requisicao protegida, o sistema avalia se o principal tem os claims requeridos
5. **Authorization**: Acesso concedido ou negado baseado na presenca e valores dos claims

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       CLAIMS-BASED AUTHORIZATION FLOW                         │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                         SERVER                          DATABASE
    ──────                         ──────                          ────────
       │                              │                               │
       │  1. POST /login             │                               │
       │     {email, password}        │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  2. Validate credentials      │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  3. Load user data           │
       │                              │     including attributes      │
       │                              │◄──────────────────────────────│
       │                              │                               │
       │                              │  4. Generate claims          │
       │                              │     from user attributes:     │
       │                              │     - Department             │
       │                              │     - SubscriptionTier        │
       │                              │     - Permissions            │
       │                              │     - Region                  │
       │                              │                               │
       │                              │  5. Create Identity          │
       │                              │     with all claims          │
       │                              │                               │
       │  6. {claims: {...}}         │                               │
       │◄────────────────────────────│                               │
       │                              │                               │
       │  7. GET /protected/premium  │                               │
       │     Authorization: Bearer   │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  8. Authorization middleware │
       │                              │     evaluates policy:        │
       │                              │     RequireClaim(            │
       │                              │       "Subscription:Tier",   │
       │                              │       "Premium"               │
       │                              │     )                         │
       │                              │                               │
       │                              │  9. Check claim value        │
       │                              │     user.Claims.FirstOrDefault│
       │                              │     (c => c.Type ==           │
       │                              │      "Subscription:Tier")     │
       │                              │                               │
       │                              │  10. Claim value = "Premium"?│
       │                              │      YES → Allow             │
       │                              │      NO  → Deny (403)         │
       │                              │                               │
       │  11. 200 OK / 403 Forbidden │                               │
       │◄────────────────────────────│                               │


┌─────────────────────────────────────────────────────────────────────────────┐
│                        CLAIMS EVALUATION EXAMPLES                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Policy: CanEditDocuments                                                    │
│  └── RequireClaim("Document:Edit", "true")                                   │
│                                                                              │
│  User Claims:                                                                │
│  ├── Document:Edit = "true"  → ALLOW                                       │
│  ├── Document:Edit = "false" → DENY                                        │
│  └── (no claim)             → DENY                                        │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  Policy: CanAccessRegion                                                     │
│  └── RequireClaim("Region", "US", "EU")                                     │
│                                                                              │
│  User Claims:                                                                │
│  ├── Region = "US"    → ALLOW                                              │
│  ├── Region = "EU"    → ALLOW                                              │
│  ├── Region = "APAC"  → DENY                                               │
│  └── (no claim)       → DENY                                               │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  Policy: PremiumOrEnterprise                                                 │
│  └── RequireAssertion(context =>                                           │
│        context.User.HasClaim("Subscription:Tier", "Premium") ||             │
│        context.User.HasClaim("Subscription:Tier", "Enterprise"))             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- Quando permissoes dependem de atributos alem de roles (departamento, regiao, etc)
- Sistemas com niveis de assinatura/servico (Free, Premium, Enterprise)
- Quando a mesma role precisa de permissoes diferentes baseadas em contexto
- Integracao com sistemas externos que fornecem claims (OAuth, OIDC)
- Cenarios onde permissoes mudam frequentemente (nao quer重新 deploy)
- Federalizacao de identidade (claims de multiplos identity providers)
- Multi-tenant onde cada tenant pode ter claims customizados

## Quando NAO usar

- Sistemas simples com poucas permissoes fixas (RBAC e mais simples)
- Quando performance e critica (claims evaluation tem overhead)
- Quando claims sao extremamente granulares (considere ABAC - Attribute-Based)
- Sistemas onde roles sao suficientes e nao mudam frequentemente
- Quando voce precisa de hierarquia de roles simples

## Alertas e caveats importantes

1. **Credenciais hardcoded**: Todas as senhas estao em texto plano no codigo. ISSO E UM RISCO CRITICO.

2. **Sem sessao de autenticacao**: O login retorna claims mas NAO cria cookie de autenticacao. O `[Authorize]` falhara.

3. **Sem hashing de senha**: Senhas sao comparadas diretamente, sem hash.

4. **Validacao duplicada**: Dados de usuario duplicados em `AuthController.cs` e `ClaimsService.cs`.

5. **Armazenamento em memoria**: Nao ha persistencia - usuarios se perdem ao reiniciar.

6. **Sem HTTPS enforcement**: Comunicacao sem SSL/TLS explicito.

7. **Politica frouxa**: Validacao de email/password e minima (apenas dictionary lookup).

8. **Missing handler check**: `CustomClaimHandler` pode nao estar sendo invocado corretamente.

## Configuracao necessaria

```csharp
// Program.cs - Policies
services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditDocuments", policy =>
        policy.RequireClaim("Document:Edit", "true"));

    options.AddPolicy("CanDeleteDocuments", policy =>
        policy.RequireClaim("Document:Delete", "true"));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireClaim("User:Manage", "true"));

    options.AddPolicy("IsPremiumUser", policy =>
        policy.RequireClaim("Subscription:Tier", "Premium"));
        
    options.AddPolicy("CanAccessRegion", policy =>
        policy.RequireClaim("Region", "US", "EU", "LATAM"));
        
    // Custom policy with assertion
    options.AddPolicy("SeniorOrPremium", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("Level", "Senior") ||
            context.User.HasClaim("Subscription:Tier", "Premium")));
});

// Cookie Authentication (para ter HttpContext.User)
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
```

## Claims dos usuarios de demonstracao

| Email | Senha | Claims |
|-------|-------|--------|
| admin@authlabs.com | Admin123! | `Document:Edit=true`, `Document:Delete=true`, `User:Manage=true`, `Subscription:Tier=Premium` |
| manager@authlabs.com | Manager123! | `Document:Edit=true`, `Subscription:Tier=Standard` |
| user@authlabs.com | User123! | `Document:Edit=true`, `Subscription:Tier=Basic` |
| guest@authlabs.com | Guest123! | (nenhum claim) |

### Matriz de Acesso

| Usuario | Edit | Delete | Admin | Premium |
|---------|------|--------|-------|---------|
| admin | Sim | Sim | Sim | Sim |
| manager | Sim | Nao | Nao | Nao |
| user | Sim | Nao | Nao | Nao |
| guest | Nao | Nao | Nao | Nao |

## Endpoints principais

| Metodo | Path | Policy | Descricao |
|--------|------|--------|-----------|
| POST | /api/auth/login | - | Login (retorna claims) |
| GET | /api/protected | `[Authorize]` | Info do usuario atual |
| GET | /api/protected/edit | `CanEditDocuments` | Editar documentos |
| GET | /api/protected/delete | `CanDeleteDocuments` | Deletar documentos |
| GET | /api/protected/admin | `CanManageUsers` | Funcoes de admin |
| GET | /api/protected/premium | `IsPremiumUser` | Recursos premium |

## Exemplo de uso

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}'
```

**Resposta:**
```json
{
  "email": "admin@authlabs.com",
  "claims": {
    "Document:Edit": "true",
    "Document:Delete": "true",
    "User:Manage": "true",
    "Subscription:Tier": "Premium"
  }
}
```

### Tentar acessar premium (admin - deve funcionar)
```bash
# Assumindo que a sessao esta configurada
curl -X GET http://localhost:5000/api/protected/premium \
  -H "Cookie: AuthLabs.Claims=..."
```

### Tentar acessar premium (guest - deve falhar)
```bash
# Guest nao tem claim Subscription:Tier=Premium
# Retorna: 403 Forbidden
```

### Verificar claims de um usuario
```json
// GET /api/protected
{
  "email": "manager@authlabs.com",
  "claims": {
    "Document:Edit": "true",
    "Subscription:Tier": "Standard"
  }
}
```

## Common Errors

### 1. 401 Unauthorized mesmo apos login

**Sintoma:** Login funciona mas proxima requisicao retorna 401.

**Causa:** Login nao esta criando cookie de autenticacao, apenas retornando claims.

**Solucao:**
```bash
# Verificar se cookie esta sendo setado
curl -v -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}' 2>&1 | grep -i set-cookie

# Se NAO houver cookie, o sistema precisa criar Identity apos login
# [Authorize] requer que HttpContext.User esteja populado
```

### 2. 403 Forbidden em endpoint com policy

**Sintoma:** Policy deveria permitir mas retorna 403.

**Causas:**
- Claim necessario nao esta presente
- Valor do claim nao corresponde
- Policy incorretamente configurada

**Solucao:**
```bash
# Primeiro, verificar todos os claims do usuario
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: AuthLabs.Claims=..."

# Verificar policy configurada
# Admin tem: Document:Edit=true, Document:Delete=true, User:Manage=true, Subscription:Tier=Premium
# Policy IsPremiumUser requer: Subscription:Tier=Premium
# Admin TEM esse claim, entao deveria funcionar
```

### 3. Claim com dois pontos (:) nao funciona

**Sintoma:** Policy com claim type contendo `:` nao e reconhecido.

**Causa:** ASP.NET Core pode ter problemas com claim types que contem `:`.

**Solucao:**
```csharp
// Usar constant em vez de string literal
public static class ClaimTypes
{
    public const string DocumentPermission = "Document";
}

// Entao usar:
policy.RequireClaim("Document:Edit", "true");

// Ou usar ClaimsPrincipal extensions
public static bool HasDocumentEdit(this ClaimsPrincipal user)
{
    return user.HasClaim("Document:Edit", "true");
}
```

### 4. Claims nao persistem entre requisicoes

**Sintoma:** A cada requisicao, claims precisam ser recarregados.

**Causa:** Claims nao estao sendo serializados no cookie/sessao.

**Solucao:**
```csharp
// Ao fazer login, criar cookie com claims
var claims = new List<Claim>
{
    new(ClaimTypes.Email, user.Email),
    new("Document:Edit", user.CanEdit.ToString()),
    // ...
};

var identity = new ClaimsIdentity(claims, "Cookie");
var principal = new ClaimsPrincipal(identity);

await HttpContext.SignInAsync(principal);
```

## Security Considerations

### 1. Claims de Sources Confiaveis

```csharp
// Apenas trusted identity providers podem definir claims
services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar emissor
            ValidIssuer = "https://trusted-issuer.com",
            
            // Validar audiencia
            ValidAudience = "my-api",
            
            // Nao confiar em claims customizados de fontes nao confiaveis
            NameClaimType = ClaimTypes.Email,
            RoleClaimType = ClaimTypes.Role
        };
    });
```

### 2. Validacao de Claims

```csharp
// Claims devem ser validados antes de usar
public class ClaimsValidator
{
    public static bool ValidateSubscriptionClaim(ClaimsPrincipal user)
    {
        var tierClaim = user.FindFirst("Subscription:Tier");
        if (tierClaim == null)
            return false;
            
        var validTiers = new[] { "Free", "Basic", "Standard", "Premium", "Enterprise" };
        return validTiers.Contains(tierClaim.Value);
    }
}
```

### 3. Claims para Auditoria

```csharp
// Sempre logar claims relevantes para auditoria
[Authorize]
public async Task<IActionResult> SensitiveOperation()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var department = User.FindFirst("Department")?.Value;
    var tier = User.FindFirst("Subscription:Tier")?.Value;
    
    _auditLogger.Log(
        "SensitiveOperation",
        userId,
        new { Department = department, Tier = tier });
        
    // ...
}
```

## Implementacao de Claims Requirements

### Custom Requirement
```csharp
public class CustomClaimRequirement : IAuthorizationRequirement
{
    public string ClaimType { get; }
    public string[] AllowedValues { get; }

    public CustomClaimRequirement(string claimType, params string[] allowedValues)
    {
        ClaimType = claimType;
        AllowedValues = allowedValues;
    }
}
```

### Handler
```csharp
public class CustomClaimHandler : AuthorizationHandler<CustomClaimRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomClaimRequirement requirement)
    {
        var claim = context.User.FindFirst(requirement.ClaimType);

        if (claim != null && requirement.AllowedValues.Contains(claim.Value))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### Registrar
```csharp
services.AddScoped<IAuthorizationHandler, CustomClaimHandler>();

services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessPremium", policy =>
        policy.Requirements.Add(new CustomClaimRequirement(
            "Subscription:Tier", "Premium", "Enterprise")));
});
```

## Policies vs Claims Requirements

### Policy simples (built-in)
```csharp
policy.RequireClaim("Document:Edit", "true")
```
Simples, verifica se claim existe com valor especifico.

### Custom Requirement (para logicas complexas)
```csharp
// Multiple values
policy.Requirements.Add(new CustomClaimRequirement("Subscription:Tier", "Premium", "Enterprise"));

// Multiple claims
policy.RequireAssertion(context =>
    context.User.HasClaim("Department", "Sales") &&
    context.User.HasClaim("Level", "Senior"));
```

## Claims vs Roles

| Aspecto | Roles | Claims |
|---------|-------|--------|
| Granularidade | Gross | Fina |
| Flexibilidade | Baixa | Alta |
| Performance | Mais rapido | Overhead |
| Manutencao | Simples | Complexo |
| Uso tipico | App interno | Federacao, SaaS |

## Referências

- [Microsoft Docs - Claims-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/claims)
- [Microsoft - Policy-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/policies)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
- [NIST ABAC Guide](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.8625.pdf)
