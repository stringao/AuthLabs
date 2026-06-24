namespace AuthLabs.OAuth.Infrastructure;

/// <summary>
/// Configuração individual de um provider OAuth/OIDC.
/// Cada provider (Google, GitHub, etc) tem suas próprias credenciais e endpoints.
/// </summary>
/// <remarks>
/// JUNIOR: O que é OAuth Provider?
/// Um "provider" é o serviço externo que autentica o usuário. Exemplos:
/// - Google: permite login com conta Google
/// - GitHub: permite login com conta GitHub
/// - Facebook: permite login com conta Facebook
///
/// Cada provider fornece:
/// - ClientId: identificador único da sua aplicação naquele provider
/// - ClientSecret: senha secreta que só você conhece (nunca exponha no código!)
/// - Endpoints: URLs específicas para cada provider trocar informações
/// </remarks>
public class OAuthProviderConfig
{
    /// <summary>
    /// Nome amigável do provider (ex: "Google", "GitHub").
    /// Usado para identificar qual provider usar na hora do login.
    /// </summary>
    /// <example>Google</example>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único da aplicação registrado no provider OAuth.
    /// Também chamado de "App ID" ou "Client ID" dependendo do provider.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Como obter?
    /// Vá ao portal do developer do provider (console.cloud.google.com para Google,
    /// developer.github.com para GitHub) e registre sua aplicação.
    /// O provider te dará um ClientId público e um ClientSecret privado.
    /// </remarks>
    /// <example>123456789-abcdefghijklmnop.apps.googleusercontent.com</example>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Senha secreta da aplicação. NUNCA commite este valor no git!
    /// Armazene em variáveis de ambiente ou secrets manager.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Segredos
    /// O ClientSecret é como uma senha. Se alguém obtém acesso, pode:
    /// - Se passar por sua aplicação
    /// - roubar dados de usuários
    /// - incorrer em custos em seu nome
    ///
    /// Use: appsettings.Development.json, Azure Key Vault, GitHub Secrets, etc.
    /// </remarks>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// URL do provider para iniciar o fluxo de autorização.
    /// O usuário será redirecionado para esta URL para dar permissão.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Authorization Endpoint - O que acontece aqui?
    /// 1. Sua app redireciona o usuário para esta URL
    /// 2. O usuário faz login no provider (se já não estiver logado)
    /// 3. O usuário dá permissão para sua app acessar seus dados
    /// 4. O provider redireciona de volta para sua app com um "código de autorização"
    ///
    /// Exemplo Google: https://accounts.google.com/o/oauth2/v2/auth
    /// </remarks>
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// URL do provider para trocar o código de autorização por tokens.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Token Endpoint - Trocando código por tokens
    /// Após o usuário dar permissão, você recebe um "código temporário".
    /// Você envia esse código + seu ClientSecret para esta URL.
    /// O provider retorna tokens (access_token, id_token, refresh_token).
    ///
    /// Este passo acontece no backend, nunca no navegador!
    /// O código de autorização tem curta duração e só pode ser usado uma vez.
    /// </remarks>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// URL do provider para buscar informações do usuário autenticado.
    /// </summary>
    /// <remarks>
    /// JUNIOR: UserInfo Endpoint - Quem é o usuário?
    /// Após obter o access_token, você pode chamar esta URL
    /// para obter dados do perfil do usuário (nome, email, foto, etc).
    ///
    /// Esta é a diferença entre OAuth (apenas autorização) e OIDC (identidade):
    /// OIDC adiciona o UserInfo endpoint para saber "quem é" o usuário.
    /// </remarks>
    public string UserInfoEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Caminho URL de callback onde o provider redireciona após autenticação.
    /// Deve ser registrado no provider e coincidir exatamente.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Callback Path - O retorno
    /// Após o usuário fazer login, o provider redireciona para ESTA URL.
    /// O provider adiciona parâmetros como ?code=xxx ou ?error=xxx.
    ///
    /// Exemplo: Se CallbackPath = "/signin-google"
    /// O redirect será: https://seuapp.com/signin-google?code=abc123
    ///
    ///ATENÇÃO: Este caminho deve SER EXATAMENTE IGUAL ao registrado no provider!
    /// </remarks>
    public string CallbackPath { get; set; } = string.Empty;
}

/// <summary>
/// Agrupa todas as configurações de providers OAuth/OIDC da aplicação.
/// </summary>
/// <remarks>
/// JUNIOR: Por que um Dicionário?
/// Você pode querer suportar múltiplos providers (Google + GitHub + Microsoft).
/// Cada provider tem sua própria entrada nesta coleção.
///
/// Exemplo de estrutura no appsettings.json:
/// {
///   "OAuth": {
///     "Providers": {
///       "Google": { "ClientId": "...", "ClientSecret": "..." },
///       "GitHub": { "ClientId": "...", "ClientSecret": "..." }
///     }
///   }
/// }
/// </remarks>
public class OAuthSettings
{
    /// <summary>
    /// Dicionário de providers configurados, indexados pelo nome do provider.
    /// </summary>
    /// <remarks>
    /// JUNIOR: Dictionary é como um dicionário de português
    /// A "chave" é o nome (ex: "Google") e o "valor" é a configuração completa.
    /// Permite acesso rápido: providers["Google"] retorna a config do Google.
    /// </remarks>
    public Dictionary<string, OAuthProviderConfig> Providers { get; set; } = new();
}
