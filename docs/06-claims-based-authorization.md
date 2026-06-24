# Claims-Based Authorization

## O que é

Claims-based Authorization é um modelo de autorização onde permissões são concedidas com base em Claims (afirmações) - pares de chave-valor que descrevem características do usuário como cargo, departamento, nível de assinatura, etc. Diferente de RBAC (Role-Based), claims permitem modelagem mais granular e flexível.

## Como funciona

1. **Autenticação**: Usuário faz login e recebe claims do identity provider
2. **Claims Generation**: Sistema gera claims baseados nos dados do usuário (ex: `Department=Sales`, `Subscription=Tier=Premium`)
3. **Policies Definition**: Administrador define policies que requerem claims específicos
4. **Request Evaluation**: Em cada requisição protegida, o sistema avalia se o principal tem os claims requeridos
5. **Authorization**: Acesso concedido ou negado baseado na presença e valores dos claims

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
     │  + Get Claims │                  │
     │─────────────►│                  │
     │               │◄────────────────│
     │               │                  │
     │  3. Set Cookie│                  │
     │  with Claims  │                  │
     │◄─────────────│                  │
     │               │                  │
     │  4. Protected │                  │
     │  Request     │                  │
     │─────────────►│                  │
     │               │                  │
     │  5. Evaluate  │                  │
     │  Policy       │                  │
     │  (RequireClaim)                  │
     │               │                  │
     │  6. Access    │                  │
     │  Granted/Denied                  │
     │◄─────────────│                  │
```

## Quando usar

- Quando permissões dependem de atributos além de roles (departamento, região, etc)
- Sistemas com níveis de assinatura/serviço (Free, Premium, Enterprise)
- Quando同一个 role precisa de permissões diferentes baseadas em contexto
- Integração com sistemas externos que fornecem claims (OAuth, OIDC)
- Cenários onde permissões mudam frequentemente (não quer重新 deploy)
- Federalização de identidade (claims de múltiplos identity providers)

## Quando NÃO usar

- Sistemas simples com poucas permissões fixas (RBAC é mais simples)
- Quando performance é crítica (claims evaluation tem overhead)
- Quando claims são extremamente granulares (considere ABAC - Attribute-Based)
- Sistemas onde roles são suficientes e não mudam frequentemente

## Alertas e caveats importantes

1. **Credenciais hardcoded**: Todas as senhas estão em texto plano no código. ISSO É UM RISCO CRÍTICO.

2. **Sem sessão de autenticação**: O login retorna claims mas NÃO cria cookie de autenticação. O `[Authorize]` falhará.

3. **Sem hashing de senha**: Senhas são comparadas diretamente, sem hash.

4. **Validação duplicada**: Dados de usuário duplicados em `AuthController.cs` e `ClaimsService.cs`.

5. **Armazenamento em memória**: Não há persistência - usuários se perdem ao reiniciar.

6. **Sem HTTPS enforcement**: Comunicação sem SSL/TLS explícito.

7. **Política frouxa**: Validação de email/password é mínima (apenas dictionary lookup).

8. **Missing handler check**: `CustomClaimHandler` pode não estar sendo invocado corretamente.

## Configuração necessária

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
});

// Cookie Authentication (para ter HttpContext.User)
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
```

## Claims dos usuários de demonstração

| Email | Senha | Claims |
|-------|-------|--------|
| admin@authlabs.com | Admin123! | `Document:Edit=true`, `Document:Delete=true`, `User:Manage=true`, `Subscription:Tier=Premium` |
| manager@authlabs.com | Manager123! | `Document:Edit=true`, `Subscription:Tier=Standard` |
| user@authlabs.com | User123! | `Document:Edit=true`, `Subscription:Tier=Basic` |
| guest@authlabs.com | Guest123! | (nenhum claim) |

### Matriz de Acesso

| Usuário | Edit | Delete | Admin | Premium |
|---------|------|--------|-------|---------|
| admin | Sim | Sim | Sim | Sim |
| manager | Sim | Não | Não | Não |
| user | Sim | Não | Não | Não |
| guest | Não | Não | Não | Não |

## Endpoints principais

| Método | Path | Policy | Descrição |
|--------|------|--------|-----------|
| POST | /api/auth/login | - | Login (retorna claims) |
| GET | /api/protected | `[Authorize]` | Info do usuário atual |
| GET | /api/protected/edit | `CanEditDocuments` | Editar documentos |
| GET | /api/protected/delete | `CanDeleteDocuments` | Deletar documentos |
| GET | /api/protected/admin | `CanManageUsers` | Funções de admin |
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
# Assumindo que a sessão está configurada
curl -X GET http://localhost:5000/api/protected/premium \
  -H "Cookie: AuthLabs.Claims=..."
```

### Tentar acessar premium (guest - deve falhar)
```bash
# Guest não tem claim Subscription:Tier=Premium
# Retorna: 403 Forbidden
```

### Verificar claims de um usuário
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

## Implementação de Claims Requirements

```csharp
// Custom requirement
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

// Handler
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

## Policies vs Claims Requirements

### Policy simples (built-in)
```csharp
policy.RequireClaim("Document:Edit", "true")
```
Simples, verifica se claim existe com valor específico.

### Custom Requirement (para lógicas complexas)
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
| Performance | Mais rápido | Overhead |
| Manutenção | Simples | Complexo |
| Uso típico | App interno | Federação, SaaS |

## Referências

- [Microsoft Docs - Claims-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/claims)
- [Microsoft - Policy-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/policies)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
