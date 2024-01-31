using Microsoft.AspNetCore.SignalR;
using backend.DTOs;
using StackExchange.Redis;
using Newtonsoft.Json;
namespace backend.Hubs;

public class ChatHub : Hub{
    public readonly string _botUser;
        private readonly IDictionary<string, UserConnection> _connections;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _redisDB;

        public ChatHub(IDictionary<string, UserConnection> connections, IConnectionMultiplexer redis)
        {
            _botUser = "MyChat bot";
            _connections = connections;
            _redis = redis;
            _redisDB = _redis.GetDatabase();
        }

        public async Task SendMessage(string message)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection connection))
            {
                await _redisDB.ListLeftPushAsync(connection.Room, JsonConvert.SerializeObject(new Poruka{Sadrzaj = message, TimeStamp = DateTime.Now}));
                await Clients.Group(connection.Room).SendAsync("ReceiveMessage", connection.User, message);
            }
        }

        public async Task JoinRoom(string user, string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
            _connections[Context.ConnectionId] = new UserConnection(user, room);
            //await Clients.All.SendAsync("ReceiveMessage", user, "Hey everyone");
        }


}