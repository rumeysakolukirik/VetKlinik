namespace VetKlinik;

/// <summary>Kimlik doğrulama şeması ve politika adları.</summary>
public static class ApiAuth
{
    /// <summary>REST API: tarayıcıdan cookie, dış istemciden JWT kabul eden birleşik şema.</summary>
    public const string Sema = "CookieVeyaJwt";

    /// <summary>MCP ucu: yalnızca JWT.</summary>
    public const string McpPolitikasi = "Mcp";

    public static bool ApiIstegiMi(HttpRequest r) =>
        r.Path.StartsWithSegments("/api") || r.Path.StartsWithSegments("/mcp");
}
