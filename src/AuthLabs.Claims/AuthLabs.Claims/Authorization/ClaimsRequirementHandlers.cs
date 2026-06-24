using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Claims.Authorization;

/// <summary>
/// Handler de autorização que valida claims específicos do usuário.
/// </summary>
/// <remarks>
/// JUNIOR: O que é um AuthorizationHandler?
/// Um AuthorizationHandler é responsável por "como" verificar se um requisito
/// (IAuthorizationRequirement) foi satisfeito.
///
/// O padrão Authorization no ASP.NET Core funciona assim:
/// 1. Você define um Requirement (O QUÊ verificar) - implementa IAuthorizationRequirement
/// 2. Você cria um Handler (COMO verificar) - implementa AuthorizationHandler
/// 3. Você configura políticas no Program.cs que associam Requirements a Handlers
/// 4. Quando um endpoint exige uma política, o framework chama o handler
///
/// Este CustomClaimHandler verifica se o usuário possui um claim específico
/// com um valor específico, conforme definido no CustomClaimRequirement.
/// </remarks>
/// <example>
/// <code>
/// // No Program.cs:
/// services.AddScoped<IAuthorizationHandler, CustomClaimHandler>();
///
/// // Em um controller:
/// [Authorize(Policy = "CanEditDocuments")]
/// public IActionResult Edit() { ... }
/// </code>
/// </example>
public class CustomClaimHandler : AuthorizationHandler<CustomClaimRequirement>
{
    /// <summary>
    /// Executa a lógica de validação do requisito de claim.
    /// </summary>
    /// <param name="context">Contexto da autorização contendo informações do usuário e recursos.</param>
    /// <param name="requirement">O requisito de claim a ser verificado.</param>
    /// <returns>Task representando a operação assíncrona.</returns>
    /// <remarks>
    /// JUNIOR: Como funciona este método?
    ///
    /// 1. O parâmetro 'context' contém:
    ///    - context.User: a identidade do usuário logado com todos os seus claims
    ///    - context.Resource: o recurso sendo acessado (pode ser null em muitos casos)
    ///    - context.User: pode ser usado para verificar se o usuário está autenticado
    ///
    /// 2. O parâmetro 'requirement' contém:
    ///    - requirement.ClaimType: o tipo de claim esperado (ex: "Document:Edit")
    ///    - requirement.ClaimValue: o valor esperado (ex: "true")
    ///
    /// 3. A verificação usa context.User.HasClaim() que verifica TODOS os claims
    ///    do usuário. Se encontrar um claim com o tipo E valor corretos, a
    ///    autorização é bem-sucedida.
    ///
    /// 4. context.Succeed(requirement) marca este requisito como satisfeito.
    ///    Se você chamar context.Fail() em vez disso, a autorização será negada.
    ///
    /// 5. IMPORTANTE: mesmo após chamar context.Succeed(), outros handlers para
    ///    o mesmo requisito ainda serão chamados (a menos que a política use
    ///    Requirements mais restritivos). Todos os requisitos devem succeeder.
    /// </remarks>
    /// <seealso cref="CustomClaimRequirement"/>
    /// <seealso cref="AuthorizationHandlerContext"/>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CustomClaimRequirement requirement)
    {
        // JUNIOR: Aqui está a lógica principal de verificação!
        // HasClaim recebe um predicado (função que retorna true/false).
        // O predicado verifica se existe algum claim cujo Type seja igual
        // ao ClaimType do requirement E cujo Value seja igual ao ClaimValue.
        //
        // Se essa condição for verdadeira, significa que o usuário tem
        // a permissão necessária, então chamamos context.Succeed().
        if (context.User.HasClaim(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue))
        {
            // JUNIOR: context.Succeed() marca que este requisito foi satisfeito.
            // O ASP.NET Core continua verificando outros requisitos da política.
            // Se TODOS os requisitos succeedem, o acesso é concedido.
            context.Succeed(requirement);
        }

        // JUNIOR: Se a condição não for satisfeita, simplesmente não chamamos
        // Succeed(). O handler retorna Task.CompletedTask e o framework
        // entenderá que este requisito não foi satisfeito.
        //
        // Não é necessário chamar context.Fail() explicitamente aqui -
        // a ausência de Success é interpretada como falha.
        return Task.CompletedTask;
    }
}
