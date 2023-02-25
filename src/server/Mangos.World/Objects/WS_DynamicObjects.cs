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
using Mangos.World.Spells;
using System;
using System.Collections.Generic;

namespace Mangos.World.Objects;

public class WS_DynamicObjects
{
    private ulong GetNewGUID()
    {
        ref var dynamicObjectsGUIDCounter = ref WorldServiceLocator.WorldServer.DynamicObjectsGUIDCounter;
        dynamicObjectsGUIDCounter = Convert.ToUInt64(decimal.Add(new decimal(dynamicObjectsGUIDCounter), 1m));
        return WorldServiceLocator.WorldServer.DynamicObjectsGUIDCounter;
    }

    public class DynamicObject : WS_Base.BaseObject, IDisposable
    {
        private bool _disposedValue;

        public int Bytes;

        public WS_Base.BaseUnit Caster;

        public int CastTime;

        public int Duration;

        public List<WS_Spells.SpellEffect> Effects;

        public float Radius;
        public int SpellID;

        public DynamicObject(
            ref WS_Base.BaseUnit Caster_,
            int SpellID_,
            float PosX,
            float PosY,
            float PosZ,
            int Duration_,
            float Radius_)
        {
            SpellID = 0;
            Effects = new List<WS_Spells.SpellEffect>();
            Duration = 0;
            Radius = 0f;
            CastTime = 0;
            Bytes = 1;
            GUID = WorldServiceLocator.WSDynamicObjects.GetNewGUID();
            WorldServiceLocator.WorldServer.WORLD_DYNAMICOBJECTs.Add(GUID, this);
            Caster = Caster_ ?? throw new ArgumentNullException(nameof(Caster_));
            SpellID = SpellID_;
            positionX = PosX;
            positionY = PosY;
            positionZ = PosZ;
            orientation = 0f;
            MapID = Caster.MapID;
            instance = Caster.instance;
            Duration = Duration_;
            Radius = Radius_;
            CastTime = WorldServiceLocator.NativeMethods.timeGetTime(string.Empty);
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
                WorldServiceLocator.WorldServer.WORLD_DYNAMICOBJECTs.Remove(GUID);
            }
            _disposedValue = true;
        }

        public void AddEffect(WS_Spells.SpellEffect EffectInfo)
        {
            if(EffectInfo is null)
            {
                throw new ArgumentNullException(nameof(EffectInfo));
            }

            Effects.Add(EffectInfo);
        }

