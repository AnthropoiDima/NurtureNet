namespace backend.Model;

public class Oglas
{
    public static int OglasID = 0;
    public int Id { get; set; }
    public String? Opis { get; set; }
    public double Plata { get; set; }
    public String? RadnoVreme { get; set; }
    public String? Vestine { get; set; }
    public bool JeDadilja { get; set; }
    public string Oglasivac { get; set; }
    public Korisnik? Korisnik { get; set; }
    public Dadilja? Dadilja { get; set; }

    public Oglas()
    {
        // Id = OglasID++;
    }

    public Oglas(string opis, double plata, string radnoVreme, string vestine, bool jeDadilja)
    {
        // Id = OglasID++;
        Opis = opis;
        Plata = plata;
        RadnoVreme = radnoVreme;
        Vestine = vestine;
        JeDadilja = jeDadilja;
    }
}