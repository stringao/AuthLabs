# Resource-Based Authorization

## O que e

Resource-based Authorization e um modelo onde as permissoes sao avaliadas nao apenas baseada em quem o usuario e (roles/claims), mas tambem baseada no recurso especifico que esta sendo acessado. O sistema verifica se o usuario tem permissao para executar uma operacao especifica naquele recurso especifico.

Diferente de RBAC onde verificamos "e Admin?", aqui verificamos "este usuario pode editar ESTE documento especifico?".

## Conceitos Fundamentais

### Resource vs Identity

| Tipo | Exemplo | Questao |
|------|---------|---------|
| Identity | Role, Claim | "O usuario e Admin?" |
| Resource | Document, Order | "O usuario pode editar este documento?" |

### Quando Resource-Based e Necessario

- Documento 1: Dono e User A, Editor e User B
- Documento 2: Dono e User B, Editor e User A
- SAME USER, DIFFERENT PERMISSIONS per document!

## Como funciona

1. **Resource Identification**: Recurso e identificado pelo seu ID (ex: documento com ID=123)
2. **Resource Loading**: Sistema carrega o recurso do banco de dados
3. **Authorization Handler**: Handler avalia se o principal pode executar a operacao
4. **Ownership Check**: Comum verificar se o usuario e o dono do recurso
5. **Permission Check**: Sistema verifica se ha permissoes explicitas para o usuario no recurso
6. **Decision**: Acesso concedido ou negado baseado na avaliacao

## Diagrama de fluxo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     RESOURCE-BASED AUTHORIZATION FLOW                       │
└─────────────────────────────────────────────────────────────────────────────┘

    CLIENT                         SERVER                          DATABASE
    ──────                         ──────                          ────────
       │                              │                               │
       │  1. PUT /documents/42        │                               │
       │     Authorization: Bearer     │                               │
       │     {title: "New Title"}    │                               │
       │───────────────────────────►│                               │
       │                              │                               │
       │                              │  2. Extract resource ID       │
       │                              │     from route: 42           │
       │                              │                               │
       │                              │  3. Load Document(id=42)     │
       │                              │──────────────────────────────►│
       │                              │                               │
       │                              │  4. SELECT * FROM documents   │
       │                              │     JOIN permissions ON ...   │
       │                              │     WHERE id = 42             │
       │                              │◄──────────────────────────────│
       │                              │                               │
       │                              │  5. Document found:           │
       │                              │     - OwnerId = 1 (Alice)     │
       │                              │     - Permissions:           │
       │                              │       Bob: CanEdit=true      │
       │                              │       Carol: CanEdit=false   │
       │                              │                               │
       │                              │  6. AuthorizationService     │
       │                              │     .AuthorizeAsync(         │
       │                              │       User,                  │
       │                              │       document,              │
       │                              │       operation              │
       │                              │     )                        │
       │                              │                               │
       │                              │  7. DocumentAuthorization     │
       │                              │     .HandleRequirementAsync  │
       │                              │                               │
       │                              │  8. CHECK OWNERSHIP:         │
       │                              │     currentUser.Id == 1?     │
       │                              │     YES → ALLOW (owner)      │
       │                              │     NO → check permissions   │
       │                              │                               │
       │                              │  9. CHECK PERMISSIONS:       │
       │                              │     currentUser.Id == 2?     │
       │                              │     Bob has CanEdit=true     │
       │                              │     YES → ALLOW              │
       │                              │     NO → DENY                │
       │                              │                               │
       │  10. 200 OK / 403 Forbidden │                               │
       │◄───────────────────────────│                               │


┌─────────────────────────────────────────────────────────────────────────────┐
│                        OWNER vs PERMISSION FLOW                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO 1: Owner accessing their document                                  │
│  ───────────────────────────────────────────────────                         │
│                                                                              │
│  User: Alice (id=1)                                                         │
│  Document: 42 (OwnerId=1)                                                   │
│                                                                              │
│  Handler: Is resource.OwnerId == currentUser.Id?                           │
│  1 == 1 → TRUE                                                              │
│  context.Succeed(requirement)  ← Early return, skip permission check       │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  SCENARIO 2: Non-owner with explicit permission                             │
│  ───────────────────────────────────────────────────                         │
│                                                                              │
│  User: Bob (id=2)                                                           │
│  Document: 42 (OwnerId=1, Alice)                                            │
│  Permissions: Bob: CanEdit=true                                             │
│                                                                              │
│  Handler: Is resource.OwnerId == currentUser.Id?                           │
│  1 == 2 → FALSE → continue to permission check                              │
│                                                                              │
│  Handler: permission = permissions.find(userId=2)                          │
│  permission.CanEdit == true? → TRUE                                        │
│  context.Succeed(requirement)                                               │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                              │
│  SCENARIO 3: Non-owner without permission                                   │
│  ───────────────────────────────────────────────────                         │
│                                                                              │
│  User: Carol (id=3)                                                         │
│  Document: 42 (OwnerId=1, Alice)                                            │
│  Permissions: Bob: CanEdit=true  (Carol not listed)                        │
│                                                                              │
│  Handler: Is resource.OwnerId == currentUser.Id?                           │
│  1 == 3 → FALSE → continue to permission check                              │
│                                                                              │
│  Handler: permission = permissions.find(userId=3)                          │
│  permission == null → return (no success)                                  │
│                                                                              │
│  403 Forbidden                                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Quando usar

