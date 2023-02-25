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

using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Map;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.DataStores;
using Mangos.World.Network;
using Mangos.World.Objects;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Mangos.World.Maps;

public partial class WS_Maps
{
    private readonly DataStoreProvider dataStoreProvider;

    public Dictionary<int, TArea> AreaTable;

    public string MapList;

    public Dictionary<uint, TMap> Maps;

    public int RESOLUTION_ZMAP;

    public WS_Maps(DataStoreProvider dataStoreProvider)
    {
        this.dataStoreProvider = dataStoreProvider ??
            throw new ArgumentNullException(nameof(dataStoreProvider));
        AreaTable = new Dictionary<int, TArea>();
        RESOLUTION_ZMAP = 0;
        Maps = new Dictionary<uint, TMap>();
    }

    private float GetHeight(uint Map, byte MapTileX, byte MapTileY, byte MapTileLocalX, byte MapTileLocalY)
    {
        checked
        {
            if(MapTileLocalX > RESOLUTION_ZMAP)
            {
                MapTileX = (byte)(MapTileX + 1);
                MapTileLocalX = (byte)(MapTileLocalX - (RESOLUTION_ZMAP + 1));
            } else if (MapTileLocalX < 0)
            {
                MapTileX = (byte)(MapTileX - 1);
                MapTileLocalX = (byte)(((short)unchecked(-MapTileLocalX)) - 1);
            }
            if(MapTileLocalY > RESOLUTION_ZMAP)
            {
                MapTileY = (byte)(MapTileY + 1);
                MapTileLocalY = (byte)(MapTileLocalY - (RESOLUTION_ZMAP + 1));
            } else if (MapTileLocalY < 0)
            {
                MapTileY = (byte)(MapTileY - 1);
                MapTileLocalY = (byte)(((short)unchecked(-MapTileLocalY)) - 1);
            }
            return Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTileLocalX, MapTileLocalY];
        }
    }

    public int GetAreaFlag(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_FLAGS *
                    (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_FLAGS *
                    (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY));
            return (Maps[(uint)Map].Tiles[MapTileX, MapTileY] == null)
                ? 0
                : Maps[(uint)Map].Tiles[MapTileX, MapTileY].AreaFlag[MapTile_LocalX, MapTile_LocalY];
        }
    }

    public int GetAreaIDByMapandParent(int mapId, int parentID)
    {
        foreach(var thisArea in AreaTable)
        {
            var thisMap = thisArea.Value.mapId;
            var thisParent = thisArea.Value.Zone;
            if((thisMap == mapId) && (thisParent == parentID))
            {
                return thisArea.Key;
            }
        }
        return -999;
    }

    public void GetMapTile(float x, float y, ref byte MapTileX, ref byte MapTileY)
    {
        checked
        {
            MapTileX = (byte)(32f - (ValidateMapCoord(x) / WorldServiceLocator.GlobalConstants.SIZE));
            MapTileY = (byte)(32f - (ValidateMapCoord(y) / WorldServiceLocator.GlobalConstants.SIZE));
        }
    }

    public byte GetMapTileX(float x)
    { return checked((byte)(32f - (ValidateMapCoord(x) / WorldServiceLocator.GlobalConstants.SIZE))); }

    public byte GetMapTileY(float y)
    { return checked((byte)(32f - (ValidateMapCoord(y) / WorldServiceLocator.GlobalConstants.SIZE))); }

    public bool GetObjectHitPos(
        ref WS_Base.BaseObject obj,
        ref WS_Base.BaseObject obj2,
        ref float rx,
        ref float ry,
        ref float rz,
        float pModifyDist)
    {
        if(obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if(obj2 is null)
        {
            throw new ArgumentNullException(nameof(obj2));
        }

        rx = ValidateMapCoord(rx);
        ry = ValidateMapCoord(ry);
        rz = ValidateMapCoord(rz);
        return GetObjectHitPos(
            obj.MapID,
            obj.positionX,
            obj.positionY,
            obj.positionZ + 2f,
            obj2.positionX,
            obj2.positionY,
            obj2.positionZ + 2f,
            ref rx,
            ref ry,
            ref rz,
            pModifyDist);
    }

    public bool GetObjectHitPos(
        ref WS_Base.BaseObject obj,
        float x2,
        float y2,
        float z2,
        ref float rx,
        ref float ry,
        ref float rz,
        float pModifyDist)
    {
        if(obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        rx = ValidateMapCoord(rx);
        ry = ValidateMapCoord(ry);
        rz = ValidateMapCoord(rz);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return GetObjectHitPos(
            obj.MapID,
            obj.positionX,
            obj.positionY,
            obj.positionZ + 2f,
            x2,
            y2,
            z2,
            ref rx,
            ref ry,
            ref rz,
            pModifyDist);
    }

    public bool GetObjectHitPos(
        uint MapID,
        float x1,
        float y1,
        float z1,
        float x2,
        float y2,
        float z2,
        ref float rx,
        ref float ry,
        ref float rz,
        float pModifyDist)
    {
        x1 = ValidateMapCoord(x1);
        y1 = ValidateMapCoord(y1);
        z1 = ValidateMapCoord(z1);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return false;
    }

    public byte GetSubMapTileX(float x)
    {
        return checked((byte)(RESOLUTION_ZMAP *
            (32f -
                (ValidateMapCoord(x) / WorldServiceLocator.GlobalConstants.SIZE) -
                Conversion.Fix(32f - (ValidateMapCoord(x) / WorldServiceLocator.GlobalConstants.SIZE)))));
    }

    public byte GetSubMapTileY(float y)
    {
        return checked((byte)(RESOLUTION_ZMAP *
            (32f -
                (ValidateMapCoord(y) / WorldServiceLocator.GlobalConstants.SIZE) -
                Conversion.Fix(32f - (ValidateMapCoord(y) / WorldServiceLocator.GlobalConstants.SIZE)))));
    }

    public byte GetTerrainType(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_TERRAIN *
                    (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_TERRAIN *
                    (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY));
            return (byte)((Maps[(uint)Map].Tiles[MapTileX, MapTileY] == null)
                ? 0
                : Maps[(uint)Map].Tiles[MapTileX, MapTileY].AreaTerrain[MapTile_LocalX, MapTile_LocalY]);
        }
    }

    public float GetVMapHeight(uint MapID, float x, float y, float z)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        z = ValidateMapCoord(z);
        return WorldServiceLocator.GlobalConstants.VMAP_INVALID_HEIGHT_VALUE;
    }

    public float GetWaterLevel(float x, float y, int Map)
    {
        x = ValidateMapCoord(x);
        y = ValidateMapCoord(y);
        checked
        {
            var MapTileX = (byte)(32f - (x / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTileY = (byte)(32f - (y / WorldServiceLocator.GlobalConstants.SIZE));
            var MapTile_LocalX = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_WATER *
                    (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX));
            var MapTile_LocalY = (byte)Math.Round(
                WorldServiceLocator.GlobalConstants.RESOLUTION_WATER *
                    (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY));
            return (Maps[(uint)Map].Tiles[MapTileX, MapTileY]?.WaterLevel[MapTile_LocalX, MapTile_LocalY]) ?? 0f;
        }
    }

    public float GetZCoord(float x, float y, uint Map)
    {
        checked
        {
            try
            {
                x = ValidateMapCoord(x);
                y = ValidateMapCoord(y);
                var MapTileX = (byte)(32f - (x / WorldServiceLocator.GlobalConstants.SIZE));
                var MapTileY = (byte)(32f - (y / WorldServiceLocator.GlobalConstants.SIZE));
                var MapTile_LocalX = (byte)Math.Round(
                    RESOLUTION_ZMAP * (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX));
                var MapTile_LocalY = (byte)Math.Round(
                    RESOLUTION_ZMAP * (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY));
                float xNormalized;
                float yNormalized;
                unchecked
                {
                    xNormalized = (RESOLUTION_ZMAP *
                            (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX)) -
                        MapTile_LocalX;
                    yNormalized = (RESOLUTION_ZMAP *
                            (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY)) -
                        MapTile_LocalY;
                    if(Maps[Map].Tiles[MapTileX, MapTileY] == null)
                    {
                        return 0f;
                    }
                }
                try
                {
                    var topHeight = WorldServiceLocator.Functions
                        .MathLerp(
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY),
                            GetHeight(Map, MapTileX, MapTileY, (byte)(MapTile_LocalX + 1), MapTile_LocalY),
                            xNormalized);
                    var bottomHeight = WorldServiceLocator.Functions
                        .MathLerp(
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, (byte)(MapTile_LocalY + 1)),
                            GetHeight(
                                Map,
                                MapTileX,
                                MapTileY,
                                (byte)(MapTile_LocalX + 1),
                                (byte)(MapTile_LocalY + 1)),
                            xNormalized);
                    return WorldServiceLocator.Functions.MathLerp(topHeight, bottomHeight, yNormalized);
                } catch(Exception ex)
                {
                    var GetZCoord = Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY];
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.WARNING,
                            "GetHeight threw an Exception : GetZCoord {0}, {1}",
                            GetZCoord,
                            ex);
                    return GetZCoord;
                }
            } catch(Exception ex2)
            {
                const float GetZCoord = 0f;
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "GetZCoord threw an Exception : Coord X {0} Coord Y {1} Coord Z {2}, {3}",
                        x,
                        y,
                        GetZCoord,
                        ex2);
                return GetZCoord;
            }
        }
    }

    public float GetZCoord(float x, float y, float z, uint Map)
    {
        checked
        {
            try
            {
                x = ValidateMapCoord(x);
                y = ValidateMapCoord(y);
                z = ValidateMapCoord(z);
                var MapTileX = (byte)(32f - (x / WorldServiceLocator.GlobalConstants.SIZE));
                var MapTileY = (byte)(32f - (y / WorldServiceLocator.GlobalConstants.SIZE));
                var MapTile_LocalX = (byte)Math.Round(
                    RESOLUTION_ZMAP * (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX));
                var MapTile_LocalY = (byte)Math.Round(
                    RESOLUTION_ZMAP * (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY));
                float xNormalized;
                float yNormalized;
                unchecked
                {
                    xNormalized = (RESOLUTION_ZMAP *
                            (32f - (x / WorldServiceLocator.GlobalConstants.SIZE) - MapTileX)) -
                        MapTile_LocalX;
                    yNormalized = (RESOLUTION_ZMAP *
                            (32f - (y / WorldServiceLocator.GlobalConstants.SIZE) - MapTileY)) -
                        MapTile_LocalY;
                    if(Maps[Map].Tiles[MapTileX, MapTileY] == null)
                    {
                        var VMapHeight2 = GetVMapHeight(Map, x, y, z + 5f);
                        return (VMapHeight2 != WorldServiceLocator.GlobalConstants.VMAP_INVALID_HEIGHT_VALUE)
                            ? VMapHeight2
                            : 0f;
                    }
                    if(Math.Abs(Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY] - z) >=
                        2f)
                    {
                        var VMapHeight = GetVMapHeight(Map, x, y, z + 5f);
                        if(VMapHeight != WorldServiceLocator.GlobalConstants.VMAP_INVALID_HEIGHT_VALUE)
                        {
                            return VMapHeight;
                        }
                    }
                }
                try
                {
                    var topHeight = WorldServiceLocator.Functions
                        .MathLerp(
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY),
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY),
                            xNormalized);
                    var bottomHeight = WorldServiceLocator.Functions
                        .MathLerp(
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY),
                            GetHeight(Map, MapTileX, MapTileY, MapTile_LocalX, MapTile_LocalY),
                            xNormalized);
                    return WorldServiceLocator.Functions.MathLerp(topHeight, bottomHeight, yNormalized);
                } catch(Exception ex)
                {
                    var GetZCoord = Maps[Map].Tiles[MapTileX, MapTileY].ZCoord[MapTile_LocalX, MapTile_LocalY];
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.WARNING,
                            "GetZCoord threw an Exception : Coord X {0} Coord Y {1} Coord Z {2}, {3}",
                            x,
                            y,
                            GetZCoord,
                            ex);
                    return GetZCoord;
                }
            } catch(Exception ex2)
            {
                WorldServiceLocator.WorldServer.Log.WriteLine(LogType.FAILED, ex2.ToString());
                return z;
            }
        }
    }

    public async Task InitializeMapsAsync()
    {
        var e = WorldServiceLocator.MangosConfiguration.World.Maps.GetEnumerator();
        if(e.MoveNext())
        {
            MapList = Conversions.ToString(e.Current);
            while(e.MoveNext())
            {
                MapList = Conversions.ToString(
                    Operators.AddObject(MapList, Operators.ConcatenateObject(", ", e.Current)));
            }
        }
        foreach(var map2 in WorldServiceLocator.MangosConfiguration.World.Maps)
        {
            var id = Conversions.ToUInteger(map2);
            TMap map = new(checked((int)id), await dataStoreProvider.GetDataStoreAsync("Map.dbc"));
        }
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.INFORMATION, "Initalizing: {0} Maps initialized.", Maps.Count);
    }

    public bool IsInLineOfSight(ref WS_Base.BaseObject obj, ref WS_Base.BaseObject obj2)
    {
        if(obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if(obj2 is null)
        {
            throw new ArgumentNullException(nameof(obj2));
        }

        return IsInLineOfSight(
            obj.MapID,
            obj.positionX,
            obj.positionY,
            obj.positionZ + 2f,
            obj2.positionX,
            obj2.positionY,
            obj2.positionZ + 2f);
    }

    public bool IsInLineOfSight(ref WS_Base.BaseObject obj, float x2, float y2, float z2)
    {
        if(obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return IsInLineOfSight(obj.MapID, obj.positionX, obj.positionY, obj.positionZ + 2f, x2, y2, z2);
    }

    public bool IsInLineOfSight(uint MapID, float x1, float y1, float z1, float x2, float y2, float z2)
    {
        x1 = ValidateMapCoord(x1);
        y1 = ValidateMapCoord(y1);
        z1 = ValidateMapCoord(z1);
        x2 = ValidateMapCoord(x2);
        y2 = ValidateMapCoord(y2);
        z2 = ValidateMapCoord(z2);
        return true;
    }

    public bool IsOutsideOfMap(ref WS_Base.BaseObject objCharacter)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        return false;
    }

    public void LoadSpawns(byte TileX, byte TileY, uint TileMap, uint TileInstance)
    {
        checked
        {
            var MinX = (32 - TileX) * WorldServiceLocator.GlobalConstants.SIZE;
            var MaxX = (32 - (TileX + 1)) * WorldServiceLocator.GlobalConstants.SIZE;
            var MinY = (32 - TileY) * WorldServiceLocator.GlobalConstants.SIZE;
            var MaxY = (32 - (TileY + 1)) * WorldServiceLocator.GlobalConstants.SIZE;
            if(MinX > MaxX)
            {
                (MaxX, MinX) = (MinX, MaxX);
            }
            if(MinY > MaxY)
            {
                (MaxY, MinY) = (MinY, MaxY);
            }
            var InstanceGuidAdd = 0uL;
            if(TileInstance > 0L)
            {
                InstanceGuidAdd = Convert.ToUInt64(
                    decimal.Add(
                        new decimal(1000000L),
                        decimal.Multiply(new decimal(TileInstance - 1L), new decimal(100000L))));
            }
            DataTable MysqlQuery = new();
            WorldServiceLocator.WorldServer.WorldDatabase
                .Query(
                    $"SELECT * FROM creature LEFT OUTER JOIN game_event_creature ON creature.guid = game_event_creature.guid WHERE map={TileMap} AND position_X BETWEEN '{MinX}' AND '{MaxX}' AND position_Y BETWEEN '{MinY}' AND '{MaxY}';",
                    ref MysqlQuery);
            IEnumerator enumerator = default;
            try
            {
                enumerator = MysqlQuery.Rows.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    var row = (DataRow)enumerator.Current;
                    if(WorldServiceLocator.WorldServer.WORLD_CREATUREs
                        .ContainsKey(
                            Convert.ToUInt64(
                                decimal.Add(
                                    decimal.Add(new decimal(row.As<long>("guid")), new decimal(InstanceGuidAdd)),
                                    new decimal(WorldServiceLocator.GlobalConstants.GUID_UNIT)))))
                    {
                        continue;
                    }
                    try
                    {
                        WS_Creatures.CreatureObject tmpCr = new(
                            Convert.ToUInt64(
                                decimal.Add(new decimal(row.As<long>("guid")), new decimal(InstanceGuidAdd))),
                            row);
                        if(tmpCr.GameEvent == 0)
                        {
                            tmpCr.instance = TileInstance;
                            tmpCr.AddToWorld();
                        }
                    } catch(Exception ex4)
                    {
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.CRITICAL,
                                "Error when creating creature [{0}].{1}{2}",
                                row["id"],
                                Environment.NewLine,
                                ex4.ToString());
                    }
                }
            } finally
            {
                if(enumerator is IDisposable)
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }
            MysqlQuery.Clear();
            WorldServiceLocator.WorldServer.WorldDatabase
                .Query(
                    $"SELECT * FROM gameobject LEFT OUTER JOIN game_event_gameobject ON gameobject.guid = game_event_gameobject.guid WHERE map={TileMap} AND spawntimesecs>=0 AND position_X BETWEEN '{MinX}' AND '{MaxX}' AND position_Y BETWEEN '{MinY}' AND '{MaxY}';",
                    ref MysqlQuery);
            IEnumerator enumerator2 = default;
            try
            {
                enumerator2 = MysqlQuery.Rows.GetEnumerator();
                while(enumerator2.MoveNext())
                {
                    var row = (DataRow)enumerator2.Current;
                    if(WorldServiceLocator.WorldServer.WORLD_GAMEOBJECTs
                            .ContainsKey(
                                row.As<ulong>("guid") +
                                        InstanceGuidAdd +
                                        WorldServiceLocator.GlobalConstants.GUID_GAMEOBJECT) ||
                        WorldServiceLocator.WorldServer.WORLD_GAMEOBJECTs
                            .ContainsKey(
                                row.As<ulong>("guid") +
                                        InstanceGuidAdd +
                                        WorldServiceLocator.GlobalConstants.GUID_TRANSPORT))
                    {
                        continue;
                    }
                    try
                    {
                        WS_GameObjects.GameObject tmpGo = new(row.As<ulong>("guid") + InstanceGuidAdd, row);
                        if(tmpGo.GameEvent == 0)
                        {
                            tmpGo.instance = TileInstance;
                            tmpGo.AddToWorld();
                        }
                    } catch(Exception ex3)
                    {
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.CRITICAL,
                                "Error when creating gameobject [{0}].{1}{2}",
                                row["id"],
                                Environment.NewLine,
                                ex3.ToString());
                    }
                }
            } finally
            {
                if(enumerator2 is IDisposable)
                {
                    (enumerator2 as IDisposable)?.Dispose();
                }
            }
            MysqlQuery.Clear();
            WorldServiceLocator.WorldServer.CharacterDatabase
                .Query(
                    $"SELECT * FROM corpse WHERE map={TileMap} AND instance={TileInstance} AND position_x BETWEEN '{MinX}' AND '{MaxX}' AND position_y BETWEEN '{MinY}' AND '{MaxY}';",
                    ref MysqlQuery);
            IEnumerator enumerator3 = default;
            try
            {
                enumerator3 = MysqlQuery.Rows.GetEnumerator();
                while(enumerator3.MoveNext())
                {
                    var InfoRow = (DataRow)enumerator3.Current;
                    if(!WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs
                        .ContainsKey(
                            Conversions.ToULong(InfoRow["guid"]) + WorldServiceLocator.GlobalConstants.GUID_CORPSE))
                    {
                        try
                        {
                            WS_Corpses.CorpseObject tmpCorpse = new(Conversions.ToULong(InfoRow["guid"]), InfoRow)
                            {
                                instance = TileInstance
                            };
                            tmpCorpse.AddToWorld();
                        } catch(Exception ex)
                        {
                            WorldServiceLocator.WorldServer.Log
                                .WriteLine(
                                    LogType.CRITICAL,
                                    "Error when creating corpse [{0}].{1}{2}",
                                    InfoRow["guid"],
                                    Environment.NewLine,
                                    ex.ToString());
                        }
                    }
                }
            } finally
            {
                if(enumerator3 is IDisposable)
                {
                    (enumerator3 as IDisposable)?.Dispose();
                }
            }
            try
            {
                WorldServiceLocator.WorldServer.WORLD_TRANSPORTs_Lock.AcquireReaderLock(1000);
                foreach(var Transport in WorldServiceLocator.WorldServer.WORLD_TRANSPORTs)
                {
                    try
                    {
                        if((Transport.Value.MapID == TileMap) &&
                            (Transport.Value.positionX >= MinX) &&
                            (Transport.Value.positionX <= MaxX) &&
                            (Transport.Value.positionY >= MinY) &&
                            (Transport.Value.positionY <= MaxY))
                        {
                            if(!Maps[TileMap].Tiles[TileX, TileY].GameObjectsHere.Contains(Transport.Value.GUID))
                            {
                                Maps[TileMap].Tiles[TileX, TileY].GameObjectsHere.Add(Transport.Value.GUID);
                            }
                            Transport.Value.NotifyEnter();
                        }
                    } catch(Exception ex)
                    {
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.CRITICAL,
                                "Error when creating transport [{0}].{1}{2}",
                                Transport.Key - WorldServiceLocator.GlobalConstants.GUID_MO_TRANSPORT,
                                Environment.NewLine,
                                ex.ToString());
                    }
                }
            } catch(Exception ex2)
            {
                WorldServiceLocator.WorldServer.Log.WriteLine(LogType.CRITICAL, "Error in Transport Loop", ex2);
            } finally
            {
                WorldServiceLocator.WorldServer.WORLD_TRANSPORTs_Lock.ReleaseReaderLock();
            }
        }
    }

    public void SendTransferAborted(ref WS_Network.ClientClass client, int Map, TransferAbortReason Reason)
    {
        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] SMSG_TRANSFER_ABORTED [{2}:{3}]",
                client.IP,
                client.Port,
                Map,
                Reason);
        Packets.Packets.PacketClass p = new(Opcodes.SMSG_TRANSFER_ABORTED);
        try
        {
            p.AddInt32(Map);
            p.AddInt16((short)Reason);
            client.Send(ref p);
        } finally
        {
            p.Dispose();
        }
    }

    public void UnloadSpawns(byte TileX, byte TileY, uint TileMap)
    {
        checked
        {
            var MinX = (32 - TileX) * WorldServiceLocator.GlobalConstants.SIZE;
            var MaxX = (32 - (TileX + 1)) * WorldServiceLocator.GlobalConstants.SIZE;
            var MinY = (32 - TileY) * WorldServiceLocator.GlobalConstants.SIZE;
            var MaxY = (32 - (TileY + 1)) * WorldServiceLocator.GlobalConstants.SIZE;
            if(MinX > MaxX)
            {
                (MaxX, MinX) = (MinX, MaxX);
            }
            if(MinY > MaxY)
            {
                (MaxY, MinY) = (MinY, MaxY);
            }
            try
            {
                WorldServiceLocator.WorldServer.WORLD_CREATUREs_Lock
                    .AcquireReaderLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                foreach(var Creature in WorldServiceLocator.WorldServer.WORLD_CREATUREs)
                {
                    if((Creature.Value.MapID == TileMap) &&
                        (Creature.Value.SpawnX >= MinX) &&
                        (Creature.Value.SpawnX <= MaxX) &&
                        (Creature.Value.SpawnY >= MinY) &&
                        (Creature.Value.SpawnY <= MaxY))
                    {
                        Creature.Value.Destroy();
                    }
                }
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log.WriteLine(LogType.CRITICAL, ex.ToString(), null);
            } finally
            {
                WorldServiceLocator.WorldServer.WORLD_CREATUREs_Lock.ReleaseReaderLock();
            }
            foreach(var Gameobject in WorldServiceLocator.WorldServer.WORLD_GAMEOBJECTs)
            {
                if((Gameobject.Value.MapID == TileMap) &&
                    (Gameobject.Value.positionX >= MinX) &&
                    (Gameobject.Value.positionX <= MaxX) &&
                    (Gameobject.Value.positionY >= MinY) &&
                    (Gameobject.Value.positionY <= MaxY))
                {
                    Gameobject.Value.Destroy(Gameobject);
                }
            }
            foreach(var Corpseobject in WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs)
            {
                if((Corpseobject.Value.MapID == TileMap) &&
                    (Corpseobject.Value.positionX >= MinX) &&
                    (Corpseobject.Value.positionX <= MaxX) &&
                    (Corpseobject.Value.positionY >= MinY) &&
                    (Corpseobject.Value.positionY <= MaxY))
                {
                    Corpseobject.Value.Destroy();
                }
            }
        }
    }

    public float ValidateMapCoord(float coord)
    {
        if(coord > (32f * WorldServiceLocator.GlobalConstants.SIZE))
        {
            return 32f * WorldServiceLocator.GlobalConstants.SIZE;
        } else if(coord < ((-32f) * WorldServiceLocator.GlobalConstants.SIZE))
        {
            return (-32f) * WorldServiceLocator.GlobalConstants.SIZE;
        }
        return coord;
    }
}
