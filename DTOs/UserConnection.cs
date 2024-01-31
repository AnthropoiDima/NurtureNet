namespace backend.DTOs;
    public class UserConnection
    {
        public string User { get; set; }
        public string Room { get; set; }

        public UserConnection(string user, string room)
        {
            User = user;
            Room = room;
        }
    }