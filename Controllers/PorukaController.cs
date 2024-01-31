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

    [HttpGet("PreuzmiPoruke/{emailSender}/{emailReceiver}")]
    public async Task<ActionResult> PreuzmiPoruke(string emailSender, string emailReceiver)
    {
        try
        {
            string roomName = emailSender.CompareTo(emailReceiver) < 0 ? emailSender + emailReceiver : emailReceiver + emailSender;
            
            var RedislistaPoruka = await _redisDB.ListRangeAsync(roomName, 0, 20);
            List<Poruka> listaPoruka = RedislistaPoruka.Select(p => JsonConvert.DeserializeObject<Poruka>(p)).ToList();

            return Ok(listaPoruka);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Neuspesno preuzimanje poruka.");
        }
    }  
}