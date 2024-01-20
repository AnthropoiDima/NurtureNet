namespace backend.Model;


public class Ocena
{
    public static int OcenaID = 0;
    public int Id { get; set; }
    public int Vrednost { get; set; }
    public String Komentar { get; set; } = null!;
    public Korisnik? Korisnik { get; set; }
    public Dadilja? Dadilja { get; set; }


    public Ocena()
    {
        Id = OcenaID++;
    }
    public Ocena(int vrednost, string komentar, Korisnik korisnik, Dadilja dadilja)
    {
        Id = OcenaID++;
        Vrednost = vrednost;
        Komentar = komentar;
        Korisnik = korisnik;
        Dadilja = dadilja;
    }
}