- Sistemas de documentos/arquivos onde usuarios tem permissoes diferentes por documento
- Aplicacoes multi-tenant onde dados devem ser isolados por organizacao
- Workflows de aprovacao onde aprovadores tem acesso granular
- Sistemas de content management (CMS) com permissoes por item
- Dashboards customizaveis onde usuarios criam seus proprios recursos
- Qualquer cenario onde a mesma operacao pode ser permitida ou negada baseado no recurso especifico

## Quando NAO usar

- Quando todas as operacoes sao uniformes para todos os recursos (use RBAC)
- Sistemas com milhoes de recursos (performance de authorization queries)
- Cenarios onde ownership e suficiente (simples owner = full access)
- Quando resources sao imutaveis e nao requerem permissoes customizadas
- Aplicacoes simples onde apenas roles sao suficientes

## Alertas e caveats importantes

1. **Controlador de documentos nao implementado**: Authorization handlers existem mas nao ha API para testar.

2. **Operacao de Read nao implementada**: Handler só verifica Edit e Delete, nao Read.

3. **Possivel null reference**: Handler nao verifica se `permission` e null antes de acessar propriedades.

4. **Login sem validacao de senha**: Endpoint de login nao valida senha - apenas verifica se usuario existe.

5. **Middleware de autorizacao nao configurado**: Handlers podem nao estar wireados corretamente no Program.cs.

6. **Sem cache de permissoes**: Cada requisicao faz query no banco.

7. **Ausencia de audit trail**: Nao ha logging de quais permissoes foram verificadas.

## Configuracao necessaria

```json
{
  "Jwt": {
    "Key": "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
    "Issuer": "AuthLabs.Resource",
    "Audience": "AuthLabs.Resource.Api"
  }
}
```

**Models:**
```csharp
public class Document
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string OwnerId { get; set; }
    public ApplicationUser Owner { get; set; }
    public ICollection<DocumentPermission> Permissions { get; set; }
}

public class DocumentPermission
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string UserId { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public ApplicationUser User { get; set; }
}
```

## Authorization Handler

```csharp
public class DocumentOperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public DocumentOperationRequirement(string operation)
    {
        Operation = operation;
    }
}

public class DocumentAuthorizationHandler :
    AuthorizationHandler<DocumentOperationRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document resource)
    {
        if (resource == null)
        {
            return Task.CompletedTask;
        }

        // Owner tem todas as permissoes
        if (resource.OwnerId == context.User.GetUserId())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Verificar permissoes explicitas
        var permission = resource.Permissions
            .FirstOrDefault(p => p.UserId == context.User.GetUserId());

        // NULL CHECK - importante!
        if (permission == null)
            return Task.CompletedTask;  // Nao succeeded = deny

        switch (requirement.Operation)
        {
            case "Edit":
                if (permission.CanEdit)
                    context.Succeed(requirement);
                break;
            case "Delete":
                if (permission.CanDelete)
                    context.Succeed(requirement);
                break;
            case "Read":
                // Poderia implementar leitura se tivesse campo
                context.Succeed(requirement);  // Exemplo: todos podem ler
                break;
        }

        return Task.CompletedTask;
    }
}
```

## Endpoints

**ATENCAO**: Este projeto esta incompleto - só há AuthController com endpoint de login.

| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| POST | /api/auth/login | Nao | Login (JWT) |

**Endpoints de documento serão implementados futuramente:**
| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| GET | /api/documents | Sim | Listar documentos |
| GET | /api/documents/{id} | Sim | Ver documento |
| PUT | /api/documents/{id} | Sim | Editar (requer CanEdit) |
| DELETE | /api/documents/{id} | Sim | Deletar (requer CanDelete) |

## Usuarios de demonstracao

| Email | Senha | Role |
|-------|-------|------|
| admin@authlabs.com | Admin123! | Admin |
| user@authlabs.com | User123! | User |

**NOTA**: Seeds使用的是 AuthLabs.Shared.

## Exemplo de uso (quando implementado)

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@authlabs.com","password":"User123!"}'
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "1",
  "email": "user@authlabs.com",
  "role": "User"
}
```

### Tentar editar documento proprio
```bash
curl -X PUT http://localhost:5000/api/documents/1 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated Title"}'
# Owner do documento -> Sucesso
```

### Tentar editar documento de outro usuario (sem permissao)
```bash
curl -X PUT http://localhost:5000/api/documents/2 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"title":"Hacked Title"}'
# Retorna: 403 Forbidden
```

### Compartilhar documento com outro usuario
```bash
# Owner pode compartilhar documento
curl -X POST http://localhost:5000/api/documents/1/share \
  -H "Authorization: Bearer <owner_token>" \
  -H "Content-Type: application/json" \
  -d '{"userId": "2", "canEdit": true, "canDelete": false}'
