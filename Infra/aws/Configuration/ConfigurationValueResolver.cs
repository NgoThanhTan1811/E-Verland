using Microsoft.Extensions.Configuration;

namespace Infra.AWS.Configuration;

internal static class ConfigurationValueResolver
{
    public static string? GetOptional(IConfiguration configuration, string configKey, string? envVarName = null)
    {
        var value = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!string.IsNullOrWhiteSpace(envVarName))
        {
            value = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static string GetRequired(IConfiguration configuration, string configKey, string envVarName)
    {
        return GetOptional(configuration, configKey, envVarName)
            ?? throw new InvalidOperationException($"Missing required configuration '{configKey}' (or environment variable '{envVarName}').");
    }
}
