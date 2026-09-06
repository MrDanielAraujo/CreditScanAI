using System.Security.Claims;
using CreditScanAI.Api.Contracts;
using CreditScanAI.Api.Contracts.Users;
using CreditScanAI.Api.Services;
using CreditScanAI.Domain.Entities;
using CreditScanAI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditScanAI.Api.Controllers;

/// <summary>
/// Gerenciamento de usuários (Fase 9 / UC-08) - só Admin. Cadastro público
/// (Fase 8) continua existindo em paralelo (sempre cria Analyst); esta tela
/// é como um Admin cria alguém já com outro papel, ou ajusta papel/senha/
/// acesso de quem já existe - sem isso não havia nenhuma forma de virar
/// Reviewer/CFO/Admin/Compliance a não ser SQL direto.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.CanAdmin)]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentTenantProvider _tenantProvider;

    public UsersController(UserManager<User> userManager, ICurrentTenantProvider tenantProvider)
    {
        _userManager = userManager;
        _tenantProvider = tenantProvider;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserListItemDto ToDto(User user, bool isLockedOut) =>
        new(user.Id, user.Email!, user.Name, user.Role.ToString(), isLockedOut);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserListItemDto>>>> List(CancellationToken cancellationToken)
    {
        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var users = await _userManager.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var items = users.Select(u => ToDto(u, u.LockoutEnd is not null && u.LockoutEnd > DateTimeOffset.UtcNow)).ToList();
        return Ok(ApiResponse<List<UserListItemDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserListItemDto>>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Email e senha são obrigatórios."));
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Papel inválido."));
        }

        var tenantId = await _tenantProvider.GetCurrentTenantIdAsync(cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Role = role,
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

        return CreatedAtAction(nameof(List), ApiResponse<UserListItemDto>.Ok(ToDto(user, isLockedOut: false)));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserListItemDto>>> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (id == CurrentUserId)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Você não pode alterar o seu próprio papel."));
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Papel inválido."));
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Usuário não encontrado."));
        }

        user.Role = role;
        await _userManager.UpdateAsync(user);

        var isLockedOut = user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow;
        return Ok(ApiResponse<UserListItemDto>.Ok(ToDto(user, isLockedOut)));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Usuário não encontrado."));
        }

        // Ação de Admin autenticado, não o fluxo de "esqueci minha senha" -
        // não precisa de token de reset, o Admin já está autorizado a fazer
        // isso diretamente.
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            var details = removeResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Não foi possível redefinir a senha.", details));
        }

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
        {
            var details = addResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Não foi possível redefinir a senha.", details));
        }

        return Ok(ApiResponse<object>.Ok(new { message = "Senha redefinida com sucesso." }));
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<ActionResult<ApiResponse<object>>> Lock(Guid id, CancellationToken cancellationToken)
    {
        if (id == CurrentUserId)
        {
            return Conflict(ApiResponse<object>.Fail("CONFLICT", "Você não pode revogar o seu próprio acesso."));
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Usuário não encontrado."));
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return Ok(ApiResponse<object>.Ok(new { message = "Acesso revogado." }));
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<ActionResult<ApiResponse<object>>> Unlock(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Usuário não encontrado."));
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(ApiResponse<object>.Ok(new { message = "Acesso restaurado." }));
    }
}
