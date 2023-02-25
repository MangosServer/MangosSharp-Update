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

using System.Collections.Immutable;

namespace Mangos.Configuration;

public sealed class WorldConfiguration
{
    public string? ClusterConnectHost { get; init; }
    public int ClusterConnectPort { get; init; }
    public string? LocalConnectHost { get; init; }
    public int LocalConnectPort { get; init; }

    public string? AccountDatabase { get; init; }
    public string? CharacterDatabase { get; init; }
    public string? WorldDatabase { get; init; }

    public ImmutableArray<int> Maps { get; init; }
    public ImmutableArray<string> ScriptsCompiler { get; init; }
    public bool VMapsEnabled { get; init; }
    public int MapResolution { get; init; }

    public string? CommandCharacter { get; init; }
    public bool GlobalAuction { get; init; }

    public bool LineOfSightEnabled { get; set; }
    public bool HeightCalcEnabled { get; set; }

    public float ManaRegenerationRate { get; init; }
    public float HealthRegenerationRate { get; init; }
    public float XPRate { get; init; }

    public string? LogType { get; init; }
    public string? LogConfig { get; init; }

    public bool CreateBattlegrounds { get; init; }
    public bool CreatePartyInstances { get; init; }
    public bool CreateRaidInstances { get; init; }
    public bool CreateOther { get; init; }

    public int SaveTimer { get; init; }
    public int WeatherTimer { get; init; }
}
