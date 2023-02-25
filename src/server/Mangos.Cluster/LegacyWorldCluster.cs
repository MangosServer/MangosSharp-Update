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

using Mangos.Cluster.Globals;
using Mangos.Cluster.Handlers;
using Mangos.Cluster.Network;
using Mangos.Common.Enums.Global;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Logging;
using Mangos.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mangos.Cluster;

public class LegacyWorldCluster : IDisposable
{
    private readonly SQL _accountDatabase = new();
    private readonly SQL _characterDatabase = new();
    private readonly ClusterServiceLocator _clusterServiceLocator;
    private readonly Dictionary<Opcodes, HandlePacket> _packetHandlers = new();
    private readonly SQL _worldDatabase = new();
    private readonly MangosConfiguration mangosConfiguration;
    public Dictionary<ulong, WcHandlerCharacter.CharacterObject> CharacteRs = new();
    public ReaderWriterLock CharacteRsLock = new();

    public Dictionary<uint, ClientClass> ClienTs = new();

    // Players' containers
    public long ClietniDs;
    // Public CHARACTER_NAMEs As New Hashtable

    // System Things...
    public BaseWriter Log = new ColoredConsoleWriter();

    public Random Rnd = new();

    public LegacyWorldCluster(ClusterServiceLocator clusterServiceLocator, MangosConfiguration mangosConfiguration)
    {
        _clusterServiceLocator = clusterServiceLocator ?? throw new ArgumentNullException(nameof(clusterServiceLocator));
        this.mangosConfiguration = mangosConfiguration ?? throw new ArgumentNullException(nameof(mangosConfiguration));
    }

    public delegate void HandlePacket(PacketClass packet, ClientClass client);

    public void AccountSqlEventHandler(SQL.EMessages messageId, string outBuf)
    {
        switch(messageId)
        {
            case var @case when @case == SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[ACCOUNT] {outBuf}");
                break;

            case var case1 when case1 == SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[ACCOUNT] {outBuf}");
                break;
        }
    }

    public void CharacterSqlEventHandler(SQL.EMessages messageId, string outBuf)
    {
        switch(messageId)
        {
            case var @case when @case == SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[CHARACTER] {outBuf}");
                break;

            case var case1 when case1 == SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[CHARACTER] {outBuf}");
                break;
        }
    }

    public void Dispose() { throw new NotImplementedException(); }

    public SQL GetAccountDatabase() { return _accountDatabase; }

    public SQL GetCharacterDatabase() { return _characterDatabase; }

    public Dictionary<Opcodes, HandlePacket> GetPacketHandlers() { return _packetHandlers; }

    public SQL GetWorldDatabase() { return _worldDatabase; }

    public void LoadConfig()
    {
        // DONE: Setting SQL Connections
        var accountDbSettings = Strings.Split(mangosConfiguration.Cluster.AccountDatabase, ";");
        if(accountDbSettings.Length != 6)
        {
            Console.WriteLine("Invalid connect string for the account database!");
        } else
        {
            GetAccountDatabase().SQLDBName = accountDbSettings[4];
            GetAccountDatabase().SQLHost = accountDbSettings[2];
            GetAccountDatabase().SQLPort = accountDbSettings[3];
            GetAccountDatabase().SQLUser = accountDbSettings[0];
            GetAccountDatabase().SQLPass = accountDbSettings[1];
            GetAccountDatabase().SQLTypeServer = (SQL.DB_Type)Enum.Parse(typeof(SQL.DB_Type), accountDbSettings[5]);
        }

        var characterDbSettings = Strings.Split(mangosConfiguration.Cluster.CharacterDatabase, ";");
        if(characterDbSettings.Length != 6)
        {
            Console.WriteLine("Invalid connect string for the character database!");
        } else
        {
            GetCharacterDatabase().SQLDBName = characterDbSettings[4];
            GetCharacterDatabase().SQLHost = characterDbSettings[2];
            GetCharacterDatabase().SQLPort = characterDbSettings[3];
            GetCharacterDatabase().SQLUser = characterDbSettings[0];
            GetCharacterDatabase().SQLPass = characterDbSettings[1];
            GetCharacterDatabase().SQLTypeServer = (SQL.DB_Type)Enum.Parse(
                typeof(SQL.DB_Type),
                characterDbSettings[5]);
        }

        var worldDbSettings = Strings.Split(mangosConfiguration.Cluster.WorldDatabase, ";");
        if(worldDbSettings.Length != 6)
        {
            Console.WriteLine("Invalid connect string for the world database!");
        } else
        {
            GetWorldDatabase().SQLDBName = worldDbSettings[4];
            GetWorldDatabase().SQLHost = worldDbSettings[2];
            GetWorldDatabase().SQLPort = worldDbSettings[3];
            GetWorldDatabase().SQLUser = worldDbSettings[0];
            GetWorldDatabase().SQLPass = worldDbSettings[1];
            GetWorldDatabase().SQLTypeServer = (SQL.DB_Type)Enum.Parse(typeof(SQL.DB_Type), worldDbSettings[5]);
        }
    }

