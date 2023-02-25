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
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Logging;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Player;
using Mangos.World.Quests;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mangos.World;

public class WorldServer : IDisposable
{
    public const int SERVERSEED = -569166080;
    private readonly ICluster cluster;
    public SQL AccountDatabase;

    public ScriptedObject AI;

    public WS_GraveYards AllGraveYards;

    public WS_Quests ALLQUESTS;

    public ScriptedObject AreaTriggers;

    public Dictionary<ulong, WS_Handlers_Battleground> BATTLEGROUNDs;

    public SQL CharacterDatabase;

    public Dictionary<ulong, WS_PlayerData.CharacterObject> CHARACTERs;

    //public Dictionary<ulong, WS_Handlers_Warden> WARDENs;

    public ReaderWriterLock CHARACTERs_Lock;

    public Dictionary<ulong, CharManagementHandler> CHARMANAGEMENTHANDLERs;

    public Dictionary<ulong, WS_CharMovement> CHARMOVEMENTs;

    public Dictionary<ulong, WS_Handlers_Chat> CHATs;

    public Dictionary<uint, WS_Network.ClientClass> CLIENTs;

    public WS_Network.WorldServerClass ClsWorldServer;

    public Dictionary<ulong, WS_Combat> COMBATs;

    public ulong CorpseGUIDCounter;

    public ulong CreatureGUIDCounter;

    public Dictionary<int, List<int>> CreatureQuestFinishers;

    public Dictionary<int, List<int>> CreatureQuestStarters;

    public Dictionary<int, CreatureInfo> CREATURESDatabase;

    public ulong DynamicObjectsGUIDCounter;

    public Dictionary<ulong, WS_Handlers_Gamemaster> GAMEMASTERs;

    public Dictionary<int, List<int>> GameobjectQuestFinishers;

    public Dictionary<int, List<int>> GameObjectQuestStarters;

    public Dictionary<int, WS_GameObjects.GameObjectInfo> GAMEOBJECTSDatabase;

    public ulong GameObjectsGUIDCounter;

    public Dictionary<ulong, WS_Handlers_Instance> INSTANCEs;

    public Dictionary<int, WS_Items.ItemInfo> ITEMDatabase;

    public ulong itemGuidCounter;

    public BaseWriter Log;

    public Dictionary<ulong, WS_Handlers_Misc> MISCs;

    public Dictionary<Opcodes, HandlePacket> PacketHandlers;

    public Random Rnd;

    public Dictionary<ulong, WS_Handlers_Taxi> TAXIs;

    public Dictionary<ulong, WS_Handlers_Trade> TRADEs;

    public ulong TransportGUIDCounter;

    public Dictionary<ulong, WS_Corpses.CorpseObject> WORLD_CORPSEOBJECTs;

    public Dictionary<ulong, WS_Creatures.CreatureObject> WORLD_CREATUREs;

    public ReaderWriterLock WORLD_CREATUREs_Lock;

    public ArrayList WORLD_CREATUREsKeys;

    public Dictionary<ulong, WS_DynamicObjects.DynamicObject> WORLD_DYNAMICOBJECTs;

    public ReaderWriterLock WORLD_DYNAMICOBJECTs_Lock;

    public Dictionary<ulong, WS_GameObjects.GameObject> WORLD_GAMEOBJECTs;

    public Dictionary<ulong, ItemObject> WORLD_ITEMs;

    public Dictionary<ulong, WS_Transports.TransportObject> WORLD_TRANSPORTs;

    public ReaderWriterLock WORLD_TRANSPORTs_Lock;

    public SQL WorldDatabase;

