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
using Mangos.Common.Legacy;
using Mangos.World.TimerBasedEvents;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Mangos.World.DataStores;

public class WS_DBCDatabase
{
    public const int DurabilityCosts_MAX = 300;

    public const int FACTION_TEMPLATES_COUNT = 2074;

    public Dictionary<byte, TBattleground> Battlegrounds;

    public Dictionary<int, byte> Battlemasters;

    public Dictionary<int, TCharClass> CharClasses;

    public Dictionary<int, TCharRace> CharRaces;

    public Dictionary<int, CreatureEquipInfo> CreatureEquip;

    public Dictionary<ulong, int> CreatureGossip;

    public Dictionary<int, CreatureModelInfo> CreatureModel;

    public Dictionary<int, Dictionary<int, CreatureMovePoint>> CreatureMovement;

    public Dictionary<int, CreatureFamilyInfo> CreaturesFamily;

    public short[,] DurabilityCosts;

    public Dictionary<int, int> EmotesState;

    public Dictionary<int, int> EmotesText;

    public Dictionary<int, TFaction> FactionInfo;

    public Dictionary<int, TFactionTemplate> FactionTemplatesInfo;

    public List<float> gtOCTRegenHP;

    public List<float> gtOCTRegenMP;

    public List<float> gtRegenHPPerSpt;

    public List<float> gtRegenMPPerSpt;

    public Dictionary<int, TItemDisplayInfo> ItemDisplayInfo;

    public Dictionary<int, TItemRandomPropertiesInfo> ItemRandomPropertiesInfo;

    public Dictionary<int, TItemSet> ItemSet;

    public Dictionary<int, TSkillLineAbility> SkillLineAbility;

    public Dictionary<int, int> SkillLines;

    public Dictionary<int, TSpellItemEnchantment> SpellItemEnchantments;

    public List<TSpellShapeshiftForm> SpellShapeShiftForm;

    public Dictionary<int, TalentInfo> Talents;

    public Dictionary<int, int> TalentsTab;

    public Dictionary<int, TTaxiNode> TaxiNodes;

    public Dictionary<int, Dictionary<int, TTaxiPathNode>> TaxiPathNodes;

    public Dictionary<int, TTaxiPath> TaxiPaths;

    public Dictionary<int, TTeleportCoords> TeleportCoords;

    public WS_DBCDatabase()
    {
        EmotesState = new Dictionary<int, int>();
        EmotesText = new Dictionary<int, int>();
        SkillLines = new Dictionary<int, int>();
        SkillLineAbility = new Dictionary<int, TSkillLineAbility>();
        TaxiNodes = new Dictionary<int, TTaxiNode>();
        TaxiPaths = new Dictionary<int, TTaxiPath>();
        TaxiPathNodes = new Dictionary<int, Dictionary<int, TTaxiPathNode>>();
        TalentsTab = new Dictionary<int, int>(30);
        Talents = new Dictionary<int, TalentInfo>(500);
        CharRaces = new Dictionary<int, TCharRace>();
        CharClasses = new Dictionary<int, TCharClass>();
        FactionInfo = new Dictionary<int, TFaction>();
        FactionTemplatesInfo = new Dictionary<int, TFactionTemplate>();
        SpellShapeShiftForm = new List<TSpellShapeshiftForm>();
        gtOCTRegenHP = new List<float>();
        gtOCTRegenMP = new List<float>();
        gtRegenHPPerSpt = new List<float>();
        gtRegenMPPerSpt = new List<float>();
        DurabilityCosts = (new short[301, 29]);
        SpellItemEnchantments = new Dictionary<int, TSpellItemEnchantment>();
        ItemSet = new Dictionary<int, TItemSet>();
        ItemDisplayInfo = new Dictionary<int, TItemDisplayInfo>();
        ItemRandomPropertiesInfo = new Dictionary<int, TItemRandomPropertiesInfo>();
        Battlemasters = new Dictionary<int, byte>();
        Battlegrounds = new Dictionary<byte, TBattleground>();
        TeleportCoords = new Dictionary<int, TTeleportCoords>();
        CreatureGossip = new Dictionary<ulong, int>();
        CreaturesFamily = new Dictionary<int, CreatureFamilyInfo>();
        CreatureMovement = new Dictionary<int, Dictionary<int, CreatureMovePoint>>();
        CreatureEquip = new Dictionary<int, CreatureEquipInfo>();
        CreatureModel = new Dictionary<int, CreatureModelInfo>();
    }

