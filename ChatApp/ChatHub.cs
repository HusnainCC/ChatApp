using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, List<string>> _userConnections = new();

    public override Task OnConnectedAsync()
    {
        var user = Context.GetHttpContext()?.Request.Query["user"].ToString();

        if (string.IsNullOrWhiteSpace(user))
        {
            Console.WriteLine("❌ Empty username. Connection aborted.");
            Context.Abort();
            return Task.CompletedTask;
        }

        lock (_userConnections)
        {
            if (!_userConnections.ContainsKey(user))
                _userConnections[user] = new List<string>();

            if (!_userConnections[user].Contains(Context.ConnectionId))
                _userConnections[user].Add(Context.ConnectionId);
        }

        Console.WriteLine($"✅ {user} connected with {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_userConnections)
        {
            foreach (var pair in _userConnections)
            {
                pair.Value.Remove(Context.ConnectionId);
            }

            var emptyKeys = _userConnections.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in emptyKeys)
            {
                _userConnections.Remove(key);
            }
        }

        Console.WriteLine($"❌ Disconnected: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessageToUser(string toUser, string fromUser, string message)
    {
        try
        {
            if (_userConnections.TryGetValue(toUser, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", fromUser, message);
                }
            }
            else
            {
                Console.WriteLine($"⚠️ No connections for user {toUser}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in SendMessageToUser: {ex.Message}");
            throw; // rethrow so 1011 is still visible
        }
    }
}
