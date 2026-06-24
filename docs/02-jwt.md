# JWT Authentication

## O que é

JWT (JSON Web Token) é um padrão RFC 7519 para representar Claims (afirmações) de forma compacta e autossuficiente. Na autenticação, o JWT funciona como um "token de acesso" assinado cryptographicamente que contém informações do usuário e é validado em cada requisição sem necessidade de consultar o banco de dados.

O JWT é particularmente útil para arquiteturas stateless onde você não quer manter sessão no servidor. O token contém toda a informação necessária para validar a identidade do usuário, e pode ser verificado por qualquer serviço que tenha a chave secreta.

## Estrutura do JWT

Um JWT é composto por três partes separadas por ponto (`.`):

```
header.payload.signature
```

### Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### Payload (Claims)
```json
{
  "sub": "1",
  "email": "admin@authlabs.com",
  "jti": "unique-token-id",
  "roles": "Admin",
  "iss": "AuthLabs.Jwt",
  "aud": "AuthLabs.Jwt.Api",
  "exp": 1735689600,
  "iat": 1735689000
}
```

### Signature
```
HMACSHA256(
  base64UrlEncode(header) + "." +
  base64UrlEncode(payload),
  secret_key
)
```

## Conceitos Fundamentais

### Access Token vs Refresh Token

| Aspecto | Access Token | Refresh Token |
|---------|--------------|---------------|
| Validade | Curta (15 min) | Longa (7 dias) |
| Armazenamento | Cliente (memória) | Cliente (secure storage) |
| Validação | Stateless (sem DB) | Requer DB lookup |
| Conteúdo | Claims básicos | Revokable reference |

### Por que separar?

- **Access Token curto**: Minimiza janela de ataque se token for comprometido
- **Refresh Token longo**: Evita login frequente mantendo segurança
- **Revogação**: Refresh tokens podem ser revogados no banco

## Como funciona

1. **Login**: Usuário envia credenciais para `/api/auth/login`
2. **Validação**: Servidor valida email/senha contra o banco de dados
3. **Geração do Access Token**: Servidor cria um JWT assinado contendo:
   - `sub`: ID do usuário
   - `email`: Email do usuário
   - `jti`: JWT ID (identificador único)
   - `roles`: Papéis do usuário
   - `iss`: Emissor (Issuer)
   - `aud`: Audiência (Audience)
   - `exp`: Expiração
4. **Geração do Refresh Token**: Servidor cria um refresh token de longa duração (7 dias) e armazena no banco
5. **Retorno ao Cliente**: Access token e refresh token são retornados ao cliente
6. **Requisições**: Cliente envia access token no header `Authorization: Bearer <token>`
7. **Validação**: Servidor valida assinatura e claims do JWT
8. **Refresh**: Quando access token expira, cliente envia refresh token para obter novo par

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           JWT AUTHENTICATION FLOW                             │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                         SERVER                         DATABASE
    ──────                         ──────                         ────────

       │                              │                               │
       │  1. POST /login            │                               │
       │     {email, password}        │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  2. Validate credentials      │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  3. Verify password hash      │
       │                              │◄──────────────────────────────│
       │                              │                               │
       │                              │  4. Generate tokens           │
       │                              │     - Access JWT (15min)       │
       │                              │     - Refresh Token (7 days)  │
       │                              │                               │
       │                              │  5. Store Refresh Token       │
       │                              │──────────────────────────────►│
       │                              │                               │
       │  6. {accessToken,            │                               │
       │      refreshToken,           │                               │
       │      expiresIn: 900}         │                               │
       │◄────────────────────────────│                               │
       │                              │                               │
       │  7. GET /protected          │                               │
       │     Authorization:           │                               │
       │     Bearer <accessToken>     │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  8. Validate JWT signature    │
       │                              │     (NO DATABASE CALL)         │
       │                              │     - Verify issuer           │
       │                              │     - Verify audience         │
       │                              │     - Check expiration        │
       │                              │     - Extract claims          │
       │                              │                               │
       │  9. 200 OK                  │                               │
       │◄────────────────────────────│                               │
       │                              │                               │
       │                              │  *** Token Expires ***        │
       │                              │                               │
       │  10. GET /protected         │                               │
       │      (expired token)         │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  11. 401 Token Expired        │
       │  12. 401 Unauthorized        │                               │
       │◄────────────────────────────│                               │
       │                              │                               │
       │  13. POST /refresh          │                               │
       │      {refreshToken}          │                               │
       │────────────────────────────►│                               │
       │                              │                               │
       │                              │  14. Lookup refresh token     │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  15. Verify not revoked       │
       │                              │◄──────────────────────────────│
       │                              │                               │
       │                              │  16. Revoke old refresh       │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  17. Generate new pair        │
       │                              │──────────────────────────────►│
       │                              │                               │
       │  18. {newAccessToken,        │                               │
       │       newRefreshToken}       │                               │
       │◄────────────────────────────│                               │


