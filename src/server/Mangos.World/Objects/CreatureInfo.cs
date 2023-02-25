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
using Mangos.World.Globals;
using Mangos.World.Gossip;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mangos.World.Objects;

public class CreatureInfo : IDisposable
{
    private bool _disposedValue;
    private readonly bool found_;

    public string AIScriptSource;

    public int AttackPower;

    public short BaseAttackTime;

    public short BaseRangedAttackTime;

    public int cFlags;

    public byte Class;

    public int cNpcFlags;

    public byte CreatureFamily;

    public byte CreatureType;

    public WS_Items.TDamage Damage;

    public int DynFlags;

    public byte Elite;

    public int EquipmentID;

    public short Faction;

    public byte HonorRank;

    public int Id;

    public byte Leader;

    public byte LevelMax;

    public byte LevelMin;

    public int LootID;

    public byte ManaType;

    public uint MaxGold;

    public int MaxLife;

    public int MaxMana;

    public uint MechanicImmune;

    public uint MinGold;

    public int MinLife;

    public int MinMana;

    public int ModelA1;

    public int ModelA2;

    public int ModelH1;

    public int ModelH2;

    public string Name;

    public int PetSpellDataID;

    public int PocketLootID;

    public byte Race;

    public int RangedAttackPower;

    public WS_Items.TDamage RangedDamage;

    public int[] Resistances;

    public float RunSpeed;

    public float Size;

    public int SkinLootID;

    public int[] Spells;

    public string SubName;

    public TBaseTalk TalkScript;

    public int TrainerSpell;

    public int TrainerType;

    public uint TypeFlags;

    public float UnkFloat1;

    public float UnkFloat2;

    public float WalkSpeed;

    public CreatureInfo()
    {
        found_ = false;
        Id = 0;
        Name = "MISSING_CREATURE_INFO";
        SubName = string.Empty;
        Size = 1f;
        ModelA1 = 262;
        ModelA2 = 0;
        ModelH1 = 0;
        ModelH2 = 0;
        MinLife = 1;
        MaxLife = 1;
        MinMana = 1;
        MaxMana = 1;
        ManaType = 0;
        Faction = 0;
        CreatureType = 0;
        CreatureFamily = 0;
        Elite = 0;
        HonorRank = 0;
        Damage = new WS_Items.TDamage();
        RangedDamage = new WS_Items.TDamage();
        AttackPower = 0;
        RangedAttackPower = 0;
        Resistances = (new int[7]);
        WalkSpeed = WorldServiceLocator.GlobalConstants.UNIT_NORMAL_WALK_SPEED;
        RunSpeed = WorldServiceLocator.GlobalConstants.UNIT_NORMAL_RUN_SPEED;
        BaseAttackTime = 2000;
        BaseRangedAttackTime = 2000;
        LevelMin = 1;
        LevelMax = 1;
        Leader = 0;
        TrainerSpell = 0;
        Class = 0;
        Race = 0;
        PetSpellDataID = 0;
        Spells = (new int[4]);
        LootID = 0;
        SkinLootID = 0;
        PocketLootID = 0;
        MinGold = 0u;
        MaxGold = 0u;
        EquipmentID = 0;
        MechanicImmune = 0u;
        UnkFloat1 = 1f;
        UnkFloat2 = 1f;
        AIScriptSource = string.Empty;
        TalkScript = null;
        Damage.Minimum = 0.8f * BaseAttackTime / 1000f * (LevelMin * 10f);
        Damage.Maximum = 1.2f * BaseAttackTime / 1000f * (LevelMax * 10f);
    }

