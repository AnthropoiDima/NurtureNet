using backend.Servisi.Autentifikacija;
using backend.DTOs;
using System.Text.RegularExpressions;
using backend.Servisi.KorisnikFje;
[ApiController]
[Route("[controller]")]
public class AutentifikacijaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private Autentifikacija _autentifikacija;
    private KorisnikFje _korisnikFje;

    public AutentifikacijaController(IConfiguration configuration, IGraphClient graphClient)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _korisnikFje = new KorisnikFje();
    }

    [HttpPost("RegistracijaDadilja")]
    public async Task<ActionResult> RegistracijaDadilja([FromBody] DadiljaRegisterDto dto)
    {
        try
        {

            if(!Regex.IsMatch(dto.Email!,
            @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
            RegexOptions.IgnoreCase)){
                return BadRequest("Email ne postoji!");
            }

            var queryD = await _client.Cypher
                .Match("(d:Dadilja)")
                .Where((Dadilja d) => d.Email == dto.Email)
                .Return(d => d.As<Dadilja>()).ResultsAsync;

            if (queryD.Count() != 0) 
            {
                return BadRequest("Korisnik sa ovim emailom vec postoji.");
            }
            var queryK = await _client.Cypher
                .Match("(d:Korisnik)")
                .Where((Korisnik d) => d.Email == dto.Email)
                .Return(d => d.As<Korisnik>()).ResultsAsync;

            if (queryK.Count() != 0) 
            {
                return BadRequest("Korisnik sa ovim emailom vec postoji.");
            }

            Dadilja novaDadilja = new Dadilja
            {
                Ime = dto.Ime!,
                Prezime = dto.Prezime!,
                Email = dto.Email!,
                Password = _autentifikacija.HesirajPassword(dto.Password!),
                Drzava = dto.Drzava!,
                Grad = dto.Grad!,
                DatumRodjenja = dto.DatumRodjenja,
                Pol = dto.Pol!,
                BrojTelefona = dto.BrojTelefona!,
                Vestine = dto.Vestine!
            };

            await _client.Cypher
                .Create("(d:Dadilja {nova})")
                .WithParam("nova", novaDadilja)
                .ExecuteWithoutResultsAsync();

            return Ok("Uspesno registrovan profil.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesna registracija.");
        }
    }

    [HttpPost("RegistracijaKorisnik")]
    public async Task<ActionResult> RegistracijaKorisnik([FromBody] KorisnikRegisterDto dto)
    {
        try
        {
            if(!Regex.IsMatch(dto.Email!,
            @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
            RegexOptions.IgnoreCase)){
                return BadRequest("Email ne postoji!");
            }

            var query = await _client.Cypher
                .Match("(k:Korisnik)")
                .Where((Korisnik k) => k.Email == dto.Email)
                .Return(k => k.As<Korisnik>()).ResultsAsync;

            if (query.Count() != 0)
            {
                return BadRequest("Korisnik sa ovim emailom vec postoji.");
            }
            
            var queryD = await _client.Cypher
                .Match("(d:Dadilja)")
                .Where((Dadilja d) => d.Email == dto.Email)
                .Return(d => d.As<Dadilja>()).ResultsAsync;

            if (queryD.Count() != 0) 
            {
                return BadRequest("Korisnik sa ovim emailom vec postoji.");
            }

            Korisnik noviKorisnik = new Korisnik
            {
                Ime = dto.Ime!,
                Prezime = dto.Prezime!,
                Email = dto.Email!,
                Password = _autentifikacija.HesirajPassword(dto.Password!),
                Drzava = dto.Drzava!,
                Grad = dto.Grad!,
                DatumRodjenja = dto.DatumRodjenja,
                Pol = dto.Pol!,
                BrojTelefona = dto.BrojTelefona!
            };

            await _client.Cypher
                .Create("(k:Korisnik $novi)")
                .WithParam("novi", noviKorisnik)
                .ExecuteWithoutResultsAsync();

            return Ok("Uspesno registrovan profil.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesna registracija.");
        }
    }

    [HttpPost("Prijavljivanje")]
    public async Task<ActionResult> Prijavljivanje([FromBody] LoginDto dto)
    {
        try
        {
            var queryK = await _client.Cypher
                .Match("(k:Korisnik)")
                .Where((Korisnik k) => k.Email == dto.Email)
                .Return(k => new {k.As<Korisnik>().Ime,
                k.As<Korisnik>().Prezime,
                k.As<Korisnik>().Pol,
                k.As<Korisnik>().DatumRodjenja,
                k.As<Korisnik>().Email,
                k.As<Korisnik>().Drzava,
                k.As<Korisnik>().Grad,
                k.As<Korisnik>().BrojTelefona,
                k.As<Korisnik>().Password,
                }).ResultsAsync;

            var queryD = await _client.Cypher
                .Match("(d:Dadilja)")
                .Where((Dadilja d) => d.Email == dto.Email)
                .Return(d => d.As<Dadilja>()).ResultsAsync;

            if(queryD.Count() != 0)
            {
                Dadilja dadilja = queryD.First();
                if (!_autentifikacija.ProveriPassword(dto.Password!, dadilja.Password))
                    {
                        return BadRequest("Pogresna lozinka.");
                    }
                var token = _autentifikacija.GenerisiTokenDadilja(dadilja);
                return Ok($"{token}");
            }
            else if(queryK.Count() != 0)
            {
                var k = queryK.First();
                Korisnik korisnik = new Korisnik{
                    Ime = k.Ime,
                    Prezime = k.Prezime,
                    DatumRodjenja = k.DatumRodjenja,
                    Pol = k.Pol,
                    Email = k.Email,
                    Drzava = k.Drzava,
                    Grad = k.Grad,
                    BrojTelefona = k.BrojTelefona,
                    Password = k.Password
                };
                Console.WriteLine(korisnik.Password + " " + korisnik.Email);
                if (!_autentifikacija.ProveriPassword(dto.Password!, korisnik.Password))
                    {
                        return BadRequest("Pogresna lozinka.");
                    }
                var token = _autentifikacija.GenerisiTokenKorisnik(korisnik);
                return Ok($"{token}");
            }
            else
            {
                return BadRequest("Korisnik sa ovim emailom ne postoji.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesna prijava.");
        }
    }

    [HttpGet("PreuzmiTipKorisnika")]
    public ActionResult PreuzmiTipKorisnika()
    {
        try
        {
            string tip = _korisnikFje.GetCurrentUserRole(User)!;
            return Ok(tip);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesno preuzimanje tipa korisnika.");
        }
    }
}
