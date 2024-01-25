using System.Security.Claims;
using backend.DTOs;
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

    public string GenerisiTokenDadilja(Dadilja dadilja){
        List<Claim> claims = new List<Claim>{
            new Claim(ClaimTypes.Name, dadilja.Email),
            new Claim(ClaimTypes.Email, dadilja.Email),
            new Claim(ClaimTypes.Role, "dadilja")
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

    public string GenerisiTokenKorisnik(Korisnik korisnik){
        List<Claim> claims = new List<Claim>{
            new Claim(ClaimTypes.Name, korisnik.Email),
            new Claim(ClaimTypes.Email, korisnik.Email),
            new Claim(ClaimTypes.Role, "korisnik")
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