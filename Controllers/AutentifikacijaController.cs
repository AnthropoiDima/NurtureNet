using backend.Servisi.Autentifikacija;
using backend.DTOs;
using System.Text.RegularExpressions;
[ApiController]
[Route("[controller]")]
public class AutentifikacijaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private Autentifikacija _autentifikacija;

    public AutentifikacijaController(IConfiguration configuration, IGraphClient graphClient, Autentifikacija autentifikacija)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
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

            var query = await _client.Cypher
                .Match("(k:Korisnik)")
                .Where((Dadilja d) => d.Email == dto.Email)
                .Return(k => k.As<Dadilja>()).ResultsAsync;

            if (query.Count() != 0) 
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
                .Create("(k:Korisnik {novi})")
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
                .Return(k => k.As<Korisnik>()).ResultsAsync;

            if (queryK.Count() == 0)
            {
                return BadRequest("Korisnik sa ovim emailom ne postoji.");
            }

            var queryD = await _client.Cypher
                .Match("(d:Dadilja)")
                .Where((Dadilja d) => d.Email == dto.Email)
                .Return(k => k.As<Dadilja>()).ResultsAsync;

            if (queryD.Count() == 0)
            {
                return BadRequest("Korisnik sa ovim emailom ne postoji.");
            }

            var dadilja = queryD.First();

            if (!_autentifikacija.ProveriPassword(dto.Password!, dadilja.Password))
            {
                return BadRequest("Pogresna lozinka.");
            }

            var token = _autentifikacija.GenerisiTokenDadilja(dadilja);

            return Ok(new { token });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesna prijava.");
        }
    }
}
