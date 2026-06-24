# Cookie Authentication

## O que é

Cookie Authentication é um mecanismo de autenticação onde o servidor cria um cookie criptográfico após o login bem-sucedido. Esse cookie contém informações do usuário (geralmente de forma encriptada) e é enviado automaticamente pelo navegador a cada requisição subsequente. O servidor valida o cookie em cada pedido para identificar o usuário.

Este é um dos métodos de autenticação mais tradicionais e amplamente utilizados em aplicações web servidor (SSR - Server-Side Rendering). O cookie é armazenado pelo navegador e enviado automaticamente em todas as requisições para o mesmo domínio, permitindo uma experiência de sessão persistente sem que o usuário precise se autenticar em cada requisição.

## Conceitos Fundamentais

### O que é um Cookie de Autenticação?

Um cookie de autenticação é um pequeno arquivo de texto armazenado pelo navegador que contém informações criptografadas sobre a sessão do usuário. Diferente de tokens JWT que são armazenados no cliente, o cookie de sessão é mantido exclusivamente no servidor.

**Componentes principais:**
- **Sessão ID**: Identificador único da sessão armazenado no servidor
- **Dados do usuário**: Claims, roles e outras informações criptografadas
- **Expiration**: Data/hora de expiração da sessão
- **Security flags**: HttpOnly, Secure, SameSite

### Como o Cookie é Criado

Quando um usuário faz login:
1. Servidor valida as credenciais
2. Servidor cria uma sessão no servidor (ou dados criptografados)
3. Servidor envia o cookie no header `Set-Cookie`
4. Navegador armazena o cookie
5. Navegador envia o cookie em todas as requisições subsequentes

## Como funciona

1. **Login**: Usuário envia credenciais (email/senha) para `/api/auth/login`
2. **Validação**: Servidor valida credenciais contra o banco de dados usando `SignInManager.PasswordSignInAsync`
3. **Criação do Cookie**: Em caso de sucesso, o middleware de autenticação cria um cookie encriptado contendo:
   - Identidade do usuário (userId, email)
   - Roles/Claims
   - Data de expiração
4. **Requisições Subsequentes**: Navegador envia automaticamente o cookie em todas as requisições
5. **Validação**: Middleware intercepta requisição, descriptografa e valida o cookie
6. **Logout**: `POST /api/auth/logout` destrói o cookie de autenticação

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           COOKIE AUTHENTICATION FLOW                         │
└─────────────────────────────────────────────────────────────────────────────┘

    BROWSER                      SERVER                           DATABASE
    ───────                      ──────                           ───────

       │                            │                                 │
       │  1. POST /login           │                                 │
       │     email + password      │                                 │
       │──────────────────────────►│                                 │
       │                            │                                 │
       │                            │  2. Validate credentials        │
       │                            │────────────────────────────────►│
       │                            │                                 │
       │                            │  3. Check password hash         │
       │                            │◄────────────────────────────────│
       │                            │                                 │
       │                            │  4. Create encrypted session    │
       │                            │     containing user claims       │
       │                            │                                 │
       │  5. Set-Cookie:            │                                 │
       │     AuthLabs.Cookie=...    │                                 │
       │◄──────────────────────────│                                 │
       │                            │                                 │
       │  6. GET /protected         │                                 │
       │     Cookie: AuthLabs.Cookie│                                 │
       │──────────────────────────►│                                 │
       │                            │                                 │
       │                            │  7. Decrypt & validate cookie   │
       │                            │     Extract user identity       │
       │                            │                                 │
       │  8. 200 OK                │                                 │
       │     Response data         │                                 │
       │◄──────────────────────────│                                 │
       │                            │                                 │
       │  9. POST /logout          │                                 │
       │     Cookie: AuthLabs.Cookie│                                 │
       │──────────────────────────►│                                 │
       │                            │                                 │
       │                            │  10. Destroy session           │
       │                            │     Mark cookie as expired      │
       │                            │                                 │
       │  11. Set-Cookie:           │                                 │
       │     AuthLabs.Cookie=;      │                                 │
       │     Max-Age=0             │                                 │
       │◄──────────────────────────│                                 │


