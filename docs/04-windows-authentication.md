# Windows Authentication

## O que e

Windows Authentication (tambem conhecida como NTLM ou Kerberos) e um mecanismo de autenticacao onde as credenciais do usuario sao gerenciadas pelo sistema operacional Windows. O servidor usa o Active Directory ou a conta local do Windows para validar usuarios, sem necessidade de digitar senha novamente.

Este metodo e particularmente util em ambientes corporativos onde todos os usuarios ja estao autenticados no dominio Windows (Active Directory), permitindo Single Sign-On (SSO) transparente.

## Protocolos de Autenticacao

### NTLM (NT LAN Manager)

NTLM e um protocolo challenge-response mais antigo, ainda suportado por compatibilidade.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              NTLM AUTHENTICATION                              │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                          SERVER                        DC/AD
    ──────                          ──────                        ─────
       │                               │                            │
       │  1. GET /protected           │                            │
       │     (no auth header)          │                            │
       │─────────────────────────────►│                            │
       │                               │                            │
       │  2. 401 Unauthorized         │                            │
       │     WWW-Authenticate: NTLM   │                            │
       │◄────────────────────────────│                            │
       │                               │                            │
       │  3. Authorization: NTLM     │                            │
       │     <base64 negotiation>      │                            │
       │─────────────────────────────►│                            │
       │                               │                            │
       │                               │  4. NTLM Challenge        │
       │                               │◄──────────────────────────│
       │                               │                            │
       │  4. NTLM Response            │                            │
       │     (encrypted challenge)     │                            │
       │─────────────────────────────►│                            │
       │                               │                            │
       │                               │  5. Validate response     │
       │                               │───────────────────────────►│
       │                               │                            │
       │                               │  6. Access granted        │
       │                               │◄──────────────────────────│
       │                               │                            │
       │  5. 200 OK                   │                            │
       │     (authenticated)          │                            │
       │◄────────────────────────────│                            │
```

### Kerberos (Preferido para dominios)

Kerberos e o protocolo moderno e mais seguro para ambientes Active Directory.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            KERBEROS AUTHENTICATION                            │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                      SERVER                    KDC (DC)          SERVICE
    ──────                      ──────                    ─────────          ───────
       │                           │                         │                │
       │  1. Already has TGT       │                         │                │
       │     (from login)          │                         │                │
       │                           │                         │                │
       │  2. Request service       │                         │                │
       │     ticket (TGS-REQ)      │                         │                │
       │──────────────────────────►│                         │                │
       │                           │                         │                │
       │  3. Forward TGT +         │                         │                │
       │     service name          │                         │                │
       │───────────────────────────│────────────────────────►│                │
       │                           │                         │                │
       │                           │  4. KDC validates TGT   │                │
       │                           │     Checks PAC ( privileges) │             │
       │                           │                         │                │
       │                           │  5. Issue service ticket│                │
       │                           │     (encrypted with     │                │
       │                           │      service's key)     │                │
       │                           │◄────────────────────────│                │
       │                           │                         │                │
       │  6. Service ticket        │                         │                │
       │◄─────────────────────────│                         │                │
       │                           │                         │                │
       │  7. Request to service   │                         │                │
       │     with service ticket  │                         │                │
       │──────────────────────────►│                         │                │
       │                           │                         │                │
       │                           │  8. Validate ticket     │                │
       │                           │     (check signature)   │                │
       │                           │                         │                │
       │                           │  9. Access granted     │                │
       │                           │◄────────────────────────│                │
```

### Comparacao NTLM vs Kerberos

| Aspecto | NTLM | Kerberos |
|---------|------|----------|
| Protocolo | Challenge-response | Ticket-based |
| Seguranca | Media | Alta |
| Performance | Mais calls | Menos calls |
| Cross-domain | Limitado | Total com trusts |
| Configuracao | Simples | Complexa |
| Smart card | Nao suporta nativamente | Suporta |
| Delegation | Unconstrained only | Constrained available |
| Ticket lifetime | No expiration | 10 hours default |

## Como funciona

### Fluxo de Autenticacao Windows

1. **Negociação**: Cliente envia requisicao com header `Authorization: Negotiate`
2. **Challenge**: Servidor responde com 401 + header `WWW-Authenticate: Negotiate`
3. **Response**: Cliente criptografa o challenge com hash da senha do usuario
4. **Validacao**: Servidor valida contra AD/conta local
5. **Token**: Sessao estabelecida com WindowsIdentity

### IIS/ASP.NET Core Integration

No ASP.NET Core com IIS ou HTTP.sys:

```csharp
// Configurar autenticacao Windows
services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Extrair identidade
var identity = HttpContext.User.Identity as WindowsIdentity;
var username = identity?.Name;  // "DOMAIN\\username"
var groups = identity?.Groups;  // SIDs dos grupos
```

## Quando usar

