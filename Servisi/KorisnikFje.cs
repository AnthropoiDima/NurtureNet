using System.Security.Claims;

namespace backend.Servisi.KorisnikFje;

public class KorisnikFje{
    public string? GetCurrentUserEmail(ClaimsPrincipal korisnik){
    var idKorisnikaObj = korisnik.Claims.Where(v=>v.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").Select(p=>new string(p.Value));
    if(idKorisnikaObj == null) {Console.WriteLine("null"); return null;}
        string idKorisnika = idKorisnikaObj.ElementAtOrDefault(0)!;
        if(string.IsNullOrWhiteSpace(idKorisnika)){
            return null;
        }
        return idKorisnika;
        
    }

    public bool CheckIfAdmin(ClaimsPrincipal korisnik){
        var ulogaKorisnikaObj = korisnik.Claims.Where(v=>v.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(p=>new string(p.Value));
        string ulogaKorisnika = ulogaKorisnikaObj.ElementAtOrDefault(0)!;
        if(String.IsNullOrWhiteSpace(ulogaKorisnika) || ulogaKorisnika != "admin"){
            return false;
        }
        return true;
    }
}