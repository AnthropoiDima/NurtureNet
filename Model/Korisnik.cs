namespace backend.Model;

public class Korisnik
{
    public String Ime { get; set; } = null!;
    public String Prezime { get; set; } = null!;
    public String Email { get; set; } = null!;
    public String Password { get; set; } = null!;
    public String Drzava { get; set; } = null!;
    public String Grad { get; set; } = null!;
    public String Pol { get; set; } = null!;
    public String BrojTelefona { get; set; } = null!;
    public DateTime DatumRodjenja { get; set; }
    
    [JsonIgnore]
    public ICollection<Ocena> OcenePrimeljene { get; set; } = new List<Ocena>();
    [JsonIgnore]
    public ICollection<Ocena> OceneOstavljene { get; set; } = new List<Ocena>();
    [JsonIgnore]
    public ICollection<Oglas> Oglasi { get; set; } = new List<Oglas>();
    [JsonIgnore]
    public ICollection<Oglas> SacuvaniOglasi { get; set; } = new List<Oglas>();
}