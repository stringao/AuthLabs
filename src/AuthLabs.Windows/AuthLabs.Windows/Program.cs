using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using AuthLabs.Windows.Services;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Configura autenticacao Windows usando o esquema Negotiate.
/// Negotiate automaticamente escolhe entre Kerberos (preferido) e NTLM (fallback).
/// </summary>
/// <remarks>
/// JUNIOR: pipeline DE CONFIGURACAO (builder pattern):
/// ================================================
///
/// O que estamos fazendo aqui e CONFIGURAR a aplicacao, nao executar logica.
/// Esta configuracao e lida uma vez quando a aplicacao inicia.
///
/// AddAuthentication() registra o servico de autenticacao.
/// AddNegotiate() adiciona o handler de Windows Authentication.
///
/// SCHEME (esquema) DE AUTENTICACAO:
/// - AuthenticationScheme e um nome unico para cada tipo de autenticacao
/// - NegotiateDefaults.AuthenticationScheme = "Negotiate"
/// - Este nome e usado em [Authorize(AuthenticationSchemes = "Negotiate")]
///
/// ALERTA CRITICO - WINDOWS SOMENTE:
/// ================================
/// O pacote AddNegotiate() SO funciona no Windows!
/// - Em producao Windows: funciona perfeitamente (Kerberos/NTLM real)
/// - Em producao Linux/Mac: NAO vai funcionar, lancara excecao
///
/// Se voce precisa cross-platform, considere:
/// - LDAP Authentication (System.DirectoryServices.Protocols)
/// - External Identity Provider (IdentityServer, Auth0, Okta)
/// - JWT com tokens emitidos por outro servico
///
/// KERBEROS vs NTLM (resumo):
/// ==========================
/// Kerberos (padrao, mais seguro):
/// - Usa tickets criptografados com chave secreta
/// - Funciona em multiplos dominios (com relacoes de confiança)
/// - Requer registro do SPN (Service Principal Name)
/// - Porta 88 UDP/TCP
///
/// NTLM (fallback):
/// - Usa desafio-resposta (challenge-response)
/// - Funciona em redes simples sem relacao de confiança
/// - Menos seguro, nao suporta delegation
/// - Senha nunca e enviada pela rede
///
/// RegisterAuthenticationScheme vs AddNegotiate:
/// ============================================
/// builder.Services.AddAuthentication() configura O SISTEMA de autenticacao.
/// .AddNegotiate() adiciona o handler especifico para Windows.
/// Voce pode ter multiplos esquemas (Windows + Cookie + JWT) simultaneamente.
/// </remarks>
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

/// <summary>
/// Configura politicas de autorizacao baseadas em Windows Authentication.
/// </summary>
/// <remarks>
/// JUNIOR: Authorization Policies no ASP.NET Core:
/// ================================================
///
/// Policies sao nomes reutilizaveis para regras de acesso.
/// Em vez de escrever [Authorize(Roles = "Admin")] em todo lugar,
/// voce cria uma policy "AdminOnly" e usa [Authorize(Policy = "AdminOnly")].
///
/// POLICIES DEFINIDAS AQUI:
/// 1. RequireWindowsAuth:
///    - Requer que usuario esteja autenticado via Windows
///    - Usa esquema Negotiate
///
/// 2. AdminOnly:
///    - Requer role "Admin" (ClaimTypes.Role = "Admin")
///    - Também requer Windows Authentication
///
/// BENEFICIOS DE POLICIES:
/// - Reutilizavel em multiplos controllers/metodos
/// - Logica de negocio centralizada
/// - Facil de modificar (muda em um lugar, afeta toda a aplicacao)
///
/// EXEMPLO DE USO:
/// [Authorize(Policy = "AdminOnly")]
/// public IActionResult AdminArea() { ... }
/// </remarks>
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireWindowsAuth", policy =>
    {
        policy.AuthenticationSchemes.Add(NegotiateDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add(NegotiateDefaults.AuthenticationScheme);
        policy.RequireRole("Admin");
    });
});

/// <summary>
/// Registra o servico de autenticacao Windows no container de DI.
/// </summary>
/// <remarks>
/// JUNIOR: Dependency Injection (DI) Container:
/// ===========================================
///
/// AddScoped&lt;TInterface, TImplementation&gt;():
/// - Registra WindowsAuthService para implementar IWindowsAuthService
/// - Uma nova instancia e criada POR REQUEST HTTP
/// - A mesma instancia e reutilizada dentro de um mesmo request
/// - Instancias sao descartadas ao final do request
///
/// ALTERNATIVAS:
/// - AddSingleton: Uma instancia para toda vida da aplicacao
/// - AddTransient: Nova instancia cada vez que e solicitado
/// - Scoped e o mais comum para services que guardam estado por request
///
/// O que acontece quando um controller pede IWindowsAuthService:
/// 1. ASP.NET Core ve que AuthController pede IWindowsAuthService
/// 2. Olha no container e encontra o registro
/// 3. Cria uma nova instancia de WindowsAuthService
/// 4. Injeta no construtor do controller
/// 5. Ao final do request, descarta a instancia
/// </remarks>
builder.Services.AddScoped<IWindowsAuthService, WindowsAuthService>();

