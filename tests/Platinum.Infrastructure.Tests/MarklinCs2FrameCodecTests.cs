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
    public void BuildsDocumentedDccLocomotiveSpeedCommand()
    {
        var packet = MarklinCs2FrameCodec.CreateLocomotiveSpeed(0x4711, 0x0000C003, 0x0320);

        Assert.True(MarklinCs2FrameCodec.TryParse(packet, out var frame));
        Assert.Equal(0x04, frame.Command);
        Assert.Equal(3, frame.Priority);
        Assert.Equal(new byte[] { 0x00, 0x00, 0xC0, 0x03, 0x03, 0x20 }, frame.Data[..6]);
    }

    [Fact]
    public void BuildsDirectionFunctionAndAccessoryFramesWithoutSendingThem()
    {
        Assert.True(MarklinCs2FrameCodec.TryParse(
            MarklinCs2FrameCodec.CreateLocomotiveDirection(0x4711, 0x00000002, true), out var direction));
        Assert.Equal(0x05, direction.Command);
        Assert.Equal(2, direction.Data[4]);

        Assert.True(MarklinCs2FrameCodec.TryParse(
            MarklinCs2FrameCodec.CreateLocomotiveFunction(0x4711, 0x00000002, 7, true), out var function));
        Assert.Equal(0x06, function.Command);
        Assert.Equal(new byte[] { 7, 1 }, function.Data[4..6]);

        Assert.True(MarklinCs2FrameCodec.TryParse(
            MarklinCs2FrameCodec.CreateAccessory(0x4711, 0x00003003, 1, 1), out var accessory));
        Assert.Equal(0x0B, accessory.Command);
        Assert.Equal(new byte[] { 1, 1 }, accessory.Data[4..6]);
    }

    [Fact]
    public void RejectsOutOfRangeNativeCommandValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MarklinCs2FrameCodec.CreateLocomotiveSpeed(0x4711, 0xC003, 1024));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MarklinCs2FrameCodec.CreateLocomotiveFunction(0x4711, 0xC003, 32, true));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MarklinCs2FrameCodec.CreateAccessory(0x4711, 0x3003, 1, 32));
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
