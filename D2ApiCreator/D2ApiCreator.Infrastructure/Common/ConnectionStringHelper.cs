namespace D2ApiCreator.Infrastructure.Common;

/// <summary>
/// Helper class for safely working with connection strings
/// </summary>
public static class ConnectionStringHelper
{
    /// <summary>
    /// Redacts sensitive information from connection string for logging
    /// </summary>
    public static string RedactConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "[Empty]";

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var redactedParts = new List<string>();

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLowerInvariant();
                var value = keyValue[1].Trim();

                // Redact sensitive keys
                if (key.Contains("password") || key.Contains("pwd") || 
                    key.Contains("secret") || key.Contains("token"))
                {
                    redactedParts.Add($"{keyValue[0]}=***");
                }
                else
                {
                    redactedParts.Add(part);
                }
            }
            else
            {
                redactedParts.Add(part);
            }
        }

        return string.Join("; ", redactedParts);
    }

    /// <summary>
    /// Validates if connection string has required components
    /// </summary>
    public static (bool isValid, string errorMessage) ValidateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Connection string is empty");

        var requiredKeys = new[] { "host", "database" };
        var lowerConnectionString = connectionString.ToLowerInvariant();

        foreach (var key in requiredKeys)
        {
            if (!lowerConnectionString.Contains(key))
                return (false, $"Connection string is missing required parameter: {key}");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Gets database name from connection string
    /// </summary>
    public static string? GetDatabaseName(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim().ToLowerInvariant();
                if (key == "database" || key == "initial catalog")
                {
                    return keyValue[1].Trim();
                }
            }
        }

        return null;
    }
}

