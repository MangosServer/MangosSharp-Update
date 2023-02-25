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
using Mangos.Common.Enums.Group;
using Mangos.Common.Globals;
using Mangos.World.Player;
using System;
using System.Collections.Generic;

namespace Mangos.World.Social;

public class WS_Group
{
    private ulong _lastLooter;

    public readonly Dictionary<long, Group> Groups;

    public WS_Group()
    {
        Groups = new Dictionary<long, Group>();
        _lastLooter = 0uL;
    }

    public Packets.Packets.PacketClass BuildPartyMemberStats(
        ref WS_PlayerData.CharacterObject objCharacter,
        uint flag)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        var opCode = Opcodes.SMSG_PARTY_MEMBER_STATS;
        if(flag is 1015 or 524279)
        {
            opCode = Opcodes.SMSG_PARTY_MEMBER_STATS_FULL;
            if(objCharacter.ManaType != 0)
            {
                flag |= 8u;
            }
        }
        Packets.Packets.PacketClass packet = new(opCode);
        packet.AddPackGUID(objCharacter.GUID);
        packet.AddUInt32(flag);
        if((flag & (true ? 1u : 0u)) != 0)
        {
            byte memberFlags = 1;
            if(objCharacter.IsPvP)
            {
                memberFlags = (byte)(memberFlags | 2);
            }
            if(objCharacter.DEAD)
            {
                memberFlags = (byte)(memberFlags | 0x10);
            }
            packet.AddInt8(memberFlags);
        }
        checked
        {
            if((flag & 2u) != 0)
            {
                packet.AddUInt16((ushort)objCharacter.Life.Current);
            }
            if((flag & 4u) != 0)
            {
                packet.AddUInt16((ushort)objCharacter.Life.Maximum);
            }
            if((flag & 8u) != 0)
            {
                packet.AddInt8((byte)objCharacter.ManaType);
            }
            if((flag & 0x10u) != 0)
            {
                if(objCharacter.ManaType == ManaTypes.TYPE_RAGE)
                {
                    packet.AddUInt16((ushort)objCharacter.Rage.Current);
                } else if(objCharacter.ManaType == ManaTypes.TYPE_ENERGY)
                {
                    packet.AddUInt16((ushort)objCharacter.Energy.Current);
                } else
                {
                    packet.AddUInt16((ushort)objCharacter.Mana.Current);
                }
            }
            if((flag & 0x20u) != 0)
            {
                if(objCharacter.ManaType == ManaTypes.TYPE_RAGE)
                {
                    packet.AddUInt16((ushort)objCharacter.Rage.Maximum);
                } else if(objCharacter.ManaType == ManaTypes.TYPE_ENERGY)
                {
                    packet.AddUInt16((ushort)objCharacter.Energy.Maximum);
                } else
                {
                    packet.AddUInt16((ushort)objCharacter.Mana.Maximum);
                }
            }
            if((flag & 0x40u) != 0)
            {
                packet.AddUInt16(objCharacter.Level);
            }
            if((flag & 0x80u) != 0)
            {
                packet.AddUInt16((ushort)objCharacter.ZoneID);
            }
            if((flag & 0x100u) != 0)
            {
                packet.AddInt16((short)objCharacter.positionX);
                packet.AddInt16((short)objCharacter.positionY);
            }
            if((flag & 0x200u) != 0)
            {
                var auraMask2 = 0uL;
                var auraPos2 = packet.Data.Length;
                packet.AddUInt64(0uL);
                var num = WorldServiceLocator.GlobalConstants.MAX_AURA_EFFECTs_VISIBLE - 1;
                for(var j = 0; j <= num; j++)
                {
                    if(objCharacter.ActiveSpells[j] != null)
                    {
                        unchecked
                        {
                            auraMask2 |= (ulong)(1L << checked((int)(ulong)j));
                        }
                        packet.AddUInt16((ushort)objCharacter.ActiveSpells[j].SpellID);
                        packet.AddInt8(1);
                    }
                }
                packet.AddUInt64(auraMask2, auraPos2);
            }
            if((flag & 0x400u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt64(objCharacter.Pet.GUID);
                } else
                {
                    packet.AddInt64(0L);
                }
            }
            if((flag & 0x800u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddString(objCharacter.Pet.PetName);
                } else
                {
                    packet.AddString(string.Empty);
                }
            }
            if((flag & 0x1000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt16((ushort)objCharacter.Pet.Model);
                } else
                {
                    packet.AddInt16(0);
                }
            }
            if((flag & 0x2000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt16((ushort)objCharacter.Pet.Life.Current);
                } else
                {
                    packet.AddInt16(0);
                }
            }
            if((flag & 0x4000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt16((ushort)objCharacter.Pet.Life.Maximum);
                } else
                {
                    packet.AddInt16(0);
                }
            }
            if((flag & 0x8000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddInt8(2);
                } else
                {
                    packet.AddInt8(0);
                }
            }
            if((flag & 0x10000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt16((ushort)objCharacter.Pet.Mana.Current);
                } else
                {
                    packet.AddInt16(0);
                }
            }
            if((flag & 0x20000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    packet.AddUInt16((ushort)objCharacter.Pet.Mana.Maximum);
                } else
                {
                    packet.AddInt16(0);
                }
            }
            if((flag & 0x40000u) != 0)
            {
                if(objCharacter.Pet != null)
                {
                    var auraMask = 0uL;
                    var auraPos = packet.Data.Length;
                    packet.AddUInt64(0uL);
                    var num2 = WorldServiceLocator.GlobalConstants.MAX_AURA_EFFECTs_VISIBLE - 1;
                    for(var i = 0; i <= num2; i++)
                    {
                        if(objCharacter.Pet.ActiveSpells[i] != null)
                        {
                            unchecked
                            {
                                auraMask |= (ulong)(1L << checked((int)(ulong)i));
                            }
                            packet.AddUInt16((ushort)objCharacter.Pet.ActiveSpells[i].SpellID);
                            packet.AddInt8(1);
                        }
                    }
                    packet.AddUInt64(auraMask, auraPos);
                } else
                {
                    packet.AddInt64(0L);
                }
            }
            return packet;
        }
    }

    public sealed class Group : IDisposable
    {
        private bool _disposedValue;

        public GroupDungeonDifficulty DungeonDifficulty;
        public readonly long ID;

        public ulong Leader;

        public WS_PlayerData.CharacterObject LocalLootMaster;

        public List<ulong> LocalMembers;

        public GroupLootMethod LootMethod;

        public GroupLootThreshold LootThreshold;

        public GroupType Type;

        public Group(long groupID)
        {
            Type = GroupType.PARTY;
            DungeonDifficulty = GroupDungeonDifficulty.DIFFICULTY_NORMAL;
            LootMethod = GroupLootMethod.LOOT_GROUP;
            LootThreshold = GroupLootThreshold.Uncommon;
            ID = groupID;
            WorldServiceLocator.WSGroup.Groups.Add(ID, this);
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        private void Dispose(bool disposing)
        {
            if(!_disposedValue)
            {
                WorldServiceLocator.WSGroup.Groups.Remove(ID);
            }
            _disposedValue = true;
        }

        public void Broadcast(Packets.Packets.PacketClass p)
        {
            if(p is null)
            {
                throw new ArgumentNullException(nameof(p));
            }

            p.UpdateLength();
            WorldServiceLocator.WorldServer.ClsWorldServer.Cluster.BroadcastGroup(ID, p.Data);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public int GetMembersCount() { return LocalMembers.Count; }

        public WS_PlayerData.CharacterObject GetNextLooter()
        {
            var nextIsLooter = false;
            var nextLooterFound = false;
            foreach(var guid in LocalMembers)
            {
                if(nextIsLooter)
                {
                    WorldServiceLocator.WSGroup._lastLooter = guid;
                    nextLooterFound = true;
                    break;
                }
                if(guid == WorldServiceLocator.WSGroup._lastLooter)
                {
                    nextIsLooter = true;
                }
            }
            if(!nextLooterFound)
            {
                WorldServiceLocator.WSGroup._lastLooter = LocalMembers[0];
            }
            return WorldServiceLocator.WorldServer.CHARACTERs[WorldServiceLocator.WSGroup._lastLooter];
        }
    }
}
