using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.WebSockets;
using Xunit;
using WebSocketServer;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using System.Text;

namespace WebSocketServer.tests;

public class UnitTest1 : IClassFixture<WebApplicationFactory<Program>>
{
    private WebApplicationFactory<Program>? _factory = null;

    public UnitTest1(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/online", "sim!")]
    public async Task Online_ReturnsJsonString(string end, string resp)
    {
        WebSocketClient client = _factory!.Server.CreateWebSocketClient();
        using WebSocket web_socket = await client.ConnectAsync(new Uri(_factory.Server.BaseAddress, end), CancellationToken.None);
        Assert.Equal(WebSocketState.Open, web_socket.State);

        byte[] buf = new byte[1024 * 4];
        WebSocketReceiveResult? result = await web_socket.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);

        string? msg = Encoding.UTF8.GetString(buf, 0, result.Count);

        Assert.Contains(resp, msg);
    }
}
