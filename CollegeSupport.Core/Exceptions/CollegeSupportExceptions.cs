namespace CollegeSupport.Core.Exceptions;

/// <summary>Базовое исключение предметной области.</summary>
public abstract class CollegeSupportException : Exception
{
    protected CollegeSupportException(string message) : base(message) { }
}

/// <summary>Объект не найден (HTTP 404).</summary>
public class NotFoundException : CollegeSupportException
{
    public NotFoundException(string entityName, object key)
        : base($"Объект «{entityName}» с идентификатором {key} не найден.") { }
}

/// <summary>Нарушено бизнес-правило (HTTP 400).</summary>
public class BusinessRuleException : CollegeSupportException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Конфликт с уже сохранёнными данными (HTTP 409).</summary>
public class ConflictException : CollegeSupportException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Неверный логин или пароль (HTTP 401).</summary>
public class AuthenticationFailedException : CollegeSupportException
{
    public AuthenticationFailedException() : base("Неверный логин или пароль.") { }
}
