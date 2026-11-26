using System.Text.Json;
using System.Text.Json.Serialization;

namespace CollegeSupport.Core.Contracts;

/// <summary>Единые настройки JSON для сервера и клиента: camelCase, перечисления строками.</summary>
public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
