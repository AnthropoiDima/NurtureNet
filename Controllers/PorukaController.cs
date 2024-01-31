using backend.Servisi.Autentifikacija;
using StackExchange.Redis;
using backend.Servisi.KorisnikFje;
using Microsoft.AspNetCore.Authorization;
using backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
[ApiController]
[Route("[controller]")]
public class PorukaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    private readonly IHubContext<ChatHub> _hubContext;
    private Autentifikacija _autentifikacija;
    private KorisnikFje _korisnikFje;

    public PorukaController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis, IHubContext<ChatHub> hubContext)
    {
        _config = configuration;
        _client = graphClient;
        _hubContext = hubContext;
        _autentifikacija = new Autentifikacija(_config);
        _korisnikFje = new KorisnikFje();
        _redis = redis;
        _redisDB = _redis.GetDatabase();
    }
    // [Authorize(Roles = "korisnik, dadilja")]
    [HttpPost("PosaljiPoruku/{emailSender}/{emailReceiver}")]
    public ActionResult PosaljiPoruku(string emailSender, string emailReceiver, [FromBody] Poruka poruka)
    {
        try
        {   
            string roomName = emailSender.CompareTo(emailReceiver) < 0 ? emailSender + emailReceiver : emailReceiver + emailSender;

            //_redisDB.Publish(new RedisChannel("Kanal:" + email, RedisChannel.PatternMode.Auto), JsonConvert.SerializeObject(poruka));
            // _hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", poruka.Sadrzaj);
            
            return Ok("Uspesno slanje poruke.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesno slanje poruke.");
        }
    }


    // [HttpGet("PreuzmiPoruke")]
    // public async Task<ActionResult> PreuzmiPoruke()
    // {
    //     try
    //     {
    //         var query = _client.Cypher.Match("(p:Poruka)")
    //             .Return(p => new{ 
    //                 p.As<Poruka>().Id,
    //                 p.As<Poruka>().Posiljalac,
    //                 p.As<Poruka>().Primalac,
    //                 p.As<Poruka>().Sadrzaj,
    //                 p.As<Poruka>().DatumSlanja,
    //                 p.As<Poruka>().Procitana                                    
    //             });

    //         _redisDB.StringSet("foo", "bar");

    //         var result = await query.ResultsAsync;
    //         return Ok(_redisDB.StringGet("foo"));
    //     }
    //     catch (Exception e)
    //     {
    //         return BadRequest(e.Message);
    //     }
    // }

    // [HttpGet("PreuzmiPorukuPoId/{id}")]
    // public async Task<ActionResult> PreuzmiPorukuPoId(int id)
    // {
    //     try
    //     {

    //         var query = await _client.Cypher
    //             .Match("(p:Poruka)")
    //             .Where((Poruka p) => p.Id == id)
    //             .Return(p => p.As<Poruka>()).ResultsAsync;

    //         return Ok(query);
    //     }
    //     catch (Exception e)
    //     {
    //         return BadRequest(e.Message);
    //     }
    // }

}