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
using Mangos.Common.Enums.Player;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.World.AI;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Player;
using System;
using System.Collections.Generic;
using System.Data;

namespace Mangos.World.Handlers;

public class WS_Handlers_Misc
{
    public void CharacterRepop(ref WS_Network.ClientClass client)
    {
        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        try
        {
            if(client.Character is null)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "[{0}:{1} Account:{2} CharName:{3} CharGUID:{4}] Client is Null!",
                        client.IP,
                        client.Port,
                        client.Account,
                        client.Character.UnitName,
                        client.Character.GUID);
                return;
            }
            client.Character.Mana.Current = 0;
            client.Character.Rage.Current = 0;
            client.Character.Energy.Current = 0;
            client.Character.Life.Current = 1;
            client.Character.DEAD = true;
            client.Character.cUnitFlags = 8;
            client.Character.cDynamicFlags = 0;
            client.Character.cPlayerFlags |= PlayerFlags.PLAYER_FLAGS_DEAD;
            WorldServiceLocator.Functions.SendCorpseReclaimDelay(ref client, ref client.Character);
            client.Character.StopMirrorTimer(MirrorTimer.FATIGUE);
            client.Character.StopMirrorTimer(MirrorTimer.DROWNING);
            if(client.Character.underWaterTimer != null)
            {
                client.Character.underWaterTimer.Dispose();
                client.Character.underWaterTimer = null;
            }
            WS_Corpses.CorpseObject myCorpse = new(ref client.Character);
            myCorpse.Save();
            myCorpse.AddToWorld();
            client.Character.Invisibility = InvisibilityLevel.DEAD;
            client.Character.CanSeeInvisibility = InvisibilityLevel.DEAD;
            WorldServiceLocator.WSCharMovement.UpdateCell(ref client.Character);
            checked
            {
                for(var i = 0; i <= (WorldServiceLocator.GlobalConstants.MAX_AURA_EFFECTs - 1); i++)
                {
                    if(client.Character.ActiveSpells[i] != null)
                    {
                        client.Character.RemoveAura(i, ref client.Character.ActiveSpells[i].SpellCaster);
                    }
                }
                client.Character.SetWaterWalk();
                client.Character.SetMoveUnroot();
                if(client.Character.Race == Races.RACE_NIGHT_ELF)
                {
                    client.Character.ApplySpell(20584);
                } else
                {
                    client.Character.ApplySpell(8326);
                }
                client.Character.SetUpdateFlag(22, 1);
            }
            client.Character.SetUpdateFlag((int)checked(23 + client.Character.ManaType), 0);
            client.Character.SetUpdateFlag(190, (int)client.Character.cPlayerFlags);
            client.Character.SetUpdateFlag(46, client.Character.cUnitFlags);
            client.Character.SetUpdateFlag(143, client.Character.cDynamicFlags);
            client.Character.SetUpdateFlag(138, 16777216);
            client.Character.SendCharacterUpdate();
            WorldServiceLocator.WorldServer.AllGraveYards
                .GoToNearestGraveyard(ref client.Character, Alive: false, Teleport: true);
        } catch(Exception e)
        {
            WorldServiceLocator.WorldServer.Log.WriteLine(LogType.FAILED, "Error on repop: {0}", e.ToString());
        }
    }

    public void CharacterResurrect(ref WS_PlayerData.CharacterObject Character)
    {
        if(Character is null)
        {
            throw new ArgumentNullException(nameof(Character));
        }

        if(Character.repopTimer != null)
        {
            Character.repopTimer.Dispose();
            Character.repopTimer = null;
        }
        Character.Mana.Current = 0;
        Character.Rage.Current = 0;
        Character.Energy.Current = 0;
        Character.Life.Current = checked((int)Math.Round(Character.Life.Maximum / 2.0));
        Character.DEAD = false;
        Character.cPlayerFlags &= ~PlayerFlags.PLAYER_FLAGS_DEAD;
        Character.cUnitFlags = 8;
        Character.cDynamicFlags = 0;
        Character.InvisibilityReset();
        WorldServiceLocator.WSCharMovement.UpdateCell(ref Character);
        Character.SetLandWalk();
        if(Character.Race == Races.RACE_NIGHT_ELF)
        {
            Character.RemoveAuraBySpell(20584);
        } else
        {
            Character.RemoveAuraBySpell(8326);
        }
        Character.SetUpdateFlag(22, Character.Life.Current);
        Character.SetUpdateFlag(190, (int)Character.cPlayerFlags);
        Character.SetUpdateFlag(46, Character.cUnitFlags);
        Character.SetUpdateFlag(143, Character.cDynamicFlags);
        Character.SendCharacterUpdate();
        if(decimal.Compare(new decimal(Character.corpseGUID), 0m) != 0)
        {
            if(WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs.ContainsKey(Character.corpseGUID))
            {
                WorldServiceLocator.WorldServer.WORLD_CORPSEOBJECTs[Character.corpseGUID].ConvertToBones();
            } else
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.DEBUG,
                        "Corpse wasn't found [{0}]!",
                        checked(Character.corpseGUID - WorldServiceLocator.GlobalConstants.GUID_CORPSE));
                WorldServiceLocator.WorldServer.CharacterDatabase
                    .Update($"DELETE FROM corpse WHERE player = \"{Character.GUID}\";");
            }
            Character.corpseGUID = 0uL;
            Character.corpseMapID = 0;
            Character.corpsePositionX = 0f;
            Character.corpsePositionY = 0f;
            Character.corpsePositionZ = 0f;
        }
    }

    public void On_CMSG_BATTLEFIELD_STATUS(
        ref Packets.Packets.PacketClass packet,
        ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BATTLEFIELD_STATUS", client.IP, client.Port);
    }

    public void On_CMSG_EMOTE(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(checked(packet.Data.Length - 1) >= 9)
        {
            packet.GetInt16();
            var emoteID = packet.GetInt32();
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_EMOTE [{2}]", client.IP, client.Port, emoteID);
            Packets.Packets.PacketClass response = new(Opcodes.SMSG_EMOTE);
            try
            {
                response.AddInt32(emoteID);
                response.AddUInt64(client.Character.GUID);
                client.Character.SendToNearPlayers(ref response);
            } finally
            {
                response.Dispose();
            }
        }
    }

    public void On_CMSG_MEETINGSTONE_INFO(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MEETINGSTONE_INFO", client.IP, client.Port);
    }

    public void On_CMSG_MOUNTSPECIAL_ANIM(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MOUNTSPECIAL_ANIM", client.IP, client.Port);
        Packets.Packets.PacketClass response = new(Opcodes.SMSG_MOUNTSPECIAL_ANIM);
        try
        {
            response.AddPackGUID(client.Character.GUID);
            client.Character.SendToNearPlayers(ref response);
        } finally
        {
            response.Dispose();
        }
    }

    public void On_CMSG_MOVE_FALL_RESET(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MOVE_FALL_RESET", client.IP, client.Port);
        WS_Network.ClientClass client2 = null;
        WorldServiceLocator.Packets.DumpPacket(packet.Data, client2);
    }

    public void On_CMSG_NAME_QUERY(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        try
        {
            if(checked(packet.Data.Length - 1) < 13)
            {
                return;
            }
            packet.GetInt16();
            var GUID = packet.GetUInt64();
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_NAME_QUERY [GUID={2:X}]", client.IP, client.Port, GUID);
            Packets.Packets.PacketClass SMSG_NAME_QUERY_RESPONSE = new(Opcodes.SMSG_NAME_QUERY_RESPONSE);
            switch(GUID)
            {
                case int.MaxValue:
                    try
                    {
                        SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID);
                        SMSG_NAME_QUERY_RESPONSE.AddString("System");
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(1);
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(1);
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(1);
                        client.Send(ref SMSG_NAME_QUERY_RESPONSE);
                    } catch(Exception ex)
                    {
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.WARNING,
                                "SMSG_NAME_QUERY_RESPONSE Exception  {0} : {1} : {2} : {3} : {4}",
                                client,
                                client.IP,
                                client.Port,
                                GUID,
                                ex);
                    } finally
                    {
                        SMSG_NAME_QUERY_RESPONSE.Dispose();
                    }
                    break;

                default:
                    if(WorldServiceLocator.CommonGlobalFunctions.GuidIsPlayer(GUID))
                    {
                        if(WorldServiceLocator.WorldServer.CHARACTERs.ContainsKey(GUID))
                        {
                            try
                            {
                                SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID);
                                SMSG_NAME_QUERY_RESPONSE.AddString(
                                    WorldServiceLocator.WorldServer.CHARACTERs[GUID].Name);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(
                                    (int)WorldServiceLocator.WorldServer.CHARACTERs[GUID].Race);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(
                                    (int)WorldServiceLocator.WorldServer.CHARACTERs[GUID].Gender);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(
                                    (int)WorldServiceLocator.WorldServer.CHARACTERs[GUID].Class);
                                client.Send(ref SMSG_NAME_QUERY_RESPONSE);
                            } catch(Exception ex)
                            {
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(
                                        LogType.WARNING,
                                        "SMSG_NAME_QUERY_RESPONSE Exception  {0} : {1} : {2} : {3} : {4}",
                                        client,
                                        client.IP,
                                        client.Port,
                                        GUID,
                                        ex);
                            } finally
                            {
                                SMSG_NAME_QUERY_RESPONSE.Dispose();
                            }
                            return;
                        }
                        DataTable MySQLQuery = new();
                        WorldServiceLocator.WorldServer.CharacterDatabase
                            .Query(
                                $"SELECT char_name, char_race, char_class, char_gender FROM characters WHERE char_guid = \"{GUID}\";",
                                ref MySQLQuery);
                        switch(MySQLQuery.Rows.Count)
                        {
                            case > 0:
                                try
                                {
                                    SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID);
                                    SMSG_NAME_QUERY_RESPONSE.AddString(MySQLQuery.Rows[0].As<string>("char_name"));
                                    SMSG_NAME_QUERY_RESPONSE.AddInt32(MySQLQuery.Rows[0].As<int>("char_race"));
                                    SMSG_NAME_QUERY_RESPONSE.AddInt32(MySQLQuery.Rows[0].As<int>("char_gender"));
                                    SMSG_NAME_QUERY_RESPONSE.AddInt32(MySQLQuery.Rows[0].As<int>("char_class"));
                                    client.Send(ref SMSG_NAME_QUERY_RESPONSE);
                                } catch(Exception ex)
                                {
                                    WorldServiceLocator.WorldServer.Log
                                        .WriteLine(
                                            LogType.FAILED,
                                            "Failed to Query Character Information: GUID: {0} Name: {1} Race: {2} Gender: {3} Class: {4} {5}",
                                            GUID,
                                            MySQLQuery.Rows[0].As<string>("char_name"),
                                            MySQLQuery.Rows[0].As<int>("char_race"),
                                            MySQLQuery.Rows[0].As<int>("char_gender"),
                                            MySQLQuery.Rows[0].As<int>("char_class"),
                                            ex);
                                } finally
                                {
                                    SMSG_NAME_QUERY_RESPONSE.Dispose();
                                }
                                break;

                            default:
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(
                                        LogType.DEBUG,
                                        "[{0}:{1}] SMSG_NAME_QUERY_RESPONSE [Character GUID={2:X} not found]",
                                        client.IP,
                                        client.Port,
                                        GUID);
                                break;
                        }
                        MySQLQuery.Dispose();
                    } else
                    {
                        if(!WorldServiceLocator.CommonGlobalFunctions.GuidIsCreature(GUID))
                        {
                            return;
                        }
                        if(WorldServiceLocator.WorldServer.WORLD_CREATUREs.ContainsKey(GUID))
                        {
                            try
                            {
                                SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID);
                                SMSG_NAME_QUERY_RESPONSE.AddString(
                                    WorldServiceLocator.WorldServer.WORLD_CREATUREs[GUID].Name);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(0);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(0);
                                SMSG_NAME_QUERY_RESPONSE.AddInt32(0);
                                client.Send(ref SMSG_NAME_QUERY_RESPONSE);
                            } catch(Exception ex)
                            {
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(
                                        LogType.FAILED,
                                        "Failed to get Creature Information: GUID: {0} Name: {1} {2}",
                                        GUID,
                                        WorldServiceLocator.WorldServer.WORLD_CREATUREs[GUID].Name,
                                        ex);
                            } finally
                            {
                                SMSG_NAME_QUERY_RESPONSE.Dispose();
                            }
                        } else
                        {
                            WorldServiceLocator.WorldServer.Log
                                .WriteLine(
                                    LogType.DEBUG,
                                    "[{0}:{1}] SMSG_NAME_QUERY_RESPONSE [Creature GUID={2:X} not found]",
                                    client.IP,
                                    client.Port,
                                    GUID);
                        }
                    }

                    break;
            }
        } catch(Exception e)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.CRITICAL, "Error at name query.{0}", $"{Environment.NewLine}{e}");
        }
    }

    public void On_CMSG_RECLAIM_CORPSE(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(checked(packet.Data.Length - 1) >= 13)
        {
            packet.GetInt16();
            var GUID = packet.GetUInt64();
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.DEBUG,
                    "[{0}:{1}] CMSG_RECLAIM_CORPSE [GUID={2:X}]",
                    client.IP,
                    client.Port,
                    GUID);
            CharacterResurrect(ref client.Character);
        }
    }

    public void On_CMSG_REPOP_REQUEST(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] CMSG_REPOP_REQUEST [GUID={2:X}]",
                client.IP,
                client.Port,
                client.Character.GUID);
        if(client.Character.repopTimer != null)
        {
            client.Character.repopTimer.Dispose();
            client.Character.repopTimer = null;
        }
        CharacterRepop(ref client);
    }

    public void On_CMSG_SET_ACTIONBAR_TOGGLES(
        ref Packets.Packets.PacketClass packet,
        ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        packet.GetInt16();
        var ActionBar = packet.GetInt8();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] CMSG_SET_ACTIONBAR_TOGGLES [{2:X}]",
                client.IP,
                client.Port,
                ActionBar);
        client.Character.cPlayerFieldBytes = (client.Character.cPlayerFieldBytes & (-983041)) |
            ((byte)(ActionBar << (0x10 & 7)));
        client.Character.SetUpdateFlag(1222, client.Character.cPlayerFieldBytes);
        client.Character.SendCharacterUpdate();
    }

    public void On_CMSG_SET_ACTIVE_MOVER(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        packet.GetInt16();
        var GUID = packet.GetUInt64();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SET_ACTIVE_MOVER [GUID={2:X}]", client.IP, client.Port, GUID);
    }

    public void On_CMSG_SET_FACTION_ATWAR(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        packet.GetInt16();
        var faction = packet.GetInt32();
        var enabled = packet.GetInt8();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] CMSG_SET_FACTION_ATWAR [faction={2:X} enabled={3}]",
                client.IP,
                client.Port,
                faction,
                enabled);
        if(enabled <= 1)
        {
            client.Character.Reputation[faction].Flags = (enabled == 1)
                ? (client.Character.Reputation[faction].Flags | 2)
                : (client.Character.Reputation[faction].Flags & (-3));
            Packets.Packets.PacketClass response = new(Opcodes.SMSG_SET_FACTION_STANDING);
            try
            {
                response.AddInt32(client.Character.Reputation[faction].Flags);
                response.AddInt32(faction);
                response.AddInt32(client.Character.Reputation[faction].Value);
                client.Send(ref response);
            } finally
            {
                response.Dispose();
            }
        }
    }

    public void On_CMSG_SET_FACTION_INACTIVE(
        ref Packets.Packets.PacketClass packet,
        ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        packet.GetInt16();
        var faction = packet.GetInt32();
        var enabled = packet.GetInt8();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] CMSG_SET_FACTION_INACTIVE [faction={2:X} enabled={3}]",
                client.IP,
                client.Port,
                faction,
                enabled);
        if(enabled <= 1)
        {
        }
    }

    public void On_CMSG_SET_WATCHED_FACTION(
        ref Packets.Packets.PacketClass packet,
        ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        packet.GetInt16();
        var faction = packet.GetInt32();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(
                LogType.DEBUG,
                "[{0}:{1}] CMSG_SET_WATCHED_FACTION [faction={2:X}]",
                client.IP,
                client.Port,
                faction);
        if(faction == (-1))
        {
            faction = 255;
        }
        if(faction is >= 0 and <= 255)
        {
            client.Character.WatchedFactionIndex = checked((byte)faction);
            client.Character.SetUpdateFlag(1261, faction);
            client.Character.SendCharacterUpdate(toNear: false);
        }
    }

    public void On_CMSG_TEXT_EMOTE(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        checked
        {
            if(((packet.Data.Length - 1) < 21) && (client.Character != null))
            {
                return;
            }
            packet.GetInt16();
            var TextEmote = packet.GetInt32();
            var Unk = packet.GetInt32();
            var GUID = packet.GetUInt64();
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.DEBUG,
                    "[{0}:{1}] CMSG_TEXT_EMOTE [TextEmote={2} Unk={3}]",
                    client.IP,
                    client.Port,
                    TextEmote,
                    Unk);
            if(WorldServiceLocator.CommonGlobalFunctions.GuidIsCreature(GUID) &&
                WorldServiceLocator.WorldServer.WORLD_CREATUREs.ContainsKey(GUID))
            {
                ref var character = ref client.Character;
                ulong key;
                Dictionary<ulong, WS_Creatures.CreatureObject> WORLD_CREATUREs;
                var creature = (WORLD_CREATUREs = WorldServiceLocator.WorldServer.WORLD_CREATUREs)[key = GUID];
                WorldServiceLocator.WorldServer.ALLQUESTS.OnQuestDoEmote(ref character, ref creature, TextEmote);
                WORLD_CREATUREs[key] = creature;
                if(WorldServiceLocator.WorldServer.WORLD_CREATUREs[GUID].aiScript is not null and WS_Creatures_AI.GuardAI)
                {
                    ((WS_Creatures_AI.GuardAI)WorldServiceLocator.WorldServer.WORLD_CREATUREs[GUID].aiScript).OnEmote(
                        TextEmote);
                }
            }
            if(WorldServiceLocator.WSDBCDatabase.EmotesText.ContainsKey(TextEmote))
            {
                switch(WorldServiceLocator.WSDBCDatabase.EmotesState[
                    WorldServiceLocator.WSDBCDatabase.EmotesText[TextEmote]])
                {
                    case 0:
                        client.Character.DoEmote(WorldServiceLocator.WSDBCDatabase.EmotesText[TextEmote]);
                        break;

                    case 2:
                        client.Character.cEmoteState = WorldServiceLocator.WSDBCDatabase.EmotesText[TextEmote];
                        client.Character.SetUpdateFlag(148, client.Character.cEmoteState);
                        client.Character.SendCharacterUpdate();
                        break;

                    default:
                        break;
                }
            }
            var secondName = string.Empty;
            if(decimal.Compare(new decimal(GUID), 0m) > 0)
            {
                if(WorldServiceLocator.WorldServer.CHARACTERs.ContainsKey(GUID))
                {
                    secondName = WorldServiceLocator.WorldServer.CHARACTERs[GUID].Name;
                } else if(WorldServiceLocator.WorldServer.WORLD_CREATUREs.ContainsKey(GUID))
                {
                    secondName = WorldServiceLocator.WorldServer.WORLD_CREATUREs[GUID].Name;
                }
            }
            Packets.Packets.PacketClass SMSG_TEXT_EMOTE = new(Opcodes.SMSG_TEXT_EMOTE);
            try
            {
                SMSG_TEXT_EMOTE.AddUInt64(client.Character.GUID);
                SMSG_TEXT_EMOTE.AddInt32(TextEmote);
                SMSG_TEXT_EMOTE.AddInt32(255);
                SMSG_TEXT_EMOTE.AddInt32(secondName.Length + 1);
                SMSG_TEXT_EMOTE.AddString(secondName);
                client.Character.SendToNearPlayers(ref SMSG_TEXT_EMOTE);
            } finally
            {
                SMSG_TEXT_EMOTE.Dispose();
            }
        }
    }

    public void On_CMSG_TOGGLE_CLOAK(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_CLOAK", client.IP, client.Port);
        if((client.Character.cPlayerFlags & PlayerFlags.PLAYER_FLAGS_HIDE_CLOAK) != 0)
        {
            client.Character.cPlayerFlags &= ~PlayerFlags.PLAYER_FLAGS_HIDE_CLOAK;
        } else
        {
            client.Character.cPlayerFlags |= PlayerFlags.PLAYER_FLAGS_HIDE_CLOAK;
        }
        client.Character.SetUpdateFlag(190, (int)client.Character.cPlayerFlags);
        client.Character.SendCharacterUpdate();
    }

    public void On_CMSG_TOGGLE_HELM(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_HELM", client.IP, client.Port);
        if((client.Character.cPlayerFlags & PlayerFlags.PLAYER_FLAGS_HIDE_HELM) != 0)
        {
            client.Character.cPlayerFlags &= ~PlayerFlags.PLAYER_FLAGS_HIDE_HELM;
        } else
        {
            client.Character.cPlayerFlags |= PlayerFlags.PLAYER_FLAGS_HIDE_HELM;
        }
        client.Character.SetUpdateFlag(190, (int)client.Character.cPlayerFlags);
        client.Character.SendCharacterUpdate();
    }

    public void On_CMSG_TOGGLE_PVP(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_PVP", client.IP, client.Port);
        client.Character.IsPvP = !client.Character.IsPvP;
        client.Character.SetUpdateFlag(46, client.Character.cUnitFlags);
        client.Character.SendCharacterUpdate();
    }

    public void On_CMSG_TUTORIAL_CLEAR(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TUTORIAL_CLEAR", client.IP, client.Port);
        var i = 0;
        do
        {
            client.Character.TutorialFlags[i] = byte.MaxValue;
            i = checked(i + 1);
        } while (i <= 31);
        client.Character.SaveCharacter();
    }

    public void On_CMSG_TUTORIAL_FLAG(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        checked
        {
            if((packet.Data.Length - 1) >= 9)
            {
                packet.GetInt16();
                var Flag = packet.GetInt32();
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.DEBUG,
                        "[{0}:{1}] CMSG_TUTORIAL_FLAG [flag={2}]",
                        client.IP,
                        client.Port,
                        Flag);
                client.Character.TutorialFlags[Flag / 8] = (byte)(client.Character.TutorialFlags[Flag / 8] +
                    (0 << (7 - (Flag % 8))));
                client.Character.SaveCharacter();
            }
        }
    }

    public void On_CMSG_TUTORIAL_RESET(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TUTORIAL_RESET", client.IP, client.Port);
        var i = 0;
        do
        {
            client.Character.TutorialFlags[i] = 0;
            i = checked(i + 1);
        } while (i <= 31);
        client.Character.SaveCharacter();
    }

    public void On_MSG_CORPSE_QUERY(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(decimal.Compare(new decimal(client.Character.corpseGUID), 0m) != 0)
        {
            Packets.Packets.PacketClass MSG_CORPSE_QUERY = new(Opcodes.MSG_CORPSE_QUERY);
            try
            {
                MSG_CORPSE_QUERY.AddInt8(1);
                MSG_CORPSE_QUERY.AddInt32(checked((int)client.Character.MapID));
                MSG_CORPSE_QUERY.AddSingle(client.Character.corpsePositionX);
                MSG_CORPSE_QUERY.AddSingle(client.Character.corpsePositionY);
                MSG_CORPSE_QUERY.AddSingle(client.Character.corpsePositionZ);
                MSG_CORPSE_QUERY.AddInt32(client.Character.corpseMapID);
                client.Send(ref MSG_CORPSE_QUERY);
            } finally
            {
                MSG_CORPSE_QUERY.Dispose();
            }
            Packets.Packets.PacketClass MSG_MINIMAP_PING = new(Opcodes.MSG_MINIMAP_PING);
            try
            {
                MSG_MINIMAP_PING.AddUInt64(client.Character.corpseGUID);
                MSG_MINIMAP_PING.AddSingle(client.Character.corpsePositionX);
                MSG_MINIMAP_PING.AddSingle(client.Character.corpsePositionY);
                client.Send(ref MSG_MINIMAP_PING);
            } finally
            {
                MSG_MINIMAP_PING.Dispose();
            }
        }
    }

    public void On_MSG_INSPECT_HONOR_STATS(
        ref Packets.Packets.PacketClass packet,
        ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(checked(packet.Data.Length - 1) < 13)
        {
            return;
        }
        packet.GetInt16();
        var GUID = packet.GetUInt64();
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_INSPECT_HONOR_STATS [{2:X}]", client.IP, client.Port, GUID);
        if(WorldServiceLocator.WorldServer.CHARACTERs.ContainsKey(GUID))
        {
            Packets.Packets.PacketClass response = new(Opcodes.MSG_INSPECT_HONOR_STATS);
            try
            {
                response.AddUInt64(GUID);
                WorldServiceLocator.WorldServer.CHARACTERs_Lock
                    .AcquireReaderLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                response.AddInt8((byte)WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorRank);
                response.AddInt32(
                    checked(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorKillsToday +
                            WorldServiceLocator.WorldServer.CHARACTERs[GUID].DishonorKillsToday) <<
                        16);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorKillsYesterday);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorKillsLastWeek);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorKillsThisWeek);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorKillsLifeTime);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].DishonorKillsLifeTime);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorPointsYesterday);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorPointsLastWeek);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorPointsThisWeek);
                response.AddInt32(WorldServiceLocator.WorldServer.CHARACTERs[GUID].StandingLastWeek);
                response.AddInt8((byte)WorldServiceLocator.WorldServer.CHARACTERs[GUID].HonorHighestRank);
                WorldServiceLocator.WorldServer.CHARACTERs_Lock.ReleaseReaderLock();
                client.Send(ref response);
            } finally
            {
                response.Dispose();
            }
        }
    }

    public void On_MSG_PVP_LOG_DATA(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(WorldServiceLocator.WSMaps.Maps[client.Character.MapID].IsBattleGround)
        {
        }
    }
}