    public WorldServer(ICluster cluster)
    {
        CLIENTs = new Dictionary<uint, WS_Network.ClientClass>();
        CHARACTERs = new Dictionary<ulong, WS_PlayerData.CharacterObject>();
        CHARACTERs_Lock = new ReaderWriterLock();
        ALLQUESTS = new WS_Quests();
        AllGraveYards = new WS_GraveYards(WorldServiceLocator.DataStoreProvider);
        CreatureQuestStarters = new Dictionary<int, List<int>>();
        CreatureQuestFinishers = new Dictionary<int, List<int>>();
        GameObjectQuestStarters = new Dictionary<int, List<int>>();
        GameobjectQuestFinishers = new Dictionary<int, List<int>>();
        WORLD_CREATUREs_Lock = new ReaderWriterLock();
        WORLD_CREATUREs = new Dictionary<ulong, WS_Creatures.CreatureObject>();
        WORLD_CREATUREsKeys = new ArrayList();
        WORLD_GAMEOBJECTs = new Dictionary<ulong, WS_GameObjects.GameObject>();
        WORLD_CORPSEOBJECTs = new Dictionary<ulong, WS_Corpses.CorpseObject>();
        WORLD_DYNAMICOBJECTs_Lock = new ReaderWriterLock();
        WORLD_DYNAMICOBJECTs = new Dictionary<ulong, WS_DynamicObjects.DynamicObject>();
        WORLD_TRANSPORTs_Lock = new ReaderWriterLock();
        WORLD_TRANSPORTs = new Dictionary<ulong, WS_Transports.TransportObject>();
        WORLD_ITEMs = new Dictionary<ulong, ItemObject>();
        BATTLEGROUNDs = new Dictionary<ulong, WS_Handlers_Battleground>();
        ITEMDatabase = new Dictionary<int, WS_Items.ItemInfo>();
        CREATURESDatabase = new Dictionary<int, CreatureInfo>();
        GAMEOBJECTSDatabase = new Dictionary<int, WS_GameObjects.GameObjectInfo>();
        itemGuidCounter = WorldServiceLocator.GlobalConstants.GUID_ITEM;
        CreatureGUIDCounter = WorldServiceLocator.GlobalConstants.GUID_UNIT;
        GameObjectsGUIDCounter = WorldServiceLocator.GlobalConstants.GUID_GAMEOBJECT;
        CorpseGUIDCounter = WorldServiceLocator.GlobalConstants.GUID_CORPSE;
        DynamicObjectsGUIDCounter = WorldServiceLocator.GlobalConstants.GUID_DYNAMICOBJECT;
        TransportGUIDCounter = WorldServiceLocator.GlobalConstants.GUID_MO_TRANSPORT;
        Log = new BaseWriter();
        PacketHandlers = new Dictionary<Opcodes, HandlePacket>();
        Rnd = new Random();
        AccountDatabase = new SQL();
        CharacterDatabase = new SQL();
        WorldDatabase = new SQL();
        this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
    }

    public delegate void HandlePacket(ref Packets.Packets.PacketClass Packet, ref WS_Network.ClientClass client);

