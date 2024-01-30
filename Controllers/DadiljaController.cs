using backend.Servisi.Autentifikacija;
using StackExchange.Redis;
using NRedisStack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using backend.Servisi.KorisnikFje;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("[controller]")] 
public class DadiljaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    private Autentifikacija _autentifikacija;
    private KorisnikFje _korisnikFje;

    public DadiljaController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _redis = redis;
        _redisDB = _redis.GetDatabase();
        _korisnikFje = new KorisnikFje();
    }
    
    [HttpGet("PreuzmiDadilje")]
    public async Task<ActionResult> PreuzmiDadilje()
    {
        try
        {
            var query = _client.Cypher.Match("(d:Dadilja)")
                .Return(d => new{ 
                    d.As<Dadilja>().Ime,
                    d.As<Dadilja>().Prezime,
                    d.As<Dadilja>().Email,
                    d.As<Dadilja>().Drzava,
                    d.As<Dadilja>().DatumRodjenja,
                    d.As<Dadilja>().Pol,
                    d.As<Dadilja>().BrojTelefona                                    
                });

            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesno preuzimanje podataka o dadiljama.");
        }
    }

    [HttpGet("PreuzmiDadiljuPoEmailu/{email}")]
    public async Task<ActionResult> PreuzmiDadiljuPoEmailu(string email)
    {
        try
        {
            string json;
            string kljuc = "Dadilja:" + email;
            json = await _redisDB.StringGetAsync(kljuc);
            Console.WriteLine(json);
            if (string.IsNullOrEmpty(json))
            {
                var query = _client.Cypher.Match("(d:Dadilja)")
                    .Where((Dadilja d) => d.Email == email)
                    .Return(d => d.As<Dadilja>());
                var result = await query.ResultsAsync;

                json = JsonConvert.SerializeObject(result);
                await _redisDB.StringSetAsync(kljuc, json);
                return Ok(result);
            }
            else{
                List<Dadilja> dadiljaList = JsonConvert.DeserializeObject<List<Dadilja>>(json);
                Dadilja dadilja = dadiljaList.FirstOrDefault();
                return Ok(dadilja);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesno preuzimanje dadilje.");
        }

    }

    [HttpPost("DodajDadilju/{ime}/{prezime}/{email}/{password}/{drzava}/{grad}/{datum}/{pol}/{broj}/{vestine}")]
    public async Task<ActionResult> DodajDadilju(string ime, string prezime,
        string email, string password, string drzava, string grad, DateTime datum,
        string pol, string broj, string vestine)
    {
        try
        {
            Dadilja novaDadilja = new Dadilja {
                Ime = ime,
                Prezime = prezime,
                Email = email,
                Password = password,
                Drzava = drzava,
                Grad = grad,
                DatumRodjenja = datum,
                Pol = pol,
                BrojTelefona = broj,
                Vestine = vestine
            };
           await _client.Cypher
           .Create("(dadilja:Dadilja $novaDadilja)")
           .WithParam("novaDadilja", novaDadilja)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodata dadilja.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("DodajDadiljuFromBody")] // ovu fju mozemo da stavimo samo kao admin
    public async Task<ActionResult> DodajDadiljuFromBody([FromBody] Dadilja novaDadilja)
    {
        try
        {
           await _client.Cypher
           .Create("(dadilja:Dadilja $novaDadilja)") // $ umesto {} za parametar, malkice netacna dokumentacija :)
           .WithParam("novaDadilja", novaDadilja)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodata dadilja.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [Authorize (Roles = "dadilja, admin")]
    [HttpPut("IzmeniImeDadilje/{ime}")]
    public async Task<ActionResult> IzmeniImeDadilje(string ime)
    {
        try
        {   
            string json;
            string email = _korisnikFje.GetCurrentUserEmail(User);
            string kljuc = "Dadilja:" + email;
           var result = await _client.Cypher
           .Match("(d:Dadilja)")
           .Where((Dadilja d) => d.Email == email)
           .Set("d.Ime = $ime")
           .WithParam("ime", ime)
           .Return(d => d.As<Dadilja>()).ResultsAsync;

            json = JsonConvert.SerializeObject(result);
            await _redisDB.StringSetAsync(kljuc, json);
           return Ok("Uspesno izmenjena dadilja.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna izmena profila.");
        }
    }
    [Authorize (Roles = "dadilja, admin")]
    [HttpPut("IzmeniLozinku")]
    public async Task<ActionResult> IzmeniLozinku([FromBody] string novaLozinka)
    {
        try
        {
            novaLozinka = _autentifikacija.HesirajPassword(novaLozinka);
           await _client.Cypher
           .Match("(dadilja:Dadilja)")
           .Where((Dadilja dadilja) => dadilja.Email == _korisnikFje.GetCurrentUserEmail(User))
           .Set("dadilja.Password = $novaLozinka")
           .WithParam("novaLozinka", novaLozinka)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjena lozinka.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno izmenjena lozinka.");
        }
    }
    [Authorize (Roles = "dadilja, admin")]
    [HttpDelete("ObrisiDadilju/{email}")]
    public async Task<ActionResult> ObrisiDadilju(string email)
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(d:Dadilja)")
           .Where((Dadilja d) => d.Email == _korisnikFje.GetCurrentUserEmail(User))
           .DetachDelete("d")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisan nalog.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno brisanje naloga.");
        }
    }
    [Authorize (Roles = "dadilja, admin")]
    [HttpPost("DodajOglasDadilja/{opis}/{plata}/{vreme}/{vestine}")]
    public async Task<ActionResult> DodajOglasDadilja(string opis, double plata,
        string vreme, string vestine)
    {
        try
        {
            Oglas noviOglas = new Oglas{
                Opis = opis,
                Plata = plata,
                RadnoVreme = vreme,
                Vestine = vestine,
                JeDadilja = true
            };
            await _client.Cypher
                .Match("(dadilja:Dadilja)")
                .Where((Dadilja dadilja) => dadilja.Email == _korisnikFje.GetCurrentUserEmail(User))
                .Create("(dadilja)-[:OBJAVLJUJE]->(oglas:Oglas $noviOglas)")
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
    [Authorize (Roles = "dadilja, admin")]
    [HttpPut("RezervisiOglas/{oglasId}")] //proveri s Mikom kako odgovara
    public async Task<ActionResult> RezervisiOglas(int oglasId)
    {
        try
        {
           await _client.Cypher
           .Match("(dadilja:Dadilja)", "(oglas:Oglas)")
           .Where((Dadilja dadilja) => dadilja.Email == _korisnikFje.GetCurrentUserEmail(User))
           .AndWhere((Oglas oglas) => oglas.Id == oglasId)
           .Create("(dadilja)-[:SE_PRIJAVLJUJE]->(oglas)")
           .ExecuteWithoutResultsAsync();
 
           return Ok("Uspesna prijava na oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna prijava na oglas.");
        }
    }
    [Authorize (Roles = "dadilja, admin")]
    [HttpGet("PreuzmiPrijavljeneOglase")]
    public async Task<ActionResult> PreuzmiPrijavljeneOglase()
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(dadilja:Dadilja)-[:SE_PRIJAVLJUJE]->(oglas:Oglas)")
            .Where((Dadilja dadilja) => dadilja.Email == _korisnikFje.GetCurrentUserEmail(User))
            .Return(oglas => new
            {
                oglas.As<Oglas>().Opis,
                oglas.As<Oglas>().Plata,
                oglas.As<Oglas>().RadnoVreme,
                oglas.As<Oglas>().Vestine
            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje oglasa na koje ste prijavljeni.");
        }
    }
}