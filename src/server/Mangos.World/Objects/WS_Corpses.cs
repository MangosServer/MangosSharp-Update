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
using Mangos.Common.Globals;
using Mangos.World.Player;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace Mangos.World.Objects;

public class WS_Corpses
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    private ulong GetNewGUID()
    {
        ref var corpseGUIDCounter = ref WorldServiceLocator.WorldServer.CorpseGUIDCounter;
        corpseGUIDCounter = Convert.ToUInt64(decimal.Add(new decimal(corpseGUIDCounter), 1m));
        return WorldServiceLocator.WorldServer.CorpseGUIDCounter;
    }

    public class CorpseObject : WS_Base.BaseObject, IDisposable
    {
        private bool _disposedValue;

        public int Bytes1;

        public int Bytes2;
        public int DynFlags;

        public int Flags;

        public int Guild;

        public int[] Items;

        public int Model;

        public ulong Owner;

        public CorpseObject(ref WS_PlayerData.CharacterObject Character)
        {
            if(Character is null)
            {
                throw new ArgumentNullException(nameof(Character));
            }

            DynFlags = 0;
            Flags = 0;
            Owner = 0uL;
            Bytes1 = 0;
            Bytes2 = 0;
            Model = 0;
            Guild = 0;
            Items = (new int[19]);
            GUID = WorldServiceLocator.WSCorpses.GetNewGUID();
            checked
            {
                Bytes1 = unchecked((int)(((uint)Character.Race) << 8)) +
                    unchecked((int)(((uint)Character.Gender) << 16)) +
                    (Character.Skin << 24);
                Bytes2 = Character.Face +
                    (Character.HairStyle << 8) +
                    (Character.HairColor << 16) +
                    (Character.FacialHair << 24);
                Model = Character.Model;
                positionX = Character.positionX;
                positionY = Character.positionY;
                positionZ = Character.positionZ;
                orientation = Character.orientation;
                MapID = Character.MapID;
                Owner = Character.GUID;
                Character.corpseGUID = GUID;
                Character.corpsePositionX = positionX;
                Character.corpsePositionY = positionY;
                Character.corpsePositionZ = positionZ;
                Character.corpseMapID = (int)MapID;
                Character.corpseCorpseType = Character.IsPvP
                    ? CorpseType.CORPSE_RESURRECTABLE_PVP
                    : CorpseType.CORPSE_RESURRECTABLE_PVE;
                Character.corpseCorpseType = CorpseType;
                byte i = 0;
                do
                {
                    Items[i] = Character.Items.ContainsKey(i)
                        ? (Character.Items[i].ItemInfo.Model +
                            unchecked((int)(((uint)Character.Items[i].ItemInfo.InventoryType) << 24)))
                        : 0;
                    i = (byte)unchecked((uint)(i + 1));
                } while (i <= 18u);
                Flags = 4;
                WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs.Add(GUID, this);
            }
        }

        public CorpseObject(ulong cGUID, DataRow Info = null)
        {
            DynFlags = 0;
            Flags = 0;
            Owner = 0uL;
            Bytes1 = 0;
            Bytes2 = 0;
            Model = 0;
            Guild = 0;
            Items = (new int[19]);
            if(Info == null)
            {
                DataTable MySQLQuery = new();
                WorldServiceLocator.WorldServer.CharacterDatabase
                    .Query($"SELECT * FROM corpse WHERE guid = {cGUID};", ref MySQLQuery);
                if(MySQLQuery.Rows.Count <= 0)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(LogType.FAILED, "Corpse not found in database. [corpseGUID={0:X}]", cGUID);
                    return;
                }
                Info = MySQLQuery.Rows[0];
            }
            positionX = Conversions.ToSingle(Info["position_x"]);
            positionY = Conversions.ToSingle(Info["position_y"]);
            positionZ = Conversions.ToSingle(Info["position_z"]);
            orientation = Conversions.ToSingle(Info["orientation"]);
            MapID = Conversions.ToUInteger(Info["map"]);
            instance = Conversions.ToUInteger(Info["instance"]);
            Owner = Conversions.ToULong(Info["player"]);
            CorpseType = (CorpseType)Conversions.ToInteger(Info["corpse_type"]);
            Flags = 4;
            GUID = checked(cGUID + WorldServiceLocator.GlobalConstants.GUID_CORPSE);
            WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs.Add(GUID, this);
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!_disposedValue)
            {
                RemoveFromWorld();
                WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs.Remove(GUID);
            }
            _disposedValue = true;
        }

        public void AddToWorld()
        {
            WorldServiceLocator.WSMaps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            if(WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY] == null)
            {
                WorldServiceLocator.WSCharMovement.MAP_Load(CellX, CellY, MapID);
            }
            WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY].CorpseObjectsHere.Add(GUID);
            Packets.Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            checked
            {
                try
                {
                    Packets.Packets.UpdateClass tmpUpdate = new(
                        WorldServiceLocator.GlobalConstants.FIELD_MASK_SIZE_CORPSE);
                    try
                    {
                        packet.AddInt32(1);
                        packet.AddInt8(0);
                        FillAllUpdateFlags(ref tmpUpdate);
                        var updateClass = tmpUpdate;
                        var updateObject = this;
                        updateClass.AddToPacket(
                            ref packet,
                            ObjectUpdateType.UPDATETYPE_CREATE_OBJECT,
                            ref updateObject);
                    } finally
                    {
                        tmpUpdate.Dispose();
                    }
                    short i = -1;
                    do
                    {
                        short j = -1;
                        do
                        {
                            if((((short)unchecked(CellX + i)) >= 0) &&
                                (((short)unchecked(CellX + i)) <= 63) &&
                                (((short)unchecked(CellY + j)) >= 0) &&
                                (((short)unchecked(CellY + j)) <= 63) &&
                                ((WorldServiceLocator.WSMaps.Maps[MapID].Tiles[
                                        (short)unchecked(CellX + i),
                                        (short)unchecked(CellY + j)]?.PlayersHere.Count) >
                                    0))
                            {
                                var tMapTile = WorldServiceLocator.WSMaps.Maps[MapID].Tiles[
                                    (short)unchecked(CellX + i),
                                    (short)unchecked(CellY + j)];
                                foreach(var plGUID in tMapTile.PlayersHere.ToArray())
                                {
                                    int num;
                                    if(WorldServiceLocator.WorldServer.CHARACTERs.ContainsKey(plGUID))
                                    {
                                        var characterObject = WorldServiceLocator.WorldServer.CHARACTERs[plGUID];
                                        WS_Base.BaseObject objCharacter = this;
                                        num = characterObject.CanSee(ref objCharacter) ? 1 : 0;
                                    } else
                                    {
                                        num = 0;
                                    }
                                    if(num != 0)
                                    {
                                        WorldServiceLocator.WorldServer.CHARACTERs[plGUID].client
                                            .SendMultiplyPackets(ref packet);
                                        WorldServiceLocator.WorldServer.CHARACTERs[plGUID].corpseObjectsNear
                                            .Add(GUID);
                                        SeenBy.Add(plGUID);
                                    }
                                }
                            }
                            j = (short)unchecked(j + 1);
                        } while (j <= 1);
                        i = (short)unchecked(i + 1);
                    } while (i <= 1);
                } finally
                {
                    packet.Dispose();
                }
            }
        }

        public void ConvertToBones()
        {
            WorldServiceLocator.WorldServer.CharacterDatabase
                .Update($"DELETE FROM corpse WHERE player = \"{Owner}\";");
            Flags = 5;
            Owner = 0uL;
            var j = 0;
            checked
            {
                do
                {
                    Items[j] = 0;
                    j++;
                } while (j <= 18);
                Packets.Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                try
                {
                    packet.AddInt32(1);
                    packet.AddInt8(0);
                    Packets.Packets.UpdateClass tmpUpdate = new(
                        WorldServiceLocator.GlobalConstants.FIELD_MASK_SIZE_CORPSE);
                    try
                    {
                        tmpUpdate.SetUpdateFlag(6, 0);
                        tmpUpdate.SetUpdateFlag(35, 5);
                        var i = 0;
                        do
                        {
                            tmpUpdate.SetUpdateFlag(13 + i, 0);
                            i++;
                        } while (i <= 18);
                        var updateObject = this;
                        tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                        SendToNearPlayers(ref packet);
                    } finally
                    {
                        tmpUpdate.Dispose();
                    }
                } finally
                {
                    packet.Dispose();
                }
            }
        }

        public void Destroy()
        {
            Packets.Packets.PacketClass packet = new(Opcodes.SMSG_DESTROY_OBJECT);
            try
            {
                packet.AddUInt64(GUID);
                SendToNearPlayers(ref packet);
            } finally
            {
                packet.Dispose();
            }
            Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void FillAllUpdateFlags(ref Packets.Packets.UpdateClass Update)
        {
            if(Update is null)
            {
                throw new ArgumentNullException(nameof(Update));
            }

            Update.SetUpdateFlag(0, GUID);
            Update.SetUpdateFlag(2, 129);
            Update.SetUpdateFlag(3, 0);
            Update.SetUpdateFlag(4, 1f);
            Update.SetUpdateFlag(6, Owner);
            Update.SetUpdateFlag(8, orientation);
            Update.SetUpdateFlag(9, positionX);
            Update.SetUpdateFlag(10, positionY);
            Update.SetUpdateFlag(11, positionZ);
            Update.SetUpdateFlag(12, Model);
            var i = 0;
            checked
            {
                do
                {
                    Update.SetUpdateFlag(13 + i, Items[i]);
                    i++;
                } while (i <= 18);
                Update.SetUpdateFlag(32, Bytes1);
                Update.SetUpdateFlag(33, Bytes2);
                Update.SetUpdateFlag(34, Guild);
                Update.SetUpdateFlag(35, Flags);
                Update.SetUpdateFlag(36, DynFlags);
            }
        }

        public void RemoveFromWorld()
        {
            WorldServiceLocator.WSMaps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY].CorpseObjectsHere.Remove(GUID);
            if(WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY].PlayersHere.Count == 0)
            {
                return;
            }
            var tMapTile = WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY];
            foreach(var plGUID in tMapTile.PlayersHere.ToArray())
            {
                if(WorldServiceLocator.WorldServer.CHARACTERs[plGUID].corpseObjectsNear.Contains(GUID))
                {
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving_Lock
                        .AcquireWriterLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving.Add(GUID);
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving_Lock.ReleaseWriterLock();
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].corpseObjectsNear.Remove(GUID);
                }
            }
        }

        public void Save()
        {
            var tmpCmd = "INSERT INTO corpse (guid";
            var tmpValues = $" VALUES ({Conversions.ToString(checked(GUID - WorldServiceLocator.GlobalConstants.GUID_CORPSE))}";
            tmpCmd += ", player";
            tmpValues = $"{tmpValues}, {Conversions.ToString(Owner)}";
            tmpCmd += ", position_x";
            tmpValues = $"{tmpValues}, {Strings.Trim(Conversion.Str(positionX))}";
            tmpCmd += ", position_y";
            tmpValues = $"{tmpValues}, {Strings.Trim(Conversion.Str(positionY))}";
            tmpCmd += ", position_z";
            tmpValues = $"{tmpValues}, {Strings.Trim(Conversion.Str(positionZ))}";
            tmpCmd += ", map";
            tmpValues = $"{tmpValues}, {Conversions.ToString(MapID)}";
            tmpCmd += ", instance";
            tmpValues = $"{tmpValues}, {Conversions.ToString(instance)}";
            tmpCmd += ", orientation";
            tmpValues = $"{tmpValues}, {Strings.Trim(Conversion.Str(orientation))}";
            tmpCmd += ", time";
            tmpValues += ", UNIX_TIMESTAMP()";
            tmpCmd += ", corpse_type";
            tmpValues = $"{tmpValues}, {Conversions.ToString((int)CorpseType)}";
            tmpCmd = $"{tmpCmd}) {tmpValues});";
            WorldServiceLocator.WorldServer.CharacterDatabase.Update(tmpCmd);
        }
    }
}
