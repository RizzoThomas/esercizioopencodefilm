namespace FilmAPI.Services;

public static class RedirectUrlValidator
{
    public static bool IsValidRedirectPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (path.Contains("://") || path.StartsWith("//")) return false;
        return path.StartsWith('/');
    }

    public static string Sanitize(string? path, string fallback = "/index.html")
    {
        return IsValidRedirectPath(path) ? path! : fallback;
    }
}
