using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthLabs.Shared.Data;
using AuthLabs.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthLabs.Jwt.Services;

// ============================================================================
// JwtSettings - Configurações do JWT
// ============================================================================

/// <summary>
/// Agrupa todas as configurações necessárias para o JWT funcionar.
/// Estas configurações vem do appsettings.json ou variáveis de ambiente.
///
/// JUNIOR: Pense nestas configurações como "as instruções de preparo" -
/// sem elas, o JWT não sabe como criar ou validar tokens.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Chave secreta usada para assinar digitalmente o token.
    /// JUNIOR: É como a "senha mestra" - quem conhece esta chave pode criar
    /// tokens válidos. Nunca exponha esta chave em código público!
    /// Deve ter pelo menos 32 caracteres para HMAC-SHA256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de quem emitiu o token (Issuer).
    /// JUNIOR: É como o "remetente" no envelope de uma carta.
    /// Usado para verificar se o token foi realmente emitido por nós.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de quem pode receber o token (Audience).
    /// JUNIOR: É como o "destinatário" no envelope. Garante que o token
    /// foi emitido para a aplicação correta.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Quanto tempo o access token fica válido, em minutos.
    /// JUNIOR: 15 minutos é um bom balanceamento entre segurança (token
    /// expira rápido) e experiência do usuário (não precisa renovar demais).
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Quanto tempo o refresh token fica válido, em dias.
    /// JUNIOR: O refresh token é mais longo porque é usado para obter
    /// novos access tokens sem precisar fazer login novamente.
    /// 7 dias significa que o usuário pode ficar "ausente" até 7 dias.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

// ============================================================================
// IJwtService - Interface para o serviço de JWT
// ============================================================================

/// <summary>
/// Interface pública do serviço de JWT.
/// JUNIOR: Interfaces definem um "contrato" - qualquer classe que implemente
/// esta interface promete implementar todos estes métodos.
/// Usamos interfaces para poder trocar a implementação sem mudar o código
/// que a usa (isso se chama "inversão de controle" ou "duck typing").
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gera um access token JWT para o usuário.
    /// </summary>
    /// <param name="user">O usuário para quem gerar o token</param>
    /// <param name="additionalClaims">Claims extras além das padrão (ex: roles)</param>
    /// <returns>String do token JWT codificado</returns>
    string GenerateAccessToken(User user, IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// Gera um refresh token aleatório e seguro.
    /// JUNIOR: Refresh token não é JWT - é só uma string aleatória
    /// que armazenamos no banco para validar depois.
    /// </summary>
    /// <returns>String aleatória de 44 caracteres (base64)</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida um access token e retorna as informações do usuário.
    /// </summary>
    /// <param name="token">O token JWT a validar</param>
    /// <returns>ClaimsPrincipal com as claims do usuário, ou null se inválido</returns>
    ClaimsPrincipal? ValidateToken(string token);
}

// ============================================================================
// JwtService - Implementação do serviço de JWT
// ============================================================================

