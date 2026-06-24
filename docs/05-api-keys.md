# API Keys Authentication

## O que é

API Keys é um mecanismo de autenticação onde uma chave única (string longa) identifica o cliente/serviço que faz a requisição. A chave é enviada no header `X-Api-Key` e validada contra um banco de dados de chaves registradas.

## Como funciona

1. **Registro da API Key**: Administrador cria uma API key para o cliente no banco de dados
2. **Distribuição**: Chave é entregue ao cliente de forma segura
3. **Requisição**: Cliente envia chave no header `X-Api-Key: <key>`
4. **Extração**: Middleware extrai a chave do header
5. **Hash**: Middleware calcula hash SHA256 da chave
6. **Validação**: Consulta banco de dados para encontrar chave com hash correspondente
7. **Verificações**: Valida se chave está ativa e não expirou
8. **Claims**: Se válida, cria principal com claims do cliente (nome, role, scopes)
9. **Autorização**: ASP.NET Core authorization avalia policies

## Diagrama de fluxo
```
┌────────┐     ┌──────────────┐     ┌─────────┐
│ Client │────►│   Server     │────►│   DB    │
│  API   │◄────│  (ASP.NET)   │◄────│  (PG)   │
└────────┘     └──────────────┘     └─────────┘
     │               │                  │
     │  1. Request  │                  │
     │  X-Api-Key   │                  │
     │─────────────►│                  │
     │               │                  │
     │  2. Hash     │                  │
     │  SHA256      │                  │
     │               │                  │
     │  3. Lookup   │                  │
     │  by hash     │                  │
     │─────────────►│                  │
     │               │◄────────────────│
     │               │                  │
     │  4. Validate  │                  │
     │  (active,     │                  │
     │   not expired)│                  │
     │               │                  │
     │  5. Create    │                  │
     │  Principal    │                  │
     │               │                  │
     │  6. Authorize │                  │
     │  (role+scope) │                  │
     │               │                  │
     │  7. Response  │                  │
     │◄─────────────│                  │
```

## Quando usar

- Autenticação de serviços (machine-to-machine)
- APIs públicas que precisam de rate limiting por cliente
- Sistemas de integração onde OAuth seria overkill
- Clientes que não suportam OAuth (sistemas legados, IoT)
- Quando precisa identificar qual aplicação faz a requisição
- APIs de партнеров e integrações de terceiros

## Quando NÃO usar

- Autenticação de usuários finais (melhor com OAuth/OIDC)
- Quando precisa de身份的-flexibilidade (claims, roles)
- Cenários onde a chave pode ser comprometida facilmente
- Aplicações web onde header customizado é inconveniente
- Quando precisa de logout/invalidação imediata
- Cenários de alta segurança (melhor com mutual TLS)

## Alertas e caveats importantes

1. **Chaves em código fonte**: As chaves demo estão hardcoded em `Program.cs`. Produção deve usar secrets manager.

2. **Sem rotação automática**: Não há mecanismo nativo para rotacionar ou regenerar chaves.

3. **Sem rate limiting**: Não há proteção contra força bruta nas validações.

4. **Sem logging de falhas**: Tentativas de autenticação falhadas não são logadas.

5. **Sem endpoint de revogação**: Não há API para revogar chaves comprometidas em runtime.

6. **Dependência do banco**: Autenticação falha se PostgreSQL estiver indisponível.

7. **Validação de scope manual**: Usa `User.HasClaim()` ao invés de policy-based authorization.

8. **Hash reversível**: SHA256 é hash unidirecional, mas se o banco for comprometido, atacantes podem validar contra rainbow tables.

9. **Storage em localStorage (SPA)**: Se usado em browsers, chaves em localStorage são vulneráveis a XSS.

## Configuração necessária

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=authlabs_apikey;Username=postgres;Password=postgres"
  }
}
```

**Entidade API Key (banco de dados):**
```csharp
public class ApiKey
{
    public int Id { get; set; }
    public string Name { get; set; }          // Nome do cliente
    public string KeyHash { get; set; }       // SHA256 hash (base64)
    public string KeyPrefix { get; set; }     // Primeiros 8 chars (para identificação)
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Role { get; set; }          // User, Admin, Guest
    public ICollection<ApiKeyScope> Scopes { get; set; }
}
```

**Middleware registration (Program.cs):**
```csharp
// Demo keys em memória (NÃO USAR EM PRODUÇÃO)
var demoKeys = new Dictionary<string, (string Name, string Role, string[] Scopes)>
{
    ["mobile-app-key-12345678"] = ("mobile-app", "User", new[] { "read", "write" }),
    ["web-frontend-key-87654321"] = ("web-frontend", "User", new[] { "read" }),
    ["admin-panel-key-11223344"] = ("admin-panel", "Admin", new[] { "read", "write", "delete" }),
    ["external-partner-key-55667788"] = ("external-partner", "Guest", new[] { "read" })
};