    public async Task StartAsync()
    {
        LoadConfig();
        GetAccountDatabase().SQLMessage += AccountSqlEventHandler;
        GetCharacterDatabase().SQLMessage += CharacterSqlEventHandler;
        GetWorldDatabase().SQLMessage += WorldSqlEventHandler;
        var returnValues = GetAccountDatabase().Connect();
        if(returnValues > ((int)SQL.ReturnState.Success))   // Ok, An error occurred
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
            Environment.Exit(0);
        }

        GetAccountDatabase().Update("SET NAMES 'utf8';");
        returnValues = GetCharacterDatabase().Connect();
        if(returnValues > ((int)SQL.ReturnState.Success))   // Ok, An error occurred
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
            Environment.Exit(0);
        }

        GetCharacterDatabase().Update("SET NAMES 'utf8';");
        returnValues = GetWorldDatabase().Connect();
        if(returnValues > ((int)SQL.ReturnState.Success))   // Ok, An error occurred
        {
            Console.WriteLine($"[{Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss")}] An SQL Error has occurred");
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Environment.Exit(0);
        }

        GetWorldDatabase().Update("SET NAMES 'utf8';");
        await _clusterServiceLocator.WsDbcLoad.InitializeInternalDatabaseAsync();
        _clusterServiceLocator.WcHandlers.InitializePacketHandlers();
        if(!_clusterServiceLocator.CommonGlobalFunctions
                    .CheckRequiredDbVersion(GetAccountDatabase(), ServerDb.Realm))         // Check the Database version, exit if its wrong
        {
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Environment.Exit(0);
        }

        if(!_clusterServiceLocator.CommonGlobalFunctions
                    .CheckRequiredDbVersion(GetCharacterDatabase(), ServerDb.Character))         // Check the Database version, exit if its wrong
        {
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Environment.Exit(0);
        }

        if(!_clusterServiceLocator.CommonGlobalFunctions.CheckRequiredDbVersion(GetWorldDatabase(), ServerDb.World))         // Check the Database version, exit if its wrong
        {
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Environment.Exit(0);
        }

        _clusterServiceLocator.WorldServerClass.Start();

        Log.WriteLine(
            LogType.INFORMATION,
            "Load Time: {0}",
            Strings.Format(DateAndTime.DateDiff(DateInterval.Second, DateAndTime.Now, DateAndTime.Now), "0 seconds"));
        Log.WriteLine(
            LogType.INFORMATION,
            "Used memory: {0}",
            Strings.Format(GC.GetTotalMemory(false), "### ### ##0 bytes"));
    }

    public void WaitConsoleCommand()
    {
        var tmp = string.Empty;
        string[] commandList;
        string[] cmds;
        var cmd = Array.Empty<string>();
        int varList;
        while(!_clusterServiceLocator.WcNetwork.WorldServer.MFlagStopListen)
        {
            try
            {
                tmp = Log.ReadLine();
                commandList = tmp.Split(";");
                var loopTo = Information.UBound(commandList);
                for(varList = Information.LBound(commandList); varList <= loopTo; varList++)
                {
                    cmds = Strings.Split(commandList[varList], " ", 2);
                    if(commandList[varList].Length > 0)
                    {
                        // <<<<<<<<<<<COMMAND STRUCTURE>>>>>>>>>>
                        switch(cmds[0].ToLower() ?? string.Empty)
                        {
                            case "shutdown":
                                Log.WriteLine(LogType.WARNING, "Server shutting down...");
                                _clusterServiceLocator.WcNetwork.WorldServer.MFlagStopListen = true;
                                break;

                            case "info":
                                Log.WriteLine(
                                    LogType.INFORMATION,
                                    "Used memory: {0}",
                                    Strings.Format(GC.GetTotalMemory(false), "### ### ##0 bytes"));
                                break;

                            case "help":
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("'WorldCluster' Command list:");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("---------------------------------");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'help' - Brings up the 'WorldCluster' Command list (this).");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'info' - Displays used memory.");
                                Console.WriteLine(string.Empty);
                                Console.WriteLine("'shutdown' - Shuts down WorldCluster.");
                                break;

                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(
                                    "Error! Cannot find specified command. Please type 'help' for information on console for commands.");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                break;
                        }
                        // <<<<<<<<<<</END COMMAND STRUCTURE>>>>>>>>>>>>
                    }
                }
            } catch(Exception e)
            {
                Log.WriteLine(
                    LogType.FAILED,
                    "Error executing command [{0}]. {2}{1}",
                    Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"),
                    tmp,
                    e.ToString(),
                    Constants.vbCrLf);
            }
        }
    }

    public void WorldSqlEventHandler(SQL.EMessages messageId, string outBuf)
    {
        switch(messageId)
        {
            case var @case when @case == SQL.EMessages.ID_Error:
                Log.WriteLine(LogType.FAILED, $"[WORLD] {outBuf}");
                break;

            case var case1 when case1 == SQL.EMessages.ID_Message:
                Log.WriteLine(LogType.SUCCESS, $"[WORLD] {outBuf}");
                break;
        }
    }
}