┌─────────────────────────────────────────────────────────────────────────────┐
│                         JWT VALIDATION STEPS                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  1. Check token format (3 parts separated by .)                              │
│  2. Verify signature with server's secret key                                │
│  3. Validate issuer (iss claim) matches expected                            │
│  4. Validate audience (aud claim) matches expected                           │
│  5. Check expiration (exp) is in the future                                 │
│  6. Verify not before (nbf) if present                                      │
│  7. Extract and use claims                                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- APIs REST stateless que servem múltiplos clientes (web, mobile, IoT)
- Microserviços que precisam de autenticação distribuída
- Single Page Applications (SPAs) que se autenticam diretamente com APIs
- Quando precisa de escalabilidade horizontal (múltiplos servidores)
- Cenários onde o token precisa ser validado sem session store
- Sistemas com múltiplos serviços que precisam validar identidade

## Quando NÃO usar

- Sessões de longa duração (melhor com refresh tokens + server-side state)
- Quando precisa invalidar tokens imediatamente (logout instantâneo)
- Dados extremamente sensíveis que requerem validação constante de permissões
- Cenários onde o payload do token pode crescer muito
- Aplicações que exigem "forgot password" com invalidação imediata de todos os tokens
- Quando refresh token rotation é complexo demais para o caso de uso

## Alertas e caveats importantes

1. **Chave secreta hardcoded**: A chave JWT `SecretKey` está hardcoded em `appsettings.json`. Em produção, usar Environment Variables ou Azure Key Vault.

2. **Politica de senha fraca**: Minimo de 6 caracteres e muito curto. Recomenda-se 8-12+ com caracteres especiais.

3. **Access Token nao e revogavel**: Tokens de acesso nao sao armazenados em blacklist - expiram naturalmente. Janela de 15 minutos mitiga isso.

4. **Refresh Token em texto plano**: O `RefreshToken` e armazenado sem hash no banco. Em producao, should be hashed like passwords.

5. **Sem rate limiting**: Endpoint de login vulneravel a ataques de forca bruta.

6. **Sem HTTPS enforcement**: Sem requisito explícito para HTTPS na configuracao.

7. **Credenciais no banco em texto plano**: Connection string com credenciais no config - usar secrets manager.

8. **Clock skew**: Configurado com `ClockSkew = TimeSpan.Zero` pode causar problemas com multiplos servidores em fusos diferentes.

## Configuracao necessaria

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123"
  },
  "Jwt": {
    "SecretKey": "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
    "Issuer": "AuthLabs.Jwt",
    "Audience": "AuthLabs.Jwt.Api"
  }
}
```

**Configuracao JWT (Program.cs):**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "AuthLabs.Jwt",
            ValidAudience = "AuthLabs.Jwt.Api",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("ThisIsAVeryLongSecretKeyForTestingPurposes123!")),
            ClockSkew = TimeSpan.FromMinutes(1),  // Allow 1 minute clock skew
            RequireExpirationTime = true,
            ValidAlgorithms = new[] { "HS256" }
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });
```

## Endpoints principais

| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| POST | /api/auth/login | Nao | Login retorna access + refresh token |
| POST | /api/auth/refresh | Nao | Refresh: troca refresh token por novo par |
| POST | /api/auth/logout | Sim | Logout: revoga refresh token |
| GET | /api/protected | Sim | Endpoint protegido (qualquer usuario) |
| GET | /api/protected/admin | Sim (Admin) | Endpoint protegido (apenas Admin) |

## Usuarios de demonstracao

| Email | Senha | Roles |
|-------|-------|-------|
| admin@authlabs.com | Admin123! | Admin |
| manager@authlabs.com | Manager123! | Manager |
| user@authlabs.com | User123! | User |
| guest@authlabs.com | Guest123! | Guest |

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
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900
}
```

### Acessar endpoint protegido
```bash
curl -X GET http://localhost:5000/api/protected \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Resposta:**
```json
{
  "message": "Hello, admin@authlabs.com"
}
```

### Refresh do token
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."}'
```

**Resposta:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900
}
```

### Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Testar token expirado (apos 15 minutos)
```bash
# Tentar acessar com token expirado
curl -X GET http://localhost:5000/api/protected \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
# Retorna: 401 Unauthorized
```

### Decodificar JWT manualmente

```bash
# Instalar jq e base64 decode
# Header
echo "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" | base64 -d | jq .

# Payload
echo "eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkBhdXRo" | base64 -d | jq .
```

## Common Errors

### 1. "Signature validation failed"

**Sintoma:** 401 Unauthorized com mensagem de erro "Signature validation failed".

**Causas possiveis:**
- Chave secreta diferente entre servidor e token
- Token foi gerado com outra chave
- Algoritmo esperado diferente do algoritmo do token

**Solucao:**
```bash
# Verifique a chave secreta no servidor
# Compare com a chave usada para gerar o token
# JWT.io permite debugar com diferentes chaves
```

