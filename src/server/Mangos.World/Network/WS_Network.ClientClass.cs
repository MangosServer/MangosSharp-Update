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
using Mangos.World.Player;
using System;
using System.Collections.Concurrent;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Mangos.World.Network;

public partial class WS_Network
{
    public class ClientClass : ClientInfo, IDisposable
    {
        private readonly object _lockObj = new();
        private readonly object _lockUpdateObj = new();
        private readonly object _sempahoreLock = new();
        private volatile bool IsActive = true;
        private readonly object lockObj = new();
        private readonly ManualResetEvent ProcessQueueSempahore = new(false);
        private Thread ProcessQueueThread;
        public WS_PlayerData.CharacterObject Character;
        public bool DEBUG_CONNECTION;
        public ConcurrentQueue<Packets.Packets.PacketClass> Packets = new();

        public ClientClass(ClientInfo client, bool isDebug = false)
        {
            if(client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if(client is null)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "{0} is Null", client);
                return;
            }

            if(isDebug)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "Creating debug connection!", null);
                DEBUG_CONNECTION = true;
            }

            Access = client.Access;
            Account = (client?.Account);
            Index = client.Index;
            IP = (client?.IP);
            Port = client.Port;

            try
            {
                ProcessQueueThread = new Thread(QueueProcessor) { IsBackground = true };
                ProcessQueueThread?.Start();
            } catch(ArgumentNullException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.FAILED,
                "ArgumentNullException occured on New Thread: {0}",
                ex?.ToString());
            } catch(ThreadStartException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} Invalid Thread State - Thread ID: {1} Thread State: {2}",
                ex,
                Environment.CurrentManagedThreadId,
                Thread.CurrentThread.ThreadState);
            } catch(OutOfMemoryException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.FAILED,
                "OutOfMemoryException occured on ThreadStart: {0}",
                ex?.ToString());
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.FAILED,
                "Exception occured on New ProcessQueueThread: {0}",
                ex?.ToString());
            }
        }

        private void DumpPacket(Packets.Packets.PacketClass packet)
        {
            if(packet == null)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "Unable to dump packet");
                return;
            }

            try
            {
                var packets = WorldServiceLocator.Packets;
                var data = packet?.Data;
                var client = this;
                packets?.DumpPacket(data, client);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "Unable to dump packet", ex);
            }
        }

        private void QueueProcessor()
        {
            try
            {
                if(_sempahoreLock != null)
                {
                    while(IsActive)
                    {
                        if(Packets.IsEmpty)
                        {
                            ProcessQueueSempahore?.WaitOne();
                            //else

                            if(Packets == null)
                            {
                                //ProcessQueueSempahore?.Close();
                                WorldServiceLocator.WorldServer?.Log.WriteLine(
                                LogType.WARNING,
                                $"[{IP}:{Port}] Packet Count 0x{(int)(Packets?.Count):X2}]");
                            }

                            if (Packets.Count > 10000)
                            {
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(LogType.WARNING, "Packet Length is greater then 10000: [{0}]", Packets.Count);
                                return;
                            }

                            if (Packets.Count == (-1))
                            {
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(LogType.WARNING, "Packet Length is invalid [{0}]", Packets.Count);
                                return;
                            }

                            if (!IsActive)
                            {
                                break;
                            }

                            lock(_sempahoreLock)
                            {
                                ProcessQueueSempahore?.Reset();
                            }
                        }

                        while(Packets.TryDequeue(out var packet))
                        {
                            var p = packet;
                            using(p = new Packets.Packets.PacketClass(packet.OpCode))
                            {
                                if(p != null)
                                {
                                    if(!WorldServiceLocator.WorldServer.PacketHandlers.ContainsKey(packet.OpCode))
                                    {
                                        WorldServiceLocator.WorldServer?.Log.WriteLine(
                                        LogType.WARNING,
                                        $"[{IP}:{Port}] Unknown Opcode 0x{(int)(packet?.OpCode):X2} [DataLen={packet?.Data.Length} {packet?.OpCode}]");
                                        DumpPacket(packet);
                                    } else
                                    {
                                        var start = WorldServiceLocator.NativeMethods.timeGetTime(string.Empty);
                                        checked
                                        {
                                            try
                                            {
                                                var handlePacket = WorldServiceLocator.WorldServer.PacketHandlers[
                                                    packet.OpCode];
                                                var client = this;
                                                handlePacket(ref packet, ref client);
                                                if((handlePacket != null) || (client != null))
                                                {
                                                    WorldServiceLocator.WorldServer?.Log.WriteLine(
                                                    LogType.WARNING,
                                                    $"DEBUG OpCode: [0x{(int)(packet?.OpCode):X2}] [{packet?.OpCode} > DataLen={packet?.Data.Length} > Offset={packet?.Offset}]");
                                                    DumpPacket(packet);
                                                }
                                                if (client != null && (WorldServiceLocator.NativeMethods.timeGetTime(string.Empty) -
                                                            start) >
                                                        100)
                                                {
                                                    WorldServiceLocator.WorldServer?.Log.WriteLine(
                                                    LogType.WARNING,
                                                    "Packet processing took too long: {0}, {1}ms",
                                                    packet?.OpCode,
                                                    WorldServiceLocator.NativeMethods.timeGetTime(string.Empty) -
                                                        start);
                                                }
                                            } catch(Exception ex3)
                                            {
                                                DumpPacket(packet);
                                                SetError(
                                                    ex3,
                                                    $"Opcode handler {packet?.OpCode}:{packet?.OpCode} caused an error: {ex3?.Message}{Environment.NewLine}",
                                                    LogType.FAILED);
                                                SetError(
                                                    ex3,
                                                    $"Connection from [{IP}:{Port}] cause error {ex3?.Message}{Environment.NewLine}",
                                                    LogType.FAILED);
                                                Delete();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                } else
                {
                    WorldServiceLocator.WorldServer?.Log.WriteLine(
                    LogType.WARNING,
                    "SemaphoreLock was Null {0}",
                    _sempahoreLock);
                }
            } catch(ThreadInterruptedException ex) //Disposing
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} ThreadInterupptedException Thread ID: {1}",
                ex,
                Environment.CurrentManagedThreadId);
            } catch(InvalidOperationException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} InvalidOperationException Thread ID: {1}",
                ex,
                Environment.CurrentManagedThreadId);
            } catch(AbandonedMutexException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} AbandonedMutexException Thread ID: {1}",
                ex,
                Environment.CurrentManagedThreadId);
            } catch(Exception ex)
            {
                SetError(
                    ex,
                    $"Connection from [{IP}:{Port}] cause error {ex?.Message}{Environment.NewLine}",
                    LogType.FAILED);
                Delete();
            }
        }

        private void SetError(Exception ex, string message, LogType logType)
        { WorldServiceLocator.WorldServer.Log.WriteLine(logType, message, ex); }

        public void Delete()
        {
            try
            {
                Dispose();
            } catch(Exception ex)
            {
                SetError(ex, string.Empty, LogType.FAILED);
            }
        }

        public void Disconnect() { Delete(); }

        public void Dispose()
        {
            WorldServiceLocator.WorldServer?.Log.WriteLine(
            LogType.NETWORK,
            $"Connection from [{IP}:{Port}] disposed.");
            IsActive = false;
            try
            {
                ProcessQueueSempahore?.Dispose();
            } catch(ObjectDisposedException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} ObjectDisposedException on Dispose",
                ex);
                return;
            }

            try
            {
                if(IsActive)
                {
                    ProcessQueueThread?.Interrupt();
                    ProcessQueueThread?.Join(1000);
                } else
                {
                    return;
                }
            } catch(ThreadStateException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} Invalid Thread State - Thread ID: {1} Thread State: {2}",
                ex,
                Environment.CurrentManagedThreadId,
                Thread.CurrentThread.ThreadState);
                //return;
            } catch(ThreadInterruptedException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} Thread ID: {1}",
                ex,
                Environment.CurrentManagedThreadId);
            }

            ProcessQueueThread = null;

            if(ProcessQueueSempahore != null)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "ProcessQueueSemaphore Null!: {1}",
                Environment.CurrentManagedThreadId);
            }

            if((ProcessQueueThread?.IsAlive) == true)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "ProcessQueueThread Null!: {1}",
                Environment.CurrentManagedThreadId);
            }

            if(!IsActive)
            {
                return;
            }

            Packets?.Clear();

            try
            {
                if(!IsActive)
                {
                    return;
                }
                WorldServiceLocator.WorldServer.CLIENTs.Remove(Index);
                WorldServiceLocator.WorldServer.ClsWorldServer.Cluster?.ClientDrop(Index);
                WorldServiceLocator.WorldServer.CLIENTs.Clear();
                if(Character != null)
                {
                    Character.client = null;
                    Character.client.Disconnect();
                    Character.client.Dispose();
                    Character?.Dispose();
                    Character = null;
                }
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.FAILED,
                $"Connection from [{IP}:{Port}] was not properly disposed.",
                ex);
            }
        }

        public void PushPacket(Packets.Packets.PacketClass packet)
        {
            if (packet is null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            try
            {
                Packets?.Enqueue(packet);
                if ((packet is null) || (Character is null) || (Character.client is null))
                {
                    WorldServiceLocator.WorldServer?.Log.WriteLine(
                    LogType.WARNING,
                    "Enqueued Packet [{0}] Length [{1}] Offset [{2}] is Null",
                    packet.OpCode,
                    packet.Length,
                    packet.Offset);
                    DumpPacket(packet);
                    return;
                }

                if (packet.Length > 256)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(LogType.WARNING, "Enqueued Packet Length is greater then 256: [{0}]", packet.Length);
                    return;
                }

                if (packet.Length == 0)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(LogType.WARNING, "Enqueued Packet Length is zero [{0}]", packet.Length);
                    return;
                }

                if (packet.Length == (-1))
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(LogType.WARNING, "Enqueued Packet Length is invalid [{0}]", packet.Length);
                    return;
                }
                if (_sempahoreLock != null)
                {
                    lock (_sempahoreLock)
                    {
                        if (ProcessQueueSempahore != null)
                        {
                            ProcessQueueSempahore?.Set();
                        }
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.WARNING,
                "{0} ObjectDisposedException on PushPacket",
                ex);
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(
                LogType.FAILED,
                "Error on PushPacket: {0}",
                ex?.ToString());
            }
        }

        public void Send(ref byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length > 256)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Data Length is greater then 256: [{0}]", data.Length);
                return;
            }

            if (data.Length == 0)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Data Length is zero [{0}]", data.Length);
                return;
            }

            if (data.Length == (-1))
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Data Length is invalid [{0}]", data.Length);
                return;
            }
            if (lockObj != null)
            {
                lock (lockObj)
                {
                    try
                    {
                        WorldServiceLocator.WorldServer.ClsWorldServer.Cluster?.ClientSend(Index, data);
                    }
                    catch (Exception ex)
                    {
                        SetError(
                            ex,
                            $"Connection from [{IP}:{Port}] cause error {ex?.Message}{Environment.NewLine}",
                            LogType.CRITICAL);

                        if (DEBUG_CONNECTION)
                        {
                            return;
                        }

                        WorldServiceLocator.WorldServer.ClsWorldServer.Cluster = null;
                        Delete();
                    }
                }
            }
        }

        public void Send(ref Packets.Packets.PacketClass packet)
        {
            if (packet is null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet.Length > 750000)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Send Packet Length is greater then 750000: [{0}]", packet.Length);
                return;
            }

            if (packet.Length < 0)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Send Packet Length less then zero [{0}]", packet.Length);
                return;
            }

            if (packet.Length == (-1))
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Send Packet Length is invalid [{0}]", packet.Length);
                return;
            }

            if (_lockUpdateObj != null)
            {
                lock (_lockUpdateObj)
                {
                    try
                    {
                        using (packet)
                        {
                            if ((packet?.OpCode) == Opcodes.SMSG_UPDATE_OBJECT)
                            {
                                packet?.CompressUpdatePacket();
                            }
                            packet?.UpdateLength();

                            WorldServiceLocator.WorldServer.ClsWorldServer.Cluster?.ClientSend(Index, packet?.Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetError(
                            ex,
                            $"Connection from [{IP}:{Port}] cause error {ex?.Message}{Environment.NewLine}",
                            LogType.CRITICAL);

                        if (DEBUG_CONNECTION)
                        {
                            return;
                        }

                        WorldServiceLocator.WorldServer.ClsWorldServer.Cluster = null;
                        Delete();
                    }
                }
            }
        }

        public void SendMultiplyPackets(ref Packets.Packets.PacketClass packet)
        {
            if (packet is null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet is null)
            {
                WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "Packet was Null {0}", packet);
                return;
            }

            if (packet.Length > 256)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Miltiply Packets Length is greater then 256: [{0}]", packet.Length);
                return;
            }

            if (packet.Length < 0)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Multiply Packets Length is less then zero [{0}]", packet.Length);
                return;
            }

            if (packet.Length == (-1))
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Mulitply Packets Length is invalid [{0}]", packet.Length);
                return;
            }

            if (_lockObj != null)
            {
                lock (_lockObj)
                {
                    try
                    {
                        if ((packet?.OpCode) == Opcodes.SMSG_UPDATE_OBJECT)
                        {
                            packet?.CompressUpdatePacket();
                        }
                        packet?.UpdateLength();
                        var data = (packet?.Data.Clone()) as byte[];
                        //if (data != null)
                        //{
                        //    WorldServiceLocator.WorldServer?.Log.WriteLine(LogType.WARNING, "Data was Null {0}", data);
                        //    return;
                        //}

                        WorldServiceLocator.WorldServer.ClsWorldServer.Cluster?.ClientSend(Index, data);
                    }
                    catch (Exception ex)
                    {
                        SetError(
                            ex,
                            $"Connection from [{IP}:{Port}] cause error {ex?.Message}{Environment.NewLine}",
                            LogType.CRITICAL);

                        if (DEBUG_CONNECTION)
                        {
                            return;
                        }

                        WorldServiceLocator.WorldServer.ClsWorldServer.Cluster = null;
                        Delete();
                    }
                }
            }
        }
    }
}
