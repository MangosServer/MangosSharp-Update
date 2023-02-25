//
// Copyright (C) 2013-2023 getMaNGOS <https://getmangos.eu>
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using GameServer.Responses;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Network;
using Mangos.Tcp;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace GameServer.Network;

internal sealed class GameTcpConnection : ITcpConnection
{
    private const int MAX_PACKET_LENGTH = 10000;
    private readonly IHandlerDispatcher[] dispatchers;
    private readonly ClientClass legacyClientClass;
    private readonly MemoryPool<byte> memoryPool = MemoryPool<byte>.Shared;

    public GameTcpConnection(ClientClass legacyClientClass, IEnumerable<IHandlerDispatcher> dispatchers)
    {
        if (dispatchers is null)
        {
            throw new ArgumentNullException(nameof(dispatchers));
        }

        this.legacyClientClass = legacyClientClass ?? throw new ArgumentNullException(nameof(legacyClientClass));

        this.dispatchers = dispatchers.ToArray();
    }

    private void DecodePacketHeader(Span<byte> data)
    {
        if(!legacyClientClass.Client.PacketEncryption.IsEncryptionEnabled)
        {
            return;
        }

        var key = legacyClientClass.Client.PacketEncryption.Key;
        if ((key == null) || (key.Length == 0))
        {
            return;
        }

        var hash = legacyClientClass.Client.PacketEncryption.Hash;
        if ((hash == null) || (hash.Length == 0))
        {
            return;
        }

        for (var i = 0; i < 6; i++)
        {
            var tmp = data[i];
            data[i] = (byte)(hash[key[1]] ^ ((256 + data[i] - key[0]) % 256));
            key[0] = tmp;
            key[1] = (byte)((key[1] + 1) % 40);
        }
    }

    private async Task ExecuteHandlerAsync(
        IHandlerDispatcher dispatcher,
        Memory<byte> body,
        Socket socket,
        CancellationToken cancellationToken)
    {
        if (dispatcher is null)
        {
            throw new ArgumentNullException(nameof(dispatcher));
        }

        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }
        if (dispatcher == null)
        {
            return;
        }

        using var result = await dispatcher.ExecuteAsync(new PacketReader(body));
        if (result == null)
        {
            return;
        }

        using var memoryOwner = memoryPool.Rent(MAX_PACKET_LENGTH);
        if (memoryOwner == null)
        {
            return;
        }

        foreach (var response in result.GetResponseMessages())
        {
            await SendAsync(socket, memoryOwner.Memory, response, cancellationToken);
        }
    }

    private void ExecuteLegacyHandler(ReadOnlyMemory<byte> packet)
    {
        var legacyPacket = new PacketClass(packet.ToArray());
        legacyClientClass.OnPacket(legacyPacket);
    }

    private async Task HandlePacketAsync(Socket socket, CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        using var memoryOwner = memoryPool.Rent(MAX_PACKET_LENGTH);
        if (memoryOwner == null)
        {
            return;
        }

        var header = await ReadPacketHeaderAsync(socket, memoryOwner.Memory, cancellationToken);
        var body = await ReadPacketBodyAsync(socket, memoryOwner.Memory, cancellationToken);

        var opcode = (Opcodes)BinaryPrimitives.ReadUInt32LittleEndian(header.Span[2..]);

        var dispatcher = Array.Find(dispatchers, x => x.Opcode == opcode);
        if(dispatcher != null)
        {
            await ExecuteHandlerAsync(dispatcher, body, socket, cancellationToken);
        } else
        {
            ExecuteLegacyHandler(memoryOwner.Memory[..(header.Length + body.Length)]);
        }
    }

    private async ValueTask ReadAsync(Socket socket, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        if (buffer.Length == 0)
        {
            return;
        }

        var length = await socket.ReceiveAsync(buffer, cancellationToken);
        if(length != buffer.Length)
        {
            throw new NotImplementedException("Invalid number of bytes was readed from socket");
        }
    }

    private async ValueTask<Memory<byte>> ReadPacketBodyAsync(
        Socket socket,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        var length = BinaryPrimitives.ReadUInt16BigEndian(buffer.Span) - 4;
        var body = buffer.Slice(6, length);
        await ReadAsync(socket, body, cancellationToken);
        return body;
    }

    private async ValueTask<Memory<byte>> ReadPacketHeaderAsync(
        Socket socket,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (socket == null)
        {
            return Memory<byte>.Empty;
        }

        var header = buffer[..6];
        await ReadAsync(socket, header, cancellationToken);
        DecodePacketHeader(header.Span);
        return header;
    }

    private async ValueTask SendAsync(Socket socket, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (socket == null)
        {
            return;
        }

        var length = await socket.SendAsync(buffer, cancellationToken);
        if(length != buffer.Length)
        {
            throw new NotImplementedException("Invalid number of bytes was sended to socket");
        }
    }

    private async ValueTask SendAsync(
        Socket socket,
        Memory<byte> buffer,
        IResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var packetWriter = new PacketWriter(buffer, response.Opcode);
        response.Write(packetWriter);
        var packet = packetWriter.ToPacket();
        EncodePacketHeader(packet.Span);
        await SendAsync(socket, packet, cancellationToken);
    }

    private async ValueTask WaitForNextPacketAsync(Socket socket, CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        var array = Array.Empty<byte>();
        if ((array == null) || (array.Length == 0))
        {
            return;
        }

        await socket.ReceiveAsync(array, cancellationToken);
    }

    public void EncodePacketHeader(Span<byte> data)
    {
        if(!legacyClientClass.Client.PacketEncryption.IsEncryptionEnabled)
        {
            return;
        }

        var key = legacyClientClass.Client.PacketEncryption.Key;
        if ((key == null) || (key.Length == 0))
        {
            return;
        }

        var hash = legacyClientClass.Client.PacketEncryption.Hash;
        if ((hash == null) || (hash.Length == 0))
        {
            return;
        }

        for (var i = 0; i < 4; i++)
        {
            data[i] = (byte)(((hash[key[3]] ^ data[i]) + key[2]) % 256);
            key[2] = data[i];
            key[3] = (byte)((key[3] + 1) % 40);
        }
    }

    public async Task ExecuteAsync(Socket socket, CancellationToken cancellationToken)
    {
        if (socket is null)
        {
            throw new ArgumentNullException(nameof(socket));
        }

        legacyClientClass.Socket = socket;
        await legacyClientClass.OnConnectAsync();

        while(!cancellationToken.IsCancellationRequested)
        {
            await WaitForNextPacketAsync(socket, cancellationToken);
            await HandlePacketAsync(socket, cancellationToken);
        }
    }
}
