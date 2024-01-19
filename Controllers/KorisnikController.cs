[ApiController]
[Route("[controller]")]
public class KorisnikController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    

    public KorisnikController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
    }
    

    [HttpGet("PreuzmiKorisnike")]
    public async Task<ActionResult> PreuzmiKorisnike()
    {
        try
        {
            var query = _client.Cypher.Match("(p:Korisnik)").Return(p => new
            {
                p.As<Korisnik>().Ime,
                p.As<Korisnik>().Prezime,
                p.As<Korisnik>().DatumRodjenja,
                p.As<Korisnik>().Email,
                p.As<Korisnik>().Drzava,
                p.As<Korisnik>().Grad,
                p.As<Korisnik>().BrojTelefona

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest("Neuspesno preuzimanje korisnika.");
        }
    }

    
    [HttpPost("DodajKorisnika/{ime}/{prezime}/{email}/{password}/{drzava}/{grad}/{datum}/{pol}/{broj}")]
    public async Task<ActionResult> DodajKorisnika(string ime, string prezime,
        string email, string password, string drzava, string grad, DateTime datum,
        string pol, string broj)
    {
        try
        {   
            Korisnik novi = new Korisnik{
                Ime = ime,
                Prezime = prezime,
                Email = email,
                Password = password,
                Drzava = drzava,
                Grad = grad,
                DatumRodjenja = datum,
                Pol = pol,
                BrojTelefona = broj
            };
           await _client.Cypher
           .Create("(k:Korisnik $noviKorisnik)")
           .WithParam("noviKorisnik", novi)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodavanje korisnika.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("DodajKorisnikaFromBody")]
    public async Task<ActionResult> DodajKorisnikaFromBody([FromBody] Korisnik korisnik)
    {
        try
        {
           await _client.Cypher
           .Create("(k:Korisnik $noviKorisnik)")
           .WithParam("noviKorisnik", korisnik)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodat korisnik.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("IzmeniImeKorisnika/{email}/{ime}")]
    public async Task<ActionResult> IzmeniImeKorisnika(string email, string ime)
    {
        try
        {
           await _client.Cypher
           .Match("(d:korisnik)")
           .Where((Korisnik k) => k.Email == email)
           .Set("k.Ime = $ime").
           WithParam("ime", ime)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjen korisnik.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpDelete("ObrisiKorisnika/{email}")]
    public async Task<ActionResult> ObrisiKorisnika(string email)
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(k:Korisnik)")
           .Where((Korisnik k) => k.Email == email)
           .DetachDelete("k")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisan korisnik.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("DodajOglasKorisnik/{email}/{opis}/{plata}/{vreme}/{vestine}")]
    public async Task<ActionResult> DodajOglasKorisnik(string email, string opis, double plata,
        string vreme, string vestine)
    {
        try
        {
            Oglas noviOglas = new Oglas{
                Opis = opis,
                Plata = plata,
                RadnoVreme = vreme,
                Vestine = vestine,
                JeDadilja = false
            };
            await _client.Cypher
                .Match("(korisnik:Korisnik)")
                .Where((Korisnik korisnik) => korisnik.Email == email)
                .Create("(korisnik)-(:OBJAVLJUJE)->(oglas:Oglas $noviOglas)")
                .WithParam("noviOglas", noviOglas)
                .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}