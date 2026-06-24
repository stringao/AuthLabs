# API Keys Authentication

## O que e

API Keys e um mecanismo de autenticacao onde uma chave unica (string longa) identifica o cliente/servico que faz a requisicao. A chave e enviada no header `X-Api-Key` e validada contra um banco de dados de chaves registradas.

API Keys sao ideais para autenticacao machine-to-machine (M2M) onde nao ha interacao humana com um navegador. Sao comumente usadas para:
- Autenticacao de servicos (backends, microservices)
- APis publicas com rate limiting por cliente
- Integracao com sistemas de terceiros
- Clientes que nao suportam OAuth (IoT, sistemas legados)

## Como funciona

1. **Registro da API Key**: Administrador cria uma API key para o cliente no banco de dados
2. **Distribuicao**: Chave e entregue ao cliente de forma segura (HTTPS, secrets manager)
3. **Requisição**: Cliente envia chave no header `X-Api-Key: <key>`
4. **Extracao**: Middleware extrai a chave do header
5. **Hash**: Middleware calcula hash SHA256 da chave
6. **Validacao**: Consulta banco de dados para encontrar chave com hash correspondente
7. **Verificacoes**: Valida se chave esta ativa e nao expirou
8. **Claims**: Se valida, cria principal com claims do cliente (nome, role, scopes)
9. **Autorizacao**: ASP.NET Core authorization avalia policies

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           API KEY AUTHENTICATION FLOW                         │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                         SERVER                         DATABASE
    ──────                         ──────                         ────────
       │                              │                               │
       │  1. API Request             │                               │
       │     X-Api-Key: key-123456  │                               │
       │───────────────────────────►│                               │
       │                              │                               │
       │                              │  2. Extract API key          │
       │                              │     from X-Api-Key header    │
       │                              │                               │
       │                              │  3. Hash the key            │
       │                              │     SHA256(api_key)         │
       │                              │     = key_hash             │
       │                              │                               │
       │                              │  4. Lookup by hash         │
       │                              │────────────────────────────►│
       │                              │                               │
       │                              │  5. Return matching key    │
       │                              │     (or null if not found)  │
       │                              │◄────────────────────────────│
       │                              │                               │
       │                              │  6. Validate key            │
       │                              │     - IsActive = true       │
       │                              │     - ExpiresAt > now       │
       │                              │     - Role matches          │
       │                              │                               │
       │                              │  7. Create ClaimsPrincipal   │
       │                              │     - Name = client_name    │
       │                              │     - Role = client_role    │
       │                              │     - Scopes = permissions  │
       │                              │                               │
       │                              │  8. Authorize request       │
       │                              │     (check policies)        │
       │                              │                               │
       │  9. 200 OK                  │                               │
       │     {data}                  │                               │
       │◄───────────────────────────│                               │


┌─────────────────────────────────────────────────────────────────────────────┐
│                          API KEY SECURITY FLOW                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  KEY STORAGE (Server)          │  KEY TRANSMISSION (Client → Server)        │
│  ──────────────────            │  ──────────────────────────────────         │
│                               │                                             │
│  ┌─────────────────┐          │    Client         Server                   │
│  │ API Key (raw)   │          │    ──────         ──────                   │
│  │ NEVER stored    │          │       │               │                     │
│  └────────┬────────┘          │       │  1. Key hash  │                     │
│           │                   │       │     SHA256     │                     │
│           ▼                   │       │───────────────►│                     │
│  ┌─────────────────┐          │       │               │ 2. Lookup hash     │
│  │ Key Hash (SHA256)│         │       │               │    in database     │
│  │ Stored in DB    │          │       │               │                   │
│  └─────────────────┘          │       │               │                   │
│           │                   │       │               │                   │
│  ┌─────────────────┐          │       │               │                   │
│  │ Key Prefix      │          │       │               │                   │
│  │ (first 8 chars) │          │       │               │                   │
│  │ For logging     │          │       │               │                   │
│  └─────────────────┘          │       │               │                   │
│                               │       │               │                   │
│  IMPORTANT: Even if DB is     │       │               │                   │
│  compromised, attacker       │       │               │                   │
│  cannot derive original key   │       │               │                   │
│                               │       │               │                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- Autenticacao de servicos (machine-to-machine)
- APIs publicas que precisam de rate limiting por cliente
- Sistemas de integracao onde OAuth seria overkill
- Clientes que nao suportam OAuth (sistemas legados, IoT)
- Quando precisa identificar qual aplicacao faz a requisicao
- APIs de parceiros e integracoes de terceiros
- CLIs e ferramentas de linha de comando

## Quando NAO usar

- Autenticacao de usuarios finais (melhor com OAuth/OIDC)
- Quando precisa de flexibilidade de claims (roles dinamicos)
- Cenarios onde a chave pode ser comprometida facilmente
- Aplicacoes web onde header customizado e inconvenimente
- Quando precisa de logout/invalidiacao imediata
- Cenarios de alta seguranca (melhor com mutual TLS)
- Quando precisa de audit trail detalhado por usuario

