namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> OnlineUsers = [];

        public Task<bool> UserConnected(string username, string ConnectionId)
        {
            var isOnline = false;
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(username))
                {
                    OnlineUsers[username].Add(ConnectionId);
                }
                else
                {
                    OnlineUsers.Add(username, [ConnectionId]);
                    isOnline = true;
                }
            }

            return Task.FromResult(isOnline);
        }

        public Task<bool> UserDisconnected(string username, string ConnectionId)
        {
            var isOffline = false;
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(username)) return Task.FromResult(isOffline);

                OnlineUsers[username].Remove(ConnectionId);

                if(OnlineUsers[username].Count == 0)
                {
                    isOffline = true;
                    OnlineUsers.Remove(username);
                }
            }

            return Task.FromResult(!isOffline);
        }

        public Task<string[]> GetOnlineUsers()
        {
            string[] onlineUsers;
            lock (OnlineUsers)
            {
                onlineUsers = OnlineUsers.OrderBy(k => k.Key)
                    .Select(k => k.Key)
                    .ToArray();
            }

            return Task.FromResult(onlineUsers);
        }

        public static Task<List<string>> GetConnectionsForUser(string username)
        {
            List<string> connectionIds;
            if (OnlineUsers.TryGetValue(username, out var connections))
            {
                lock (OnlineUsers)
                {
                    connectionIds = [.. connections]; //same as connections.ToList()
                }
            }
            else
            {
                connectionIds = [];
            }
            return Task.FromResult(connectionIds);
        }
    }
}
