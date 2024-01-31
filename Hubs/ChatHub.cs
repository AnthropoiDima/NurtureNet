using Microsoft.AspNetCore.SignalR;
using backend.DTOs;
namespace backend.Hubs;

public class ChatHub : Hub{
    public readonly string _botUser;
        private readonly IDictionary<string, UserConnection> _connections;

        public ChatHub(IDictionary<string, UserConnection> connections)
        {
            _botUser = "MyChat bot";
            _connections = connections;
        }

        public async Task SendMessage(string message)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection connection))
            {
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