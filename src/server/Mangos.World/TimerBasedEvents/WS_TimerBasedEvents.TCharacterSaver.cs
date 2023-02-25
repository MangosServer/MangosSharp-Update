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
using System;
using System.Threading;

namespace Mangos.World.TimerBasedEvents;

public partial class WS_TimerBasedEvents
{
    public class TCharacterSaver : IDisposable
    {
        private bool _disposedValue;
        private bool CharacterSaverWorking;
        public Timer CharacterSaverTimer;

        public int UPDATE_TIMER;

        public TCharacterSaver()
        {
            CharacterSaverTimer = null;
            CharacterSaverWorking = false;
            UPDATE_TIMER = WorldServiceLocator.MangosConfiguration.World.SaveTimer;
            CharacterSaverTimer = new Timer(Update, null, 10000, UPDATE_TIMER);
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        private void Update(object state)
        {
            if(CharacterSaverWorking)
            {
                WorldServiceLocator.WorldServer.Log
                    .WriteLine(LogType.WARNING, "Update: Character Saver skipping update");
                return;
            }
            CharacterSaverWorking = true;
            try
            {
                WorldServiceLocator.WorldServer.CHARACTERs_Lock
                    .AcquireReaderLock(WorldServiceLocator.GlobalConstants.DEFAULT_LOCK_TIMEOUT);
                foreach(var cHARACTER in WorldServiceLocator.WorldServer.CHARACTERs)
                {
                    cHARACTER.Value.SaveCharacter();
                }
            } catch(Exception ex)
            {
                WorldServiceLocator.WorldServer.Log.WriteLine(LogType.FAILED, ex.ToString(), null);
            } finally
            {
                WorldServiceLocator.WorldServer.CHARACTERs_Lock.ReleaseReaderLock();
            }
            WorldServiceLocator.WSHandlersInstance.InstanceMapUpdate();
            CharacterSaverWorking = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!_disposedValue)
            {
                CharacterSaverTimer.Dispose();
                CharacterSaverTimer = null;
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
