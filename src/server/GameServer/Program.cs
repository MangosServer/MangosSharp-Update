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
using GameServer;
using Mangos.Cluster;
using Mangos.Configuration;
using Mangos.Logging;
using Mangos.MySql;
using Mangos.Tcp;
using Mangos.World;

Console.Title = "Game server";

var builder = new ContainerBuilder();
builder.RegisterModule<ConfigurationModule>();
builder.RegisterModule<LoggingModule>();
builder.RegisterModule<MySqlModule>();
builder.RegisterModule<TcpModule>();
builder.RegisterModule<GameModule>();
builder.RegisterModule<LegacyClusterModule>();
builder.RegisterModule<LegacyWorldModule>();

var container = builder.Build();
var configuration = container.Resolve<MangosConfiguration>();
var logger = container.Resolve<IMangosLogger>();
var tcpServer = container.Resolve<TcpServer>();
var legacyWorldCluster = container.Resolve<LegacyWorldCluster>();
WorldServiceLocator.Container = container;
var worldServer = container.Resolve<WorldServer>();

logger.Trace("  __  __      _  _  ___  ___  ___               ");
logger.Trace(@"|  \/  |__ _| \| |/ __|/ _ \/ __|   We Love    ");
logger.Trace(@"| |\/| / _` | .` | (_ | (_) \__ \   Vanilla Wow");
logger.Trace(@"|_|  |_\__,_|_|\_|\___|\___/|___/              ");
logger.Trace("                                                ");
logger.Trace("Website / Forum / Support: https://getmangos.eu/");

logger.Information("Starting legacy cluster server");
await legacyWorldCluster.StartAsync();

logger.Information("Starting legacy world server");
await worldServer.StartAsync();

logger.Information("Starting game tcp server");
var clusterEndpoint = configuration!.Cluster?.ClusterServerEndpoint!;

if(clusterEndpoint != null)
{
    await tcpServer!.RunAsync(configuration!.Cluster?.ClusterServerEndpoint!);
}