/// <summary>
/// Registra o transformer de claims como Singleton.
/// </summary>
/// <remarks>
/// JUNIOR: IClaimsTransformation - Transformacao de Claims:
/// ========================================================
///
/// AddSingleton vs AddScoped:
/// - Singleton: Uma unica instancia para toda aplicacao
/// - Scoped: Uma instancia por request
///
/// Por que Singleton aqui?
/// - O WindowsClaimsTransformer nao guarda estado por request
/// - Ele apenas transforma o principal (metodo TransformAsync)
/// - Nao ha problema de concorrencia porque o dicionario UserAdGroups e readonly
/// - Singleton e mais eficiente (nao cria novo objeto a cada request)
///
/// CUIDADO: Se sua implementacao de IClaimsTransformation guardar estado,
/// use Scoped para evitar problemas com multithreading!
///
/// MULTITHREADING NOTA:
/// O ASP.NET Core pode processar multiplos requests simultaneamente.
/// Se WindowsClaimsTransformer tivesse estado mutavel, precisaria ser thread-safe.
/// A implementacao atual e stateless (somente le o dicionario estatico).
/// </remarks>
builder.Services.AddSingleton<IClaimsTransformation, WindowsClaimsTransformer>();

builder.Services.AddControllers();

var app = builder.Build();

/// <summary>
/// Configura o pipeline de request HTTP.
/// </summary>
/// <remarks>
/// JUNIOR: Pipeline de Request HTTP:
/// =================================
///
/// O pipeline e uma serie de middleware componentes que processam cada request.
/// A ordem em que voce adiciona middleware IMPORTA!
///
/// ORDEM DO PIPELINE DESTE PROJETO:
/// 1. (se Development) MapOpenApi() - Gera documentacao Swagger
/// 2. UseAuthentication() - Valida credenciais e cria ClaimsPrincipal
/// 3. UseAuthorization() - Verifica se usuario pode acessar o recurso
/// 4. MapControllers() - Executa o controller correto
///
/// FLOW DE UM REQUEST:
/// ==================
/// Request -&gt; [OpenApi if dev] -&gt; AuthenticationMiddleware
///                                      |
///                                      v (se autenticado)
///                               ClaimsTransformer
///                                      |
///                                      v
///                              AuthorizationMiddleware
///                                      |
///                                      v (se autorizado)
///                               Seu Controller
///                                      |
///                                      v
///                              Response ao cliente
///
/// IMPORTANTE: middleware de autenticacao DEVE vir antes de authorization!
/// Se voce inverter, o authorization tera um principal vazio.
/// </remarks>

// Configura pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/// <summary>
/// Endpoint de health check na raiz do servico.
/// </summary>
/// <remarks>
/// JUNIOR: Health Check / Root Endpoint:
/// =====================================
///
/// Este endpoint na "/" retorna status da aplicacao.
/// E util para:
/// - Verificar se a aplicacao esta rodando (monitoring)
/// - Load balancers usam para verificar se servidor esta saudavel
/// - Debug inicial para confirmar que a aplicacao iniciou
///
/// Note que este endpoint usa Results.Ok() - forma mais concisa
/// de retornar JSON sem criar uma classe wrapper.
/// </remarks>
app.MapGet("/", () => Results.Ok(new
{
    service = "AuthLabs.Windows",
    authentication = "Windows Authentication (Negotiate/Kerberos/NTLM)",
    status = "running"
}));

/// <summary>
/// Inicia a aplicacao.
/// </summary>
/// <remarks>
/// JUNIOR: app.Run():
/// ================
///
/// Este metodo BLOQUEIA a thread principal.
/// A aplicacao fica rodando ate ser encerrada (Ctrl+C, kill, etc).
///
/// Em producao, voce tipicamente usa:
/// - IIS (Internet Information Services) no Windows
/// - Kestrel standalone
/// - Container Docker
/// - IIS Express para desenvolvimento
///
/// Para DESENVOLVIMENTO em Windows:
/// 1. Visual Studio: F5 para debug
/// 2. VS Code: dotnet run
/// 3. dotnet watch run: reinicia automaticamente ao mudar arquivos
///
/// REQUISITOS PARA WINDOWS AUTHENTICATION REAL:
/// 1. Servidor deve estar no dominio AD (ou ter relacao de confiança)
/// 2. Kestrel/IIS deve ter Windows Authentication habilitada
/// 3. Cliente deve estar no mesmo dominio (ou dominios com confiança)
/// 4. Browser deve suportar Integrated Windows Authentication
///    (IE, Edge, Chrome - Firefox requer configuracao manual)
/// </remarks>
app.Run();