/// <summary>
/// Implementação do serviço de geração e validação de JWT tokens.
///
/// ARQUITETURA DO JWT:
/// O JWT é composto por 3 partes separadas por ponto (.):
///   HEADER.PAYLOAD.SIGNATURE
///
/// 1. HEADER: Metadados como tipo de token e algoritmo de assinatura
/// 2. PAYLOAD: Os dados (claims) que queremos transmitir - VISÍVEL a todos!
/// 3. SIGNATURE: Assinatura digital que prova que o token não foi adulterado
///
/// JUNIOR: Pense num JWT como um "diploma encadernado":
/// - Qualquer um pode ler o conteúdo (payload)
/// - Mas só quem tem a chave correta pode verificar se é autêntico
/// - E só quem emitiu pode criar um novo diploma válido
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    /// <summary>
    /// Chave de assinatura pré-computada para reuse.
    /// JUNIOR: Criamos uma vez no construtor para não recriar a cada
    /// operação - melhora performance.
    /// </summary>
    private readonly SymmetricSecurityKey _securityKey;

    /// <summary>
    /// Handler para processar tokens JWT.
    /// JUNIOR: JwtSecurityTokenHandler é a "máquina" que sabe ler e criar
    /// tokens JWT. Ela converte o formato texto (base64) em objetos e vice-versa.
    /// </summary>
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Parâmetros de validação - configurados uma vez e reuse.
    /// JUNIOR: Estes parâmetros definem REGRAS de validação:
    /// - IssuerSigningKey: cual chave usar (a nossa secreta)
    /// - ValidateIssuer/Audience: verificar remetente/destinatário
    /// - ValidateLifetime: rejeitar tokens expirados
    /// - ClockSkew: tolerância de tempo (0 = sem tolerância)
    /// </summary>
    private readonly TokenValidationParameters _validationParameters;

    public JwtService(JwtSettings settings)
    {
        _settings = settings;

        // Converter a chave secreta de texto para bytes e criar chave simétrica
        // JUNIOR: SymmetricSecurityKey = chave que usa o MESMO algoritmo
        // para criptografar e descriptografar (mais simples que chave pública/privada)
        _securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));

        // Criar o handler uma única vez - ele é thread-safe
        _tokenHandler = new JwtSecurityTokenHandler();

        // Configurar parâmetros de validação uma única vez
        // Estes valores não mudam durante a execução, então podemos criar uma vez
        _validationParameters = new TokenValidationParameters
        {
            // Verificar se a assinatura foi feita com nossa chave secreta
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,

            // Verificar se o emissor do token é o que esperamos
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,

            // Verificar se o token foi emitido para nós
            ValidateAudience = true,
            ValidAudience = _settings.Audience,

            // Verificar se o token ainda não expirou
            ValidateLifetime = true,

            // Sem tolerância de tempo - se expirou, expirou
            // JUNIOR: ClockSkew permite uma margem de erro no tempo.
            // Ex: 5 segundos = aceita tokens que expiraram há até 5s
            // Colocamos 0 porque preferimos rejeitar borderline do que aceitar algo suspeito
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Gera um Access Token JWT para o usuário.
    ///
    /// FLUXO:
    /// 1. Criar lista de claims (dados do usuário)
    /// 2. Adicionar claims extras (roles, permissões)
    /// 3. Criar credenciais de assinatura
    /// 4. Construir o token com todas as informações
    /// 5. Serializar para string
    ///
    /// JUNIOR: Claims são "declarações sobre o usuário".
    /// Ex: "este usuário tem email=user@email.com", "este usuário é Admin".
    /// Claims são públicos no JWT - não coloque senhas ou dados sensíveis!
    /// </summary>
    public string GenerateAccessToken(User user, IEnumerable<Claim>? additionalClaims = null)
    {
        // Lista de claims básicas que todo token terá
        // JUNIOR: ClaimTypes contém constantes padronizadas para tipos comuns:
        // - JwtRegisteredClaimNames.Sub = identificador único do usuário
        // - JwtRegisteredClaimNames.Email = email do usuário
        // - JwtRegisteredClaimNames.Jti = ID único deste token (para tracking/revogação)
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Se há claims extras (como roles), adicionar à lista
        // JUNIOR: O parâmetro "additionalClaims" é opcional (pode ser null).
        // Por isso verificamos antes de usar.
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        // Criar credenciais de assinatura usando HMAC-SHA256
        // JUNIOR: SigningCredentials combina a chave + algoritmo.
        // "HmacSha256" é o algoritmo - seguro e rápido.
        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

        // Construir o token JWT
        // JUNIOR: JwtSecurityToken é a "classe modelo" do token antes de serializar.
        // Aqui definimos: quem emite, para quem, o que contém, quando expira, como assina.
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        // Serializar o token para string base64
        // JUNIOR: WriteToken converte o objeto JwtSecurityToken em string.
        // A string tem formato: "header.payload.signature" (cada parte é base64)
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gera um refresh token seguro e aleatório.
    ///
    /// DIFERENÇA ENTRE ACCESS TOKEN E REFRESH TOKEN:
    /// - Access Token: curta duração (15min), contém informações do usuário,
    ///   pode ser lido pelo cliente, usado para acessar recursos
    /// - Refresh Token: longa duração (7 dias), não contém dados do usuário,
    ///   só um ID aleatório, usado para obter novos access tokens
    ///
    /// JUNIOR: O refresh token é como a "chave reserva" do carro.
    /// Você guarda em lugar seguro e usa só quando perde a chave principal.
    /// Se alguém roubar a chave reserva, você pode revogá-la sem mudar a fechadura.
    ///
    /// SEGURANÇA:
    /// - 32 bytes aleatórios = 256 bits de entropia
    /// - Base64 = representação textual segura
    /// - Nunca em texto puro - só hash no banco!
    /// </summary>
    public string GenerateRefreshToken()
    {
        // Gerar 32 bytes pseudo-aleatórios criptograficamente seguros
        // JUNIOR: RandomNumberGenerator é "seguro para criptografia"
        // Diferente de Random comum - não é previsível
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Converter para base64 = string de 44 caracteres
        // JUNIOR: Base64 é uma forma de representar dados binários como texto.
        // Por que não hex? Base64 é mais compacto (4 chars vs 8 para 4 bytes)
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Valida um access token e retorna as claims do usuário.
    ///
    /// FLUXO DE VALIDAÇÃO:
    /// 1. Parsear a string do token
    /// 2. Verificar assinatura com nossa chave
    /// 3. Verificar issuer e audience
    /// 4. Verificar se não expirou
    /// 5. Retornar as claims se tudo OK, ou null se falhou
    ///
    /// JUNIOR: ValidateToken faz TUDO isso automaticamente com os parâmetros
    /// que configuramos no construtor. Se qualquer verificação falhar,
    /// uma exceção é lancada (que capturamos e retornamos null).
    ///
    /// NOTA DE SEGURANÇA: Este método retorna null em caso de falha.
    /// Num sistema real, você deveria LOGAR a tentativa de token inválido
    /// para detectar ataques. O catch vazio aqui é uma simplificação.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            // ValidateToken verifica: assinatura, issuer, audience, lifetime
            // out _ = ignoramos o token validado (poderia usar para logging)
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return principal;
        }
        catch
        {
            // Se qualquer exceção (expirado, assinatura inválida, etc), retorna null
            // JUNIOR: Num sistema real,logged esta tentativa para auditoria!
            // token tampering, replay attacks, etc podem ser detectados nos logs
            return null;
        }
    }
}

