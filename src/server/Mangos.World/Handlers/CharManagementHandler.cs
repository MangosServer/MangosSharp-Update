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
using Mangos.Common.Enums.Item;
using Mangos.Common.Globals;
using Mangos.World.Network;
using Mangos.World.Player;
using System;
using System.Threading;

namespace Mangos.World.Handlers;

public class CharManagementHandler
{
    public InventoryChangeFailure CanUseAmmo(ref WS_PlayerData.CharacterObject objCharacter, int AmmoID)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        if(objCharacter.DEAD)
        {
            return InventoryChangeFailure.EQUIP_ERR_YOU_ARE_DEAD;
        }

        if((AmmoID == 0) || !WorldServiceLocator.WorldServer.ITEMDatabase.ContainsKey(AmmoID))
        {
            return InventoryChangeFailure.EQUIP_ERR_ITEM_NOT_FOUND;
        }

        if(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].InventoryType != INVENTORY_TYPES.INVTYPE_AMMO)
        {
            return InventoryChangeFailure.EQUIP_ERR_ONLY_AMMO_CAN_GO_HERE;
        }
        if((WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].AvailableClasses != 0L) &&
            (((ulong)(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].AvailableClasses & objCharacter.ClassMask)) ==
                0))
        {
            return InventoryChangeFailure.EQUIP_ERR_YOU_CAN_NEVER_USE_THAT_ITEM;
        }
        if((WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].AvailableRaces != 0L) &&
            (((ulong)(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].AvailableRaces & objCharacter.RaceMask)) ==
                0))
        {
            return InventoryChangeFailure.EQUIP_ERR_YOU_CAN_NEVER_USE_THAT_ITEM;
        }
        if(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSkill != 0)
        {
            if(!objCharacter.HaveSkill(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSkill))
            {
                return InventoryChangeFailure.EQUIP_ERR_NO_REQUIRED_PROFICIENCY;
            }
            if(!objCharacter.HaveSkill(
                WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSkill,
                WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSkillRank))
            {
                return InventoryChangeFailure.EQUIP_ERR_SKILL_ISNT_HIGH_ENOUGH;
            }
        }
        if((WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSpell != 0) &&
            !objCharacter.HaveSpell(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqSpell))
        {
            return InventoryChangeFailure.EQUIP_ERR_NO_REQUIRED_PROFICIENCY;
        }
        if(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].ReqLevel > objCharacter.Level)
        {
            return InventoryChangeFailure.EQUIP_ERR_YOU_MUST_REACH_LEVEL_N;
        }
        return objCharacter.HavePassiveAura(46699)
            ? InventoryChangeFailure.EQUIP_ERR_BAG_FULL6
            : InventoryChangeFailure.EQUIP_ERR_OK;
    }

    public bool CheckAmmoCompatibility(ref WS_PlayerData.CharacterObject objCharacter, int AmmoID)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        if((AmmoID == 0) || !WorldServiceLocator.WorldServer.ITEMDatabase.ContainsKey(AmmoID))
        {
            return false;
        }
        if(!objCharacter.Items.ContainsKey(17) || objCharacter.Items[17].IsBroken())
        {
            return false;
        }
        if(objCharacter.Items[17].ItemInfo.ObjectClass != ITEM_CLASS.ITEM_CLASS_WEAPON)
        {
            return false;
        }
        switch(objCharacter.Items[17].ItemInfo.SubClass)
        {
            case ITEM_SUBCLASS.ITEM_SUBCLASS_LIQUID:
            case ITEM_SUBCLASS.ITEM_SUBCLASS_CROSSBOW:
                if(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].SubClass !=
                    ITEM_SUBCLASS.ITEM_SUBCLASS_LIQUID)
                {
                    return false;
                }
                break;

            case ITEM_SUBCLASS.ITEM_SUBCLASS_POTION:
                if(WorldServiceLocator.WorldServer.ITEMDatabase[AmmoID].SubClass !=
                    ITEM_SUBCLASS.ITEM_SUBCLASS_POTION)
                {
                    return false;
                }
                break;

            default:
                return false;
        }
        return true;
    }

    public void On_CMSG_LOGOUT_CANCEL(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
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
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_LOGOUT_CANCEL", client.IP, client.Port);
            if((client?.Character?.LogoutTimer) != null)
            {
                client.Character.LogoutTimer?.Dispose();
                client.Character.LogoutTimer = null;

                Packets.Packets.UpdateClass UpdateData = new(
                    WorldServiceLocator.GlobalConstants.FIELD_MASK_SIZE_PLAYER);
                Packets.Packets.PacketClass SMSG_UPDATE_OBJECT = new(Opcodes.SMSG_UPDATE_OBJECT);
                try
                {
                    SMSG_UPDATE_OBJECT.AddInt32(1);
                    SMSG_UPDATE_OBJECT.AddInt8(0);
                    client.Character.cUnitFlags &= -262145;
                    UpdateData.SetUpdateFlag(46, client.Character.cUnitFlags);
                    client.Character.StandState = 0;
                    UpdateData.SetUpdateFlag(138, client.Character.cBytes1);
                    var updateObject = client.Character;
                    UpdateData.AddToPacket(
                        ref SMSG_UPDATE_OBJECT,
                        ObjectUpdateType.UPDATETYPE_VALUES,
                        ref updateObject);
                    client.Send(ref SMSG_UPDATE_OBJECT);
                } catch(Exception ex)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.WARNING,
                            "SMSG_STANDSTATE_CHANGE_ACK Exception  {0} : {1} : {2} : {3} : {4}",
                            client,
                            client.IP,
                            client.Port,
                            UpdateData,
                            ex);
                } finally
                {
                    SMSG_UPDATE_OBJECT.Dispose();
                }
                Packets.Packets.PacketClass packetACK = new(Opcodes.SMSG_STANDSTATE_CHANGE_ACK);
                try
                {
                    packetACK.AddInt8(0);
                    client.Send(ref packetACK);
                } catch(Exception ex)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.WARNING,
                            "SMSG_STANDSTATE_CHANGE_ACK Exception  {0} : {1} : {2} : {3} : {4}",
                            client,
                            client.IP,
                            client.Port,
                            packetACK,
                            ex);
                } finally
                {
                    packetACK.Dispose();
                }
                Packets.Packets.PacketClass SMSG_LOGOUT_CANCEL_ACK = new(Opcodes.SMSG_LOGOUT_CANCEL_ACK);
                try
                {
                    client.Send(ref SMSG_LOGOUT_CANCEL_ACK);
                } catch(Exception ex)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.WARNING,
                            "SMSG_LOGOUT_CANCEL_ACK Exception  {0} : {1} : {2} : {3} : {4}",
                            client,
                            client.IP,
                            client.Port,
                            SMSG_LOGOUT_CANCEL_ACK,
                            ex);
                } finally
                {
                    SMSG_LOGOUT_CANCEL_ACK.Dispose();
                }
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_LOGOUT_CANCEL_ACK", client.IP, client.Port);
                client.Character.SetMoveUnroot();
            }
        } catch(Exception e)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.CRITICAL, "Error while trying to cancel logout.{0}", $"{Environment.NewLine}{e}");
        }
    }

    public void On_CMSG_LOGOUT_REQUEST(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
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
            .WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_LOGOUT_REQUEST", client.IP, client.Port);
        client.Character.Save();
        if(client.Character.IsInCombat)
        {
            Packets.Packets.PacketClass LOGOUT_RESPONSE_DENIED = new(Opcodes.SMSG_LOGOUT_RESPONSE);
            try
            {
                LOGOUT_RESPONSE_DENIED.AddInt32(0);
                LOGOUT_RESPONSE_DENIED.AddInt8(12);
                client.Send(ref LOGOUT_RESPONSE_DENIED);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "CMSG_LOGOUT_REQUEST Exception  {0} : {1} : {2} : {3}",
                        client,
                        client.IP,
                        client.Port,
                        ex);
            } finally
            {
                LOGOUT_RESPONSE_DENIED.Dispose();
            }
            return;
        }
        if(!(client.Character.positionZ <=
            (WorldServiceLocator.WSMaps
                    .GetZCoord(
                        client.Character.positionX,
                        client.Character.positionY,
                        client.Character.positionZ,
                        client.Character.MapID) +
                10f)))
        {
            Packets.Packets.UpdateClass UpdateData = new(WorldServiceLocator.GlobalConstants.FIELD_MASK_SIZE_PLAYER);
            Packets.Packets.PacketClass SMSG_UPDATE_OBJECT = new(Opcodes.SMSG_UPDATE_OBJECT);
            try
            {
                SMSG_UPDATE_OBJECT.AddInt32(1);
                SMSG_UPDATE_OBJECT.AddInt8(0);
                client.Character.cUnitFlags |= 0x40000;
                UpdateData.SetUpdateFlag(46, client.Character.cUnitFlags);
                client.Character.StandState = 1;
                UpdateData.SetUpdateFlag(138, client.Character.cBytes1);
                var updateObject = client.Character;
                UpdateData.AddToPacket(ref SMSG_UPDATE_OBJECT, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                client.Character.SendToNearPlayers(ref SMSG_UPDATE_OBJECT);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "SMSG_UPDATE_OBJECT Logout Exception  {0} : {1} : {2} : {3} : {4}",
                        client,
                        client.IP,
                        client.Port,
                        UpdateData,
                        ex);
            } finally
            {
                SMSG_UPDATE_OBJECT.Dispose();
            }
            Packets.Packets.PacketClass packetACK = new(Opcodes.SMSG_STANDSTATE_CHANGE_ACK);
            try
            {
                packetACK.AddInt8(1);
                client.Send(ref packetACK);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "SMSG_STANDSTATE_CHANGE_ACK Exception  {0} : {1} : {2} : {3} : {4}",
                        client,
                        client.IP,
                        client.Port,
                        packetACK,
                        ex);
            } finally
            {
                packetACK.Dispose();
            }
        }
        Packets.Packets.PacketClass SMSG_LOGOUT_RESPONSE = new(Opcodes.SMSG_LOGOUT_RESPONSE);
        try
        {
            SMSG_LOGOUT_RESPONSE.AddInt32(0);
            SMSG_LOGOUT_RESPONSE.AddInt8(0);
            client.Send(ref SMSG_LOGOUT_RESPONSE);
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.WARNING,
                    "SMSG_LOGOUT_RESPONSE Exception  {0} : {1} : {2} : {3} : {4}",
                    client,
                    client.IP,
                    client.Port,
                    SMSG_LOGOUT_RESPONSE,
                    ex);
        } finally
        {
            SMSG_LOGOUT_RESPONSE.Dispose();
        }
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_LOGOUT_RESPONSE", client.IP, client.Port);
        client.Character.SetMoveRoot();
        client.Character.ZoneCheck();
        if(client.Character.IsResting)
        {
            client.Character.Logout();
        } else
        {
            client.Character.LogoutTimer = new Timer(client.Character.Logout, null, 20000, -1);
        }
    }

    public void On_CMSG_SET_ACTION_BUTTON(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(checked(packet.Data.Length - 1) < 10)
        {
            return;
        }
        packet.GetInt16();
        var button = packet.GetInt8();
        var action = packet.GetUInt16();
        var actionMisc = packet.GetInt8();
        var actionType = packet.GetInt8();
        if(action == 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.DEBUG,
                    "[{0}:{1}] MSG_SET_ACTION_BUTTON [Remove action from button {2}]",
                    client.IP,
                    client.Port,
                    button);
            client.Character.ActionButtons.Remove(button);
        } else
        {
            switch(actionType)
            {
                case 64:
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.DEBUG,
                            "[{0}:{1}] CMSG_SET_ACTION_BUTTON [Added Macro {2} into button {3}]",
                            client.IP,
                            client.Port,
                            action,
                            button);
                    break;

                case 128:
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.DEBUG,
                            "[{0}:{1}] CMSG_SET_ACTION_BUTTON [Added Item {2} into button {3}]",
                            client.IP,
                            client.Port,
                            action,
                            button);
                    break;

                default:
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.DEBUG,
                            "[{0}:{1}] CMSG_SET_ACTION_BUTTON [Added Action {2}:{4}:{5} into button {3}]",
                            client.IP,
                            client.Port,
                            action,
                            button,
                            actionType,
                            actionMisc);
                    break;
            }
        }
        client.Character.ActionButtons[button] = new WS_PlayerHelper.TActionButton(action, actionType, actionMisc);
    }

    public void On_CMSG_STANDSTATECHANGE(ref Packets.Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if(packet is null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        if(client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if(checked(packet.Data.Length - 1) >= 6)
        {
            packet.GetInt16();
            var StandState = packet.GetInt8();
            if(StandState == 0)
            {
                client.Character.RemoveAurasByInterruptFlag(262144);
            }
            client.Character.StandState = StandState;
            client.Character.SetUpdateFlag(138, client.Character.cBytes1);
            client.Character.SendCharacterUpdate();
            Packets.Packets.PacketClass packetACK = new(Opcodes.SMSG_STANDSTATE_CHANGE_ACK);
            try
            {
                packetACK.AddInt8(StandState);
                client.Send(ref packetACK);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.WARNING,
                        "SMSG_STANDSTATE_CHANGE_ACK Exception  {0} : {1} : {2} : {3} : {4} : {5}",
                        client,
                        client.IP,
                        client.Port,
                        packetACK,
                        StandState,
                        ex);
            } finally
            {
                packetACK.Dispose();
            }
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.DEBUG,
                    "[{0}:{1}] CMSG_STANDSTATECHANGE [{2}]",
                    client.IP,
                    client.Port,
                    client.Character.StandState);
        }
    }
}
