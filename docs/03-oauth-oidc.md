# OAuth 2.0 + OpenID Connect Authentication

## O que é

OAuth 2.0 é um framework de autorização que permite a um aplicativo obter acesso limitado a contas de usuários em serviços HTTP, como Facebook, Google, GitHub, etc. OpenID Connect (OIDC) é uma camada de autenticação sobre OAuth 2.0 que adiciona a capacidade de verificar a identidade do usuário e obter informações básicas de perfil.

## Como funciona

### Fluxo Authorization Code (Recomendado para Web Apps)

1. **Redirecionamento**: Usuário clica em "Login com Google/Facebook/etc"
2. **Authorization Request**: App redireciona para o provider com:
   - `client_id`: Identificador do app
   - `redirect_uri`: URL de callback
   - `scope`: Permissões solicitadas (openid, profile, email)
   - `response_type`: "code"
   - `state`: Token CSRF para segurança
3. **Login no Provider**: Usuário faz login e consente as permissões
4. **Authorization Code**: Provider redireciona de volta com code
5. **Troca de Code por Tokens**: Backend troca code por:
   - Access Token (para API)
   - ID Token (JWT com identidade do usuário)
   - Refresh Token (para renovação)
6. **Criação de Sessão**: Backend cria sessão local ou retorna tokens ao cliente

### Fluxo Device Authorization (Para CLIs e TVs)

1. **Device Code Request**: App solicita código de dispositivo
2. **Exibição de URL**: Usuário abre URL e digita código
3. **Polling**: App faz polling até usuário aprovar
4. **Tokens**: Provider retorna tokens

## Diagrama de fluxo
```
┌────────┐     ┌──────────────┐     ┌──────────────────┐
│ Client │────►│   Server     │────►│  Identity        │
│(Browser)│◄────│  (Backend)  │◄────│  Provider        │
└────────┘     └──────────────┘     │ (Google, GitHub) │
     │               │               └──────────────────┘
     │  1. Login     │
     │  Redirect     │
     │─────────────► │
     │               │
     │  2. Auth      │
     │  Request      │
     │──────────────►│
     │               │
     │  3. User      │
     │  Login+Consent│
     │◄──────────────│
     │               │
     │  4. Auth Code │
     │  (redirect)   │
     │◄───────────── │
     │               │
     │  5. Code      │
     │  to Backend   │
     │─────────────► │      6. Exchange
     │               │──────────────►│
     │               │◄──────────────│
     │               │
     │  7. Tokens    │
     │◄───────────── │
```

## Quando usar

- Login social (Google, Facebook, GitHub, Microsoft)
- SSO (Single Sign-On) entre aplicações
- Quando não quer gerenciar senhas de usuários
- Cenários onde precisa de autorização para APIs de terceiros
- Aplicações mobile que usam OAuth nativo dos providers
- Federation com identity providers empresariais (Azure AD, Okta)

## Quando NÃO usar

- Sistema de login próprio com credenciais email/senha
- Quando precisa de controle total sobre dados de usuário
- Cenários regulatórios que exigem dados localizáveis
- Quando o identity provider não suporta OAuth 2.0/OIDC
- Aplicações que requerem autenticação offline (sem interação do usuário)

## Alertas e caveats importantes

1. **Client Secret exposto em SPA**: Em SPAs, o client_secret não pode ser protegido. Usar Authorization Code com PKCE é essencial.

2. **Scopes sobre permissão**: Sempre solicitar apenas scopes necessários. Usuários podem negar scopes excessivos.

3. **State parameter obrigatório**: Sempre usar e validar o parameter state para prevenir CSRF.

4. **Redirect URI validation**: Configurar exatamente os redirect URIs permitidos. CURIs abertas podem permitir ataques.

5. **Token storage inseguro**: Access tokens em localStorage são vulneráveis a XSS. Considerar httpOnly cookies.

6. **Refresh token rotation**: Implementar rotação de refresh tokens para detectar comprometimento.

7. **Logout incompleto**: Logout no app não invalida tokens no provider. Implementar logout federado.

8. **Consentimento usuário**: Providers podem cambiar permissões. Re-autenticação pode ser necessária.

## Configuração necessária

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

**Configuração OAuth (Program.cs):**
```csharp
services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = "seu-client-id";
        googleOptions.ClientSecret = "seu-client-secret";
        googleOptions.CallbackPath = "/signin-google";
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

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | /api/auth/external/{provider} | Não | Redireciona para provider (Google, GitHub) |
| GET | /api/auth/callback/{provider} | Não | Callback do provider após autenticação |
| GET | /api/auth/me | Sim | Retorna perfil do usuário (inclui provider info) |
| POST | /api/auth/logout | Sim | Logout local e revoga tokens |
| GET | /api/auth/register | Não | Registro de novo usuário (opcional) |

## Fluxo de endpoints

### Login com Google
```
1. GET /api/auth/external/google
   → Redireciona para Google OAuth consent screen

2. Usuário faz login no Google e aprova permissões

3. GET /api/auth/callback/google?code=xxx&state=yyy
   → Backend troca code por tokens
   → Cria/atualiza usuário local
   → Retorna tokens de sessão

4. GET /api/auth/me
   → Retorna dados do usuário autenticado
```

## Usuários de demonstração

| Provider | Email | Scopes |
|----------|-------|--------|
| Google | Qualquer conta Google | openid, profile, email |
| GitHub | Qualquer conta GitHub | read:user, user:email |

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

### Callback manual (após usuário aprovar)
```bash
# Provider redireciona para:
GET http://localhost:5000/api/auth/callback/google?code=4/0Adeu5...&state=random-csrf-token
```

### Obter informações do usuário
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

## Scopes comuns por provider

### Google
| Scope | Descrição |
|-------|-----------|
| openid | Autentificação OIDC |
| profile | Nome, foto, perfil |
| email | Endereço de email |
| https://www.googleapis.com/auth/drive.readonly | Acesso ao Google Drive |

### GitHub
| Scope | Descrição |
|-------|-----------|
| read:user | Perfil público do usuário |
| user:email | Emails do usuário |
| repo | Acesso completo a repositórios |

### Microsoft (Azure AD)
| Scope | Descrição |
|-------|-----------|
| openid | Autentificação OIDC |
| profile | Perfil básico |
| email | Email |
| User.Read | Ler perfil do Graph API |

## Implementação típica do callback

```csharp
[HttpGet("callback/{provider}")]
public async Task<IActionResult> ExternalCallback(string provider, string code, string state)
{
    // 1. Validar state (CSRF protection)
    if (!ValidateState(state))
        return BadRequest("Invalid state");

    // 2. Trocar authorization code por tokens
    var tokens = await ExchangeCodeForTokens(provider, code);

    // 3. Obter user info do provider
    var userInfo = await GetUserInfo(provider, tokens.AccessToken);

    // 4. Criar ou atualizar usuário local
    var user = await UpsertUser(provider, userInfo);

    // 5. Criar sessão/cookie
    await SignInAsync(user);

    // 6. Redirecionar para página inicial
    return Redirect("/");
}
```

## Referências

- [RFC 6749 - OAuth 2.0 Authorization Framework](https://tools.ietf.org/html/rfc6749)
- [RFC 7519 - OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html)
- [OAuth 2.0 Security Best Current Practice](https://tools.ietf.org/html/draft-ietf-oauth-security-topics)
- [Microsoft Docs - OAuth 2.0 + OIDC](https://docs.microsoft.com/azure/active-directory/develop/v2-protocols-oidc)
- [Google Identity - OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [GitHub OAuth Apps](https://docs.github.com/developers/apps/authorizing-oauth-apps)