    private async void GenericExceptionHandlerAsync(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var EX = (Exception)e.ExceptionObject;
            Log.WriteLine(LogType.CRITICAL, $"{EX}{Environment.NewLine}");
            Log.WriteLine(
                LogType.FAILED,
                "Unexpected error has occured. An 'WorldServer-Error-yyyy-mmm-d-h-mm.log' file has been created. Check your log folder for more information.");
            var filename = @"""""""""WorldServer-Error-"" + ""{(DateTime.Now, "" + ""yyyy-MMM-d-H-mm"" + "")}.log""""""""";
            filename = "{filename}";
            await new StreamWriter(new FileStream(filename, FileMode.Append)).WriteAsync(
                EX.Message + EX.StackTrace);
            //await new StreamWriter(new FileStream(filename, FileMode.Append)).DisposeAsync();
            //new StreamWriter(new FileStream(filename, FileMode.Append)).Close();
        } finally
        {
            Thread.Sleep(5000); //Wait 5 Seconds to Ensure logs are created and the Operator has a chance to view the Exception in Console.
            Environment.FailFast("An Unhandled Exception has occured and the Server has Crashed!"); //Named event log and Ensure the Server closes out at all times.
        }
    }

    public void AccountSQLEventHandler(SQL.EMessages MessageID, string OutBuf)
    {
        if(OutBuf is null)
        {
            throw new ArgumentNullException(nameof(OutBuf));
        }

        switch(MessageID)
        {
            case SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[ACCOUNT] {OutBuf}");
                break;

            case SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[ACCOUNT] {OutBuf}");
                break;

            default:
                break;
        }
    }

    public void CharacterSQLEventHandler(SQL.EMessages MessageID, string OutBuf)
    {
        if(OutBuf is null)
        {
            throw new ArgumentNullException(nameof(OutBuf));
        }

        switch(MessageID)
        {
            case SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[CHARACTER] {OutBuf}");
                break;

            case SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[CHARACTER] {OutBuf}");
                break;

            default:
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void LoadConfig()
    {
        try
        {
            var array = Environment.GetCommandLineArgs();
            var configuration = WorldServiceLocator.MangosConfiguration.World;
            Console.WriteLine(".[done]");
            if(!configuration.VMapsEnabled)
            {
                configuration.LineOfSightEnabled = false;
                configuration.HeightCalcEnabled = false;
            }
            var AccountDBSettings = Strings.Split(configuration.AccountDatabase, ";");
            if(AccountDBSettings.Length == 6)
            {
                AccountDatabase.SQLDBName = AccountDBSettings[4];
                AccountDatabase.SQLHost = AccountDBSettings[2];
                AccountDatabase.SQLPort = AccountDBSettings[3];
                AccountDatabase.SQLUser = AccountDBSettings[0];
                AccountDatabase.SQLPass = AccountDBSettings[1];
                AccountDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(
                    Enum.Parse(typeof(SQL.DB_Type), AccountDBSettings[5]));
            } else
            {
                Console.WriteLine("Invalid connect string for the account database!");
            }
            var CharacterDBSettings = Strings.Split(configuration.CharacterDatabase, ";");
            if(CharacterDBSettings.Length == 6)
            {
                CharacterDatabase.SQLDBName = CharacterDBSettings[4];
                CharacterDatabase.SQLHost = CharacterDBSettings[2];
                CharacterDatabase.SQLPort = CharacterDBSettings[3];
                CharacterDatabase.SQLUser = CharacterDBSettings[0];
                CharacterDatabase.SQLPass = CharacterDBSettings[1];
                CharacterDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(
                    Enum.Parse(typeof(SQL.DB_Type), CharacterDBSettings[5]));
            } else
            {
                Console.WriteLine("Invalid connect string for the character database!");
            }
            var WorldDBSettings = Strings.Split(configuration.WorldDatabase, ";");
            if(WorldDBSettings.Length == 6)
            {
                WorldDatabase.SQLDBName = WorldDBSettings[4];
                WorldDatabase.SQLHost = WorldDBSettings[2];
                WorldDatabase.SQLPort = WorldDBSettings[3];
                WorldDatabase.SQLUser = WorldDBSettings[0];
                WorldDatabase.SQLPass = WorldDBSettings[1];
                WorldDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(
                    Enum.Parse(typeof(SQL.DB_Type), WorldDBSettings[5]));
            } else
            {
                Console.WriteLine("Invalid connect string for the world database!");
            }
            WorldServiceLocator.WSMaps.RESOLUTION_ZMAP = checked(configuration.MapResolution - 1);
            if(WorldServiceLocator.WSMaps.RESOLUTION_ZMAP < 63)
            {
                WorldServiceLocator.WSMaps.RESOLUTION_ZMAP = 63;
            }
            if(WorldServiceLocator.WSMaps.RESOLUTION_ZMAP > 255)
            {
                WorldServiceLocator.WSMaps.RESOLUTION_ZMAP = 255;
            }
            Log = BaseWriter.CreateLog(configuration.LogType, configuration.LogConfig);
            Log.LogLevel = LogType.NETWORK;
        } catch(Exception ex)
        {
            var e = ex;
            Console.WriteLine(e.ToString());
        }
    }

    [MTAThread]
    public async Task StartAsync()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"{((AssemblyProductAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), inherit: false)[0]).Product}");
        Console.WriteLine(
            ((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyCopyrightAttribute), inherit: false)[0]).Copyright);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  __  __      _  _  ___  ___  ___   __   __ ___               ");
        Console.WriteLine(" |  \\/  |__ _| \\| |/ __|/ _ \\/ __|  \\ \\ / /| _ )      We Love ");
        Console.WriteLine(" | |\\/| / _` | .` | (_ | (_) \\__ \\   \\ V / | _ \\   Vanilla Wow");
        Console.WriteLine(" |_|  |_\\__,_|_|\\_|\\___|\\___/|___/    \\_/  |___/              ");
        Console.WriteLine("                                                              ");
        Console.WriteLine(" Website / Forum / Support: https://getmangos.eu/          ");
        Console.WriteLine(string.Empty);
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(
            ((AssemblyTitleAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyTitleAttribute), inherit: false)[0]).Title);
        Console.WriteLine($" version {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(string.Empty);
        Console.ForegroundColor = ConsoleColor.Gray;
        var dateTimeStarted = DateTime.Now;
        Log.WriteLine(
            LogType.INFORMATION,
            "[{0}] World Server Starting...",
            Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"));
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += GenericExceptionHandlerAsync;
        LoadConfig();
        Console.ForegroundColor = ConsoleColor.Gray;
        AccountDatabase.SQLMessage += AccountSQLEventHandler;
        CharacterDatabase.SQLMessage += CharacterSQLEventHandler;
        WorldDatabase.SQLMessage += WorldSQLEventHandler;
        var ReturnValues = AccountDatabase.Connect();
        if(ReturnValues > 0)
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }
        AccountDatabase.Update("SET NAMES 'utf8';");
        ReturnValues = CharacterDatabase.Connect();
        if(ReturnValues > 0)
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }
        CharacterDatabase.Update("SET NAMES 'utf8';");
        ReturnValues = WorldDatabase.Connect();
        if(ReturnValues > 0)
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }
        WorldDatabase.Update("SET NAMES 'utf8';");
        var areDbVersionsOk = true;
        if(!WorldServiceLocator.CommonGlobalFunctions.CheckRequiredDbVersion(AccountDatabase, ServerDb.Realm))
        {
            areDbVersionsOk = false;
        }
        if(!WorldServiceLocator.CommonGlobalFunctions.CheckRequiredDbVersion(CharacterDatabase, ServerDb.Character))
        {
            areDbVersionsOk = false;
        }
        if(!WorldServiceLocator.CommonGlobalFunctions.CheckRequiredDbVersion(WorldDatabase, ServerDb.World))
        {
            areDbVersionsOk = false;
        }
        if(!areDbVersionsOk)
        {
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }
        await WorldServiceLocator.WSDBCDatabase.InitializeInternalDatabaseAsync();
        WorldServiceLocator.WSHandlers.IntializePacketHandlers();
        ALLQUESTS.LoadAllQuests();
        await AllGraveYards.InitializeGraveyardsAsync();
        WorldServiceLocator.WSTransports.LoadTransports();
        ClsWorldServer = new WS_Network.WorldServerClass(WorldServiceLocator.DataStoreProvider, cluster);
        var configuration = WorldServiceLocator.MangosConfiguration.World;
        ClsWorldServer.ClusterConnect();
        GC.Collect();
        if(Process.GetCurrentProcess().PriorityClass == ProcessPriorityClass.High)
        {
            Log.WriteLine(LogType.WARNING, "Setting Process Priority to HIGH..[done]");
        } else
        {
            Log.WriteLine(LogType.WARNING, "Setting Process Priority to NORMAL..[done]");
        }
        Log.WriteLine(
            LogType.INFORMATION,
            " Load Time:   {0}",
            Strings.Format(DateAndTime.DateDiff(DateInterval.Second, dateTimeStarted, DateAndTime.Now), "0 seconds"));
        Log.WriteLine(
            LogType.INFORMATION,
            " Used Memory: {0}",
            Strings.Format(GC.GetTotalMemory(forceFullCollection: false), "### ### ##0 bytes"));
    }

    public async void WaitConsoleCommandAsync()
    {
        var tmp = string.Empty;
        var cmd = Array.Empty<string>();
        while(!ClsWorldServer._flagStopListen)
        {
            try
            {
                tmp = Log.ReadLine();
                var CommandList = tmp.Split(";");
                var num = Information.LBound(CommandList);
                var num2 = Information.UBound(CommandList);
                for(var varList = num; varList <= num2; varList = checked(varList + 1))
                {
                    var cmds = Strings.Split(CommandList[varList], " ", 2);
                    if(CommandList[varList].Length > 0)
                    {
                        switch(cmds[0].ToLower())
                        {
                            case "restart":
                                Log.WriteLine(LogType.WARNING, "Server restarting...");
                                await StartAsync();
                                break;

                            case "shutdown":
                                Log.WriteLine(LogType.WARNING, "Server shutting down...");
                                ClsWorldServer._flagStopListen = true;
                                break;

                            case "info":
                                Log.WriteLine(
                                    LogType.INFORMATION,
                                    "Used memory: {0}",
                                    Strings.Format(
                                        GC.GetTotalMemory(forceFullCollection: false),
                                        "### ### ##0 bytes"));
                                break;

                            case "help":
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("'WorldServer' Command list:");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("---------------------------------");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'help' - Brings up the 'WorldServer' Command list (this).");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine(
                                    "'info' - Brings up a context menu showing server information (such as memory used).");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'shutdown' - Shuts down 'WorldServer'.");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'restart' - Restats the 'WorldServer'.");
                                break;

                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(
                                    "Error! Cannot find specified command. Please type 'help' for information on 'WorldServer' console commands.");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                break;
                        }
                    }
                }
            } catch(Exception ex)
            {
                var e = ex;
                Log.WriteLine(
                    LogType.FAILED,
                    "Error executing command [{0}]. {2}{1}",
                    Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"),
                    tmp,
                    e.ToString(),
                    Environment.NewLine);
            }
        }
    }

    public void WorldSQLEventHandler(SQL.EMessages MessageID, string OutBuf)
    {
        if(OutBuf is null)
        {
            throw new ArgumentNullException(nameof(OutBuf));
        }

        switch(MessageID)
        {
            case SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[WORLD] {OutBuf}");
                break;

            case SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[WORLD] {OutBuf}");
                break;

            default:
                break;
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
