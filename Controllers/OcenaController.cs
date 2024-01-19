[ApiController]
[Route("[controller]")]
public class OcenaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    

    public OcenaController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
    }
    

    [HttpGet("PreuzmiOcene")]
    public async Task<ActionResult> PreuzmiOcene()
    {
        try
        {
            var query = _client.Cypher.Match("(p:Ocena)").Return(p => new
            {
                p.As<Ocena>().Vrednost,
                p.As<Ocena>().Komentar

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception)
        {
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
            return BadRequest(ex.Message);
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
            return BadRequest(ex.Message);
        }
    }

    // [HttpPut("OceniDadilju/{id}/{vrednost}/{komentar}")]
    // public async Task<ActionResult> OceniDadilju(int id, int vrednost, string komentar)
    // {
    //     try
    //     {
    //        await _client.Cypher
    //        .Match("(o:Ocena)")
    //        .Where((Ocena o) => o.Id == id)
    //        .Set("o.Vrednost = $vrednost")
    //        .Set("o.Komentar = $komentar")
    //        .WithParam("vrednost", vrednost)
    //        .WithParam("komentar", komentar)
    //        .ExecuteWithoutResultsAsync();
            
    //        return Ok("Uspesno ocenjena dadilja.");
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex.Message);
    //     }
    // }
}