```

## Matriz de Permissoes

| Recurso | Dono | Permissao Edit | Permissao Delete | Others |
|---------|------|----------------|------------------|--------|
| Doc 1 (user@authlabs.com) | user | Sim (owner) | Sim (owner) | Sem acesso |
| Doc 2 (admin@authlabs.com) | admin | Se concedido | Se concedido | Sem acesso |

## Common Errors

### 1. 403 Forbidden mesmo sendo dono

**Sintoma:** Usuario e dono do documento mas recebe 403.

**Causas:**
- Handler nao esta registrado no DI
- Resource nao esta sendo passado para AuthorizeAsync
- OwnerId e string mas userId e int (type mismatch)

**Solucao:**
```csharp
// Verificar registro do handler
services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();

// Verificar passagem do resource
var document = await _context.Documents.FindAsync(id);
if (document == null) return NotFound();

var result = await _authorizationService.AuthorizeAsync(User, document, requirement);

// Verificar tipos
// Se OwnerId e string "1" e user.GetUserId() retorna 1 (int)
```

### 2. NullReferenceException no handler

**Sintoma:** Erro 500 ao acessar documento.

**Causa:** Handler tenta acessar `resource.Permissions` mas Permissions e null (lazy load nao funcionou).

**Solucao:**
```csharp
// Include permissions ao carregar documento
var document = await _context.Documents
    .Include(d => d.Permissions)
    .FirstOrDefaultAsync(d => d.Id == id);

// OU verificar null no handler
var permission = resource.Permissions
    .FirstOrDefault(p => p.UserId == context.User.GetUserId());
if (permission == null)
    return Task.CompletedTask;
```

### 3. Performance: N+1 queries

**Sintoma:** Carregar lista de documentos e muito lento.

**Causa:** Para cada documento, uma query separada para permissoes.

**Solucao:**
```csharp
// Carregar todos de uma vez com Include
var documents = await _context.Documents
    .Include(d => d.Permissions)
    .Where(d => d.OwnerId == userId || 
                d.Permissions.Any(p => p.UserId == userId))
    .ToListAsync();

// OU usar cache
_documentsCache.TryGetValue(userId, out var cached);
```

## Policy Registration

```csharp
// Program.cs
services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();

services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditDocument", policy =>
        policy.Requirements.Add(new DocumentOperationRequirement("Edit")));

    options.AddPolicy("CanDeleteDocument", policy =>
        policy.Requirements.Add(new DocumentOperationRequirement("Delete")));
});
```

## Exemplo de Controller (futuro)

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> EditDocument(int id, UpdateDocumentRequest request)
{
    var document = await _documentService.GetByIdAsync(id);
    if (document == null)
        return NotFound();

    var requirement = new DocumentOperationRequirement("Edit");
    var result = await _authorizationService.AuthorizeAsync(User, document, requirement);

    if (!result.Succeeded)
        return Forbid();

    await _documentService.UpdateAsync(id, request);
    return Ok();
}

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteDocument(int id)
{
    var document = await _documentService.GetByIdAsync(id);
    if (document == null)
        return NotFound();

    var requirement = new DocumentOperationRequirement("Delete");
    var result = await _authorizationService.AuthorizeAsync(User, document, requirement);

    if (!result.Succeeded)
        return Forbid();

    await _documentService.DeleteAsync(id);
    return NoContent();
}
```

## Security Considerations

### 1. Always Load Resource from Database

```csharp
// NUNCA usar dados do request diretamente
// BAD: var ownerId = request.OwnerId;
// GOOD: var document = await _context.Documents.FindAsync(id);
```

### 2. Audit Logging

```csharp
public class DocumentAuthorizationHandler :
    AuthorizationHandler<DocumentOperationRequirement, Document>
{
    private readonly IAuditLogger _auditLogger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DocumentOperationRequirement requirement,
        Document resource)
    {
        var userId = context.User.GetUserId();
        var allowed = EvaluatePermission(userId, resource, requirement.Operation);
        
        _auditLogger.Log(
            "DocumentAccess",
            userId,
            new
            {
                DocumentId = resource.Id,
                Operation = requirement.Operation,
                Result = allowed ? "ALLOWED" : "DENIED",
                IsOwner = resource.OwnerId == userId
            });
        
        if (allowed)
            context.Succeed(requirement);
            
        return Task.CompletedTask;
    }
}
```

### 3. Deny by Default

```csharp
// Se handler nao chama context.Succeed(), acesso e negado
// Isso e intencional - nega por padrao
```

## Referências

- [Microsoft Docs - Resource-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/resourcebased)
- [Microsoft - Custom Authorization Policies](https://docs.microsoft.com/aspnet/core/security/authorization/policies)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
- [NIST ABAC Guide](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.8625.pdf)
