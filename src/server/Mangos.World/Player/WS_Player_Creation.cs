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
using Mangos.Common.Enums.Misc;
using Mangos.Common.Enums.Player;
using Mangos.Common.Legacy;
using Mangos.World.Objects;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Mangos.World.Player;

public class WS_Player_Creation
{
    public void CreateCharacter(ref WS_PlayerData.CharacterObject objCharacter)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        DataTable CreateInfo = new();
        DataTable CreateInfoBars = new();
        DataTable CreateInfoSkills = new();
        DataTable LevelStats = new();
        DataTable ClassLevelStats = new();
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM playercreateinfo WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class};",
                ref CreateInfo);
        if(CreateInfo.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in playercreateinfo table for Race: {0}, Class: {1}",
                    objCharacter.Race,
                    objCharacter.Class);
        }
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM playercreateinfo_action WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class} ORDER BY button;",
                ref CreateInfoBars);
        if(CreateInfoBars.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in playercreateinfo_action table for Race: {0}, Class: {1}",
                    objCharacter.Race,
                    objCharacter.Class);
        }
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM playercreateinfo_skill WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class};",
                ref CreateInfoSkills);
        if(CreateInfoSkills.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in playercreateinfo_skill table for Race: {0}, Class: {1}",
                    objCharacter.Race,
                    objCharacter.Class);
        }
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM player_levelstats WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class} AND level = {objCharacter.Level};",
                ref LevelStats);
        if(LevelStats.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in player_levelstats table for Race: {0}, Class: {1}, Level: {2}",
                    objCharacter.Race,
                    objCharacter.Class,
                    objCharacter.Level);
        }
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM player_classlevelstats WHERE class = {(int)objCharacter.Class} AND level = {objCharacter.Level};",
                ref ClassLevelStats);
        if(ClassLevelStats.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in player_classlevelstats table for Class: {0}, Level: {1}",
                    objCharacter.Class,
                    objCharacter.Level);
        }
        objCharacter.Copper = 0u;
        objCharacter.XP = 0;
        objCharacter.Size = 1f;
        objCharacter.Life.Base = 0;
        objCharacter.Life.Current = 0;
        objCharacter.Mana.Base = 0;
        objCharacter.Mana.Current = 0;
        objCharacter.Rage.Current = 0;
        objCharacter.Rage.Base = 0;
        objCharacter.Energy.Current = 0;
        objCharacter.Energy.Base = 0;
        objCharacter.ManaType = WorldServiceLocator.WSPlayerInitializator.GetClassManaType(objCharacter.Class);
        objCharacter.Model = WorldServiceLocator.Functions.GetRaceModel(objCharacter.Race, (int)objCharacter.Gender);
        objCharacter.Faction = WorldServiceLocator.WSDBCDatabase.CharRaces[(int)objCharacter.Race].FactionID;
        objCharacter.MapID = Conversions.ToUInteger(CreateInfo.Rows[0]["map"]);
        objCharacter.ZoneID = Conversions.ToInteger(CreateInfo.Rows[0]["zone"]);
        objCharacter.positionX = Conversions.ToSingle(CreateInfo.Rows[0]["position_x"]);
        objCharacter.positionY = Conversions.ToSingle(CreateInfo.Rows[0]["position_y"]);
        objCharacter.positionZ = Conversions.ToSingle(CreateInfo.Rows[0]["position_z"]);
        objCharacter.orientation = Conversions.ToSingle(CreateInfo.Rows[0]["orientation"]);
        checked
        {
            objCharacter.bindPoint_map_id = (int)objCharacter.MapID;
            objCharacter.bindPoint_zone_id = objCharacter.ZoneID;
            objCharacter.bindPoint_positionX = objCharacter.positionX;
            objCharacter.bindPoint_positionY = objCharacter.positionY;
            objCharacter.bindPoint_positionZ = objCharacter.positionZ;
            objCharacter.Strength.Base = Conversions.ToInteger(LevelStats.Rows[0]["str"]);
            objCharacter.Agility.Base = Conversions.ToInteger(LevelStats.Rows[0]["agi"]);
            objCharacter.Stamina.Base = Conversions.ToInteger(LevelStats.Rows[0]["sta"]);
            objCharacter.Intellect.Base = Conversions.ToInteger(LevelStats.Rows[0]["inte"]);
            objCharacter.Spirit.Base = Conversions.ToInteger(LevelStats.Rows[0]["spi"]);
            objCharacter.Life.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basehp"]);
            objCharacter.Life.Current = objCharacter.Life.Maximum;
            switch(objCharacter.ManaType)
            {
                case ManaTypes.TYPE_MANA:
                    objCharacter.Mana.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Mana.Current = objCharacter.Mana.Maximum;
                    break;

                case ManaTypes.TYPE_RAGE:
                    objCharacter.Rage.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Rage.Current = 0;
                    break;

                case ManaTypes.TYPE_ENERGY:
                    objCharacter.Energy.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Energy.Current = 0;
                    break;
            }
            objCharacter.Damage.Minimum = 5f;
            objCharacter.Damage.Maximum = 10f;
            IEnumerator enumerator = default;
            try
            {
                enumerator = CreateInfoSkills.Rows.GetEnumerator();
                while(enumerator.MoveNext())
                {
                    var row = (DataRow)enumerator.Current;
                    objCharacter.LearnSkill(
                        row.As<int>("Skill"),
                        row.As<short>("SkillMin"),
                        row.As<short>("SkillMax"));
                }
            } finally
            {
                if(enumerator is IDisposable)
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }
            var i = 0;
            do
            {
                if((WorldServiceLocator.WSDBCDatabase.CharRaces[(int)objCharacter.Race].TaxiMask & (1 << i)) != 0)
                {
                    objCharacter.TaxiZones.Set(i + 1, value: true);
                }
                i++;
            } while (i <= 31);
            IEnumerator enumerator2 = default;
            try
            {
                enumerator2 = CreateInfoBars.Rows.GetEnumerator();
                while(enumerator2.MoveNext())
                {
                    var row = (DataRow)enumerator2.Current;
                    if(Operators.ConditionalCompareObjectGreater(row["action"], 0, TextCompare: false))
                    {
                        var ButtonPos = row.As<int>("button");
                        objCharacter.ActionButtons[(byte)ButtonPos] = new WS_PlayerHelper.TActionButton(
                            row.As<int>("action"),
                            row.As<byte>("type"),
                            0);
                    }
                }
            } finally
            {
                if(enumerator2 is IDisposable)
                {
                    (enumerator2 as IDisposable)?.Dispose();
                }
            }
        }
    }

    public int CreateCharacter(
        string Account,
        string Name,
        byte Race,
        byte Classe,
        byte Gender,
        byte Skin,
        byte Face,
        byte HairStyle,
        byte HairColor,
        byte FacialHair,
        byte OutfitID)
    {
        if(string.IsNullOrEmpty(Account))
        {
            throw new ArgumentException($"'{nameof(Account)}' cannot be null or empty.", nameof(Account));
        }

        if(string.IsNullOrEmpty(Name))
        {
            throw new ArgumentException($"'{nameof(Name)}' cannot be null or empty.", nameof(Name));
        }

        WS_PlayerData.CharacterObject Character = new();
        DataTable MySQLQuery = new();
        Character.Name = WorldServiceLocator.Functions.CapitalizeName(ref Name);
        Character.Race = (Races)Race;
        Character.Class = (Classes)Classe;
        Character.Gender = (Genders)Gender;
        Character.Skin = Skin;
        Character.Face = Face;
        Character.HairStyle = HairStyle;
        Character.HairColor = HairColor;
        Character.FacialHair = FacialHair;
        WorldServiceLocator.WorldServer.AccountDatabase
            .Query($"SELECT id, gmlevel FROM account WHERE username = \"{Account}\";", ref MySQLQuery);
        var Account_ID = MySQLQuery.Rows[0].As<int>("id");
        var Account_Access = Character.Access = (AccessLevel)MySQLQuery.Rows[0].As<byte>("gmlevel");
        if(!WorldServiceLocator.Functions.ValidateName(Character.Name))
        {
            return 70;
        }
        try
        {
            MySQLQuery.Clear();
            WorldServiceLocator.WorldServer.CharacterDatabase
                .Query($"SELECT char_name FROM characters WHERE char_name = \"{Character.Name}\";", ref MySQLQuery);
            if(MySQLQuery.Rows.Count > 0)
            {
                return 49;
            }
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.WARNING, "Error in selecting Character Name {0} : {1}", Character.Name, ex);
            return 48;
        }
        checked
        {
            if(WorldServiceLocator.GlobalConstants.SERVER_CONFIG_DISABLED_CLASSES[((int)Character.Class) - 1] ||
                (WorldServiceLocator.GlobalConstants.SERVER_CONFIG_DISABLED_RACES[((int)Character.Race) - 1] &&
                    (Account_Access < AccessLevel.GameMaster)))
            {
                return 50;
            }
            if(Account_Access <= AccessLevel.Player)
            {
                MySQLQuery.Clear();
                WorldServiceLocator.WorldServer.CharacterDatabase
                    .Query(
                        $"SELECT char_race FROM characters WHERE account_id = \"{Account_ID}\" LIMIT 1;",
                        ref MySQLQuery);
                if((MySQLQuery.Rows.Count > 0) &&
                    (Character.IsHorde !=
                        WorldServiceLocator.Functions.GetCharacterSide(MySQLQuery.Rows[0].As<byte>("char_race"))))
                {
                    return 51;
                }
            }
            MySQLQuery.Clear();
            WorldServiceLocator.WorldServer.CharacterDatabase
                .Query($"SELECT char_name FROM characters WHERE account_id = \"{Account_ID}\";", ref MySQLQuery);
            if(MySQLQuery.Rows.Count >= 10)
            {
                return 52;
            }
            MySQLQuery.Clear();
            WorldServiceLocator.WorldServer.CharacterDatabase
                .Query($"SELECT char_name FROM characters WHERE account_id = \"{Account_ID}\";", ref MySQLQuery);
            if(MySQLQuery.Rows.Count >= 10)
            {
                return 53;
            }
            try
            {
                WorldServiceLocator.WSPlayerInitializator.InitializeReputations(ref Character);
                CreateCharacter(ref Character);
                Character.SaveAsNewCharacter(Account_ID);
                CreateCharacterSpells(ref Character);
                CreateCharacterItems(ref Character);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.FAILED,
                        "Error initializing character! {0} {1}",
                        Environment.NewLine,
                        ex.ToString());
                return 48;
            } finally
            {
                Character.Dispose();
            }
            return 46;
        }
    }

    public void CreateCharacterItems(ref WS_PlayerData.CharacterObject objCharacter)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        DataTable CreateInfoItems = new();
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM playercreateinfo_item WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class};",
                ref CreateInfoItems);
        if(CreateInfoItems.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in playercreateinfo_item table for Race: {0}, Class: {1}",
                    objCharacter.Race,
                    objCharacter.Class);
        }
        Dictionary<int, int> Items = new();
        List<int> Used = new();
        IEnumerator enumerator = default;
        try
        {
            enumerator = CreateInfoItems.Rows.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var row = (DataRow)enumerator.Current;
                Items.Add(row.As<int>("itemid"), row.As<int>("amount"));
            }
        } finally
        {
            if(enumerator is IDisposable)
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
        foreach(var Item2 in Items)
        {
            if(!WorldServiceLocator.WorldServer.ITEMDatabase.ContainsKey(Item2.Key))
            {
                WS_Items.ItemInfo newItem = new(Item2.Key);
            }
            if(WorldServiceLocator.WorldServer.ITEMDatabase[Item2.Key].ContainerSlots <= 0)
            {
                continue;
            }
            foreach(var tmpSlot2 in WorldServiceLocator.WorldServer.ITEMDatabase[Item2.Key].GetSlots)
            {
                if(!objCharacter.Items.ContainsKey(tmpSlot2))
                {
                    objCharacter.ItemADD(Item2.Key, 0, tmpSlot2, Item2.Value);
                    Used.Add(Item2.Key);
                    break;
                }
            }
        }
        foreach(var Item in Items)
        {
            if(Used.Contains(Item.Key))
            {
                continue;
            }
            var array2 = WorldServiceLocator.WorldServer.ITEMDatabase[Item.Key].GetSlots;
            var num = 0;
            while(true)
            {
                if(num < array2.Length)
                {
                    var tmpSlot = array2[num];
                    if(!objCharacter.Items.ContainsKey(tmpSlot))
                    {
                        objCharacter.ItemADD(Item.Key, 0, tmpSlot, Item.Value);
                        break;
                    }
                    num = checked(num + 1);
                    continue;
                }
                objCharacter.ItemADD(Item.Key, byte.MaxValue, byte.MaxValue, Item.Value);
                break;
            }
        }
    }

    public void CreateCharacterSpells(ref WS_PlayerData.CharacterObject objCharacter)
    {
        if(objCharacter is null)
        {
            throw new ArgumentNullException(nameof(objCharacter));
        }

        DataTable CreateInfoSpells = new();
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM playercreateinfo_spell WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Class};",
                ref CreateInfoSpells);
        if(CreateInfoSpells.Rows.Count <= 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "No information found in playercreateinfo_spell table Race: {0}, Class: {1}",
                    objCharacter.Race,
                    objCharacter.Class);
        }
        IEnumerator enumerator = default;
        try
        {
            enumerator = CreateInfoSpells.Rows.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var row = (DataRow)enumerator.Current;
                objCharacter.LearnSpell(row.As<int>("Spell"));
            }
        } finally
        {
            if(enumerator is IDisposable)
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
