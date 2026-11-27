using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VetKlinik.Dtos;
using VetKlinik.Models;

namespace VetKlinik.Services;

/// <summary>
/// REST API ve MCP istemcileri için JWT üretir. İmza anahtarı depoda DEĞİLDİR:
/// "Jwt:Key" yapılandırmasından (user-secrets / ortam değişkeni) okunur, yoksa App_Data altında üretilir.
/// </summary>
public class TokenService(IConfiguration config, UserManager<ApplicationUser> users)
{
    public const string Issuer = "VetKlinik";
    public const string Audience = "VetKlinik";

    public static SymmetricSecurityKey AnahtarGetir(IConfiguration config, IWebHostEnvironment env)
    {
        var key = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            // Geliştirme kolaylığı: anahtar yoksa rastgele üretip git'e girmeyen App_Data klasörüne yaz.
            var yol = Path.Combine(env.ContentRootPath, "App_Data", "jwt.key");
            Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
            if (!File.Exists(yol))
                File.WriteAllText(yol, Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)));
            key = File.ReadAllText(yol).Trim();
            config["Jwt:Key"] = key;
        }
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public async Task<TokenDto> UretAsync(ApplicationUser user)
    {
        var roller = await users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? user.UserName ?? ""),
        };
        claims.AddRange(roller.Select(r => new Claim(ClaimTypes.Role, r)));

        var sonu = DateTime.UtcNow.AddDays(7);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = Issuer,
            Audience = Audience,
            Expires = sonu,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
        return new TokenDto(token, sonu);
    }
}
