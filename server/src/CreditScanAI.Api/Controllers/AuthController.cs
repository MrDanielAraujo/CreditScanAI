using System.Security.Claims;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Auth;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using CreditScanAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Autenticação real (Fase 8 Parte 1). Cadastro público entra no único
/// tenant já existente (não cria um tenant novo por cadastro) e sempre
/// nasce com o papel Analyst - promover alguém a Admin é uma ação futura de
/// gerenciamento de usuários, não algo que o próprio cadastro concede.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly AppDbContext _db;

    public AuthController(UserManager<User> userManager, IJwtTokenGenerator tokenGenerator, AppDbContext db)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _db = db;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Email e senha são obrigatórios."));
        }

        var tenantId = await _db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync(cancellationToken);
        if (tenantId == Guid.Empty)
        {
            return Conflict(ApiResponse<object>.Fail("NO_TENANT", "Nenhum tenant cadastrado."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Role = UserRole.Analyst,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var details = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Não foi possível criar o usuário.", details));
        }

        var token = _tokenGenerator.GenerateToken(user);
        return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(token, MapUser(user))));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Email ou senha inválidos."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(StatusCodes.Status423Locked, ApiResponse<object>.Fail(
                "LOCKED_OUT", "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde."));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(ApiResponse<object>.Fail("INVALID_CREDENTIALS", "Email ou senha inválidos."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var token = _tokenGenerator.GenerateToken(user);
        return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(token, MapUser(user))));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Me(CancellationToken cancellationToken)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is null || !Guid.TryParse(idClaim, out var id))
        {
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Token inválido."));
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Usuário não encontrado."));
        }

        return Ok(ApiResponse<UserSummaryDto>.Ok(MapUser(user)));
    }

    private static UserSummaryDto MapUser(User user) => new(user.Id, user.Email!, user.Name, user.Role.ToString());
}
