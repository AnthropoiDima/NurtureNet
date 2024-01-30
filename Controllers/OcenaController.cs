using backend.Servisi.KorisnikFje;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("[controller]")]
public class OcenaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private KorisnikFje _korisnikFje;
    

    public OcenaController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
        _korisnikFje = new KorisnikFje();
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
            return BadRequest("Neuspesno preuzimanje ocena.");
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
    
    [Authorize(Roles = "korisnik, dadilja, admin")]
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
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("OceniDadilju/{emailKorisnika}/{emailDadilje}/{vrednost}/{komentar}")]
    public async Task<ActionResult> OceniDadilju(string emailKorisnika, string emailDadilje, 
        int vrednost, string komentar)
    {
        try
        {
            // var dadilj = await _client.Cypher.Match("(d:Dadilja)")
            //     .Where((Dadilja d) => d.Email == emailDadilje)
            //     .Return<Dadilja>(d => d.As<Dadilja>()).ResultsAsync;

            // Dadilja dadilja = dadilj.FirstOrDefault();
        
            // var korisn = await _client.Cypher.Match("(d:Korisnik)")
            //     .Where((Korisnik d) => d.Email == emailKorisnika)
            //     .Return<Korisnik>(d => d.As<Korisnik>()).ResultsAsync;

            // Korisnik korisnik = korisn.FirstOrDefault();

            Ocena novaOcena = new Ocena{
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
            if(vrednost < 1 || vrednost > 5)
                return BadRequest("Neispravna vrednost ocene.");
            Ocena novaOcena = new Ocena{
                Vrednost = vrednost,
                Komentar = komentar
            };
            if(_korisnikFje.GetCurrentUserRole(User) == "dadilja"){
                await _client.Cypher
                    .Match("(dadilja: Dadilja)", "(korisnik: Korisnik)")
                    .Where((Dadilja dadilja) => dadilja.Email == emailDadilje)
                    .AndWhere((Korisnik korisnik) => korisnik.Email == emailKorisnika)
                    .Create("(dadilja)-[:OCENJUJE {Ocena:$novaOcena.Vrednost, Komentar:$novaOcena.Komentar}]->(korisnik)")
                    .WithParam("novaOcena", novaOcena)
                    .ExecuteWithoutResultsAsync();
                
            return Ok("Uspesno ocenjen korisnik.");
           }
           else if(_korisnikFje.GetCurrentUserRole(User) == "korisnik"){
                await _client.Cypher
                    .Match("(dadilja: Dadilja)", "(korisnik: Korisnik)")
                    .Where((Dadilja dadilja) => dadilja.Email == emailDadilje)
                    .AndWhere((Korisnik korisnik) => korisnik.Email == emailKorisnika)
                    .Create("(korisnik)-[:OCENJUJE {Ocena:$novaOcena.Vrednost, Komentar:$novaOcena.Komentar}]->(dadilja)")
                    .WithParam("novaOcena", novaOcena)
                    .ExecuteWithoutResultsAsync();
                
            return Ok("Uspesno ocenjena dadilja.");
           }
           else
                return BadRequest("Neuspesno ocenjivanje.");
        
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