# Windows Authentication

## O que é

Windows Authentication (também conhecida como NTLM ou Kerberos) é um mecanismo de autenticação onde as credenciais do usuário são gerenciadas pelo sistema operacional Windows. O servidor usa o Active Directory ou a conta local do Windows para validar usuários, sem necessidade de digitar senha novamente.

## Como funciona

### NTLM (NT LAN Manager)

1. **Negociação**: Cliente envia请求 com header `Authorization: Negotiate`
2. **Challenge**: Servidor responde com challenge (nonce)
3. **Response**: Cliente criptografa o challenge com hash da senha
4. **Validação**: Servidor valida contra AD/conta local
5. **Token**: Sessão estabelecida

### Kerberos (Preferred)

1. **Ticket Granting Ticket (TGT)**: Usuário já tem TGT do KDC (Domain Controller)
2. **Ticket Request**: Cliente solicita ticket para o serviço
3. **Ticket Granting**: KDC valida e emite ticket de serviço
4. **Authenticate**: Cliente usa ticket para autenticar no serviço

### IIS/ASP.NET Core Integration

No ASP.NET Core com IIS ou HTTP.sys:
- `AddAuthentication(Negotiate)` permite autenticação Windows
- O servidor extrai identidade do token Kerberos/NT
- `HttpContext.User` contém `WindowsIdentity`

## Diagrama de fluxo
```
┌─────────┐     ┌──────────────┐     ┌─────────────────┐
│ Client  │────►│   Server     │────►│  Active         │
│(Windows)│◄────│ (IIS/HTTP.sys)│◄────│  Directory/KDC  │
└─────────┘     └──────────────┘     └─────────────────┘
     │                 │                      │
     │  1. Request     │                      │
     │  ( Negotiate)   │                      │
     │────────────────►│                      │
     │                 │                      │
     │  2. 401 +       │                      │
     │  Challenge      │                      │
     │◄────────────────│                      │
     │                 │                      │
     │  3. Credentials │                      │
     │  (encrypted)   │                      │
     │────────────────►│                      │
     │                 │  4. Validate         │
     │                 │────────────────────►│
     │                 │◄────────────────────│
     │                 │                      │
     │  4. 200 OK      │                      │
     │  (authenticated)│                      │
     │◄────────────────│                      │
```

## Quando usar

- Aplicações intranet corporativas
- Quando todos os usuários têm contas no Active Directory
- Integração com sistemas legados Windows (SharePoint, Exchange)
- Cenários onde SSO é mandatório (usuário já logado na máquina)
- Aplicações em ambiente Windows Server com IIS

## Quando NÃO usar

- Aplicações web públicas (internet)
- Quando usuários não têm contas Windows/AD
- Cross-platform requirements (Linux, macOS)
- Quando precisa de identidade de terceiros (OAuth/OIDC)
- APIs que servem clientes não-Windows (mobile, web)
- Ambientes onde Kerberos/NTLM não são permitidos (muitos providers cloud)

## Alertas e caveats importantes

1. **Requer Windows**: Funciona apenas com IIS ou HTTP.sys no Windows. Kestrel não suporta nativamente.

2. **Kerberos delegation**: Configuração complexa para delegação Kerberos entre serviços.

3. **Cross-domain**: Autenticação entre domínios信任 requer configuração de domain trusts.

4. **Não funciona em HTTP**: Negotiate/Kerberos requer HTTPS (exceto localhost em alguns casos).

5. **Browser support**: Suporte varia:
   - IE/Edge: Suporte completo
   - Chrome: Requer configuração de intranet
   - Firefox: Requer configuração `network.automatic-ntlm-auth.trusted-uris`

6. **Password changes**: Mudanças de senha podem demorar para propagar no AD.

7. **Service accounts**: Serviços precisam de SPNs (Service Principal Names) registrados corretamente.

8. **NTLM vs Kerberos**: NTLM é menos seguro e mais fácil; Kerberos é mais seguro mas requer configuração.