app.UseApiKeyAuthentication(demoKeys);
```

## Endpoints principais

| Método | Path | Auth | Roles/Scopes | Descrição |
|--------|------|------|-------------|-----------|
| GET | /health | Não | - | Health check (sem auth) |
| GET | /api/protected | Sim | Qualquer | Info do cliente autenticado |
| GET | /api/protected/read | Sim | User/Admin + scope:read | Acesso de leitura |
| GET | /api/protected/write | Sim | User/Admin + scope:write | Acesso de escrita |
| GET | /api/protected/delete | Sim | Admin + scope:delete | Acesso de exclusão |

## Usuários de demonstração (API Keys)

| Client | API Key | Scopes | Role |
|--------|---------|--------|------|
| mobile-app | `mobile-app-key-12345678` | read, write | User |
| web-frontend | `web-frontend-key-87654321` | read | User |
| admin-panel | `admin-panel-key-11223344` | read, write, delete | Admin |
| external-partner | `external-partner-key-55667788` | read | Guest |

**ATENÇÃO**: As chaves são exibidas no console no primeiro start. Em produção, nunca exponha chaves em logs.

## Exemplo de uso

### Requisição autenticada
```bash
curl -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: mobile-app-key-12345678"
```

**Resposta:**
```json
{
  "message": "Hello, mobile-app",
  "clientName": "mobile-app",
  "role": "User",
  "scopes": ["read", "write"]
}
```

### Testar scope de leitura
```bash
curl -X GET http://localhost:5000/api/protected/read \
  -H "X-Api-Key: web-frontend-key-87654321"
```

**Resposta:**
```json
{
  "message": "Read access granted",
  "scopes": ["read"]
}
```

### Tentar acesso negado (scope inválido)
```bash
# web-frontend não tem scope write
curl -X GET http://localhost:5000/api/protected/write \
  -H "X-Api-Key: web-frontend-key-87654321"
```

**Resposta:**
```json
{
  "error": "Access denied: missing required scope 'write'"
}
```

### Tentar delete sem ser admin
```bash
curl -X GET http://localhost:5000/api/protected/delete \
  -H "X-Api-Key: mobile-app-key-12345678"
```

**Resposta:**
```json
{
  "error": "Access denied: requires 'Admin' role"
}
```

### Health check (sem API key)
```bash
curl -X GET http://localhost:5000/health
```

**Resposta:**
```json
{
  "status": "healthy"
}
```

### API key expirada ou inválida
```bash
curl -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: invalid-key"
```

**Resposta:**
```
401 Unauthorized
```

## Middleware Implementation

```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Extrair header
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            return AuthenticateResult.Fail("Missing X-Api-Key header");

        // 2. Calcular hash
        var keyHash = ComputeHash(apiKey.ToString());

        // 3. Buscar no banco
        var keyEntity = await _context.ApiKeys
            .Include(k => k.Scopes)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (keyEntity == null)
            return AuthenticateResult.Fail("Invalid API key");

        // 4. Verificar expiração
        if (keyEntity.ExpiresAt.HasValue && keyEntity.ExpiresAt < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key expired");

        // 5. Criar claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, keyEntity.Name),
            new(ClaimTypes.Role, keyEntity.Role),
            new("apikey_id", keyEntity.Id.ToString()),
            new("client_name", keyEntity.Name)
        };

        // 6. Adicionar scopes
        foreach (var scope in keyEntity.Scopes)
            claims.Add(new Claim("scope", scope.Name));

        // 7. Criar principal
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
```

## Hash da API Key

```csharp
public static string ComputeHash(string apiKey)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(apiKey);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

## Melhores Práticas

1. **Storage seguro**: Armazenar apenas hash, nunca a chave em plaintext
2. **Transporte seguro**: Sempre usar HTTPS
3. **Rate limiting**: Implementar por API key para prevenir brute force
4. **Logging**: Logar todas as tentativas (sucesso e falha)
5. **Expiração**: Definir data de expiração para todas as chaves
6. **Rotação**: Ter processo de rotação de chaves
7. **Revogação**: Endpoint para revogar chaves comprometidas
8. **Scopes mínimos**: Dar apenas scopes necessários

## Referências

- [OWASP API Security - API Key](https://owasp.org/www-project-api-security/)
- [RFC 7519 - JWT (para comparação)](https://tools.ietf.org/html/rfc7519)
- [Microsoft Docs - Custom authentication](https://docs.microsoft.com/aspnet/core/security/authentication/)