    public CreatureInfo(int CreatureID) : this()
    {
        Id = CreatureID;
        WorldServiceLocator.WorldServer.CREATURESDatabase.Add(Id, this);
        DataTable MySQLQuery = new();
        WorldServiceLocator.WorldServer.WorldDatabase
            .Query(
                $"SELECT * FROM creature_template LEFT JOIN creature_template_spells ON creature_template.entry = creature_template_spells.`entry` WHERE creature_template.entry = {CreatureID};",
                ref MySQLQuery);
        if(MySQLQuery.Rows.Count == 0)
        {
            WorldServiceLocator.WorldServer.Log
                .WriteLine(LogType.FAILED, "CreatureID {0} not found in SQL database.", CreatureID);
            found_ = false;
            return;
        }
        found_ = true;
        ModelA1 = MySQLQuery.Rows[0].As<int>("modelid1");
        ModelA2 = MySQLQuery.Rows[0].As<int>("Modelid2");
        ModelH1 = MySQLQuery.Rows[0].As<int>("modelid3");
        ModelH2 = MySQLQuery.Rows[0].As<int>("modelid4");
        Name = MySQLQuery.Rows[0].As<string>("name");
        try
        {
            SubName = MySQLQuery.Rows[0].As<string>("subname");
        } catch(Exception)
        {
            SubName = string.Empty;
        }
        Size = MySQLQuery.Rows[0].As<float>("scale");
        MinLife = MySQLQuery.Rows[0].As<int>("MinLevelHealth");
        MaxLife = MySQLQuery.Rows[0].As<int>("MaxLevelHealth");
        MinMana = MySQLQuery.Rows[0].As<int>("MinLevelMana");
        MaxMana = MySQLQuery.Rows[0].As<int>("MaxLevelMana");
        ManaType = 0;
        Faction = MySQLQuery.Rows[0].As<short>("factionAlliance");
        Elite = MySQLQuery.Rows[0].As<byte>("rank");
        Damage.Maximum = MySQLQuery.Rows[0].As<float>("MaxMeleeDmg");
        RangedDamage.Maximum = MySQLQuery.Rows[0].As<float>("MaxRangedDmg");
        Damage.Minimum = MySQLQuery.Rows[0].As<float>("MinMeleeDmg");
        RangedDamage.Minimum = MySQLQuery.Rows[0].As<float>("MinRangedDmg");
        AttackPower = MySQLQuery.Rows[0].As<int>("MeleeAttackPower");
        RangedAttackPower = MySQLQuery.Rows[0].As<int>("RangedAttackPower");
        WalkSpeed = MySQLQuery.Rows[0].As<float>("SpeedWalk");
        RunSpeed = MySQLQuery.Rows[0].As<float>("SpeedRun");
        BaseAttackTime = MySQLQuery.Rows[0].As<short>("MeleeBaseAttackTime");
        BaseRangedAttackTime = MySQLQuery.Rows[0].As<short>("RangedBaseAttackTime");
        cNpcFlags = MySQLQuery.Rows[0].As<int>("NpcFlags");
        DynFlags = MySQLQuery.Rows[0].As<int>("DynamicFlags");
        cFlags = MySQLQuery.Rows[0].As<int>("UnitFlags");
        TypeFlags = MySQLQuery.Rows[0].As<uint>("CreatureTypeFlags");
        CreatureType = MySQLQuery.Rows[0].As<byte>("CreatureType");
        CreatureFamily = MySQLQuery.Rows[0].As<byte>("Family");
        LevelMin = MySQLQuery.Rows[0].As<byte>("MinLevel");
        LevelMax = MySQLQuery.Rows[0].As<byte>("MaxLevel");
        TrainerType = MySQLQuery.Rows[0].As<int>("TrainerType");
        TrainerSpell = MySQLQuery.Rows[0].As<int>("TrainerSpell");
        Class = MySQLQuery.Rows[0].As<byte>("TrainerClass");
        Race = MySQLQuery.Rows[0].As<byte>("TrainerRace");
        Leader = MySQLQuery.Rows[0].As<byte>("RacialLeader");

        Spells[0] = (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["spell1"])))
            ? MySQLQuery.Rows[0].As<int>("spell1")
            : 0;
        Spells[1] = (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["spell2"])))
            ? MySQLQuery.Rows[0].As<int>("spell2")
            : 0;
        Spells[2] = (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["spell3"])))
            ? MySQLQuery.Rows[0].As<int>("spell3")
            : 0;
        Spells[3] = (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(MySQLQuery.Rows[0]["spell4"])))
            ? MySQLQuery.Rows[0].As<int>("spell4")
            : 0;
        PetSpellDataID = MySQLQuery.Rows[0].As<int>("PetSpellDataId");
        LootID = MySQLQuery.Rows[0].As<int>("LootId");
        SkinLootID = MySQLQuery.Rows[0].As<int>("SkinningLootId");
        PocketLootID = MySQLQuery.Rows[0].As<int>("PickpocketLootId");
        MinGold = MySQLQuery.Rows[0].As<uint>("MinLootGold");
        MaxGold = MySQLQuery.Rows[0].As<uint>("MaxLootGold");
        Resistances[0] = MySQLQuery.Rows[0].As<int>("Armor");
        Resistances[1] = MySQLQuery.Rows[0].As<int>("ResistanceHoly");
        Resistances[2] = MySQLQuery.Rows[0].As<int>("ResistanceFire");
        Resistances[3] = MySQLQuery.Rows[0].As<int>("ResistanceNature");
        Resistances[4] = MySQLQuery.Rows[0].As<int>("ResistanceFrost");
        Resistances[5] = MySQLQuery.Rows[0].As<int>("ResistanceShadow");
        Resistances[6] = MySQLQuery.Rows[0].As<int>("ResistanceArcane");
        EquipmentID = MySQLQuery.Rows[0].As<int>("EquipmentTemplateId");
        MechanicImmune = MySQLQuery.Rows[0].As<uint>("SchoolImmuneMask");
        if(File.Exists($"scripts\\gossip\\{WorldServiceLocator.Functions.FixName(Name)}.vb"))
        {
            ScriptedObject tmpScript = new(
                $"scripts\\gossip\\{WorldServiceLocator.Functions.FixName(Name)}.vb",
                string.Empty,
                InMemory: true);
            TalkScript = (TBaseTalk)tmpScript.InvokeConstructor("TalkScript");
            tmpScript.Dispose();
        } else if((((uint)cNpcFlags) & 0x10u) != 0)
        {
            TalkScript = new WS_NPCs.TDefaultTalk();
        } else if((((uint)cNpcFlags) & 0x40u) != 0)
        {
            TalkScript = new WS_GuardGossip.TGuardTalk();
        } else if(cNpcFlags == 0)
        {
            TalkScript = null;
        } else
        {
            TalkScript = (cNpcFlags == 1) ? new WS_NPCs.TDefaultTalk() : new WS_NPCs.TDefaultTalk();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if(!_disposedValue)
        {
            WorldServiceLocator.WorldServer.CREATURESDatabase.Remove(Id);
        }
        _disposedValue = true;
    }

    public int GetFirstModel
    {
        get
        {
            if(ModelA1 != 0)
            {
                return ModelA1;
            }
            if(ModelA2 != 0)
            {
                return ModelA2;
            }
            if(ModelH1 != 0)
            {
                return ModelH1;
            }
            return (ModelH2 != 0) ? ModelH2 : 0;
        }
    }

    public int GetRandomModel
    {
        get
        {
            var modelIDs = new int[4];
            var current = 0;
            checked
            {
                if(ModelA1 != 0)
                {
                    modelIDs[current] = ModelA1;
                    current++;
                }
                if(ModelA2 != 0)
                {
                    modelIDs[current] = ModelA2;
                    current++;
                }
                if(ModelH1 != 0)
                {
                    modelIDs[current] = ModelH1;
                    current++;
                }
                if(ModelH2 != 0)
                {
                    modelIDs[current] = ModelH2;
                    current++;
                }
                return (current == 0) ? 0 : modelIDs[WorldServiceLocator.WorldServer.Rnd.Next(0, current)];
            }
        }
    }

    public int Life => WorldServiceLocator.WorldServer.Rnd.Next(MinLife, MaxLife);

    public int Mana => WorldServiceLocator.WorldServer.Rnd.Next(MinMana, MaxMana);
}
