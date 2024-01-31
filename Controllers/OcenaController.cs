using StackExchange.Redis;

[ApiController]
[Route("[controller]")]
public class OcenaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    

    public OcenaController(IConfiguration configuration, IGraphClient graphClient, IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _redis = redis;
        _redisDB = _redis.GetDatabase();
    }
    

    [HttpGet("PreuzmiOcene")]
    public async Task<ActionResult> PreuzmiOcene()
    {
        try
        {
            var query = _client.Cypher.Match("(p:Ocena)").Return(p => new
            {
                p.As<Ocena>().Id,
                p.As<Ocena>().Vrednost,
                p.As<Ocena>().Komentar

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje dadilja.");
        }
    }

    // [HttpPost("DodajOcenu/{ime}/{prezime}/{email}/{password}/{drzava}/{grad}/{datum}/{pol}/{broj}/{vestine}")]
    // public async Task<ActionResult> DodajOcenu(string ime, string prezime,
    //     string email, string password, string drzava, string grad, DateTime datum,
    //     string pol, string broj, string vestine)
    // {
    //     try
    //     {   
    //        await _client.Cypher
    //        .Create($"(d:Dadilja {{Ime:'{ime}', Prezime:'{prezime}', Email:'{email}', Password:'{password}', Drzava:'{drzava}', Grad:'{grad}', Datum:'{datum}' Pol:'{pol}', BrojTelefona:'{broj}', Vestine:'{vestine}'}})")
    //        .ExecuteWithoutResultsAsync();
            
    //        return Ok("Uspesno dodavanje dadilje.");
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex.Message);
    //     }
    // }
    
    [HttpPut("IzmeniVrednostOcene/{id}/{vrednost}")]
    public async Task<ActionResult> IzmeniVrednostOcene(int id, int vrednost)
    {
        try
        {
            if (vrednost < 1 || vrednost > 5)
            {
                return BadRequest("Vrednost ocene mora biti izmedju 1 i 5.");
            }

           await _client.Cypher
           .Match("(o:Ocena)")
           .Where((Ocena o) => o.Id == id)
           .Set("o.Vrednost = $vrednost")
           .WithParam("vrednost", vrednost)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno izmenjena ocena.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesna izmena ocene.");
        }
    }

    [HttpDelete("ObrisiOcenu/{id}")]
    public async Task<ActionResult> ObrisiOcenu(int id)
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(o:Ocena)")
           .Where((Ocena o) => o.Id == id)
           .DetachDelete("o")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisana ocena.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno brisanje ocene.");
        }
    }
    
    [HttpPost("OceniDadilju/{emailKorisnika}/{emailDadilje}/{vrednost}/{komentar}")]
    public async Task<ActionResult> OceniDadilju(string emailKorisnika, string emailDadilje, 
        int vrednost, string komentar)
    {
        try
        {
            Ocena novaOcena = new Ocena{
                Id = int.Parse(_redisDB.StringGet("BrojacOcenaID").ToString()),
                Vrednost = vrednost,
                Komentar = komentar
            };
            await _client.Cypher
                .Match("(dadilja: Dadilja)", "(korisnik: Korisnik)")
                .Where((Dadilja dadilja) => dadilja.Email == emailDadilje)
                .AndWhere((Korisnik korisnik) => korisnik.Email == emailKorisnika)
                .Create("(korisnik)-[:OCENJUJE {Ocena:$novaOcena.Vrednost, Komentar:$novaOcena.Komentar}]->(dadilja)")
                .WithParam("novaOcena", novaOcena)
                .ExecuteWithoutResultsAsync();
            
            _redisDB.StringIncrement("BrojacOcenaID");
           return Ok("Uspesno ocenjena dadilja.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno ocenjivanje dadilje.");
        }
    }

    [HttpPost("OceniKorisnika/{emailKorisnika}/{emailDadilje}/{vrednost}/{komentar}")]
    public async Task<ActionResult> OceniKorisnika(string emailKorisnika, string emailDadilje, 
        int vrednost, string komentar)
    {
        try
        {
            Ocena novaOcena = new Ocena{
                Id = int.Parse(_redisDB.StringGet("BrojacOcenaID").ToString()),
                Vrednost = vrednost,
                Komentar = komentar
            };
            await _client.Cypher
                .Match("(dadilja: Dadilja)", "(korisnik: Korisnik)")
                .Where((Dadilja dadilja) => dadilja.Email == emailDadilje)
                .AndWhere((Korisnik korisnik) => korisnik.Email == emailKorisnika)
                .Create("(dadilja)-[:OCENJUJE {Ocena:$novaOcena.Vrednost, Komentar:$novaOcena.Komentar}]->(korisnik)")
                .WithParam("novaOcena", novaOcena)
                .ExecuteWithoutResultsAsync();
            
            _redisDB.StringIncrement("BrojacOcenaID");
           return Ok("Uspesno ocenjen korisnik.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno ocenjivanje korisnika.");
        }
    }


    [HttpGet("PreuzmiOceneDadilje/{email}")]
    public async Task<ActionResult> PreuzmiOceneDadilje(string email)   
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(korisnik: Korisnik)-[OCENJUJE]->(dadilja: Dadilja)")
            .Where((Dadilja dadilja) => dadilja.Email == email)
            .Return(ocena => new
            {
                ocena.As<Ocena>().Id,
                ocena.As<Ocena>().Vrednost,
                ocena.As<Ocena>().Komentar

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje ocena dadilje.");
        }
    }
    
    [HttpGet("PreuzmiOceneKorisnika/{email}")]
    public async Task<ActionResult> PreuzmiOceneKorisnika(string email)
    {
        try
        {
            var query = _client.Cypher
            .OptionalMatch("(dadilja: Dadilja)-[OCENJUJE]->(korisnik: Korisnik)")
            .Where((Korisnik korisnik) => korisnik.Email == email)
            .Return(ocena => new
            {
                ocena.As<Ocena>().Id,
                ocena.As<Ocena>().Vrednost,
                ocena.As<Ocena>().Komentar

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest("Neuspesno preuzimanje ocena dadilje.");
        }
    }
}