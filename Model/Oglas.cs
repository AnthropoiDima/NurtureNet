namespace backend.Model;

public class Oglas
{
    public int Id { get; set; }
    public String? Opis { get; set; }
    public double Plata { get; set; }
    public String? RadnoVreme { get; set; }
    public String? Vestine { get; set; }
    public bool JeDadilja { get; set; }
    public Korisnik? Korisnik { get; set; }
    public Dadilja? Dadilja { get; set; }
}