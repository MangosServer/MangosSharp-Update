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

using Mangos.Cluster.DataStores;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Handlers;
using Mangos.Cluster.Handlers.Guild;
using Mangos.Cluster.Network;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Zip;
using Functions = Mangos.Common.Legacy.Globals.Functions;

namespace Mangos.Cluster;

public class ClusterServiceLocator
{
    public Common.Legacy.Functions CommonFunctions { get; set; }

    public Functions CommonGlobalFunctions { get; set; }

    public Globals.Functions Functions { get; set; }

    public MangosGlobalConstants GlobalConstants { get; set; }

    public ZipService GlobalZip { get; set; }

    public NativeMethods NativeMethods { get; set; }

    public Packets Packets { get; set; }

    public WC_Guild WcGuild { get; set; }

    public WcHandlerCharacter WcHandlerCharacter { get; set; }

    public WC_Handlers WcHandlers { get; set; }

    public WC_Handlers_Auth WcHandlersAuth { get; set; }

    public WC_Handlers_Battleground WcHandlersBattleground { get; set; }

    public WC_Handlers_Chat WcHandlersChat { get; set; }

    public WcHandlersGroup WcHandlersGroup { get; set; }

    public WcHandlersGuild WcHandlersGuild { get; set; }

    public WcHandlersMisc WcHandlersMisc { get; set; }

    public WcHandlersMovement WcHandlersMovement { get; set; }

    public WcHandlersSocial WcHandlersSocial { get; set; }

    public WcHandlersTickets WcHandlersTickets { get; set; }

    public WcNetwork WcNetwork { get; set; }

    public LegacyWorldCluster WorldCluster { get; set; }

    public WorldServerClass WorldServerClass { get; set; }

    public WS_DBCDatabase WsDbcDatabase { get; set; }

    public WS_DBCLoad WsDbcLoad { get; set; }

    public WsHandlerChannels WsHandlerChannels { get; set; }
}
