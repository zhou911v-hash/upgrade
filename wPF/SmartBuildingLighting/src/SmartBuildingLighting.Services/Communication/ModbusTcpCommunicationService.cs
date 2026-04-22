using System.Net.Sockets;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Services.Communication;

/// <summary>
/// 最小化 Modbus TCP 实现，仅覆盖系统当前所需的线圈与寄存器读写。
/// </summary>
public class ModbusTcpCommunicationService : ICommunicationBackend, IDisposable
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _transactionId;
    private CommunicationProfile? _profile;

    public bool IsConnected => _client?.Connected == true && _stream != null;

    public event EventHandler<CircuitStatusChangedEventArgs>? StatusChanged;

    public async Task<bool> ConnectAsync(CommunicationProfile profile, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken);

        _profile = profile;
        _client = new TcpClient();
        try
        {
            await _client.ConnectAsync(profile.Host, profile.Port, cancellationToken);
            _stream = _client.GetStream();
            return true;
        }
        catch
        {
            await DisconnectAsync(cancellationToken);
            return false;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }

    public async Task<bool> ReadCircuitStatusAsync(int address, CancellationToken cancellationToken = default)
    {
        ushort coilAddress = ResolveCoilAddress(address);
        var response = await SendRequestAsync(0x01, coilAddress, 1, null, cancellationToken);
        return response.Length >= 10 && (response[9] & 0x01) == 0x01;
    }

    public async Task<bool> WriteCircuitControlAsync(int address, bool state, CancellationToken cancellationToken = default)
    {
        ushort coilAddress = ResolveCoilAddress(address);
        await SendRequestAsync(0x05, coilAddress, state ? (ushort)0xFF00 : (ushort)0x0000, null, cancellationToken);
        await RaiseStatusChangedAsync(address, cancellationToken);
        return true;
    }

    public async Task<int> ReadBrightnessAsync(int address, CancellationToken cancellationToken = default)
    {
        ushort registerAddress = ResolveRegisterAddress(_profile?.BrightnessRegisterBase ?? 0, address);
        var response = await SendRequestAsync(0x03, registerAddress, 1, null, cancellationToken);
        return response.Length >= 11 ? Math.Clamp((int)ReadUInt16(response, 9), 0, 100) : 0;
    }

    public async Task<bool> WriteBrightnessAsync(int address, int brightness, CancellationToken cancellationToken = default)
    {
        ushort registerAddress = ResolveRegisterAddress(_profile?.BrightnessRegisterBase ?? 0, address);
        await SendRequestAsync(0x06, registerAddress, (ushort)Math.Clamp(brightness, 0, 100), null, cancellationToken);
        await RaiseStatusChangedAsync(address, cancellationToken);
        return true;
    }

    public async Task<float> ReadCurrentAsync(int address, CancellationToken cancellationToken = default)
    {
        ushort registerAddress = ResolveRegisterAddress(_profile?.CurrentRegisterBase ?? 0, address);
        var response = await SendRequestAsync(0x03, registerAddress, 1, null, cancellationToken);
        return response.Length >= 11 ? ReadUInt16(response, 9) / 100f : 0f;
    }

    public async Task<float> ReadPowerAsync(int address, CancellationToken cancellationToken = default)
    {
        ushort registerAddress = ResolveRegisterAddress(_profile?.PowerRegisterBase ?? 0, address);
        var response = await SendRequestAsync(0x03, registerAddress, 1, null, cancellationToken);
        return response.Length >= 11 ? ReadUInt16(response, 9) / 100f : 0f;
    }

    private async Task RaiseStatusChangedAsync(int address, CancellationToken cancellationToken)
    {
        StatusChanged?.Invoke(this, new CircuitStatusChangedEventArgs
        {
            Address = address,
            IsOn = await ReadCircuitStatusAsync(address, cancellationToken),
            Brightness = await ReadBrightnessAsync(address, cancellationToken),
            Current = await ReadCurrentAsync(address, cancellationToken),
            Power = await ReadPowerAsync(address, cancellationToken),
            Timestamp = DateTime.Now
        });
    }

    private async Task<byte[]> SendRequestAsync(byte functionCode, ushort startAddress, ushort valueOrCount, byte[]? payload, CancellationToken cancellationToken)
    {
        if (_stream == null || _profile == null)
            throw new InvalidOperationException("Modbus 连接尚未建立。");

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            ushort transactionId = ++_transactionId;
            byte unitId = _profile.UnitId;
            byte[] pdu = BuildPdu(functionCode, startAddress, valueOrCount, payload);
            ushort length = (ushort)(pdu.Length + 1);

            byte[] request = new byte[7 + pdu.Length];
            WriteUInt16(request, 0, transactionId);
            WriteUInt16(request, 2, 0);
            WriteUInt16(request, 4, length);
            request[6] = unitId;
            Buffer.BlockCopy(pdu, 0, request, 7, pdu.Length);

            await _stream.WriteAsync(request, cancellationToken);

            byte[] header = await ReadExactAsync(7, cancellationToken);
            ushort responseLength = ReadUInt16(header, 4);
            byte[] body = await ReadExactAsync(responseLength - 1, cancellationToken);

            if ((body[0] & 0x80) != 0)
                throw new IOException($"Modbus 从站返回异常码 {body[1]}。");

            byte[] response = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, response, 0, header.Length);
            Buffer.BlockCopy(body, 0, response, header.Length, body.Length);
            return response;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static byte[] BuildPdu(byte functionCode, ushort startAddress, ushort valueOrCount, byte[]? payload)
    {
        if (payload != null && payload.Length > 0)
        {
            byte[] pdu = new byte[5 + payload.Length];
            pdu[0] = functionCode;
            WriteUInt16(pdu, 1, startAddress);
            pdu[3] = (byte)payload.Length;
            Buffer.BlockCopy(payload, 0, pdu, 4, payload.Length);
            return pdu;
        }

        byte[] result = new byte[5];
        result[0] = functionCode;
        WriteUInt16(result, 1, startAddress);
        WriteUInt16(result, 3, valueOrCount);
        return result;
    }

    private async Task<byte[]> ReadExactAsync(int length, CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("Modbus 连接尚未建立。");

        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await _stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
                throw new IOException("Modbus 连接已关闭。");
            offset += read;
        }

        return buffer;
    }

    private ushort ResolveCoilAddress(int address) => (ushort)((_profile?.CoilBaseAddress ?? 0) + address - 1);

    private static ushort ResolveRegisterAddress(int baseAddress, int address) => (ushort)(baseAddress + address - 1);

    private static ushort ReadUInt16(byte[] buffer, int offset) => (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)(value & 0xFF);
    }

    public void Dispose()
    {
        _syncLock.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
    }
}
