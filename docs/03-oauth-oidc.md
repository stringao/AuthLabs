# OAuth 2.0 + OpenID Connect Authentication

## O que e

OAuth 2.0 e um framework de autorizacao que permite a um aplicativo obter acesso limitado a contas de usuarios em servicos HTTP, como Facebook, Google, GitHub, etc. OpenID Connect (OIDC) e uma camada de autenticacao sobre OAuth 2.0 que adiciona a capacidade de verificar a identidade do usuario e obter informações básicas de perfil.

OAuth 2.0 foca em **autorizacao** (o que voce pode fazer), enquanto OIDC adiciona **autenticacao** (quem voce e).

## OAuth 2.0 vs OpenID Connect

| Aspecto | OAuth 2.0 | OpenID Connect |
|---------|-----------|----------------|
| Proposito | Autorizacao | Autenticacao + Autorizacao |
| Token | Access Token (opaco) | Access Token + ID Token (JWT) |
| User Info | Via /userinfo endpoint | Via ID Token claims |
| Escopo | customizado | `openid` obrigatorio |
| Usado para | APIs de terceiros | Login social, SSO |

## Flows OAuth 2.0

### 1. Authorization Code (Recomendado para Web Apps)

Fluxo mais seguro para aplicacoes que rodam em servidor.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION CODE FLOW                                   │
└─────────────────────────────────────────────────────────────────────────────┘

    BROWSER                     APP SERVER                  IDENTITY PROVIDER
    ───────                     ──────────                  ─────────────────
       │                            │                                │
       │  1. Click "Login Google"  │                                │
       │──────────────────────────►│                                │
       │                            │                                │
       │  2. Redirect to Google    │                                │
       │     /authorize?           │                                │
       │     client_id=...          │                                │
       │     redirect_uri=...       │                                │
       │     response_type=code     │                                │
       │     scope=openid profile   │                                │
       │     state=random_csrf      │                                │
       │───────────────────────────►│──────────────────────────────►│
       │                            │                                │
       │                            │  3. Google Login Page          │
       │                            │◄───────────────────────────────│
       │                            │                                │
       │  4. User enters credentials                                │
       │     and approves permissions                                │
       │◄───────────────────────────────────────────────────────────│
       │                            │                                │
       │  5. Redirect to            │                                │
       │     /callback?code=xxx     │                                │
       │     &state=random_csrf     │                                │
       │───────────────────────────►│                                │
       │                            │                                │
       │                            │  6. Validate state (CSRF)       │
       │                            │                                │
       │                            │  7. Exchange code for tokens    │
       │                            │     POST /token                │
       │                            │     code=xxx                   │
       │                            │     client_secret=xxx          │
       │                            │───────────────────────────────►│
       │                            │                                │
       │                            │  8. {access_token,             │
       │                            │       id_token,                │
       │                            │       refresh_token}           │
       │                            │◄───────────────────────────────│
       │                            │                                │
       │                            │  9. Get user info               │
       │                            │     GET /userinfo              │
       │                            │───────────────────────────────►│
       │                            │                                │
       │                            │  10. User profile data         │
       │                            │◄───────────────────────────────│
       │                            │                                │
       │  11. Create local session  │                                │
       │     or return tokens      │                                │
       │◄──────────────────────────│                                │
```

### 2. Authorization Code + PKCE (Para SPAs e Mobile)

PKCE adiciona segurança extra prevenindo code interception attacks.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION CODE + PKCE FLOW                            │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT APP                   AUTHORIZATION SERVER
    ──────────                   ────────────────────
       │                                  │
       │  1. Generate random code_verifier
       │     (43-128 chars)               │
       │                                  │
       │  2. SHA256 hash code_verifier    │
       │                                  │
       │  3. Base64URL encode hash        │
       │     = code_challenge             │
       │                                  │
       │  4. Redirect with:               │
       │     code_challenge=xxx          │
       │     code_challenge_method=S256   │
       │────────────────────────────────►│
       │                                  │
       │  ... user authenticates ...     │
       │                                  │
       │  5. Receive authorization code   │
       │◄────────────────────────────────│
       │                                  │
       │  6. POST /token with:           │
       │     code=xxx                    │
       │     code_verifier=xxx           │
       │────────────────────────────────►│
       │                                  │
       │  7. Server hashes verifier       │
       │     compares with challenge     │
       │                                  │
       │  8. Return tokens if valid      │
       │◄────────────────────────────────│
```

