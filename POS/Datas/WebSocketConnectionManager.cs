using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace POS.Datas
{
    public class WebSocketConnectionManager
    {
        private readonly List<WebSocket> _webSockets = new List<WebSocket>();

        // Handle incoming WebSocket connections
        public async Task HandleWebSocketConnectionAsync(WebSocket webSocket)
        {
            _webSockets.Add(webSocket);
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _webSockets.Remove(webSocket);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the client", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling WebSocket connection: {ex.Message}");
                // Ensure WebSocket is removed on exception as well
                _webSockets.Remove(webSocket);
            }
        }

        // Broadcast message to all connected clients
        public async Task BroadcastMessageAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var tasks = _webSockets
                .Where(ws => ws.State == WebSocketState.Open)
                .Select(socket => SendMessageAsync(socket, buffer));

            await Task.WhenAll(tasks);  // Wait for all messages to be sent
        }

        // Send message to a specific WebSocket
        public async Task SendMessageAsync(WebSocket webSocket, byte[] buffer)
        {
            try
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        // Method to send a serialized JSON message to all clients
        public async Task BroadcastJsonMessageAsync(object messageObj)
        {
            try
            {
                // Serialize the message object to JSON
                var jsonMessage = JsonSerializer.Serialize(messageObj);

                // Broadcast the serialized JSON message to all open WebSocket connections
                await BroadcastMessageAsync(jsonMessage);  // Calls the existing BroadcastMessageAsync method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing and broadcasting JSON message: {ex.Message}");
            }
        }
    }
}
