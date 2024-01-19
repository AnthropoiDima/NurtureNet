[ApiController]
[Route("[controller]")]
public class DadiljaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    

    public DadiljaController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
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
            return BadRequest("Neuspesno preuzimanje dadilja.");
        }
    }

    [HttpGet("PreuzmiDadiljuPoEmailu/{email}")]
    public async Task<ActionResult> PreuzmiDadiljuPoImenu(string email)
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
                BrojTelefona = broj
            };
            novaDadilja.Vestine.Add(vestine);
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
}