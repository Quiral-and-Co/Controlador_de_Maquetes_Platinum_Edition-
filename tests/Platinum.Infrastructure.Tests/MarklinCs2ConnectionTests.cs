using System.Net;
using System.Net.Sockets;
using Platinum.Domain.Control;
using Platinum.Infrastructure.Control;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class MarklinCs2ConnectionTests
{
    private static ControlStationProfile Profile(int commandPort) =>
        new("CS2 test",ControlStationKind.MarklinCs2,IPAddress.Loopback.ToString(),commandPort);

    [Fact]
    public async Task Ping_Connects_And_Session_Can_Reconnect()
    {
        using var station=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
        var commandPort=((IPEndPoint)station.Client.LocalEndPoint!).Port;
        int feedbackPort;
        using(var feedback=new UdpClient(new IPEndPoint(IPAddress.Loopback,0))) feedbackPort=((IPEndPoint)feedback.Client.LocalEndPoint!).Port;
        var server=Task.Run(async ()=>{
            for(var i=0;i<2;i++){
                var request=await station.ReceiveAsync();
                Assert.True(MarklinCs2FrameCodec.TryParse(request.Buffer,out var parsed));
                Assert.Equal(0x18,parsed.Command);
                var response=MarklinCs2FrameCodec.Create(0x18,0,true,0x4711,new byte[8]);
                await station.SendAsync(response,request.RemoteEndPoint);
            }
        });
        await using var adapter=new MarklinCs2Adapter(Profile(commandPort),TimeSpan.FromSeconds(2),feedbackPort);
        await adapter.ConnectAsync();Assert.True(adapter.IsConnected);
        await adapter.DisconnectAsync();Assert.False(adapter.IsConnected);
        await adapter.ConnectAsync();Assert.True(adapter.IsConnected);
        await adapter.DisconnectAsync();
        await server.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Missing_Ping_Response_Times_Out_And_Leaves_Adapter_Disconnected()
    {
        using var station=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
        var commandPort=((IPEndPoint)station.Client.LocalEndPoint!).Port;
        int feedbackPort;
        using(var feedback=new UdpClient(new IPEndPoint(IPAddress.Loopback,0))) feedbackPort=((IPEndPoint)feedback.Client.LocalEndPoint!).Port;
        await using var adapter=new MarklinCs2Adapter(Profile(commandPort),TimeSpan.FromMilliseconds(80),feedbackPort);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>adapter.ConnectAsync());
        Assert.False(adapter.IsConnected);
    }

    [Fact]
    public async Task Invalid_Ping_Response_Is_Ignored_Until_Timeout()
    {
        using var station=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
        var commandPort=((IPEndPoint)station.Client.LocalEndPoint!).Port;
        int feedbackPort;
        using(var feedback=new UdpClient(new IPEndPoint(IPAddress.Loopback,0))) feedbackPort=((IPEndPoint)feedback.Client.LocalEndPoint!).Port;
        var server=Task.Run(async ()=>{
            var request=await station.ReceiveAsync();
            var malformed=new byte[13];
            await station.SendAsync(malformed,request.RemoteEndPoint);
        });
        await using var adapter=new MarklinCs2Adapter(Profile(commandPort),TimeSpan.FromMilliseconds(120),feedbackPort);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>adapter.ConnectAsync());
        Assert.False(adapter.IsConnected);
        await server;
    }
}