### 3. Device Authorization (Para CLIs e TVs)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DEVICE AUTHORIZATION FLOW                                  │
└─────────────────────────────────────────────────────────────────────────────┘

    DEVICE APP                    AUTHORIZATION SERVER           USER
    ───────────                    ───────────────────           ─────
       │                                │                          │
       │  1. Request device code        │                          │
       │     POST /device/code          │                          │
       │───────────────────────────────►│                          │
       │                                │                          │
       │  2. { device_code: xxx,        │                          │
       │       user_code: YYY,          │                          │
       │       verification_url,        │                          │
       │       interval: 5 }            │                          │
       │◄───────────────────────────────│                          │
       │                                │                          │
       │  3. Display:                  │                          │
       │     "Go to https://...        │                          │
       │      and enter code YYY"      │                          │
       │                                │                          │
       │  4. User opens browser,       │                          │
       │     enters code YYY           │                          │
       │───────────────────────────────│─────────────────────────►│
       │                                │                          │
       │  5. User logs in, approves    │                          │
       │◄──────────────────────────────│◄──────────────────────────│
       │                                │                          │
       │  6. Poll /token              │                          │
       │     (every 5 seconds)         │                          │
       │──────────────────────────────►│                          │
       │                                │                          │
       │  7. Still waiting...          │                          │
       │◄───────────────────────────────│                          │
       │                                │                          │
       │  8. (User approved)            │                          │
       │     {access_token, ...}       │                          │
       │◄───────────────────────────────│                          │
```

## OpenID Connect - OIDC

OIDC adiciona um ID Token (JWT) ao fluxo OAuth 2.0, alem de um endpoint de userinfo.

### Claims do ID Token

```json
{
  "iss": "https://accounts.google.com",
  "azp": "client_id.apps.googleusercontent.com",
  "aud": "client_id.apps.googleusercontent.com",
  "sub": "107691986500000000000",
  "at_hash": "...",
  "iat": 1698000000,
  "exp": 1698003600,
  "email": "user@gmail.com",
  "email_verified": true,
  "name": "Usuario Exemplo",
  "picture": "https://lh3.googleusercontent.com/...",
  "given_name": "Usuario",
  "family_name": "Exemplo"
}
```

## Quando usar

- Login social (Google, Facebook, GitHub, Microsoft)
- SSO (Single Sign-On) entre aplicacoes
- Quando nao quer gerenciar senhas de usuarios
- Cenarios onde precisa de autorizacao para APIs de terceiros
- Aplicacoes mobile que usam OAuth nativo dos providers
- Federation com identity providers empresariais (Azure AD, Okta)
- Microsservicos que precisam validar identidade via token

## Quando NAO usar

- Sistema de login proprio com credenciais email/senha
- Quando precisa de controle total sobre dados de usuario
- Cenarios regulatorios que exigem dados localizaveis
- Quando o identity provider nao suporta OAuth 2.0/OIDC
- Aplicacoes que requerem autenticacao offline (sem interacao do usuario)
- Quando voce NAO quer que usuarios usem contas de terceiros

## Alertas e caveats importantes

1. **Client Secret exposto em SPA**: Em SPAs, o client_secret nao pode ser protegido. Usar Authorization Code com PKCE e essencial.

2. **Scopes sobre permissao**: Sempre solicitar apenas scopes necessarios. Usuarios podem negar scopes excessivos.

3. **State parameter obrigatorio**: Sempre usar e validar o parameter state para prevenir CSRF.

4. **Redirect URI validation**: Configurar exatamente os redirect URIs permitidos. URIs abertas podem permitir ataques.

5. **Token storage inseguro**: Access tokens em localStorage sao vulneraveis a XSS. Considerar httpOnly cookies.

6. **Refresh token rotation**: Implementar rotacao de refresh tokens para detectar comprometimento.

7. **Logout incompleto**: Logout no app nao invalida tokens no provider. Implementar logout federado.

8. **Consentimento usuario**: Providers podem cambiar permissoes. Re-autenticacao pode ser necessaria.

## Configuracao necessaria

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "seu-client-id.apps.googleusercontent.com",
      "ClientSecret": "sua-client-secret"
    },
    "GitHub": {
      "ClientId": "Iv1.xxxxxxxxxxxxxxxx",
      "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
    }
  }
}
```

**Configuracao OAuth (Program.cs):**
```csharp
services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = "seu-client-id";
        googleOptions.ClientSecret = "seu-client-secret";
        googleOptions.CallbackPath = "/signin-google";
        googleOptions.SaveTokens = true;  // Salvar tokens para uso posterior
        googleOptions.Scope.Add("openid");
        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");
    })
    .AddGitHub(gitHubOptions =>
    {
        gitHubOptions.ClientId = "seu-client-id";
        gitHubOptions.ClientSecret = "seu-client-secret";
        gitHubOptions.CallbackPath = "/signin-github";
        gitHubOptions.Scope.Add("read:user");
        gitHubOptions.Scope.Add("user:email");
    });
```