    private void InitializeXpTableFromDb()
    {
        DataTable result = null;
        try
        {
            WorldServiceLocator.WorldServer.WorldDatabase
                .Query("SELECT * FROM player_xp_for_level order by lvl;", ref result);
            if(result.Rows.Count > 0)
            {
                IEnumerator enumerator = default;
                try
                {
                    enumerator = result.Rows.GetEnumerator();
                    while(enumerator.MoveNext())
                    {
                        var row = (DataRow)enumerator.Current;
                        var dbLvl = row.As<int>("lvl");
                        var dbXp = row.As<long>("xp_for_next_level");
                        WorldServiceLocator.WSPlayerInitializator.XPTable[dbLvl] = checked((int)dbXp);
                    }
                } catch(Exception ex)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.FAILED,
                            "XPTable enumeration failed  {0} : {1} : {2}",
                            result,
                            enumerator,
                            ex);
                } finally
                {
                    if(enumerator is IDisposable)
                    {
                        (enumerator as IDisposable)?.Dispose();
                    }
                }
            }
            WorldServiceLocator.WorldServer.Log.WriteLine(LogType.INFORMATION, "Initalizing: XPTable initialized.");
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log.WriteLine(LogType.FAILED, "XPTable initialization failed.", ex);
        }
    }

    public TSpellShapeshiftForm FindShapeshiftForm(int ID)
    {
        foreach(var Form in SpellShapeShiftForm)
        {
            if(Form.ID == ID)
            {
                return Form;
            }
        }
        return null;
    }

    public int GetNearestTaxi(float x, float y, int map)
    {
        var selectedTaxiNode = 0;
        foreach(var TaxiNode in TaxiNodes)
        {
            if(TaxiNode.Value.MapID == map)
            {
                var tmp = WorldServiceLocator.WSCombat.GetDistance(x, TaxiNode.Value.x, y, TaxiNode.Value.y);
                var minDistance = 1E+08f;
                if(tmp < minDistance)
                {
                    minDistance = tmp;
                    selectedTaxiNode = TaxiNode.Key;
                }
            }
        }
        return selectedTaxiNode;
    }

    public void InitializeBattlegrounds()
    {
        DataTable mySqlQuery = new();
        WorldServiceLocator.WorldServer.WorldDatabase.Query("SELECT * FROM battleground_template", ref mySqlQuery);
        IEnumerator enumerator = default;
        try
        {
            enumerator = mySqlQuery.Rows.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var row = (DataRow)enumerator.Current;
                var entry = row.As<byte>("id");
                Battlegrounds.Add(entry, new TBattleground());
                Battlegrounds[entry].MinPlayersPerTeam = row.As<byte>("MinPlayersPerTeam");
                Battlegrounds[entry].MaxPlayersPerTeam = row.As<byte>("MaxPlayersPerTeam");
                Battlegrounds[entry].MinLevel = row.As<byte>("MinLvl");
                Battlegrounds[entry].MaxLevel = row.As<byte>("MaxLvl");
                Battlegrounds[entry].AllianceStartLoc = row.As<float>("AllianceStartLoc");
                Battlegrounds[entry].AllianceStartO = row.As<float>("AllianceStartO");
                Battlegrounds[entry].HordeStartLoc = row.As<float>("HordeStartLoc");
                Battlegrounds[entry].HordeStartO = row.As<float>("HordeStartO");
            }
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "Battleground enumeration failed  {0} : {1} : {2}",
                    mySqlQuery,
                    enumerator,
                    ex);
        } finally
        {
            if(enumerator is IDisposable)
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.INFORMATION, "World: {0} Battlegrounds Loaded.", mySqlQuery.Rows.Count);
    }

    public void InitializeBattlemasters()
    {
        DataTable MySQLQuery = new();
        WorldServiceLocator.WorldServer.WorldDatabase.Query("SELECT * FROM battlemaster_entry", ref MySQLQuery);
        IEnumerator enumerator = default;
        try
        {
            enumerator = MySQLQuery.Rows.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var row = (DataRow)enumerator.Current;
                Battlemasters.Add(row.As<int>("entry"), row.As<byte>("bg_template"));
            }
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "Battlemaster enumeration failed  {0} : {1} : {2}",
                    MySQLQuery,
                    enumerator,
                    ex);
        } finally
        {
            if(enumerator is IDisposable)
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.INFORMATION, "World: {0} Battlemasters Loaded.", MySQLQuery.Rows.Count);
    }

    public async Task InitializeInternalDatabaseAsync()
    {
        await InitializeLoadDBCsAsync();
        WorldServiceLocator.WSSpells.InitializeSpellDB();
        WorldServiceLocator.WSCommands.RegisterChatCommands();
        try
        {
            WorldServiceLocator.WSTimerBasedEvents.Regenerator = new WS_TimerBasedEvents.TRegenerator();
            WorldServiceLocator.WSTimerBasedEvents.AIManager = new WS_TimerBasedEvents.TAIManager();
            WorldServiceLocator.WSTimerBasedEvents.SpellManager = new WS_TimerBasedEvents.TSpellManager();
            WorldServiceLocator.WSTimerBasedEvents.CharacterSaver = new WS_TimerBasedEvents.TCharacterSaver();
            WorldServiceLocator.WSTimerBasedEvents.WeatherChanger = new WS_TimerBasedEvents.TWeatherChanger();
            WorldServiceLocator.WorldServer.Log.WriteLine(LogType.INFORMATION, "World: Loading Maps and Spawns....");
            DataTable MySQLQuery = new();
            try
            {
                WorldServiceLocator.WorldServer.CharacterDatabase
                    .Query("SELECT MAX(item_guid) FROM characters_inventory;", ref MySQLQuery);
                WorldServiceLocator.WorldServer.itemGuidCounter = (MySQLQuery.Rows[0][0] != DBNull.Value)
                    ? Conversions.ToULong(
                        Operators.AddObject(MySQLQuery.Rows[0][0], WorldServiceLocator.GlobalConstants.GUID_ITEM))
                    : Convert.ToUInt64(decimal.Add(0m, new decimal(WorldServiceLocator.GlobalConstants.GUID_ITEM)));
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.FAILED, "World: Failed loading characters_inventory....", ex);
            }
            MySQLQuery = new DataTable();
            try
            {
                WorldServiceLocator.WorldServer.WorldDatabase
                    .Query("SELECT MAX(guid) FROM creature;", ref MySQLQuery);
                WorldServiceLocator.WorldServer.CreatureGUIDCounter = (MySQLQuery.Rows[0][0] != DBNull.Value)
                    ? Conversions.ToULong(
                        Operators.AddObject(MySQLQuery.Rows[0][0], WorldServiceLocator.GlobalConstants.GUID_UNIT))
                    : Convert.ToUInt64(decimal.Add(0m, new decimal(WorldServiceLocator.GlobalConstants.GUID_UNIT)));
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.FAILED, "World: Failed loading creatures....", ex);
            }
            MySQLQuery = new DataTable();
            try
            {
                WorldServiceLocator.WorldServer.WorldDatabase
                    .Query("SELECT MAX(guid) FROM gameobject;", ref MySQLQuery);
                WorldServiceLocator.WorldServer.GameObjectsGUIDCounter = (MySQLQuery.Rows[0][0] != DBNull.Value)
                    ? Conversions.ToULong(
                        Operators.AddObject(
                            MySQLQuery.Rows[0][0],
                            WorldServiceLocator.GlobalConstants.GUID_GAMEOBJECT))
                    : Convert.ToUInt64(
                        decimal.Add(0m, new decimal(WorldServiceLocator.GlobalConstants.GUID_GAMEOBJECT)));
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.FAILED, "World: Failed loading gameobjects....", ex);
            }
            MySQLQuery = new DataTable();
            try
            {
                WorldServiceLocator.WorldServer.CharacterDatabase
                    .Query("SELECT MAX(guid) FROM corpse", ref MySQLQuery);
                WorldServiceLocator.WorldServer.CorpseGUIDCounter = (MySQLQuery.Rows[0][0] != DBNull.Value)
                    ? Conversions.ToULong(
                        Operators.AddObject(MySQLQuery.Rows[0][0], WorldServiceLocator.GlobalConstants.GUID_CORPSE))
                    : Convert.ToUInt64(
                        decimal.Add(0m, new decimal(WorldServiceLocator.GlobalConstants.GUID_CORPSE)));
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.FAILED, "World: Failed loading corpse....", ex);
            }
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "Internal database initialization failed! [{0}]{1}{2}",
                    ex.Message,
                    Environment.NewLine,
                    ex.ToString());
        }
    }

    public async Task InitializeLoadDBCsAsync()
    {
        InitializeXpTableFromDb();
        WorldServiceLocator.WSDBCLoad.LoadLootStores();
        WorldServiceLocator.WSDBCLoad.LoadWeather();
        InitializeBattlemasters();
        InitializeBattlegrounds();
        InitializeTeleportCoords();
        WorldServiceLocator.WSDBCLoad.LoadCreatureMovements();
        WorldServiceLocator.WSDBCLoad.LoadCreatureEquipTable();
        WorldServiceLocator.WSDBCLoad.LoadCreatureModelInfo();
        WorldServiceLocator.WSDBCLoad.LoadQuestStartersAndFinishers();
        WorldServiceLocator.WSDBCLoad.InitializeSpellChains();
        WorldServiceLocator.WSDBCLoad.LoadCreatureGossip();

        await Task.WhenAll(
            WorldServiceLocator.WSMaps.InitializeMapsAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeEmotesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeEmotesTextAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeAreaTableAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeFactionsAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeFactionTemplatesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeCharRacesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeCharClassesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSkillLinesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSkillLineAbilityAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeLocksAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeTaxiNodesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeTaxiPathsAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeTaxiPathNodesAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeDurabilityCostsAsync(),
            WorldServiceLocator.WSDBCLoad.LoadSpellItemEnchantmentsAsync(),
            WorldServiceLocator.WSDBCLoad.LoadItemSetAsync(),
            WorldServiceLocator.WSDBCLoad.LoadItemDisplayInfoDbcAsync(),
            WorldServiceLocator.WSDBCLoad.LoadItemRandomPropertiesDbcAsync(),
            WorldServiceLocator.WSDBCLoad.LoadTalentDbcAsync(),
            WorldServiceLocator.WSDBCLoad.LoadTalentTabDbcAsync(),
            WorldServiceLocator.WSDBCLoad.LoadAuctionHouseDbcAsync(),
            WorldServiceLocator.WSDBCLoad.LoadCreatureFamilyDbcAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellRadiusAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellDurationAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellCastTimeAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellRangeAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellFocusObjectAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellsAsync(),
            WorldServiceLocator.WSDBCLoad.InitializeSpellShapeShiftAsync());
    }

    public void InitializeTeleportCoords()
    {
        DataTable MySQLQuery = new();
        WorldServiceLocator.WorldServer.WorldDatabase.Query("SELECT * FROM spells_teleport_coords", ref MySQLQuery);
        IEnumerator enumerator = default;
        try
        {
            enumerator = MySQLQuery.Rows.GetEnumerator();
            while(enumerator.MoveNext())
            {
                var row = (DataRow)enumerator.Current;
                var SpellID = row.As<int>("id");
                TeleportCoords.Add(SpellID, new TTeleportCoords());
                TeleportCoords[SpellID].Name = row.As<string>("name");
                TeleportCoords[SpellID].MapID = row.As<uint>("mapId");
                TeleportCoords[SpellID].PosX = row.As<float>("position_x");
                TeleportCoords[SpellID].PosY = row.As<float>("position_y");
                TeleportCoords[SpellID].PosZ = row.As<float>("position_z");
            }
        } catch(Exception ex)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(
                    LogType.FAILED,
                    "Teleport Coords enumeration failed  {0} : {1} : {2}",
                    MySQLQuery,
                    enumerator,
                    ex);
        } finally
        {
            if(enumerator is IDisposable)
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
        WorldServiceLocator.WorldServer.Log
            .WriteLine(LogType.INFORMATION, "World: {0} Teleport Coords Loaded.", MySQLQuery.Rows.Count);
    }

    public class TSkillLineAbility
    {
        public int Forward_SpellID;
        public int ID;

        public int Max_Value;

        public int Min_Value;

        public int Required_Skill_Value;

        public int SkillID;

        public int SpellID;

        public int Unknown1;

        public int Unknown2;

        public int Unknown3;

        public int Unknown4;

        public int Unknown5;
    }

    public class TTaxiNode
    {
        public int AllianceMount;

        public int HordeMount;

        public int MapID;
        public float x;

        public float y;

        public float z;

        public TTaxiNode(float px, float py, float pz, int pMapID, int pHMount, int pAMount)
        {
            HordeMount = 0;
            AllianceMount = 0;
            x = px;
            y = py;
            z = pz;
            MapID = pMapID;
            HordeMount = pHMount;
            AllianceMount = pAMount;
        }
    }

    public class TTaxiPath
    {
        public int Price;
        public int TFrom;

        public int TTo;

        public TTaxiPath(int pFrom, int pTo, int pPrice)
        {
            TFrom = pFrom;
            TTo = pTo;
            Price = pPrice;
        }
    }

    public class TTaxiPathNode
    {
        public int action;

        public int MapID;
        public int Path;

        public int Seq;

        public int waitTime;

        public float x;

        public float y;

        public float z;

        public TTaxiPathNode(
            float px,
            float py,
            float pz,
            int pMapID,
            int pPath,
            int pSeq,
            int pAction,
            int pWaittime)
        {
            x = px;
            y = py;
            z = pz;
            MapID = pMapID;
            Path = pPath;
            Seq = pSeq;
            action = pAction;
            waitTime = pWaittime;
        }
    }

    public class TalentInfo
    {
        public int Col;

        public int[] RankID;

        public int[] RequiredPoints;

        public int[] RequiredTalent;

        public int Row;
        public int TalentID;

        public int TalentTab;

        public TalentInfo()
        {
            RankID = (new int[5]);
            RequiredTalent = (new int[3]);
            RequiredPoints = (new int[3]);
        }
    }

    public class TCharRace
    {
        public int CinematicID;
        public short FactionID;

        public int ModelFemale;

        public int ModelMale;

        public string RaceName;

        public uint TaxiMask;

        public byte TeamID;

        public TCharRace(short Faction, int ModelM, int ModelF, byte Team, uint Taxi, int Cinematic, string Name)
        {
            FactionID = Faction;
            ModelMale = ModelM;
            ModelFemale = ModelF;
            TeamID = Team;
            TaxiMask = Taxi;
            CinematicID = Cinematic;
            RaceName = Name;
        }
    }

    public class TCharClass
    {
        public int CinematicID;

        public TCharClass(int Cinematic) { CinematicID = Cinematic; }
    }

    public class TFaction
    {
        public short[] flags;
        public short ID;

        public byte[] rep_flags;

        public int[] rep_stats;

        public short VisibleID;

        public TFaction(
            short Id_,
            short VisibleID_,
            int flags1,
            int flags2,
            int flags3,
            int flags4,
            int rep_stats1,
            int rep_stats2,
            int rep_stats3,
            int rep_stats4,
            int rep_flags1,
            int rep_flags2,
            int rep_flags3,
            int rep_flags4)
        {
            flags = (new short[4]);
            rep_stats = (new int[4]);
            rep_flags = (new byte[4]);
            ID = Id_;
            VisibleID = VisibleID_;
            checked
            {
                flags[0] = (short)flags1;
                flags[1] = (short)flags2;
                flags[2] = (short)flags3;
                flags[3] = (short)flags4;
                rep_stats[0] = rep_stats1;
                rep_stats[1] = rep_stats2;
                rep_stats[2] = rep_stats3;
                rep_stats[3] = rep_stats4;
                rep_flags[0] = (byte)rep_flags1;
                rep_flags[1] = (byte)rep_flags2;
                rep_flags[2] = (byte)rep_flags3;
                rep_flags[3] = (byte)rep_flags4;
            }
        }
    }

    public class TFactionTemplate
    {
        public int enemyFaction1;

        public int enemyFaction2;

        public int enemyFaction3;

        public int enemyFaction4;

        public uint enemyMask;
        public int FactionID;

        public int friendFaction1;

        public int friendFaction2;

        public int friendFaction3;

        public int friendFaction4;

        public uint friendMask;

        public uint ourMask;
    }

    public class TSpellShapeshiftForm
    {
        public int AttackSpeed;

        public int CreatureType;

        public int Flags1;
        public int ID;

        public TSpellShapeshiftForm(int ID_, int Flags1_, int CreatureType_, int AttackSpeed_)
        {
            ID = 0;
            Flags1 = 0;
            ID = ID_;
            Flags1 = Flags1_;
            CreatureType = CreatureType_;
            AttackSpeed = AttackSpeed_;
        }
    }

    public class TSpellItemEnchantment
    {
        public int[] Amount;

        public int AuraID;

        public int Slot;

        public int[] SpellID;
        public int[] Type;

        public TSpellItemEnchantment(int[] Types, int[] Amounts, int[] SpellIDs, int AuraID_, int Slot_)
        {
            Type = (new int[3]);
            Amount = (new int[3]);
            SpellID = (new int[3]);
            byte i = 0;
            do
            {
                Type[i] = Types[i];
                Amount[i] = Amounts[i];
                SpellID[i] = SpellIDs[i];
                checked
                {
                    i = (byte)unchecked((uint)(i + 1));
                }
            } while (i <= 2u);
            AuraID = AuraID_;
            Slot = Slot_;
        }
    }

    public class TItemSet
    {
        public int ID;

        public int[] ItemCount;

        public int[] ItemID;

        public string Name;

        public int Required_Skill_ID;

        public int Required_Skill_Value;

        public int[] SpellID;

        public TItemSet(
            string Name_,
            int[] ItemID_,
            int[] SpellID_,
            int[] ItemCount_,
            int Required_Skill_ID_,
            int Required_Skill_Value_)
        {
            ItemID = (new int[8]);
            SpellID = (new int[8]);
            ItemCount = (new int[8]);
            byte i = 0;
            do
            {
                SpellID[i] = SpellID_[i];
                ItemID[i] = ItemID_[i];
                ItemCount[i] = ItemCount_[i];
                checked
                {
                    i = (byte)unchecked((uint)(i + 1));
                }
            } while (i <= 7u);
            Name = Name_;
            Required_Skill_ID = Required_Skill_ID_;
            Required_Skill_Value = Required_Skill_Value_;
        }
    }

    public class TItemDisplayInfo
    {
        public int ID;

        public int RandomPropertyChance;

        public int Unknown;
    }

    public class TItemRandomPropertiesInfo
    {
        public int[] Enchant_ID;
        public int ID;

        public TItemRandomPropertiesInfo() { Enchant_ID = (new int[4]); }
    }

    public class TBattleground
    {
        public float AllianceStartLoc;

        public float AllianceStartO;

        public float HordeStartLoc;

        public float HordeStartO;

        public byte MaxLevel;

        public byte MaxPlayersPerTeam;

        public byte MinLevel;
        public byte MinPlayersPerTeam;
    }

    public class TTeleportCoords
    {
        public uint MapID;
        public string Name;

        public float PosX;

        public float PosY;

        public float PosZ;
    }

    public class CreatureFamilyInfo
    {
        public int ID;

        public string Name;

        public int PetFoodID;

        public int Unknown1;

        public int Unknown2;
    }

    public class CreatureMovePoint
    {
        public int action;

        public int actionChance;

        public int moveFlag;

        public int waitTime;
        public float x;

        public float y;

        public float z;

        public CreatureMovePoint(
            float PosX,
            float PosY,
            float PosZ,
            int Wait,
            int MoveFlag,
            int Action,
            int ActionChance)
        {
            x = PosX;
            y = PosY;
            z = PosZ;
            waitTime = Wait;
            moveFlag = MoveFlag;
            action = Action;
            actionChance = ActionChance;
        }
    }

    public class CreatureEquipInfo
    {
        public uint[] EquipInfo;
        public int[] EquipModel;

        public int[] EquipSlot;

        public CreatureEquipInfo(
            int EquipModel1,
            int EquipModel2,
            int EquipModel3,
            uint EquipInfo1,
            uint EquipInfo2,
            uint EquipInfo3,
            int EquipSlot1,
            int EquipSlot2,
            int EquipSlot3)
        {
            EquipModel = (new int[3]);
            EquipInfo = (new uint[3]);
            EquipSlot = (new int[3]);
            EquipModel[0] = EquipModel1;
            EquipModel[1] = EquipModel2;
            EquipModel[2] = EquipModel3;
            EquipInfo[0] = EquipInfo1;
            EquipInfo[1] = EquipInfo2;
            EquipInfo[2] = EquipInfo3;
            EquipSlot[0] = EquipSlot1;
            EquipSlot[1] = EquipSlot2;
            EquipSlot[2] = EquipSlot3;
        }
    }

    public class CreatureModelInfo
    {
        public float BoundingRadius;

        public float CombatReach;

        public byte Gender;

        public int ModelIDOtherGender;

        public CreatureModelInfo(float BoundingRadius, float CombatReach, byte Gender, int ModelIDOtherGender)
        {
            this.BoundingRadius = BoundingRadius;
            this.CombatReach = CombatReach;
            this.Gender = Gender;
            this.ModelIDOtherGender = ModelIDOtherGender;
        }
    }
}
