# Resource-Based Authorization

## O que é

Resource-based Authorization é um modelo onde as permissões são avaliadas não apenas baseada em quem o usuário é (roles/claims), mas também baseada no recurso específico que está sendo acessado. O sistema verifica se o usuário tem permissão para executar uma operação específica naquele recurso específico.

## Como funciona

1. **Resource Identification**: Recurso é identificado pelo seu ID (ex: documento com ID=123)
2. **Resource Loading**: Sistema carrega o recurso do banco de dados
3. **Authorization Handler**: Handler avalia se o principal pode executar a operação
4. **Ownership Check**: Comum verificar se o usuário é o dono do recurso
5. **Permission Check**: Sistema verifica se há permissões explícitas para o usuário no recurso
6. **Decision**: Acesso concedido ou negado baseado na avaliação

## Diagrama de fluxo
```
┌────────┐     ┌──────────────┐     ┌─────────┐
│ Client │────►│   Server     │────►│   DB    │
└────────┘     └──────────────┘     └─────────┘
     │               │                  │
     │  1. Request   │                  │
     │  DELETE /docs/42                 │
     │─────────────►│                  │
     │               │                  │
     │  2. Load Doc  │                  │
     │  (id=42)     │                  │
     │─────────────►│                  │
     │               │◄────────────────│
     │               │                  │
     │  3. Check     │                  │
     │  - Owner?     │                  │
     │  - Permissions?                 │
     │               │                  │
     │  4. Evaluate  │                  │
     │  Handler      │                  │
     │               │                  │
     │  5. Decision  │                  │
     │  (Allow/Deny) │                  │
     │               │                  │
     │  6. Response  │                  │
     │◄─────────────│                  │
```

## Quando usar

- Sistemas de documentos/arquivos onde usuários têm permissões diferentes por documento
- Aplicações multi-tenant onde dados devem ser isolados por organização
- Workflows de aprovação onde aprovadores têm acesso granular
- Sistemas de content management (CMS) com permissões por item
- Dashboards customizáveis onde usuários criam seus próprios recursos

## Quando NÃO usar

- Quando todas as operações são uniformes para todos os recursos (use RBAC)
- Sistemas com milhões de recursos (performance de authorization queries)
- Cenários onde ownership é suficiente (simples owner = full access)
- Quando resources são imutáveis e não requerem permissões customizadas

## Alertas e caveats importantes

1. **Controlador de documentos não implementado**: Authorization handlers existem mas não há API para testar.

2. **Operação de Read não implementada**: Handler só verifica Edit e Delete, não Read.

3. **Possível null reference**: Handler não verifica se `permission` é null antes de acessar propriedades.

4. **Login sem validação de senha**: Endpoint de login não valida senha - apenas verifica se usuário existe.

5. **Middleware de autorização não configurado**: Handlers podem não estar wireados corretamente no Program.cs.

6. **Sem cache de permissões**: Cada requisição faz query no banco.

7. **Ausência de audit trail**: Não há logging de quais permissões foram verificadas.

## Configuração necessária

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
        // Owner tem todas as permissões
        if (resource.OwnerId == context.User.GetUserId())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Verificar permissões explícitas
        var permission = resource.Permissions
            .FirstOrDefault(p => p.UserId == context.User.GetUserId());

        if (permission == null)
            return Task.CompletedTask;

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
        }

        return Task.CompletedTask;
    }
}
```

## Endpoints

**ATENÇÃO**: Este projeto está incompleto - só há AuthController com endpoint de login.

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| POST | /api/auth/login | Não | Login (JWT) |

**Endpoints de documento serão implementados futuramente:**
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | /api/documents | Sim | Listar documentos |
| GET | /api/documents/{id} | Sim | Ver documento |
| PUT | /api/documents/{id} | Sim | Editar (requer CanEdit) |
| DELETE | /api/documents/{id} | Sim | Deletar (requer CanDelete) |

## Usuários de demonstração

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

### Tentar editar documento próprio
```bash
curl -X PUT http://localhost:5000/api/documents/1 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated Title"}'
# Owner do documento -> Sucesso
```

### Tentar editar documento de outro usuário (sem permissão)
```bash
curl -X PUT http://localhost:5000/api/documents/2 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"title":"Hacked Title"}'
# Retorna: 403 Forbidden
```

## Matriz de Permissões

| Recurso | Dono | Permissão Edit | Permissão Delete | Others |
|---------|------|----------------|------------------|--------|
| Doc 1 (user@authlabs.com) | user | Sim (owner) | Sim (owner) | Sem acesso |
| Doc 2 (admin@authlabs.com) | admin | Se concedido | Se concedido | Sem acesso |

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
```

## Referências

- [Microsoft Docs - Resource-based Authorization](https://docs.microsoft.com/aspnet/core/security/authorization/resourcebased)
- [Microsoft - Custom Authorization Policies](https://docs.microsoft.com/aspnet/core/security/authorization/policies)
- [OWASP - Access Control](https://owasp.org/www-project-top-ten/2017/A5_2017-Broken_Access_Control)