        public void AddToWorld()
        {
            WorldServiceLocator.WSMaps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            if(WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY] == null)
            {
                WorldServiceLocator.WSCharMovement.MAP_Load(CellX, CellY, MapID);
            }
            try
            {
                WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY].DynamicObjectsHere.Add(GUID);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "AddToWorld failed MapId: {0} Tile XY: {1} {2} GUID: {3} : {4}",
                        MapID,
                        CellX,
                        CellY,
                        GUID,
                        ex);
                return;
            }
            Packets.Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            packet.AddInt32(1);
            packet.AddInt8(0);
            Packets.Packets.UpdateClass tmpUpdate = new(
                WorldServiceLocator.GlobalConstants.FIELD_MASK_SIZE_DYNAMICOBJECT);
            FillAllUpdateFlags(ref tmpUpdate);
            var updateClass = tmpUpdate;
            var updateObject = this;
            updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT_SELF, ref updateObject);
            tmpUpdate.Dispose();
            short i = -1;
            checked
            {
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
                                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].dynamicObjectsNear.Add(GUID);
                                    SeenBy.Add(plGUID);
                                }
                            }
                        }
                        j = (short)unchecked(j + 1);
                    } while (j <= 1);
                    i = (short)unchecked(i + 1);
                } while (i <= 1);
                packet.Dispose();
            }
        }

        public void Delete()
        {
            if((Caster?.dynamicObjects.Contains(this)) == true)
            {
                Caster.dynamicObjects.Remove(this);
            }
            Packets.Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_DESPAWN_ANIM);
            packet.AddUInt64(GUID);
            SendToNearPlayers(ref packet);
            packet.Dispose();
            RemoveFromWorld();
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
            Update.SetUpdateFlag(2, 65);
            Update.SetUpdateFlag(4, 0.5f * Radius);
            Update.SetUpdateFlag(6, Caster.GUID);
            Update.SetUpdateFlag(8, Bytes);
            Update.SetUpdateFlag(9, SpellID);
            Update.SetUpdateFlag(10, Radius);
            Update.SetUpdateFlag(11, positionX);
            Update.SetUpdateFlag(12, positionY);
            Update.SetUpdateFlag(13, positionZ);
            Update.SetUpdateFlag(14, orientation);
        }

        public void RemoveEffect(WS_Spells.SpellEffect EffectInfo)
        {
            if(EffectInfo is null)
            {
                throw new ArgumentNullException(nameof(EffectInfo));
            }

            Effects.Remove(EffectInfo);
        }

        public void RemoveFromWorld()
        {
            WorldServiceLocator.WSMaps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            WorldServiceLocator.WSMaps.Maps[MapID].Tiles[CellX, CellY].DynamicObjectsHere.Remove(GUID);
            foreach(var plGUID in SeenBy.ToArray())
            {
                if(WorldServiceLocator.WorldServer.CHARACTERs[plGUID].dynamicObjectsNear.Contains(GUID))
                {
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving_Lock
                        .AcquireWriterLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving.Add(GUID);
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].guidsForRemoving_Lock.ReleaseWriterLock();
                    WorldServiceLocator.WorldServer.CHARACTERs[plGUID].dynamicObjectsNear.Remove(GUID);
                }
            }
        }

        public void Spawn()
        {
            AddToWorld();
            Packets.Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_SPAWN_ANIM);
            packet.AddUInt64(GUID);
            SendToNearPlayers(ref packet);
            packet.Dispose();
        }

        public bool Update()
        {
            if(Caster == null)
            {
                return true;
            }
            var DeleteThis = false;
            checked
            {
                if(Duration > 1000)
                {
                    Duration -= 1000;
                } else
                {
                    DeleteThis = true;
                }
            }
            foreach(var effect in Effects)
            {
                var Effect = effect;
                if(Effect.GetRadius == 0f)
                {
                    if((Effect.Amplitude == 0) ||
                        ((checked(WorldServiceLocator.WSSpells.SPELLs[SpellID].GetDuration - Duration) %
                                Effect.Amplitude) ==
                            0))
                    {
                        var obj = WorldServiceLocator.WSSpells.AURAs[Effect.ApplyAuraIndex];
                        ref var caster = ref Caster;
                        WS_Base.BaseObject baseObject = this;
                        obj(ref caster, ref baseObject, ref Effect, SpellID, 1, AuraAction.AURA_UPDATE);
                    }
                    continue;
                }
                foreach(var item in WorldServiceLocator.WSSpells
                    .GetEnemyAtPoint(ref Caster, positionX, positionY, positionZ, Effect.GetRadius))
                {
                    var Target = item;
                    if((Effect.Amplitude == 0) ||
                        ((checked(WorldServiceLocator.WSSpells.SPELLs[SpellID].GetDuration - Duration) %
                                Effect.Amplitude) ==
                            0))
                    {
                        var obj2 = WorldServiceLocator.WSSpells.AURAs[Effect.ApplyAuraIndex];
                        WS_Base.BaseObject baseObject = this;
                        obj2(ref Target, ref baseObject, ref Effect, SpellID, 1, AuraAction.AURA_UPDATE);
                    }
                }
            }
            if(DeleteThis)
            {
                Caster.dynamicObjects.Remove(this);
                return true;
            }
            return false;
        }
    }
}
