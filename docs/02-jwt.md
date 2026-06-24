# JWT Authentication

## O que é

JWT (JSON Web Token) é um padrão RFC 7519 para representar Claims (afirmações) de forma compacta e autossuficiente. Na autenticação, o JWT funciona como um "token de acesso" assinado cryptographicamente que contém informações do usuário e é validado em cada requisição sem necessidade de consultar o banco de dados.

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
┌────────┐     ┌──────────────┐     ┌─────────┐
│ Client │────►│   Server     │────►│   DB    │
└────────┘     └──────────────┘     └─────────┘
     │               │                  │
     │  1. Login    │                  │
     │ (email+pass) │                  │
     │────────────►│                  │
     │             │  2. Validate User │
     │             │────────────────►│
     │             │◄────────────────│
     │             │                  │
     │  3. Generate │                 │
     │  Access+Refresh               │
     │◄────────────│                 │
     │             │  4. Store       │
     │             │  Refresh Token  │
     │             │────────────────►│
     │             │                  │
     │  5. Access   │                 │
     │  Token       │                  │
     │────────────►│                  │
     │             │  6. Validate JWT │
     │             │  (no DB call)    │
     │◄────────────│                  │
     │             │                  │
     │  7. Token Expired             │
     │  (401 error) │                │
     │             │                  │
     │  8. Refresh │                 │
     │  POST /refresh               │
     │────────────►│                │
     │             │  9. Revoke Old  │
     │             │  Store New     │
     │             │──────────────►│
     │◄────────────│                │
```

## Quando usar

- APIs REST stateless que servem múltiplos clientes (web, mobile, IoT)
-Microserviços que precisam de autenticação distribuída
- Single Page Applications (SPAs) que se autenticam diretamente com APIs
- Quando precisa de escalabilidade horizontal (múltiplos servidores)
- Cenários onde o token precisa ser validado sem session store

## Quando NÃO usar

- Sessões de longa duração (melhor com refresh tokens + server-side state)
- Quando precisa invalidar tokens imediatamente (logout instantâneo)
- Dados extremamente sensíveis que requerem validação constante de permissões
- Cenários onde o payload do token pode crescer muito
- Aplicações que exigem "forgot password" com invalidação imediata de todos os tokens

## Alertas e caveats importantes

1. **Chave secreta hardcoded**: A chave JWT `SecretKey` está hardcoded em `appsettings.json`. Em produção, usar Environment Variables ou Azure Key Vault.

2. **Política de senha fraca**: Mínimo de 6 caracteres é muito curto. Recomenda-se 8-12+ com caracteres especiais.

3. **Access Token não é revogável**: Tokens de acesso não são armazenados em blacklist - expiram naturalmente. Janela de 15 minutos mitiga isso.

4. **Refresh Token em texto plano**: O `RefreshToken` é armazenado sem hash no banco. Em produção, should be hashed like passwords.

5. **Sem rate limiting**: Endpoint de login vulnerável a ataques de força bruta.

6. **Sem HTTPS enforcement**: Sem requisito explícito para HTTPS na configuração.

7. **Credenciais no banco em texto plano**: Connection string com credenciais no config - usar secrets manager.

8. **Clock skew**: Configurado com `ClockSkew = TimeSpan.Zero` pode causar problemas com múltiplos servidores em fusos diferentes.

## Configuração necessária

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

**Configuração JWT (Program.cs):**
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
            ClockSkew = TimeSpan.Zero
        };
    });
```

## Endpoints principais

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| POST | /api/auth/login | Não | Login retorna access + refresh token |
| POST | /api/auth/refresh | Não | Refresh: troca refresh token por novo par |
| POST | /api/auth/logout | Sim | Logout: revoga refresh token |
| GET | /api/protected | Sim | Endpoint protegido (qualquer usuário) |
| GET | /api/protected/admin | Sim (Admin) | Endpoint protegido (apenas Admin) |

## Usuários de demonstração

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

### Testar token expirado (após 15 minutos)
```bash
# Tentar acessar com token expirado
curl -X GET http://localhost:5000/api/protected \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
# Retorna: 401 Unauthorized
```

## Estrutura do JWT

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims):**
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

**Decodificar JWT:**
```bash
# Instalar jq e decodificar
echo "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkBhdXRo" | base64 -d | jq .
```

## Referências

- [RFC 7519 - JSON Web Token (JWT)](https://tools.ietf.org/html/rfc7519)
- [RFC 7523 - JWT Profile for OAuth 2.0](https://tools.ietf.org/html/rfc7523)
- [Microsoft Docs - JWT Bearer Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/cookie)
- [JWT.io - Debugger](https://jwt.io/)
