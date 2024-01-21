using backend.Servisi.Autentifikacija;
using StackExchange.Redis;
using NRedisStack;

[ApiController]
[Route("[controller]")]
public class PorukaController : ControllerBase
{
    private readonly IGraphClient _client;
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDB;
    private Autentifikacija _autentifikacija;

    public PorukaController(IConfiguration configuration, IGraphClient graphClient, 
    IConnectionMultiplexer redis)
    {
        _config = configuration;
        _client = graphClient;
        _autentifikacija = new Autentifikacija(_config);
        _redis = redis;
        _redisDB = _redis.GetDatabase();
    }

    public void PosaljiPoruku()
    {
        string ChatChannel = "ChatChannel";
        var pubsub = _redis.GetSubscriber();
        pubsub.Subscribe(ChatChannel, (channel, poruka) => MessageAction(poruka));
    }

    public void MessageAction(RedisValue poruka)
    {

    }
}