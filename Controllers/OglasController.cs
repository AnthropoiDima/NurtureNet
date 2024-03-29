using backend.Servisi.KorisnikFje;
using StackExchange.Redis;
[ApiController]
[Route("[controller]")]
public class OglasController : ControllerBase
{    
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private KorisnikFje _korisnikFje;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    

    public OglasController(IConfiguration configuration, IGraphClient graphClient, IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _korisnikFje = new KorisnikFje();
        _redis = redis;
        _redisDB = _redis.GetDatabase();
    }
    
    [HttpGet("PreuzmiOglase")]
    public async Task<ActionResult> PreuzmiOglase()
    {
        try
        {
            var query = _client.Cypher.Match("(p:Oglas)").Return(p => new
            {
                p.As<Oglas>().Id,
                p.As<Oglas>().Opis,
                p.As<Oglas>().Plata,
                p.As<Oglas>().RadnoVreme,
                p.As<Oglas>().Vestine,
                p.As<Oglas>().Oglasivac

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje oglasa.");
        }
    }

    [HttpPost("DodajOglas/{email}/{opis}/{plata}/{vreme}/{vestine}/{jeDadilja}")]
    public async Task<ActionResult> DodajOglas(string email, string opis, double plata,
        string vreme, string vestine, bool jeDadilja)
    {
        try
        {   
            Oglas noviOglas = new Oglas{
                Id = int.Parse(_redisDB.StringGet("BrojacOglasaID").ToString()),
                Opis = opis,
                Plata = plata,
                RadnoVreme = vreme,
                Vestine = vestine,
                JeDadilja = jeDadilja,
                Oglasivac = email
            };
            await _client.Cypher
           .Create("(oglas:Oglas $noviOglas)")
           .WithParam("noviOglas", noviOglas)
           .ExecuteWithoutResultsAsync();
            
            _redisDB.StringIncrement("BrojacOglasaID");
           return Ok("Uspesno dodavanje oglasa.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpPost("DodajOglasFromBody")]
    public async Task<ActionResult> DodajOglasFromBody([FromBody] Oglas oglas)
    {
        try
        {
            oglas.Id = int.Parse(_redisDB.StringGet("BrojacOglasaID").ToString());
           await _client.Cypher
           .Create("(o:Oglas $noviOglas)")
           .WithParam("noviOglas", oglas)
           .ExecuteWithoutResultsAsync();
            
            _redisDB.StringIncrement("BrojacOglasaID");
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpPut("IzmeniOpisOglasa/{id}/{opis}")]
    public async Task<ActionResult> IzmeniOpisOglasa(int id, string opis)
    {
        try
        {
           await _client.Cypher
           .Match("(o:Oglas)")
           .Where((Oglas o) => o.Id == id)
           .Set("o.Opis = $opis")
           .WithParam("opis", opis)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjen opis oglasa.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }

    [HttpDelete("ObrisiOglas/{id}")]
    public async Task<ActionResult> ObrisiOglas(int id)
    {
        try
        {
           var query = _client.Cypher
           .OptionalMatch("(o:Oglas)<-[:SE_PRIJAVLJUJE]-(d:Dadilja)")
           .Where((Oglas o) => o.Id == id)
           .DetachDelete("o")
           .Return(d => new {d.As<Dadilja>().Email});

            
            var emailResult = await query.ResultsAsync;
            string emailD = emailResult.FirstOrDefault()?.Email;
            
            if(!string.IsNullOrWhiteSpace(emailD)){
                await _redisDB.HashDeleteAsync("PrijavljeniOglasi:"+ emailD, "Oglas:" + id);
            }

           var queryK = _client.Cypher
           .OptionalMatch("(o:Oglas)<-[:SE_PRIJAVLJUJE]-(k:Korisnik)")
           .Where((Oglas o) => o.Id == id)
           .DetachDelete("o")
           .Return(k => new {k.As<Korisnik>().Email});
           emailResult = await queryK.ResultsAsync;
           string emailK = emailResult.FirstOrDefault()?.Email;
           if(!string.IsNullOrWhiteSpace(emailK)){
                await _redisDB.HashDeleteAsync("PrijavljeniOglasi:"+ emailK, "Oglas:" + id);
            }

           return Ok("Uspesno obrisan oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpGet("PreuzmiOglaseDadilja")]
    public async Task<ActionResult> PreuzmiOglaseDadilja()
    {
        try
        {
            var query = _client.Cypher.Match("(oglas:Oglas)")
            .Where((Oglas oglas) => oglas.JeDadilja == true)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje oglasa.");
        }
    }
    
    [HttpGet("PreuzmiSveOglaseKorisnika")]
    public async Task<ActionResult> PreuzmiSveOglaseKorisnika()
    {
        try
        {
            var query = _client.Cypher.Match("(oglas:Oglas)")
            .Where((Oglas oglas) => oglas.JeDadilja == false)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpGet("PretraziPoGraduDadilje/{grad}")]
    public async Task<ActionResult> PretraziPoGraduDadilje(string grad)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(dadilja:Dadilja)-[:OBJAVLJUJE]->(oglas:Oglas)")
            .Where((Oglas oglas) => oglas.JeDadilja == true)
            .AndWhere((Dadilja dadilja) => dadilja.Grad == grad)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpGet("PretraziPoGraduKorisnika/{grad}")]
    public async Task<ActionResult> PretraziPoGraduKorisnika(string grad)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(korisnik:Korisnik)-[:OBJAVLJUJE]->(oglas:Oglas)")
            .Where((Oglas oglas) => oglas.JeDadilja == false)
            .AndWhere((Korisnik korisnik) => korisnik.Grad == grad)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpGet("PretraziOglasePoVestinama/{email}")]
    public async Task<ActionResult> PretraziOglasePoVestinama(string email)
    {
        try
        {
            var v = await _client.Cypher
            .OptionalMatch("(dadilja:Dadilja)")
            .Where((Dadilja dadilja) => dadilja.Email == email)
            .Return(dadilja => new {
                dadilja.As<Dadilja>().Vestine
            }).ResultsAsync;
            string[]? sveVestine = v.FirstOrDefault()?.Vestine?.Split(", ");

            var query = _client.Cypher
            .Match("(oglas:Oglas)")
            .Where("ANY(vestine in $sveVestine WHERE oglas.Vestine contains vestine)")
            .AndWhere((Oglas oglas) => oglas.JeDadilja == false)
            .WithParam("sveVestine", sveVestine)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }
    
    [HttpGet("PretraziDadiljePoVestinama/{id}")]
    public async Task<ActionResult> PretraziDadiljePoVestinama(int id)
    {
        try
        {
            var v = await _client.Cypher
            .OptionalMatch("(oglas:Oglas)")
            .Where((Oglas oglas) => oglas.Id == id)
            .Return(oglas => new {
                oglas.As<Oglas>().Vestine
            }).ResultsAsync;
            string[]? sveVestine = v.FirstOrDefault()?.Vestine?.Split(", ");

            var query = _client.Cypher
            .Match("(dadilja:Dadilja)")
            .Where("ANY(vestine in $sveVestine WHERE dadilja.Vestine contains vestine)")
            .WithParam("sveVestine", sveVestine)
            .Return(dadilja => new
            {
                dadilja.As<Dadilja>().Ime,
                dadilja.As<Dadilja>().Prezime,
                dadilja.As<Dadilja>().Email,
                dadilja.As<Dadilja>().BrojTelefona,
                dadilja.As<Dadilja>().Grad,
                dadilja.As<Dadilja>().Vestine
            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }

     [HttpGet("PreuzmiOglaseDadilje/{email}")]
    public async Task<ActionResult> PreuzmiOglaseDadilje(string email)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(dadilja:Dadilja)-[:OBJAVLJUJE]->(oglas:Oglas)")
            .Where((Dadilja dadilja) => dadilja.Email == email)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }

     [HttpGet("PreuzmiOglaseKorisnika/{email}")]
    public async Task<ActionResult> PreuzmiOglaseKorisnika(string email)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(korisnik:Korisnik)-[:OBJAVLJUJE]->(oglas:Oglas)")
            .Where((Korisnik korisnik) => korisnik.Email == email)
            .Return(oglas => new
            {
                oglas.As<Oglas>().Id,
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
            return BadRequest("Neuspesno!");
        }
    }


    [HttpPost("DodajOglasDadiljaKorisnik/{email}/{opis}/{plata}/{vreme}/{vestine}")]
    public async Task<ActionResult> DodajOglasDadiljaKorisnik(string email, string opis, double plata,
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
                JeDadilja = true ? _korisnikFje.GetCurrentUserRole(User) == "dadilja" : false
            };
            if(noviOglas.JeDadilja){ 
                await _client.Cypher
                .Match("(dadilja:Dadilja)")
                .Where((Dadilja dadilja) => dadilja.Email == email)
                .Create("(dadilja)-[:OBJAVLJUJE]->(oglas:Oglas $noviOglas)")
                .WithParam("noviOglas", noviOglas)
                .ExecuteWithoutResultsAsync();
            }
            else{
                await _client.Cypher
                .Match("(korisnik:Korisnik)")
                .Where((Korisnik korisnik) => korisnik.Email == email)
                .Create("(korisnik)-[:OBJAVLJUJE]->(oglas:Oglas $noviOglas)")
                .WithParam("noviOglas", noviOglas)
                .ExecuteWithoutResultsAsync();
            }            
            _redisDB.StringIncrement("BrojacOglasaID");
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno!");
        }
    }

}