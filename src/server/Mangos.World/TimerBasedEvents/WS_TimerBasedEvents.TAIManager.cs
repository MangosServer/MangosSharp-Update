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
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Threading;

namespace Mangos.World.TimerBasedEvents;

public partial class WS_TimerBasedEvents
{
    public class TAIManager : IDisposable
    {
        public const int UPDATE_TIMER = 1000;
        private bool _disposedValue;
        private bool AIManagerWorking;
        public Timer AIManagerTimer;

        public TAIManager()
        {
            AIManagerTimer = null;
            AIManagerWorking = false;
            AIManagerTimer = new Timer(Update, null, 10000, 1000);
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        private void Update(object state)
        {
            if(state is not null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if(AIManagerWorking)
            {
                return;
            }
            var StartTime = WorldServiceLocator.NativeMethods.timeGetTime(string.Empty);
            AIManagerWorking = true;
            try
            {
                WorldServiceLocator.WorldServer.WORLD_TRANSPORTs_Lock
                    .AcquireReaderLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                foreach(var wORLD_TRANSPORT in WorldServiceLocator.WorldServer.WORLD_TRANSPORTs)
                {
                    wORLD_TRANSPORT.Value.Update();
                }
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(
                        LogType.CRITICAL,
                        "Error updating transports.{0}{1}",
                        Environment.NewLine,
                        ex.ToString());
            } finally
            {
                WorldServiceLocator.WorldServer.WORLD_TRANSPORTs_Lock.ReleaseReaderLock();
            }
            checked
            {
                try
                {
                    WorldServiceLocator.WorldServer.WORLD_CREATUREs_Lock
                        .AcquireReaderLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    try
                    {
                        long num = WorldServiceLocator.WorldServer.WORLD_CREATUREsKeys.Count - 1;
                        for(var i = 0L; i <= num; i++)
                        {
                            if((WorldServiceLocator.WorldServer.WORLD_CREATUREs[
                                    Conversions.ToULong(WorldServiceLocator.WorldServer.WORLD_CREATUREsKeys[(int)i])]?.aiScript) !=
                                null)
                            {
                                WorldServiceLocator.WorldServer.WORLD_CREATUREs[
                                    Conversions.ToULong(WorldServiceLocator.WorldServer.WORLD_CREATUREsKeys[(int)i])].aiScript
                                    .DoThink();
                            }
                        }
                    } catch(Exception ex)
                    {
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.CRITICAL,
                                "Error updating AI.{0}{1}",
                                Environment.NewLine,
                                ex.ToString());
                    } finally
                    {
                        WorldServiceLocator.WorldServer.WORLD_CREATUREs_Lock.ReleaseReaderLock();
                    }
                } catch(Exception ex)
                {
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(
                            LogType.CRITICAL,
                            "Error Acquring ReaderLock.{0}{1}",
                            Environment.NewLine,
                            ex.ToString());
                }
                AIManagerWorking = false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!_disposedValue)
            {
                AIManagerTimer.Dispose();
                AIManagerTimer = null;
            }
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