### 2. "Token expired"

**Sintoma:** 401 Unauthorized com "Token expired".

**Causa:** Access token expirou (15 minutos por padrao).

**Solucao:**
```bash
# Use o refresh token para obter um novo par
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"seu_refresh_token_aqui"}'
```

### 3. "Audience validation failed"

**Sintoma:** 401 Unauthorized com "Audience validation failed".

**Causa:** Claim `aud` do token nao corresponde ao `ValidAudience` configurado.

**Solucao:**
```csharp
// Verifique a configuracao
options.TokenValidationParameters.ValidAudience = "AuthLabs.Jwt.Api";

// Claims no token devem conter aud:"AuthLabs.Jwt.Api"
```

### 4. "Issuer validation failed"

**Sintoma:** 401 Unauthorized com "Issuer validation failed".

**Causa:** Claim `iss` do token nao corresponde ao `ValidIssuer` configurado.

**Solucao:**
```csharp
// Verifique a configuracao
options.TokenValidationParameters.ValidIssuer = "AuthLabs.Jwt";
```

### 5. Refresh token retorna 401

**Sintoma:** Refresh token e valido mas nao renova o par.

**Causas possiveis:**
- Refresh token foi revogado
- Refresh token foi usado mais de uma vez (sem rotation)
- Refresh token foi deletado do banco

**Solucao:**
```bash
# Verifique se o refresh token ainda existe no banco
# Facca login novamente se necessario
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}'
```

### 6. Token funciona no Postman mas nao na aplicacao

**Sintoma:** Token funciona em chamadas API manuais mas a aplicacao retorna 401.

**Causas possiveis:**
- Header format incorreto (`Bearer` comespaco antes do token)
- Token cortado ao copiar/colar
- Caracteres especiais nao escapados

**Solucao:**
```bash
# Verifique o header Authorization completo
curl -v -X GET http://localhost:5000/api/protected \
  -H "Authorization: Bearer seutokenaqui" 2>&1 | grep -i authorization
```

## Security Considerations

### 1. Armazenamento de Access Token

```javascript
// MEMORIA (recomendado para SPAs)
// Access token em memoria, NAO em localStorage
const accessToken = response.accessToken;
// Armazenar apenas em variavel JavaScript
// Nao usar: localStorage, sessionStorage, cookies

// Refresh token em httpOnly cookie
document.cookie = `refreshToken=${response.refreshToken}; HttpOnly; Secure`;
```

### 2. Chave Secreta

```csharp
// Em producao, usar variavel de ambiente
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrEmpty(secretKey))
    throw new InvalidOperationException("JWT_SECRET_KEY not configured");

// Chave minima de 256 bits (32 bytes) para HS256
// Usar: https://generate-secret.vercel.app/256
```

### 3. Politica de Senha

```csharp
// Validacao de senha forte
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 12;
options.Password.RequiredUniqueChars = 4;
```

### 4. Rate Limiting

```csharp
// Adicionar rate limiting ao endpoint de login
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
```

### 5. Refresh Token Rotation

```csharp
// Cada refresh deve invalidar o anterior
// Isso detecta quando tokens sao comprometidos
var oldRefreshToken = await _context.RefreshTokens
    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

if (oldRefreshToken == null || oldRefreshToken.IsRevoked)
{
    // Token reuse detectado - possivel ataque
    await RevokeAllUserTokens(userId);
    throw new SecurityException("Refresh token compromise detected");
}
```

### 6. Token Lifetime

| Token | Lifetime | Motivo |
|-------|----------|--------|
| Access Token | 15 minutos | Minimiza janela de ataque |
| Refresh Token | 7 dias | Balance User Experience / Security |
| Absolute Refresh | 30 dias | Forcar re-auth periodicamente |

## Estrutura do JWT - Claims Detalhados

### Claims Padrao (Registered)

| Claim | Nome | Descricao |
|-------|------|-----------|
| iss | Issuer | Quem emitiu o token |
| sub | Subject | Sobre quem o token e |
| aud | Audience | Para quem o token e |
| exp | Expiration Time | Quando expira |
| nbf | Not Before | Quando comeca a ser valido |
| iat | Issued At | Quando foi emitido |
| jti | JWT ID | Identificador unico |

### Claims Customizados

```csharp
// Adicionar claims customizados ao token
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new(JwtRegisteredClaimNames.Email, user.Email),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new(ClaimTypes.Role, user.Role),
    new("department", user.Department),  // Custom claim
    new("subscription_tier", user.Subscription)  // Custom claim
};
```

## Referencias

- [RFC 7519 - JSON Web Token (JWT)](https://tools.ietf.org/html/rfc7519)
- [RFC 7523 - JWT Profile for OAuth 2.0](https://tools.ietf.org/html/rfc7523)
- [Microsoft Docs - JWT Bearer Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/cookie)
- [JWT.io - Debugger](https://jwt.io/)
- [OWASP JWT Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