## Alertas e caveats importantes

1. **Chaves em codigo fonte**: As chaves demo estao hardcoded em `Program.cs`. Producao deve usar secrets manager.

2. **Sem rotacao automatica**: Nao ha mecanismo nativo para rotacionar ou regenerar chaves.

3. **Sem rate limiting**: Nao ha protecao contra forca bruta nas validacoes.

4. **Sem logging de falhas**: Tentativas de autenticacao falhadas nao sao logadas.

5. **Sem endpoint de revogacao**: Nao ha API para revogar chaves comprometidas em runtime.

6. **Dependencia do banco**: Autenticacao falha se PostgreSQL estiver indisponivel.

7. **Validacao de scope manual**: Usa `User.HasClaim()` ao inves de policy-based authorization.

8. **Hash reversivel**: SHA256 e hash unidirecional, mas se o banco for comprometido, atacantes podem validar contra rainbow tables.

9. **Storage em localStorage (SPA)**: Se usado em browsers, chaves em localStorage sao vulneraveis a XSS.

## Configuracao necessaria

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
    public string KeyPrefix { get; set; }     // Primeiros 8 chars (para identificacao)
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Role { get; set; }          // User, Admin, Guest
    public ICollection<ApiKeyScope> Scopes { get; set; }
}
```

**Middleware registration (Program.cs):**
```csharp
// Demo keys em memoria (NAO USAR EM PRODUCAO)
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

| Metodo | Path | Auth | Roles/Scopes | Descricao |
|--------|------|------|-------------|-----------|
| GET | /health | Nao | - | Health check (sem auth) |
| GET | /api/protected | Sim | Qualquer | Info do cliente autenticado |
| GET | /api/protected/read | Sim | User/Admin + scope:read | Acesso de leitura |
| GET | /api/protected/write | Sim | User/Admin + scope:write | Acesso de escrita |
| GET | /api/protected/delete | Sim | Admin + scope:delete | Acesso de exclusao |

## Usuarios de demonstracao (API Keys)

| Client | API Key | Scopes | Role |
|--------|---------|--------|------|
| mobile-app | `mobile-app-key-12345678` | read, write | User |
| web-frontend | `web-frontend-key-87654321` | read | User |
| admin-panel | `admin-panel-key-11223344` | read, write, delete | Admin |
| external-partner | `external-partner-key-55667788` | read | Guest |

**ATENCAO**: As chaves sao exibidas no console no primeiro start. Em producao, nunca exponha chaves em logs.

## Exemplo de uso

### Requisicao autenticada
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

### Tentar acesso negado (scope invalido)
```bash
# web-frontend nao tem scope write
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

### API key expirada ou invalida
```bash
curl -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: invalid-key"
```

**Resposta:**
```
401 Unauthorized
```

## Common Errors

### 1. "Missing X-Api-Key header"

**Sintoma:** 401 Unauthorized mesmo enviando a chave.

**Causas:**
- Header com nome incorreto (diferenca entre `X-Api-Key` e `Api-Key`)
- Header nao esta sendo enviado corretamente
- Chave com espacos em branco

**Solucao:**
```bash
# Verificar header sendo enviado
curl -v -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: mobile-app-key-12345678" 2>&1 | grep -i x-api-key

# Verificar permissoes do header
curl -v -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: mobile-app-key-12345678" \
  -H "Access-Control-Allow-Headers: X-Api-Key"
```

### 2. "Invalid API key"

**Sintoma:** 401 Unauthorized mesmo com chave valida.

**Causas:**
- Hash da chave nao corresponde ao banco
- Chave foi alterada apos registro
- Chave expirou

**Solucao:**
```bash
# Verificar se a chave existe no banco
# Comparar hash da chave enviada com hash no banco
# Verificar expiracao: SELECT * FROM api_keys WHERE expires_at < NOW();

# Se expirada, gerar nova chave
```

### 3. "API key expired"

**Sintoma:** 401 Unauthorized com chave que ja funcionou.

**Causa:** Chave tem data de expiracao e passou dela.

**Solucao:**
```bash
# Verificar expiracao
# SELECT key_name, expires_at FROM api_keys;

# Renovar chave (requer admin)
curl -X POST http://localhost:5000/api/admin/keys/renew \
  -H "X-Api-Key: admin-key-xxx" \
  -d '{"keyId": 1, "newExpiration": "2025-12-31"}'
```

### 4. 403 Forbidden mesmo com chave valida

**Sintoma:** Autenticacao funciona (chave valida) mas autorizacao falha.

**Causas:**
- Role da chave nao tem permissao para o endpoint
- Scope necessario nao esta presente

**Solucao:**
```bash
# Verificar qual role/scope e necessario
# Verificar role da chave

# Admin endpoint requer role=Admin
curl -X GET http://localhost:5000/api/protected/admin \
  -H "X-Api-Key: admin-panel-key-11223344"  # Tem role=Admin