// ============================================================================
// IAuthService - Interface para o serviço de autenticação
// ============================================================================

/// <summary>
/// Interface do serviço de operações de autenticação.
///
/// JUNIOR: Esta interface é separada do IJwtService porque cuida de
/// "negócio" (login, logout, refresh) enquanto IJwtService cuida só de
/// "criptografia" (gerar/validar tokens).
/// Isso segue o princípio SRP (Single Responsibility Principle).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica usuário com email e senha.
    /// </summary>
    /// <param name="email">Email do usuário</param>
    /// <param name="password">Senha do usuário</param>
    /// <returns>Tupla de tokens ou null se autenticação falhar</returns>
    Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password);

    /// <summary>
    /// Renova tokens usando refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token válido</param>
    /// <returns>Novos tokens ou null se refresh token inválido/revogado</returns>
    Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoga um refresh token (logout).
    /// </summary>
    /// <param name="refreshToken">Refresh token a revogar</param>
    /// <returns>True se token existia e foi revogado, false se não encontrado</returns>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
}

// ============================================================================
// AuthService - Implementação do serviço de autenticação
// ============================================================================

/// <summary>
/// Implementação do serviço de autenticação.
///
/// RESPONSABILIDADES:
/// 1. Validar credenciais do usuário (email + senha)
/// 2. Gerenciar lifecycle dos refresh tokens (criar, revogar)
/// 3. Coordenar entre UserManager (banco) e JwtService (tokens)
///
/// FLUXO DE LOGIN:
///   Cliente → POST /api/auth/login (email, senha)
///          → AuthService.LoginAsync
///          → UserManager valida email+senha
///          → JwtService gera access token
///          → JwtService gera refresh token
///          → Refresh token salvo no banco
///          → Retorna ambos os tokens ao cliente
///
/// JUNIOR: AuthService é o "orquestrador" - ele coordena outras peças
/// (UserManager, JwtService, DbContext) para fazer algo maior.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;

    public AuthService(
        UserManager<User> userManager,
        IJwtService jwtService,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Autentica usuário e retorna tokens de acesso.
    ///
    /// PASSOS:
    /// 1. Buscar usuário por email
    /// 2. Verificar senha
    /// 3. Coletar claims e roles do usuário
    /// 4. Gerar access token com essas informações
    /// 5. Gerar refresh token
    /// 6. Salvar refresh token no banco
    /// 7. Retornar ambos tokens
    ///
    /// JUNIOR: Por que salvar refresh token no banco?
    /// Porque podemos revogá-lo depois! Se o token só existisse no cliente,
    /// não haveria forma de "invalidar" um login antigo.
    /// </summary>
    public async Task<(string accessToken, string refreshToken)?> LoginAsync(string email, string password)
    {
        // Buscar usuário pelo email
        // JUNIOR: FindByEmailAsync é método do UserManager que busca no banco
        // Retorna null se não encontrar - por isso verificamos antes de usar
        var user = await _userManager.FindByEmailAsync(email);

        // Verificar se usuário existe E senha está correta
        // JUNIOR: CheckPasswordAsync faz hash da senha fornecida e compara
        // com o hash armazenado. Não armazenamos senhas em texto plain!
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            // Falha - retornar null indica que autenticação falhou
            return null;
        }

        // Coletar claims personalizados do usuário
        // JUNIOR: Claims são "atributos" do usuário definidos no Identity.
        // Ex: claims customizados como "Departamento=TI"
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();

        // Coletar roles do usuário e converter para claims de role
        // JUNIOR: Roles são "papéis" (Admin, User, Manager, etc).
        // Convertemos para ClaimTypes.Role porque é o formato padrão
        // que o ASP.NET Core Identity entende para autorização.
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Gerar tokens
        // JUNIOR: Access token inclui as claims para que o cliente
        // possa ler informações do usuário diretamente do token.
        // Refresh token é só um ID aleatório, não contém dados.
        var accessToken = _jwtService.GenerateAccessToken(user, claims);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Salvar refresh token no banco para poder revogá-lo depois
        // JUNIOR: RefreshToken é uma entidade do banco com:
        // - Token: o valor em si (em produção, guardar HASH, não plain!)
        // - UserId: vínculo com o usuário
        // - ExpiresAt: quando expira (para não aceitar tokens velhos)
        // - IsRevoked: se foi revogado (logout)
        // - CreatedAt: quando foi criado
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
            // CreatedAt tem valor padrão de DateTime.UtcNow no modelo
        };
        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return (accessToken, refreshToken);
    }

    /// <summary>
    /// Renova tokens usando refresh token.
    ///
    /// FLUXO DE REFRESH:
    /// 1. Buscar refresh token no banco
    /// 2. Verificar se é válido (não expirado, não revogado)
    /// 3. "Rotacionar" tokens: revogar o antigo, criar novo par
    /// 4. Retornar novos tokens
    ///
    /// JUNIOR: Por que "rotacionar"?
    /// - Segurança! Se alguém roubou seu refresh token e usou antes de você,
    ///   você detecta porque o token foi revogado.
    /// - O atacante fica com token revogado, você com novo válido.
    /// - Isso se chama "refresh token rotation" e é best practice.
    ///
    /// TRANSAÇÃO:
    /// Este método faz 2 operações no banco:
    /// 1. Marcar token antigo como IsRevoked = true
    /// 2. Inserir novo refresh token
    /// Se a segunda falhar, o usuário fica sem refresh token válido!
    /// Em produção, usar transação para garantir atomicidade.
    /// </summary>
    public async Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string refreshToken)
    {
        // Buscar token no banco com informações do usuário (Include)
        // JUNIOR: Include() faz "eager loading" - busca o usuário junto
        // para não precisarmos de outra query depois.
        //
        // Condições do token válido:
        // - Token corresponde ao fornecido
        // - Não foi revogado (logout)
        // - Não expirou
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == refreshToken &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);

        // Token não encontrado ou inválido
        if (storedToken == null)
        {
            return null;
        }

        // ROTATION: Marcar token antigo como revogado
        // JUNIOR: Isto "consome" o token antigo - não pode ser usado de novo.
        // Se alguém interceptou e usou, você ainda tem o novo e o atacante
        // não pode mais usar o token roubado.
        storedToken.IsRevoked = true;

        // Obter usuário associado ao token
        // JUNIOR: O Include() acima já carregou o usuário, então não
        // precisamos de outra query - acessamos via navegação.
        var user = storedToken.User;

        // Recriar claims e roles (mesmo processo do login)
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Gerar novos tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user, claims);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Salvar novo refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return (newAccessToken, newRefreshToken);
    }

    /// <summary>
    /// Revoga refresh token (efetivamente faz logout).
    ///
    /// JUNIOR: Logout com JWT stateless funciona assim:
    /// - Access token ainda é válido até expirar (15 min)
    /// - Refresh token é revogado, então não pode mais renovar
    /// - Após 15 min, o access token expira e o usuário precisa fazer login
    ///
    /// Se você precisar de logout "instantâneo" para access token,
    /// precisaria de uma "blocklist" de tokens revogados no Redis.
    /// </summary>
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        // Buscar token no banco
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        // Não encontrou = já foi revogado ou nunca existiu
        if (storedToken == null)
        {
            return false;
        }

        // Marcar como revogado
        storedToken.IsRevoked = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}