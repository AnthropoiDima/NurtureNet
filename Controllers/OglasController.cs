[ApiController]
[Route("[controller]")]
public class OglasController : ControllerBase
{    
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    

    public OglasController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
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
                p.As<Oglas>().Vestine

            });
            
            var result = await query.ResultsAsync;
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest("Neuspesno preuzimanje oglasa.");
        }
    }

    
    [HttpPost("DodajOglas/{email}/{opis}/{plata}/{vreme}/{vestine}/{jeDadilja}")]
    public async Task<ActionResult> DodajOglas(string opis, double plata,
        string vreme, string vestine, bool jeDadilja)
    {
        try
        {   
            Oglas noviOglas = new Oglas{
                Opis = opis,
                Plata = plata,
                RadnoVreme = vreme,
                Vestine = vestine,
                JeDadilja = jeDadilja
            };
            await _client.Cypher
           .Create("(oglas:Oglas $noviOglas)")
           .WithParam("noviOglas", noviOglas)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodavanje oglasa.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    
    [HttpPost("DodajOglasFromBody")]
    public async Task<ActionResult> DodajOglasFromBody([FromBody] Oglas oglas)
    {
        try
        {
           await _client.Cypher
           .Create("(o:Oglas $noviOglas)")
           .WithParam("noviOglas", oglas)
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno dodat oglas.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
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
            return BadRequest(ex.Message);
        }
    }


    [HttpDelete("ObrisiOglas/{id}")]
    public async Task<ActionResult> ObrisiOglas(int id)
    {
        try
        {
           await _client.Cypher
           .OptionalMatch("(o:Oglas)")
           .Where((Oglas o) => o.Id == id)
           .DetachDelete("o")
           .ExecuteWithoutResultsAsync();
            
           return Ok("Uspesno obrisan oglas.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}