using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace backend.Servisi.Autentifikacija;

public class Autentifikacija
{
    private readonly IConfiguration _config;
    public Autentifikacija(IConfiguration configuration)
    {
        _config = configuration;
    }
    public string HesirajPassword(string pass){
        return BCrypt.Net.BCrypt.HashPassword(pass);
    }
    public bool ProveriPassword(string pass, string passHash){
        return BCrypt.Net.BCrypt.Verify(pass, passHash);
    }

    public string GenerisiTokenDadilja(string email){
        List<Claim> claims = new List<Claim>{
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Dadilja")
        };
        var kljuc = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value!));
        
        var cred = new SigningCredentials(kljuc, SecurityAlgorithms.HmacSha512Signature);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: cred
        );

        string jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    public string GenerisiTokenKorisnik(string email){
        List<Claim> claims = new List<Claim>{
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Korisnik")
        };
        var kljuc = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value!));
        
        var cred = new SigningCredentials(kljuc, SecurityAlgorithms.HmacSha512Signature);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: cred
        );

        string jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}