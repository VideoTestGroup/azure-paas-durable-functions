namespace ImageIngest.Functions.Extensions;

public static class EnvironmentVariablesExtensions
{
    const string NAMES_SPACES_MAPPER = "NamespacesMapper";

    public static string GetNamespaceVariable(string @namespace)
    {
        return Environment.GetEnvironmentVariable($"{NAMES_SPACES_MAPPER}:{@namespace}");
    }
}