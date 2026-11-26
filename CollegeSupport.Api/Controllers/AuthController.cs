using CollegeSupport.Api.Infrastructure;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CollegeSupport.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, ITokenService tokens, ILogger<AuthController> logger)
    {
        _auth = auth;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Вход в систему: возвращает JWT-токен.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        CollegeSupport.Core.Entities.User user;
        try
        {
            user = await _auth.ValidateCredentialsAsync(dto.Login, dto.Password);
        }
        catch (AuthenticationFailedException ex)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Ошибка входа", detail: ex.Message);
        }
        var (token, expiresAt) = _tokens.CreateToken(user);
        _logger.LogInformation("Пользователь {Login} ({Role}) вошёл в систему", user.Login, user.Role);
        return new LoginResponseDto(token, expiresAt, user.ToDto());
    }
}
