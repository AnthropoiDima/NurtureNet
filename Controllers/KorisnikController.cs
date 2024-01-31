using backend.Servisi.Autentifikacija;
using Newtonsoft.Json;
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

    public KorisnikController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _redis = redis;
        _redisDB = _redis.GetDatabase();
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

    [HttpGet("PreuzmiKorisnikaPoEmailu/{email}")]
    public async Task<ActionResult> PreuzmiKorisnikaPoEmailu(string email)
    {
        try
        {
            string json;
            string kljuc = "Korisnik:" + email;
            json = await _redisDB.StringGetAsync(kljuc);
            Console.WriteLine(json);
            if(string.IsNullOrEmpty(json))
            {
                var query = _client.Cypher.Match("(k:Korisnik)")
                    .Where((Korisnik k) => k.Email == email)
                    .Return(k => k.As<Korisnik>());
                var result = await query.ResultsAsync;

                json = JsonConvert.SerializeObject(result);
                await _redisDB.StringSetAsync(kljuc, json, expiry: new TimeSpan(0, 10, 0));
                return Ok(result);
            }
            else{
                List<Korisnik> result = JsonConvert.DeserializeObject<List<Korisnik>>(json);
                return Ok(result);
            }
            
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

    [HttpPut("IzmeniImeKorisnika/{email}/{ime}")]
    public async Task<ActionResult> IzmeniImeKorisnika(string email, string ime)
    {
        try
        {
            string json;
           string kljuc = "Korisnik:" + email;
           var result = await _client.Cypher
           .Match("(d:Korisnik)")
           .Where((Korisnik d) => d.Email == email)
           .Set("d.Ime = $ime")
           .WithParam("ime", ime)
           .Return(d => d.As<Korisnik>()).ResultsAsync;

            json = JsonConvert.SerializeObject(result);
            await _redisDB.StringSetAsync(kljuc, json, new TimeSpan(0, 10, 0));
           return Ok("Uspesno izmenjena dadilja.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna izmena korisnika.");
        }
    }

    [HttpDelete("ObrisiKorisnika/{email}")]
    public async Task<ActionResult> ObrisiKorisnika(string email)
    {
        try
        {
            string kljuc = "Korisnik:" + email;
           await _client.Cypher
           .OptionalMatch("(k:Korisnik)")
           .Where((Korisnik k) => k.Email == email)
           .DetachDelete("k")
           .ExecuteWithoutResultsAsync();
            
            await _redisDB.KeyDeleteAsync(kljuc);
           return Ok("Uspesno obrisan korisnik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno brisanje korisnika.");
        }
    }

    [HttpPost("DodajOglasKorisnik/{email}/{opis}/{plata}/{vreme}/{vestine}")]
    public async Task<ActionResult> DodajOglasKorisnik(string email, string opis, double plata,
        string vreme, string vestine)
    {
        try
        {
            Oglas noviOglas = new Oglas{
                Id = int.Parse(_redisDB.StringGet("BrojacOglasaID").ToString()),
                Opis = opis,
                Plata = plata,
                RadnoVreme = vreme,
                Vestine = vestine,
                JeDadilja = false,
                Oglasivac = email
            };
            await _client.Cypher
                .Match("(korisnik:Korisnik)")
                .Where((Korisnik korisnik) => korisnik.Email == email)
                .Create("(korisnik)-[:OBJAVLJUJE]->(oglas:Oglas $noviOglas)")
                .WithParam("noviOglas", noviOglas)
                .ExecuteWithoutResultsAsync();
            _redisDB.StringIncrement("BrojacOglasaID");
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno dodavanje oglasa.");
        }
    }
    
    [HttpPut("IzmeniLozinku/{email}/{novaLozinka}")]
    public async Task<ActionResult> IzmeniLozinku(string email, string novaLozinka)
    {
        try
        {
            novaLozinka = _autentifikacija.HesirajPassword(novaLozinka);
           await _client.Cypher
           .Match("(korisinik:Korisnik)")
           .Where((Korisnik korisinik) => korisinik.Email == email)
           .Set("korisinik.Password = $novaLozinka")
           .WithParam("novaLozinka", novaLozinka)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjena lozinka.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna izmena lozinke.");
        }
    }

    [HttpPut("RezervisiOglas/{email}/{oglasId}")]
    public async Task<ActionResult> RezervisiOglas(string email, int oglasId)
    {
        try
        {
           await _client.Cypher
           .Match("(korisnik:Korisnik)", "(oglas:Oglas)")
           .Where((Korisnik korisnik) => korisnik.Email == email)
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
    
    [HttpGet("PreuzmiPrijavljeneOglase/{email}")]
    public async Task<ActionResult> PreuzmiPrijavljeneOglase(string email)
    {
        try
        {
            HashEntry[] jsonSet = await _redisDB.HashGetAllAsync("PrijavljeniOglasi:" + email);
            if(jsonSet.Length == 0){
            var query = _client.Cypher
            .OptionalMatch("(korisnik:Korisnik)-[:SE_PRIJAVLJUJE]->(oglas:Oglas)")
            .Where((Korisnik korisnik) => korisnik.Email == email)
            .Return(oglas => new
            {
                oglas.As<Oglas>().Id,
                oglas.As<Oglas>().Opis,
                oglas.As<Oglas>().Plata,
                oglas.As<Oglas>().RadnoVreme,
                oglas.As<Oglas>().Vestine,
                oglas.As<Oglas>().Oglasivac
            });
            
            var result = await query.ResultsAsync;
            List<HashEntry> hashEntries = new List<HashEntry>();
            foreach (var oglas in result)
            {
                hashEntries.Add(new HashEntry("Oglas:"+ oglas.Id, JsonConvert.SerializeObject(oglas)));
            }
            await _redisDB.HashSetAsync("PrijavljeniOglasi:" + email, hashEntries.ToArray());
            return Ok(result);
            }
            else{
                List<Oglas> oglasList = new List<Oglas>();
                foreach (var json in jsonSet)
                {
                    oglasList.Add(JsonConvert.DeserializeObject<Oglas>(json.Value));
                }
                return Ok(oglasList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + " " + ex.StackTrace);
            return BadRequest("Neuspesno preuzimanje oglasa.");
        }
    }
}