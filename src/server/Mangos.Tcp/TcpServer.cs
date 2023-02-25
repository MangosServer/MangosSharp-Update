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

using Autofac;
using Mangos.Logging;
using System.Net;
using System.Net.Sockets;

namespace Mangos.Tcp;

public sealed class TcpServer
{
    private readonly IMangosLogger logger;
    private readonly ILifetimeScope lifetimeScope;

    public TcpServer(IMangosLogger logger, ILifetimeScope lifetimeScope)
    {
        this.logger = logger;
        this.lifetimeScope = lifetimeScope;
    }

    public async Task RunAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(IPEndPoint.Parse(endpoint));
        socket.Listen(10);

        logger.Information($"Tcp server was started on {endpoint}");

        while (!cancellationToken.IsCancellationRequested)
        {
            HandleClientConnectionAsync(await socket.AcceptAsync(cancellationToken), cancellationToken);
        }
    }

    private async void HandleClientConnectionAsync(Socket socket, CancellationToken cancellationToken)
    {
        if (socket.RemoteEndPoint is not IPEndPoint endpoint)
        {
            logger.Error("Unable to get remote endpoint");
            return;
        }

        logger.Information($"Tcp client was conntected {endpoint}");
        try
        {
            using var scope = lifetimeScope.BeginLifetimeScope();
            var tcpConnection = scope.Resolve<ITcpConnection>();
            await tcpConnection.ExecuteAsync(socket, cancellationToken);
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.ConnectionAborted)
        {
            logger.Information("Connection aborted");
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Unhandled exception");
        }
        finally
        {
            socket.Dispose();
        }

        logger.Information("Tcp client was disconected");
    }
}