┌─────────────────────────────────────────────────────────────────────────────┐
│                        COOKIE SECURITY FLAGS                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  HttpOnly=true   → Previne XSS (JavaScript não pode ler)                    │
│  Secure=true     → Cookie só é enviado via HTTPS                            │
│  SameSite=Strict → Cookie não é enviado em requests cross-site               │
│  SameSite=Lax    → Cookie enviado em requests top-level navigation          │
│  SameSite=None   → Cookie enviado em todos os requests (requer Secure)       │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- Aplicações web tradicionais (SSR - Server-Side Rendering)
- Cenários onde o usuário interage via navegador browser
- Quando a sessão precisa persistir entre requisições de forma transparente
- Aplicações que precisam de proteção contra CSRF com SameSite
- Sistemas onde o logout deve invalidar imediatamente a sessão
- Sites que requerem sessões de longa duração com renewals automáticos
- Aplicações que usam ViewState ou outros mecanismos server-side

## Quando NÃO usar

- APIs REST/JSON que servem clientes não-browser (mobile apps, SPAs sem proxy)
- Quando precisa de autenticação stateless (melhor com JWT)
- Cenários de microservices onde tokens compartilhados são necessários
- Quando a API é consumida por múltiplos clientes diferentes
- Aplicações que precisam de autenticação de terceiros (OAuth/OIDC)
- SPAs que precisam de autenticação direta com APIs (use JWT ou OAuth)

## Alertas e caveats importantes

1. **Cookie sem flag Secure**: O cookie não configura `Secure = true`, permitindo transmissão em HTTP. Em produção, sempre configurar para requerer HTTPS.

2. **SameSite = Strict**: Protege contra CSRF, mas pode quebrar fluxos legítimos de cross-site (SSO, iframes embebidos). Considere `SameSite = Lax` para esses casos.

3. **isPersistent = true**: O cookie de login é configurado como persistente, podendo ser armazenado no navegador por mais tempo que o desejado.

4. **Sem validação customizada do Principal**: O `CookieAuthenticationEvents.ValidatePrincipal` apenas chama a implementação base, sem verificar usuários revogados ou roles alteradas.

5. **Sem lockout de conta**: `lockoutOnFailure: false` permite ataques de força bruta sem bloqueio.

6. **Sem HTTPS enforcement**: Não há middleware forçando HTTPS em produção.

7. **Credenciais demo em código**: Usuários de demonstração estão hardcoded no código - produção deve usar configuração externa.

8. **Domínio/Path padrão**: Cookie usa domínio e path padrão, pode não ser ideal para todos os cenários de deploy.

## Configuração necessária

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authlabs;Username=postgres;Password=postgres123"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Configuração do Cookie (Program.cs):**
```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthLabs.Cookie";
        options.Cookie.HttpOnly = true;          // Impede XSS
        options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;        // Renova expiração
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/api/auth/access-denied";
        
        // Events para logging e validação customizada
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });
```

## Endpoints principais

| Método | Path | Descrição |
|--------|------|-----------|
| POST | /api/auth/login | Login com email/password |
| POST | /api/auth/logout | Logout e destrói cookie |
| GET | /api/auth/me | Retorna informações do usuário atual |
| GET | /api/auth/access-denied | Handler de acesso negado |
| GET | /api/protected | Endpoint protegido (qualquer usuário autenticado) |
| GET | /api/protected/admin | Endpoint protegido (apenas Admin) |
| GET | /api/protected/manager | Endpoint protegido (apenas Manager) |
| GET | /api/protected/authenticated | Qualquer usuário autenticado |

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

**Resposta esperada:**
```json
{
  "message": "Login successful",
  "email": "admin@authlabs.com"
}
```

O cookie `AuthLabs.Cookie` será definido no response headers:
```
Set-Cookie: AuthLabs.Cookie=73DFF81...; path=/; HttpOnly; SameSite=Strict
```

### Verificar usuário autenticado
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: AuthLabs.Cookie=<cookie_value>"
```

**Resposta:**
```json
{
  "email": "admin@authlabs.com",
  "isAuthenticated": true
}
```

### Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Cookie: AuthLabs.Cookie=<cookie_value>"
```

### Acessar endpoint protegido (Admin)
```bash
curl -X GET http://localhost:5000/api/protected/admin \
  -H "Cookie: AuthLabs.Cookie=<cookie_value>"
```

### Testar acesso negado (usuário guest em endpoint admin)
```bash
# Login como guest
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"guest@authlabs.com","password":"Guest123!"}' \
  -c guest_cookies.txt

# Tentar acessar endpoint admin (deve retornar 403)
curl -X GET http://localhost:5000/api/protected/admin \
  -b guest_cookies.txt
# Retorna: 403 Forbidden
```

### Verificar todos os headers de resposta
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}' \
  -v
