using Microsoft.AspNetCore.SignalR;

public class StatusUpdateHub : Hub
{
    // This method will be called by the server to send updates to all connected clients
    public async Task SendStatusUpdate(string message)
    {
        await Clients.All.SendAsync("ReceiveStatusUpdate", message);
    }
}
