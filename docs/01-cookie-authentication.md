# Cookie Authentication

## O que é

Cookie Authentication é um mecanismo de autenticação onde o servidor cria um cookie criptográfico após o login bem-sucedido. Esse cookie contém informações do usuário (geralmente de forma encriptada) e é enviado automaticamente pelo navegador a cada requisição subsequente. O servidor valida o cookie em cada pedido para identificar o usuário.

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
┌────────┐     ┌──────────────┐     ┌─────────┐     ┌──────────────┐
│ Client │────►│   Server     │────►│   DB    │◄────│   Identity   │
│Browser │◄────│  (ASP.NET)   │────►│  (PG)   │     │   (Users)    │
└────────┘     └──────────────┘     └─────────┘     └──────────────┘
     │               │
     │  1. Login    │
     │  (email+pass)│
     │─────────────►│
     │               │
     │  2. Validate  │
     │               │──────────►
     │               │◄──────────
     │               │
     │  3. Set-Cookie│
     │◄─────────────│
     │               │
     │  4. Requests  │
     │  (with cookie)│
     │─────────────►│
```

## Quando usar

- Aplicações web tradicionais (SSR - Server-Side Rendering)
- Cenários onde o usuário interage via navegador browser
- Quando a sessão precisa persistir entre requisições de forma transparente
- Aplicações que precisam de proteção contra CSRF com SameSite
- Sistemas onde o logout deveinvalidar imediatamente a sessão

## Quando NÃO usar

- APIs REST/JSON que servem clientes não-browser (mobile apps, SPAs sem proxy)
- Quando precisa de autenticação stateless (melhor com JWT)
- Cenários de microservices onde tokens compartilhados são necessários
- Quando a API é consumida por múltiplos clientes diferentes
- Aplicações que precisam de autenticação de terceiros (OAuth/OIDC)

## Alertas e caveats importantes

1. **Cookie sem flag Secure**: O cookie não configura `Secure = true`, permitindo transmissão em HTTP. Em produção, sempre configurar para requerer HTTPS.

2. **SameSite = Strict**: Protege contra CSRF, mas pode quebrar fluxos legítimos de cross-site (SSO,iframees embebidos). Considere `SameSite = Lax` para esses casos.

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
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;        // Renova expiração
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/api/auth/access-denied";
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

O cookie `AuthLabs.Cookie` será definido no response.

### Verificar usuário autenticado
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Cookie: AuthLabs.Cookie=<cookie_value>"
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
  -d '{"email":"guest@authlabs.com","password":"Guest123!"}'

# Tentar acessar endpoint admin (deve retornar 403)
curl -X GET http://localhost:5000/api/protected/admin \
  -H "Cookie: AuthLabs.Cookie=<guest_cookie>"
```

## Referências

- [Microsoft Docs - Cookie Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/cookie)
- [OWASP Cookie Security Guidelines](https://owasp.org/www-community/vulnerabilities/Cookie_based_sessions)
- [SameSite Cookie Explained](https://developer.mozilla.org/docs/Web/HTTP/Headers/Set-Cookie/SameSite)
