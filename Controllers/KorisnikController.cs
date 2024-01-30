using backend.Servisi.Autentifikacija;
using backend.Servisi.KorisnikFje;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

[ApiController]
[Route("[controller]")]
public class KorisnikController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    private readonly IConfiguration _config;
    private Autentifikacija _autentifikacija;
    private KorisnikFje _korisnikFje;

    public KorisnikController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _redis = redis;
        _redisDB = _redis.GetDatabase();
        _korisnikFje = new KorisnikFje();
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
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno dodavanje korisnika.");
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
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno dodavanje korisnika.");
        }
    }

    [Authorize(Roles = "korisnik")]
    [HttpPut("IzmeniImeKorisnika/{ime}")]
    public async Task<ActionResult> IzmeniImeKorisnika(string ime)
    {
        try
        {
           await _client.Cypher
           .Match("(d:korisnik)")
           .Where((Korisnik k) => k.Email == _korisnikFje.GetCurrentUserEmail(User))
           .Set("k.Ime = $ime").
           WithParam("ime", ime)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjen korisnik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna izmena korisnika.");
        }
    }

    [Authorize(Roles = "korisnik, admin")]
    [HttpDelete("ObrisiKorisnika")]
    public async Task<ActionResult> ObrisiKorisnika()
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(k:Korisnik)")
           .Where((Korisnik k) => k.Email == _korisnikFje.GetCurrentUserEmail(User))
           .DetachDelete("k")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisan korisnik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno brisanje profila.");
        }
    }
    [Authorize(Roles = "korisnik, admin")]
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
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno dodavanje oglasa.");
        }
    }
    [Authorize(Roles = "korisnik, admin")]
    [HttpPut("RezervisiOglas{oglasId}")]
    public async Task<ActionResult> RezervisiOglas(int oglasId)
    {
        try
        {
           await _client.Cypher
           .Match("(korisnik:Korisnik)", "(oglas:Oglas)")
           .Where((Korisnik korisnik) => korisnik.Email == _korisnikFje.GetCurrentUserEmail(User))
           .AndWhere((Oglas oglas) => oglas.Id == oglasId)
           .Create("(korisnik)-[:SE_PRIJAVLJUJE]->(oglas)")
           .ExecuteWithoutResultsAsync();
 
           return Ok("Uspesna prijava na oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna prijava na oglas.");
        }
    }
}