## Configuração necessária

### IIS (web.config)
```xml
<system.webServer>
  <security>
    <authentication>
      <windowsAuthentication enabled="true" />
      <anonymousAuthentication enabled="false" />
    </authentication>
  </security>
</system.webServer>
```

### ASP.NET Core (Program.cs)
```csharp
// Somente funciona com IIS ou HTTP.sys
services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

services.AddAuthorization(options =>
{
    options.AddPolicy("RequireWindowsAuth", policy =>
        policy.RequireAuthenticatedUser());
});
```

### Kestrel (não suporta nativamente)
```bash
# Kestrel não suporta Windows Auth
# Use IIS ou HTTP.sys como reverse proxy
```

## Endpoints principais

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | /api/auth/me | Sim | Retorna identidade Windows do usuário |
| GET | /api/protected | Sim (WinAuth) | Endpoint protegido |
| GET | /api/protected/admin | Sim (Admin) | Endpoint restrito a admins |

## Usuários de demonstração

**ATENÇÃO**: Este projeto ainda não foi implementado. A documentação abaixo é um template do que seria implementado.

| Usuário Windows | Domínio | Descrição |
|-----------------|---------|-----------|
| DOMAIN\User1 | AD Domain | Usuário comum |
| DOMAIN\Admin | AD Domain | Administrador |
| DOMAIN\Service | AD Domain | Conta de serviço |

**Para desenvolvimento local:**
| Usuário | Descrição |
|---------|-----------|
| HOSTNAME\Developer | Conta de desenvolvimento local |
| .\LocalUser | Usuário local |

## Exemplo de uso

### Login (automático)
```bash
# Usuário acessa endpoint (browser envia credenciais automaticamente)
curl -X GET http://server/api/protected \
  --negotiate -u :  # curl com NTLM auth no Linux
```

**Resposta:**
```json
{
  "identity": {
    "name": "DOMAIN\\username",
    "authenticated": true,
    "authenticationType": "Negotiate"
  },
  "claims": {
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "DOMAIN\\username",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid": "S-1-5-21-..."
  }
}
```

### Verificar papel no AD
```csharp
// Verificar se usuário é membro de grupo
var claims = User.Claims.ToList();
var groups = claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
                   .Select(c => new SecurityIdentifier(c.Value).Translate(typeof(NTAccount)))
                   .ToList();
```

### Authorization policy
```csharp
// Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("DomainAdmins", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("DOMAIN\\Domain Admins")));
});
```

## Configuração de Service Principal Name (SPN)

```bash
# Registrar SPN para HTTP service
setspn -S HTTP/server.domain.com DOMAIN\ServiceAccount

# Verificar SPNs registrados
setspn -L DOMAIN\ServiceAccount
```

## Configuração de Delegation no AD

```
Active Directory Users and Computers →
  {Service Account} →
    Properties →
      Delegation →
        Trust this user for delegation to specified services only →
          HTTP/server.domain.com
```

## Diferenças NTLM vs Kerberos

| Aspecto | NTLM | Kerberos |
|---------|------|----------|
| Protocolo | Challenge-response | Ticket-based |
| Segurança | Média | Alta |
| Performance | Mais calls | Menos calls |
| Cross-domain | Limitado | Total com trusts |
| Configuração | Simples | Complexa |
| Smart card | Não suporta nativamente | Suporta |

## Referências

- [Microsoft Docs - Windows Authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication/windowsauth)
- [IIS - Windows Authentication](https://docs.microsoft.com/iis/configuration/system.webServer/security/authentication/windowsAuthentication)
- [Kerberos in Windows](https://docs.microsoft.com/windows-server/security/kerberos/kerberos-authentication-overview)
- [SPN (Service Principal Name)](https://docs.microsoft.com/windows-server/identity/ad-ds/manage/service-principal-names)
- [NTLM Authentication](https://docs.microsoft.com/windows-server/security/ntlm/ntlm-technical-reference)
