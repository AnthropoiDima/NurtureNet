namespace backend.Model;
public class Ocena
{
    public int Id { get; set; }
    public int Vrednost { get; set; }
    public String Komentar { get; set; } = null!;
    public Korisnik? Korisnik { get; set; }
    public Dadilja? Dadilja { get; set; }
}