- Aplicacoes intranet corporativas
- Quando todos os usuarios tem contas no Active Directory
- Integracao com sistemas legados Windows (SharePoint, Exchange)
- Cenarios onde SSO e mandatorio (usuario ja logado na maquina)
- Aplicacoes em ambiente Windows Server com IIS
- Ambiente Hibrido Azure AD Join

## Quando NAO usar

- Aplicacoes web publicas (internet)
- Quando usuarios nao tem contas Windows/AD
- Cross-platform requirements (Linux, macOS)
- Quando precisa de identidade de terceiros (OAuth/OIDC)
- APIs que servem clientes nao-Windows (mobile, web)
- Ambientes onde Kerberos/NTLM nao sao permitidos (muitos providers cloud)

## Alertas e caveats importantes

1. **Requer Windows**: Funciona apenas com IIS ou HTTP.sys no Windows. Kestrel nao suporta nativamente.

2. **Kerberos delegation**: Configuracao complexa para delegacao Kerberos entre servicos.

3. **Cross-domain**: Autenticacao entre dominios requer configuracao de domain trusts.

4. **Nao funciona em HTTP**: Negotiate/Kerberos requer HTTPS (exceto localhost em alguns casos).

5. **Browser support**: Suporte varia:
   - IE/Edge: Suporte completo
   - Chrome: Requer configuracao de intranet
   - Firefox: Requer configuracao `network.automatic-ntlm-auth.trusted-uris`

6. **Password changes**: Mudancas de senha podem demorar para propagar no AD.

7. **Service accounts**: Servicos precisam de SPNs (Service Principal Names) registrados corretamente.

8. **NTLM vs Kerberos**: NTLM e menos seguro e mais facil; Kerberos e mais seguro mas requer configuracao.

## Configuracao necessaria

### IIS (web.config)
```xml
<system.webServer>
  <security>
    <authentication>
      <windowsAuthentication enabled="true" />
      <anonymousAuthentication enabled="false" />
      <extendedProtection enabled="true" />
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

// Para usar com IIS:
services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
});
```

### Kestrel (nao suporta nativamente)

```bash
# Kestrel NAO suporta Windows Auth
# Use IIS ou HTTP.sys como reverse proxy

# Exemplo com HTTP.sys:
WebApplication.CreateBuilder(builder =>
{
    builder.WebHost.UseHttpSys(options =>
    {
        options.Authentication.Schemes = 
            AuthenticationSchemeName.Negotiate;
        options.Authentication.AutomaticAuthentication = true;
    });
});
```

## Endpoints principais

| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| GET | /api/auth/me | Sim | Retorna identidade Windows do usuario |
| GET | /api/protected | Sim (WinAuth) | Endpoint protegido |
| GET | /api/protected/admin | Sim (Admin) | Endpoint restrito a admins |

## Usuarios de demonstracao

**ATENCAO**: Este projeto ainda nao foi implementado. A documentacao abaixo e um template do que seria implementado.

| Usuario Windows | Domínio | Descricao |
|-----------------|---------|-----------|
| DOMAIN\\User1 | AD Domain | Usuario comum |
| DOMAIN\\Admin | AD Domain | Administrador |
| DOMAIN\\Service | AD Domain | Conta de servico |

**Para desenvolvimento local:**
| Usuario | Descricao |
|---------|-----------|
| HOSTNAME\\Developer | Conta de desenvolvimento local |
| .\\LocalUser | Usuario local |

## Exemplo de uso

### Login (automatico)
```bash
# Usuario acessa endpoint (browser envia credenciais automaticamente)
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
// Verificar se usuario e membro de grupo
var claims = User.Claims.ToList();
var groups = claims
    .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
    .Select(c => new SecurityIdentifier(c.Value).Translate(typeof(NTAccount)))
    .ToList();

// Resultado: ["DOMAIN\\Domain Users", "DOMAIN\\Finance", ...]
```

### Authorization policy
```csharp
// Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("DomainAdmins", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("DOMAIN\\Domain Admins")));
            
    options.AddPolicy("FinanceTeam", policy =>
        policy.RequireAssertion(context =>
            context.User.Claims.Any(c =>
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid"
                && GetGroupName(c.Value) == "Finance")));
});
```

## Common Errors

### 1. "No Kerberos credentials available"

**Sintoma:** Erro "No Kerberos credentials available" ou "The handle is invalid".

**Causas:**
- Usuario nao esta logado no dominio
- TGT (Ticket Granting Ticket) expirou
- Clock skew entre cliente e KDC (> 5 minutos)
- SPN nao registrado corretamente

**Solucao:**
```bash
# Verificar tickets Kerberos
klist

# Limpar e pedir novo TGT
klist purge
# Faca login novamente

# Verificar hora do sistema
w32tm /status
```

### 2. "SPN not found" ou "KDC_ERR_SPN_UNIQUE"

**Sintoma:** Autenticacao Kerberos falha, cai para NTLM.

**Causa:** Service Principal Name (SPN) nao registrado ou duplicado.

