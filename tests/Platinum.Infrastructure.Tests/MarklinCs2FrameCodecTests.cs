using System;
using Platinum.Domain.Control;
using Platinum.Infrastructure.Control;
using Xunit;

namespace Platinum.Infrastructure.Tests;

public sealed class MarklinCs2FrameCodecTests
{
    [Fact]
    public void EncodesTheDocumentedThirteenByteCs2EthernetFrame()
    {
        var data = new byte[] { 0x00, 0x00, 0x08, 0x03, 0x03, 0x20 };
        var packet = MarklinCs2FrameCodec.Create(0x04, 0, false, 0x4711, data);

        Assert.Equal(13, packet.Length);
        Assert.Equal(new byte[] { 0x00, 0x08, 0x47, 0x11, 0x06 }, packet[..5]);
        Assert.True(MarklinCs2FrameCodec.TryParse(packet, out var frame));
        Assert.Equal(0x04, frame.Command);
        Assert.Equal(0x4711, frame.Hash);
        Assert.Equal(6, frame.DataLength);
        Assert.Equal(data, frame.Data[..6]);
    }

    [Fact]
    public void RejectsWrongLengthAndInvalidCs2Fingerprint()
    {
        Assert.False(MarklinCs2FrameCodec.TryParse(new byte[12], out _));
        Assert.Throws<ArgumentException>(() =>
            MarklinCs2FrameCodec.Create(0x04, 0, false, 0x1234, Array.Empty<byte>()));
    }

    [Fact]
    public void FactoryCreatesNativeCs2AdapterWithoutJmri()
    {
        var adapter = ControlStationAdapterFactory.Create(
            new ControlStationProfile("CS2", ControlStationKind.MarklinCs2, "192.168.1.20", 15731));

        Assert.IsType<MarklinCs2Adapter>(adapter);
        Assert.False(adapter.IsSimulation);
    }
}
