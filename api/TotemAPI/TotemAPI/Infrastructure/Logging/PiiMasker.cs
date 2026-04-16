namespace TotemAPI.Infrastructure.Logging;

/// <summary>
/// Utilitário centralizado para mascarar dados PII (Personally Identifiable Information)
/// antes de escrever em logs.
/// </summary>
public static class PiiMasker
{
    /// <summary>
    /// Mascara um endereço de e-mail preservando o domínio e o primeiro caractere do usuário.
    /// Exemplo: "usuario@empresa.com" → "u***@empresa.com"
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "[vazio]";

        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0) return "***";

        var firstChar = email[0];
        var domain = email[(at + 1)..];

        // Valida mínimo de domínio para não expor inadvertidamente
        return $"{firstChar}***@{domain}";
    }

    /// <summary>
    /// Mascara um payload Pix (string longa) exibindo apenas os últimos 8 caracteres.
    /// O payload completo não deve aparecer em logs pois contém dados financeiros.
    /// </summary>
    public static string MaskPixPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return "[vazio]";
        if (payload.Length <= 8) return "***";
        return $"PIX:***:{payload[^8..]}";
    }

    /// <summary>
    /// Mascara um ID de transação financeira, exibindo apenas os últimos 6 caracteres.
    /// </summary>
    public static string MaskTransactionId(string? transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId)) return "[vazio]";
        if (transactionId.Length <= 6) return "***";
        return $"***{transactionId[^6..]}";
    }
}
