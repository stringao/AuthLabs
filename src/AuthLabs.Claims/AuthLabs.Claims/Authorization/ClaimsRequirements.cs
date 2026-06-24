using Microsoft.AspNetCore.Authorization;

namespace AuthLabs.Claims.Authorization;

/// <summary>
/// Requisito de autorização customizado que verifica se o usuário possui um claim específico.
/// </summary>
/// <remarks>
/// JUNIOR: O que é um IAuthorizationRequirement?
/// Um IAuthorizationRequirement é uma interface que define "o que" precisa ser verificado
/// para que o acesso seja concedido. Ela representa uma condição abstrata de autorização.
/// O handler correspondente é quem sabe "como" verificar essa condição.
///
/// Neste caso, o CustomClaimRequirement verifica se o usuário possui um claim
/// de um tipo específico (ClaimType) com um valor específico (ClaimValue).
/// Exemplo: verificar se o usuário tem o claim "Document:Edit" com valor "true".
/// </remarks>
/// <example>
/// // Exemplo de uso:
/// var requirement = new CustomClaimRequirement("Document:Edit", "true");
/// </example>
public class CustomClaimRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Obtém o tipo do claim que deve ser verificado.
    /// </summary>
    /// <remarks>
    /// JUNIOR: O que é o "tipo" de um claim?
    /// O tipo identifica o que o claim representa. É como o "nome" do claim.
    /// Exemplos comuns de tipos:
    /// - "Document:Edit" - indica permissão para editar documentos
    /// - "Subscription:Tier" - indica o nível da assinatura do usuário
    /// - System.Security.Claims.ClaimTypes.Email - email do usuário
    /// </remarks>
    /// <value>String que identifica o tipo do claim, ex: "Document:Edit"</value>
    public string ClaimType { get; }

    /// <summary>
    /// Obtém o valor esperado do claim para que a autorização seja concedida.
    /// </summary>
    /// <remarks>
    /// JUNIOR: O valor do claim é o "conteúdo" da informação.
    /// Por exemplo, se o tipo é "Document:Edit", o valor pode ser "true" ou "false".
    /// Para "Subscription:Tier", o valor pode ser "Free", "Premium" ou "Enterprise".
    ///
    /// A autorização só é concedida se TANTO o tipo QUANTO o valor corresponderem.
    /// </remarks>
    /// <value>String com o valor esperado do claim, ex: "true" ou "Premium"</value>
    public string ClaimValue { get; }

    /// <summary>
    /// Cria uma nova instância do requisito de claim customizado.
    /// </summary>
    /// <param name="claimType">O tipo do claim a ser verificado (ex: "Document:Edit").</param>
    /// <param name="claimValue">O valor que o claim deve ter para autorização ser concedida (ex: "true").</param>
    /// <remarks>
    /// JUNIOR: Este é o construtor. Quando você faz:
    /// <c>new CustomClaimRequirement("Document:Edit", "true")</c>
    /// Você está criando um requisito que verifica se o usuário tem um claim
    /// chamado "Document:Edit" com valor "true".
    /// </remarks>
    public CustomClaimRequirement(string claimType, string claimValue)
    {
        ClaimType = claimType;
        ClaimValue = claimValue;
    }
}

/// <summary>
/// Constantes que definem os nomes das políticas de autorização usadas na aplicação.
/// </summary>
/// <remarks>
/// JUNIOR: O que é uma Política de Autorização?
/// Uma política de autorização é um conjunto nomeado de requisitos que devem ser
/// satisfeitos para que o acesso a um recurso seja concedido.
///
/// Em vez de escrever os requisitos diretamente nos atributos [Authorize],
/// definimos políticas com nomes descritivos aqui. Isso traz vários benefícios:
/// 1. Reutilização: a mesma política pode ser usada em múltiplos endpoints
/// 2. Manutenção: se os requisitos mudarem, alteramos em um único lugar
/// 3. Clareza: nomes como "CanEditDocuments" são mais legíveis que expressões complexas
///
/// As políticas são configuradas no Program.cs usando services.AddAuthorization().
/// </remarks>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Política que permite editar documentos.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Para usar esta política, o usuário precisa ter o claim "Document:Edit" com valor "true".
    /// Esta política é tipicamente aplicada a endpoints que modificam documentos.
    /// </remarks>
    /// <value>String constante: "CanEditDocuments"</value>
    public const string CanEditDocuments = "CanEditDocuments";

    /// <summary>
    /// Política que permite excluir documentos.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Para usar esta política, o usuário precisa ter o claim "Document:Delete" com valor "true".
    /// Note que esta é uma permissão separada de editar - um usuário pode ter
    /// permissão para editar mas não para excluir.
    /// </remarks>
    /// <value>String constante: "CanDeleteDocuments"</value>
    public const string CanDeleteDocuments = "CanDeleteDocuments";

    /// <summary>
    /// Política que permite gerenciar usuários.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Para usar esta política, o usuário precisa ter o claim "User:Manage" com valor "true".
    /// Esta política é geralmente reservada para administradores do sistema.
    /// Tenha cuidado ao aplicar esta política - ela concede poder significativo.
    /// </remarks>
    /// <value>String constante: "CanManageUsers"</value>
    public const string CanManageUsers = "CanManageUsers";

    /// <summary>
    /// Política que indica usuário com assinatura Premium.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Para usar esta política, o usuário precisa ter o claim "Subscription:Tier" com valor "Premium".
    /// Esta política é um exemplo de controle baseado em tiers/planos.
    /// Usuários Premium podem acessar recursos exclusivos que usuários Free não têm.
    /// </remarks>
    /// <value>String constante: "IsPremiumUser"</value>
    public const string IsPremiumUser = "IsPremiumUser";
}
