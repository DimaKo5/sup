using System.Security.Cryptography;
using CollegeSupport.Core.Exceptions;

namespace CollegeSupport.Core.Security;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool Verify(string password, string hash, string salt);
}

/// <summary>Хеширование паролей по алгоритму PBKDF2 (SHA-256, 100 000 итераций, соль 16 байт).</summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] expected = Convert.FromBase64String(hash);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

/// <summary>Требования к паролю: не короче 8 символов, есть буква и цифра.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            throw new BusinessRuleException($"Пароль должен содержать не менее {MinLength} символов.");
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new BusinessRuleException("Пароль должен содержать буквы и цифры.");
    }
}
