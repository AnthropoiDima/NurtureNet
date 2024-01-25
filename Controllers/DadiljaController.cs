using backend.Servisi.Autentifikacija;
using StackExchange.Redis;
using NRedisStack;

[ApiController]
[Route("[controller]")] 
public class DadiljaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    private Autentifikacija _autentifikacija;

    public DadiljaController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _redis = redis;
        _redisDB = _redis.GetDatabase();
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

            _redisDB.StringSet("foo", "bar");

            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("PreuzmiDadiljuPoEmailu/{email}")]
    public async Task<ActionResult> PreuzmiDadiljuPoEmailu(string email)
    {
        try
        {
            
            var query = await _client.Cypher
                .Match("(d:Dadilja)")
                .Where((Dadilja d) => d.Email == email)
                .Return(d => d.As<Dadilja>()).ResultsAsync;

            return Ok(query);
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

    [HttpPost("DodajDadiljuFromBody")]
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

    [HttpPut("IzmeniImeDadilje/{email}/{ime}")]
    public async Task<ActionResult> IzmeniImeDadilje(string email, string ime)
    {
        try
        {
           await _client.Cypher
           .Match("(d:Dadilja)")
           .Where((Dadilja d) => d.Email == email)
           .Set("d.Ime = $ime")
           .WithParam("ime", ime)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjena dadilja.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("IzmeniLozinku/{email}/{novaLozinka}")]
    public async Task<ActionResult> IzmeniLozinku(string email, string novaLozinka)
    {
        try
        {
            novaLozinka = _autentifikacija.HesirajPassword(novaLozinka);
           await _client.Cypher
           .Match("(dadilja:Dadilja)")
           .Where((Dadilja dadilja) => dadilja.Email == email)
           .Set("dadilja.Password = $novaLozinka")
           .WithParam("novaLozinka", novaLozinka)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjena lozinka.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("ObrisiDadilju/{email}")]
    public async Task<ActionResult> ObrisiDadilju(string email)
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(d:Dadilja)")
           .Where((Dadilja d) => d.Email == email)
           .DetachDelete("d")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisana dadilja.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("DodajOglasDadilja/{email}/{opis}/{plata}/{vreme}/{vestine}")]
    public async Task<ActionResult> DodajOglasDadilja(string email, string opis, double plata,
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
                .Where((Dadilja dadilja) => dadilja.Email == email)
                .Create("(dadilja)-[:OBJAVLJUJE]->(oglas:Oglas $noviOglas)")
                .WithParam("noviOglas", noviOglas)
                .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("RezervisiOglas/{email}/{oglasId}")]
    public async Task<ActionResult> RezervisiOglas(string email, int oglasId)
    {
        try
        {
           await _client.Cypher
           .Match("(dadilja:Dadilja)", "(oglas:Oglas)")
           .Where((Dadilja dadilja) => dadilja.Email == email)
           .AndWhere((Oglas oglas) => oglas.Id == oglasId)
           .Create("(dadilja)-[:SE_PRIJAVLJUJE]->(oglas)")
           .ExecuteWithoutResultsAsync();
 
           return Ok("Uspesna prijava na oglas.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("PreuzmiPrijavljeneOglase/{email}")]
    public async Task<ActionResult> PreuzmiPrijavljeneOglase(string email)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(dadilja:Dadilja)-[:SE_PRIJAVLJUJE]->(oglas:Oglas)")
            .Where((Dadilja dadilja) => dadilja.Email == email)
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
            return BadRequest(ex.Message);
        }
    }
}