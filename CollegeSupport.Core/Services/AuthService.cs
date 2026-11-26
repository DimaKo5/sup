using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Interfaces;
using CollegeSupport.Core.Security;

namespace CollegeSupport.Core.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AuthService(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    /// <summary>Проверяет логин и пароль. Сообщение об ошибке одинаковое, чтобы не раскрывать существующие логины.</summary>
    public async Task<User> ValidateCredentialsAsync(string login, string password)
    {
        var user = await _users.GetByLoginAsync(login.Trim());
        if (user is null || !user.IsActive || !_hasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            throw new AuthenticationFailedException();
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        PasswordPolicy.Validate(dto.Password);
        if (await _users.GetByLoginAsync(dto.Login.Trim()) is not null)
            throw new BusinessRuleException($"Логин «{dto.Login}» уже занят.");

        var (hash, salt) = _hasher.HashPassword(dto.Password);
        var user = new User
        {
            Login = dto.Login.Trim(),
            FullName = dto.FullName.Trim(),
            Role = dto.Role,
            PasswordHash = hash,
            PasswordSalt = salt
        };
        await _users.AddAsync(user);
        return user;
    }
}
