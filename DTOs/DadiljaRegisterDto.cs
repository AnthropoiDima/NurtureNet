namespace backend.DTOs;

public class DadiljaRegisterDto
{
    public string? Ime { get; set; }
    public string? Prezime { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Drzava { get; set; }
    public string? Grad { get; set; }
    public DateTime DatumRodjenja { get; set; }
    public string? Pol { get; set; }
    public string? BrojTelefona { get; set; }
    public string? Vestine { get; set; }
}