```

Procure no output:
```
< HTTP/1.1 200 OK
< Set-Cookie: AuthLabs.Cookie=...; path=/; HttpOnly; SameSite=Strict
```

## Common Errors

### 1. Cookie não é enviado pelo navegador

**Sintoma:** Após login, o cookie não aparece no navegador nem nas requisições subsequentes.

**Causas possíveis:**
- Cookie não tem flag `HttpOnly` configurado (XSS pode estar bloqueando)
- Domínio do cookie não corresponde ao domínio da requisição
- Cookie expirou antes da próxima requisição

**Solução:**
```bash
# Verifique os headers do Set-Cookie
curl -v http://localhost:5000/api/auth/login 2>&1 | grep -i set-cookie
```

### 2. 401 Unauthorized em todas as requisições

**Sintoma:** Mesmo após login, todas as requisições retornam 401.

**Causas possíveis:**
- Cookie não está sendo enviado (verifique `Cookie` header)
- Sessão expirou (tempo muito curto em `ExpireTimeSpan`)
- Validação do principal falhou (usuário foi deletado/revogado)

**Solução:**
```bash
# Verifique se o cookie está na requisição
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: AuthLabs.Cookie=seu_cookie_aqui" \
  -v
```

### 3. 403 Forbidden em endpoint

**Sintoma:** Login funciona, mas ao acessar endpoint protegido retorna 403.

**Causas possíveis:**
- Usuário não tem a role necessária para o endpoint
- Policy de autorização não está configurada corretamente
- Claims não contém as roles esperadas

**Solução:**
```bash
# Verifique os dados do usuário
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: AuthLabs.Cookie=<cookie>"

# Tente com um usuário Admin
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authlabs.com","password":"Admin123!"}' \
  -c admin_cookies.txt

curl -X GET http://localhost:5000/api/protected/admin \
  -b admin_cookies.txt
```

### 4. Cookie funciona no Postman mas não no navegador

**Sintoma:** Requisições com cookie funcionam via Postman/curl mas não no browser.

**Causas possíveis:**
- SameSite=Strict bloqueia requests de outros domínios
- Secure flag requer HTTPS (não funciona em HTTP)
- Domínio do cookie está configurado incorretamente

**Solução:**
```bash
# Teste com SameSite=Lax
# Altere a configuração:
options.Cookie.SameSite = SameSiteMode.Lax;
```

### 5. Sessão não persiste entre reinicializações do servidor

**Sintoma:** Após restart do servidor, todos os usuários precisam fazer login novamente.

**Causa:** Data Protection keys não estão sendo persistidas.

**Solução:** Configurar persistência de Data Protection:
```csharp
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/data-protection-keys"));
```

## Security Considerations

### 1. Cookie Security Flags (OBRIGATÓRIO em produção)

```csharp
options.Cookie.HttpOnly = true;        // Impede acesso via JavaScript (XSS)
options.Cookie.Secure = CookieSecurePolicy.Always;  // HTTPS apenas
options.Cookie.SameSite = SameSiteMode.Strict;     // CSRF protection
options.Cookie.SameSite = SameSiteMode.Lax;        // Se precisar de cross-site
```

### 2. Session Expiration

```csharp
// Expiração fixa (não renova com atividade)
options.ExpireTimeSpan = TimeSpan.FromMinutes(20);

// Expiração deslizante (renova a cada request)
options.SlidingExpiration = true;
options.ExpireTimeSpan = TimeSpan.FromHours(1);

// Para sessões persistentes (remember me)
options.Cookie.Expiration = TimeSpan.FromDays(14);
```

### 3. Protecao CSRF

SameSite cookie attribute protege contra CSRF:

| Modo | Descricao | Use Case |
|------|-----------|----------|
| Strict | Nenhum cookie cross-site | Max security, quebra navegação |
| Lax | Apenas GET top-level navigation | Balance segurança/usabilidade |
| None | Todos os requests (requer Secure) | Quando precisa de iframe embed |

### 4. Account Lockout

```csharp
// Configurar lockout para prevenir brute force
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

### 5. Secure Data Protection

```csharp
// Em produção, usar Data Protection keys persistentes
services.AddDataProtection()
    .SetApplicationName("AuthLabs")
    .PersistKeysToAzureBlobStorage(/* ... */);
```

## Referencias

- [Microsoft Docs - Cookie Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/cookie)
- [OWASP Cookie Security Guidelines](https://owasp.org/www-community/vulnerabilities/Cookie_based_sessions)
- [SameSite Cookie Explained](https://developer.mozilla.org/docs/Web/HTTP/Headers/Set-Cookie/SameSite)
- [OWASP CSRF Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
