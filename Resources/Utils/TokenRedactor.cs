// Archivo: Utils/TokenRedactor.cs
namespace AppVendedores24.Utils;

public static class TokenRedactor
{
    /// <summary>
    /// Enmascara un token dejando visibles algunos caracteres al inicio y al final.
    /// Nunca loguees el token "plano".
    /// </summary>
    public static string Mask(string? token, int visiblePrefix = 3, int visibleSuffix = 3)
    {
        if (string.IsNullOrWhiteSpace(token)) return "<vacío>";

        if (token.Length <= visiblePrefix + visibleSuffix)
            return new string('•', token.Length);

        var prefix = token[..visiblePrefix];
        var suffix = token[^visibleSuffix..];
        var middleLen = token.Length - visiblePrefix - visibleSuffix;
        return $"{prefix}{new string('•', middleLen)}{suffix}";
    }
}