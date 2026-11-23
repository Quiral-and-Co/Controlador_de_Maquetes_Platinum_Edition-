using System.Net;
using System.Net.Sockets;
using System.Text;
using Platinum.Domain.Control;
using Platinum.Infrastructure.Control;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class NativeProtocolAdapterTests
{
    private sealed class StubHandler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private static ControlStationProfile Jmri(int port=12080) => new("JMRI", ControlStationKind.JmriJson, "127.0.0.1", port);
    private static ControlStationProfile WiThrottle(int port) => new("WiThrottle", ControlStationKind.WiThrottle, "127.0.0.1", port);

    [Fact]
    public async Task Jmri_Connects_And_Reconnects_After_Disconnect()
    {
        var calls=0;
        using var handler=new StubHandler((_,_)=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"status\":\"ok\"}")});});
        await using var adapter=new JmriJsonAdapter(Jmri(),handler:handler);
        await adapter.ConnectAsync();
        Assert.True(adapter.IsConnected);
        await adapter.DisconnectAsync();
        Assert.False(adapter.IsConnected);
        await adapter.ConnectAsync();
        Assert.Equal(2,calls);
    }

    [Fact]
    public async Task Jmri_Rejects_Malformed_Protocol_Response()
    {
        using var handler=new StubHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("not-json")}));
        await using var adapter=new JmriJsonAdapter(Jmri(),handler:handler);
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(()=>adapter.ConnectAsync());
        Assert.False(adapter.IsConnected);
    }

    [Fact]
    public async Task Jmri_Connection_Timeout_Is_Reported_And_State_Remains_Disconnected()
    {
        using var handler=new StubHandler(async (_,token)=>{await Task.Delay(Timeout.Infinite,token);return new HttpResponseMessage(HttpStatusCode.OK);});
        await using var adapter=new JmriJsonAdapter(Jmri(),TimeSpan.FromMilliseconds(60),handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>adapter.ConnectAsync());
        Assert.False(adapter.IsConnected);
    }

    [Fact]
    public async Task WiThrottle_Handshake_Connects_And_Reconnects()
    {
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;
        var server=Task.Run(async ()=>{
            for(var i=0;i<2;i++){
                using var client=await listener.AcceptTcpClientAsync();
                using var stream=client.GetStream();
                using var reader=new StreamReader(stream,Encoding.UTF8,leaveOpen:true);
                using var writer=new StreamWriter(stream,Encoding.UTF8,leaveOpen:true){NewLine="\n",AutoFlush=true};
                Assert.StartsWith("NPlatinum",await reader.ReadLineAsync());
                await writer.WriteLineAsync("VN2.0");
            }
        });
        try {
            await using var adapter=new WiThrottleAdapter(WiThrottle(port),TimeSpan.FromSeconds(2));
            await adapter.ConnectAsync();Assert.True(adapter.IsConnected);
            await adapter.DisconnectAsync();Assert.False(adapter.IsConnected);
            await adapter.ConnectAsync();Assert.True(adapter.IsConnected);
            await adapter.DisconnectAsync();
            await server.WaitAsync(TimeSpan.FromSeconds(3));
        } finally { listener.Stop(); }
    }

    [Fact]
    public async Task WiThrottle_Timeout_And_Bad_Handshake_Are_Fail_Closed()
    {
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;
        var server=Task.Run(async ()=>{using var client=await listener.AcceptTcpClientAsync();await Task.Delay(250);});
        try {
            await using var adapter=new WiThrottleAdapter(WiThrottle(port),TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>adapter.ConnectAsync());
            Assert.False(adapter.IsConnected);
            await server;
        } finally { listener.Stop(); }

        listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        port=((IPEndPoint)listener.LocalEndpoint).Port;
        server=Task.Run(async ()=>{using var client=await listener.AcceptTcpClientAsync();using var writer=new StreamWriter(client.GetStream()){AutoFlush=true};await writer.WriteLineAsync("ERROR");});
        try {
            await using var adapter=new WiThrottleAdapter(WiThrottle(port),TimeSpan.FromSeconds(2));
            await Assert.ThrowsAsync<InvalidDataException>(()=>adapter.ConnectAsync());
            Assert.False(adapter.IsConnected);
            await server;
        } finally { listener.Stop(); }
    }

    [Fact]
    public async Task Factory_Provides_Independent_Protocol_Adapters()
    {
        await using var jmri=ControlStationAdapterFactory.Create(Jmri());
        await using var throttle=ControlStationAdapterFactory.Create(WiThrottle(12090));
        Assert.IsType<JmriJsonAdapter>(jmri);
        Assert.IsType<WiThrottleAdapter>(throttle);
        Assert.False(jmri.IsSimulation);
        Assert.False(throttle.IsSimulation);
    }
}