curl -X GET http://localhost:5000/api/protected/admin \
  -H "X-Api-Key: mobile-app-key-12345678"   # TEM role=User, NAO Admin
```

### 5. Rate limiting triggers prematuramente

**Sintoma:** 429 Too Many Requests mesmo com poucas requisicoes.

**Causa:** Rate limiter por IP ou outra regra aplicada incorretamente.

**Solucao:**
```bash
# Verificar headers de rate limit
curl -i -X GET http://localhost:5000/api/protected \
  -H "X-Api-Key: mobile-app-key-12345678"

# Retry-After: 60
# X-RateLimit-Limit: 100
# X-RateLimit-Remaining: 0
```

## Security Considerations

### 1. Armazenamento Seguro

```csharp
// ARMAZENAR APENAS HASH, NAO A CHAVE EM TEXTO
public static string ComputeHash(string apiKey)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(apiKey);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

// Para verificacao, recalcular hash e comparar
public async Task<bool> ValidateKeyAsync(string rawKey)
{
    var hash = ComputeHash(rawKey);
    var key = await _context.ApiKeys
        .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);
    return key != null;
}
```

### 2. Geracao de Chaves Seguras

```csharp
// Usar RNG criptografico
public static string GenerateApiKey(int length = 32)
{
    var bytes = new byte[length];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);
    
    // Base64 URL safe
    return Convert.ToBase64String(bytes)
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');
}

// Armazenar prefixo para identificacao em logs (NAO para validacao)
var prefix = apiKey.Substring(0, 8);  // "ab12cd34..."
```

### 3. Transmissao Segura

```bash
# SEMPRE usar HTTPS em producao
curl -X GET https://api.example.com/api/protected \
  -H "X-Api-Key: sua-chave-aqui"

# Nunca logue a chave completa
# Logs devem mostrar apenas prefixo: "Key: ab12****"
```

### 4. Rate Limiting

```csharp
// Implementar rate limiting por API key
app.UseRateLimiter(new RateLimiterOptions()
{
    PartitionKeyResolver = context =>
        context.Request.Headers["X-Api-Key"].FirstOrDefault()
        ?? context.Connection.RemoteIpAddress.ToString(),
    
    Limiter = TimeSpan.FromMinute,
    PermitLimit = 100
});
```

### 5. Rotacao de Chaves

```csharp
// Plano de rotacao:
// 1. Nova chave gerada
// 2. Ambas chaves (antiga e nova) sao validas por periodo de carencia
// 3. Apos carencia, chave antiga e invalidada

public class ApiKeyRotationService
{
    public async Task<ApiKey> RotateKeyAsync(int keyId)
    {
        var oldKey = await _context.ApiKeys.FindAsync(keyId);
        var newKey = new ApiKey
        {
            Name = oldKey.Name,
            KeyHash = ComputeHash(GenerateApiKey()),
            KeyPrefix = GenerateApiKey().Substring(0, 8),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = oldKey.ExpiresAt,
            Role = oldKey.Role,
            PreviousKeyHash = oldKey.KeyHash,  // ainda valida por 24h
            PreviousKeyExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        
        await _context.ApiKeys.AddAsync(newKey);
        await _context.SaveChangesAsync();
        
        return newKey;  // Retornar NOVA chave ao cliente
    }
}
```

### 6. Audit Trail

```csharp
// Logar todas as tentativas
public class ApiKeyAuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var key))
        {
            _logger.LogInformation(
                "API Key Access: {Prefix} | {Method} | {Path} | {IP}",
                key.Value.Substring(0, 8),
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);
        }
        
        await _next(context);
    }
}
```

## Middleware Implementation

```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApplicationDbContext _context;

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
            .FirstOrDefaultAsync(k => 
                (k.KeyHash == keyHash || k.PreviousKeyHash == keyHash) 
                && k.IsActive);

        if (keyEntity == null)
            return AuthenticateResult.Fail("Invalid API key");

        // 4. Verificar expiracao
        if (keyEntity.ExpiresAt.HasValue && keyEntity.ExpiresAt < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key expired");
            
        // 5. Verificar expiracao da chave anterior (durante rotacao)
        if (keyEntity.PreviousKeyHash == keyHash 
            && keyEntity.PreviousKeyExpiresAt < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key expired (rotated)");

        // 6. Criar claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, keyEntity.Name),
            new(ClaimTypes.Role, keyEntity.Role),
            new("apikey_id", keyEntity.Id.ToString()),
            new("client_name", keyEntity.Name)
        };

        // 7. Adicionar scopes
        foreach (var scope in keyEntity.Scopes)
            claims.Add(new Claim("scope", scope.Name));

        // 8. Criar principal
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
```

## Referências

- [OWASP API Security - API Key](https://owasp.org/www-project-api-security/)
- [RFC 7519 - JWT (para comparacao)](https://tools.ietf.org/html/rfc7519)
- [Microsoft Docs - Custom authentication](https://docs.microsoft.com/aspnet/core/security/authentication/)
- [NIST SP 800-63B - Digital Identity Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)
