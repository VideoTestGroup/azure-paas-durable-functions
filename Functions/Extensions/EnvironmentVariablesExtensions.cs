namespace ImageIngest.Functions.Extensions;

public static class EnvironmentVariablesExtensions
{
    const string NAMES_SPACES_MAPPER = "Mapper";

    public static string GetNamespaceVariable(string @namespace)
    {
        return Environment.GetEnvironmentVariable($"{@namespace}{NAMES_SPACES_MAPPER}");
    }
}