## Endpoints principais

| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| GET | /api/auth/external/{provider} | Nao | Redireciona para provider (Google, GitHub) |
| GET | /api/auth/callback/{provider} | Nao | Callback do provider apos autenticacao |
| GET | /api/auth/me | Sim | Retorna perfil do usuario (inclui provider info) |
| POST | /api/auth/logout | Sim | Logout local e revoga tokens |
| GET | /api/auth/register | Nao | Registro de novo usuario (opcional) |

## Fluxo de endpoints

### Login com Google
```
1. GET /api/auth/external/google
   → Valida CSRF state
   → Redireciona para Google OAuth consent screen

2. Usuario faz login no Google e aprova permissoes

3. GET /api/auth/callback/google?code=xxx&state=yyy
   → Valida state (CSRF protection)
   → Troca code por tokens
   → Cria/atualiza usuario local
   → Retorna tokens de sessao

4. GET /api/auth/me
   → Retorna dados do usuario autenticado
```

## Scopes comuns por provider

### Google
| Scope | Descricao |
|-------|-----------|
| openid | Autentificacao OIDC |
| profile | Nome, foto, perfil |
| email | Endereco de email |
| https://www.googleapis.com/auth/drive.readonly | Acesso ao Google Drive |
| https://www.googleapis.com/auth/calendar | Acesso ao Google Calendar |

### GitHub
| Scope | Descricao |
|-------|-----------|
| read:user | Perfil publico do usuario |
| user:email | Emails do usuario (inclui privados) |
| repo | Acesso completo a repositorios |
| admin:org | Gestao de organizacoes |

### Microsoft (Azure AD)
| Scope | Descricao |
|-------|-----------|
| openid | Autentificacao OIDC |
| profile | Perfil basico |
| email | Email |
| User.Read | Ler perfil do Graph API |
| Calendars.Read | Acesso ao calendario |

## Exemplo de uso

### Login com Google (redirecionamento)
```bash
# Navegador redireciona para:
GET https://accounts.google.com/o/oauth2/v2/auth?
  client_id=seu-client-id.apps.googleusercontent.com&
  redirect_uri=http://localhost:5000/api/auth/callback/google&
  response_type=code&
  scope=openid%20profile%20email&
  state=random-csrf-token
```

### Callback manual (apos usuario aprovar)
```bash
# Provider redireciona para:
GET http://localhost:5000/api/auth/callback/google?code=4/0Adeu5...&state=random-csrf-token
```

### Obter informacoes do usuario
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: .AspNetCore.Session=..."
```

**Resposta:**
```json
{
  "id": "google-123456789",
  "email": "usuario@gmail.com",
  "name": "Usuario Exemplo",
  "provider": "Google",
  "picture": "https://lh3.googleusercontent.com/...",
  "emailVerified": true
}
```

### Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Cookie: .AspNetCore.Session=..."
```

## Common Errors

### 1. "redirect_uri_mismatch"

**Sintoma:** Provider OAuth retorna erro `redirect_uri_mismatch`.

**Causa:** O `redirect_uri` configurado no provider nao corresponde ao que a aplicacao esta enviando.

**Solucao:**
```bash
# Verifique o redirect_uri na configuracao do provider Google:
# https://console.cloud.google.com/apis/credentials

# Deve ser exatamente:
# http://localhost:5000/signin-google
# ou
# https://seu-dominio.com/signin-google
```

### 2. "invalid_grant" ou "code_expired"

**Sintoma:** Erro ao trocar authorization code por tokens.

**Causas:**
- Code ja foi usado (one-time use)
- Code expirou (geralmente 60 segundos)
- Redirect URI na requisicao diferente do /authorize

**Solucao:**
```bash
# Facca login novamente
# Use o code imediatamente
# Verifique o redirect_uri
```

### 3. CSRF Attack - state validation failed

**Sintoma:** Erro "Invalid state" durante callback.

**Causa:** Parameter `state` nao foi validado ou foi manipulado.

**Solucao:**
```csharp
// Sempre gerar e validar state
// Na requisicao inicial:
var state = Guid.NewGuid().ToString();
// Armazenar em sessao ou cookie seguro
Session["OAuthState"] = state;

// No callback:
var returnedState = Request.Query["state"];
if (returnedState != Session["OAuthState"])
    return BadRequest("CSRF detected");
```