**Solucao:**
```bash
# Registrar SPN para HTTP service
setspn -S HTTP/server.domain.com DOMAIN\ServiceAccount

# Verificar SPNs registrados
setspn -L DOMAIN\ServiceAccount

# Verificar duplicados
setspn -X
```

### 3. NTLM funciona mas Kerberos nao

**Sintoma:** Autenticacao funciona quando forcing NTLM, mas Kerberos falha.

**Causas:**
- SPN nao registrado
- Delegacao nao configurada
- Grupo do usuario nao tem acesso ao SPN

**Solucao:**
```bash
# Forcar NTLM temporariamente (apenas para debug)
# No Internet Explorer:
# Tools > Internet Options > Security > Local Intranet > Custom Level
# Enable "Automatic logon only in Intranet zone"

# Para debug, verificar qual protocolo esta sendo usado:
# Enable "Security Event Logging" no DC
```

### 4. Cross-domain authentication falha

**Sintoma:** Funciona para usuarios do mesmo dominio, mas nao para outros dominios no forest.

**Causa:** Trusts entre dominios nao configurados ou desativados.

**Solucao:**
```bash
# Verificar trusts configurados
netdom trust /domain:trusteddomain

# Verificar se transitivity esta habilitada
# Para one-way trust, verificar se escopo correto de usuarios tem acesso
```

### 5. Browser nao envia credenciais (Chrome/Firefox)

**Sintoma:** Browser mostra dialog de login ou 401, mesmo para usuarios no dominio.

**Causa:** Browser nao configurado para intranet zone.

**Solucao:**
```bash
# Chrome: Configurar via GPO ou flags
chrome://flags/#NtlmV2Enabled

# Firefox: Configurar network.automatic-ntlm-auth.trusted-uris
# Em about:config
network.automatic-ntlm-auth.trusted-uris = "http://server, http://intranet"
```

### 6. HTTP.sys retorna "Access denied"

**Sintoma:** Kerberos funciona, mas HTTP.sys nega acesso.

**Causa:** ACL do HTTP.sys nao inclui o service account.

**Solucao:**
```bash
# Verificar ACLs do HTTP.sys
netsh http show urlacl

# Adicionar reserva de URL
netsh http add urlacl url="http://server:80/" user="DOMAIN\ServiceAccount"
```

## Security Considerations

### 1. Enable Extended Protection

```xml
<!-- IIS - web.config -->
<extendedProtection enabled="true" flags="WhenSupported" />
```

### 2. Restringir NTLM

```xml
<!-- IIS - Only allow Kerberos -->
<providers>
    <clear />
    <provider name="Negotiate:Kerberos" />
</providers>
```

### 3. Channel Binding

```csharp
// Configurar Extended Protection para evitar relay attacks
services.Configure<IISOptions>(options =>
{
    options.AuthenticationDisplayName = "Windows";
    options.AutomaticAuthentication = true;
});
```

### 4. Service Account Permissions

```bash
# Service account precisa de:
# 1. Logon as a service
# 2. Read/Write to SPN
# 3. Delegation permissions (se necessario)

# Configurar SPN correto
setspn -S HTTP/servername domain\serviceaccount
```

### 5. Audit Logging

```xml
<!-- Enable Kerberos logging on DC -->
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Kdc\Parameters
Value: KdcExtraLogLevel = 0x1
```

## Configuracao de Service Principal Name (SPN)

```bash
# Registrar SPN para HTTP service
setspn -S HTTP/server.domain.com DOMAIN\ServiceAccount

# Registrar para porta especifica
setspn -S HTTP/server.domain.com:8080 DOMAIN\ServiceAccount

# Verificar SPNs registrados
setspn -L DOMAIN\ServiceAccount

# Listar todos os SPNs de um servidor
setspn -L servername
```

## Configuracao de Delegation no AD

```
Active Directory Users and Computers ->
  {Service Account} ->
    Properties ->
      Delegation ->
        Trust this user for delegation to specified services only ->
          HTTP/server.domain.com
```

### Tipos de Delegation

| Tipo | Descricao | Uso |
|------|-----------|-----|
| Do not trust | Nao permite delegacao | Default |
| Trust this user for delegation to specified services | Delegacao constrained | Recomendado |
| Trust user as trusting any service | Delegacao unconstrained | Evitar |

## Referências

- [Microsoft Docs - Windows Authentication in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/authentication/windowsauth)
- [IIS - Windows Authentication](https://docs.microsoft.com/iis/configuration/system.webServer/security/authentication/windowsAuthentication)
- [Kerberos in Windows](https://docs.microsoft.com/windows-server/security/kerberos/kerberos-authentication-overview)
- [SPN (Service Principal Name)](https://docs.microsoft.com/windows-server/identity/ad-ds/manage/service-principal-names)
- [NTLM Authentication](https://docs.microsoft.com/windows-server/security/ntlm/ntlm-technical-reference)
- [Kerberos Constraint Delegation](https://docs.microsoft.com/windows-server/security/kerberos/kerberos-constrained-delegation-overview)
