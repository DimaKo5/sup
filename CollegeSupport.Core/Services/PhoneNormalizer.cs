using CollegeSupport.Core.Exceptions;

namespace CollegeSupport.Core.Services;

/// <summary>Приводит российский номер телефона к виду +7XXXXXXXXXX.</summary>
public static class PhoneNormalizer
{
    public static string Normalize(string? phone)
    {
        string digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 10)
            digits = "7" + digits;
        else if (digits.Length == 11 && digits[0] == '8')
            digits = "7" + digits[1..];

        if (digits.Length != 11 || digits[0] != '7')
            throw new BusinessRuleException($"Некорректный номер телефона: «{phone}».");

        return "+" + digits;
    }
}