### 4. "access_denied" do provider

**Sintoma:** Usuario nega permissoes e provider redireciona com `error=access_denied`.

**Causa:** Usuario nao aprovou todas as permissoes solicitadas.

**Solucao:**
```csharp
// Tratar de forma amigavel
if (error == "access_denied")
{
    return Redirect("/?error=permission_denied");
}
```

### 5. Tokens expirados em SPA

**Sintoma:** Access token funciona inicialmente mas depois retorna 401.

**Causa:** Access token expirou e refresh token nao foi usado.

**Solucao:**
```javascript
// Implementar refresh automatico
async function getAccessToken() {
    const token = localStorage.getItem('access_token');
    if (isTokenExpired(token)) {
        return await refreshTokens();
    }
    return token;
}
```

## Security Considerations

### 1. PKCE para SPAs

```javascript
// Gerar code_verifier
const codeVerifier = generateRandomString(64);

// code_challenge = Base64URL(SHA256(code_verifier))
const codeChallenge = await sha256(codeVerifier)
    .then(hash => base64URLEncode(hash));

// Na requisicao /authorize
const authUrl = `https://provider.com/authorize?
    client_id=xxx
    &code_challenge=${codeChallenge}
    &code_challenge_method=S256`;
```

### 2. State Management

```csharp
// Sempre usar state anti-CSRF
public class OAuthStateService
{
    public string GenerateState()
    {
        var state = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(state);
        return Convert.ToBase64String(state);
    }
    
    public bool ValidateState(string state, string expected)
    {
        return state == expected;
    }
}
```

### 3. Redirect URI Validation

```csharp
// Validar TODOS os redirect URIs
public bool IsValidRedirectUri(string uri)
{
    var validUris = new[]
    {
        "https://app.example.com/callback",
        "https://staging.example.com/callback"
    };
    
    // Em producao, ser EXATO
    return validUris.Contains(uri);
}
```

### 4. Token Storage

| Storage | Seguranca | Recomendacao |
|---------|-----------|--------------|
| httpOnly Cookie | Alta | Refresh tokens |
| Memory (JS variable) | Alta | Access tokens (SPAs) |
| localStorage | Vulneravel a XSS | Evitar se possivel |
| sessionStorage | Vulneravel a XSS | Evitar se possivel |

### 5. Logout Federado

```csharp
// Revogar tokens no provider tambem
public async Task LogoutAsync()
{
    var accessToken = await GetStoredAccessToken();
    
    // Revogar no Google
    await new HttpClient().PostAsync(
        "https://oauth2.googleapis.io/revoke",
        new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", accessToken)
        }));
    
    // Limpar sessao local
    await HttpContext.SignOutAsync();
}
```

## Implementacao tipica do callback

```csharp
[HttpGet("callback/{provider}")]
public async Task<IActionResult> ExternalCallback(string provider, string code, string state)
{
    // 1. Validar state (CSRF protection)
    var expectedState = HttpContext.Session.GetString("OAuthState");
    if (string.IsNullOrEmpty(state) || state != expectedState)
        return BadRequest("Invalid state - possible CSRF attack");
    
    // 2. Trocar authorization code por tokens
    var tokens = await ExchangeCodeForTokensAsync(provider, code);
    
    // 3. Obter user info do provider
    var userInfo = await GetUserInfoAsync(provider, tokens.AccessToken);
    
    // 4. Criar ou atualizar usuario local
    var user = await _userService.UpsertExternalUserAsync(provider, userInfo);
    
    // 5. Criar sessao/cookie
    await HttpContext.SignInAsync(user);
    
    // 6. Redirecionar para pagina inicial
    return Redirect("/dashboard");
}

private async Task<OAuthTokens> ExchangeCodeForTokensAsync(string provider, string code)
{
    // Implementacao depende do provider
    // Google: POST https://oauth2.googleapis.com/token
    // GitHub: POST https://github.com/login/oauth/access_token
}
```

## Referências

- [RFC 6749 - OAuth 2.0 Authorization Framework](https://tools.ietf.org/html/rfc6749)
- [RFC 7519 - OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html)
- [OAuth 2.0 Security Best Current Practice](https://tools.ietf.org/html/draft-ietf-oauth-security-topics)
- [Microsoft Docs - OAuth 2.0 + OIDC](https://docs.microsoft.com/azure/active-directory/develop/v2-protocols-oidc)
- [Google Identity - OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [GitHub OAuth Apps](https://docs.github.com/developers/apps/authorizing-oauth-apps)
- [OAuth 2.0 PKCE RFC 7636](https://tools.ietf.org/html/rfc7636)
