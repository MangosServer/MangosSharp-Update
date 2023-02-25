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

using System;

namespace Mangos.World.Loots;

public partial class WS_Loot
{
    public class TLock
    {
        public int[] Keys;
        public byte[] KeyType;

        public short RequiredLockingSkill;

        public short RequiredMiningSkill;

        public TLock(byte[] KeyType_, int[] Keys_, short ReqMining, short ReqLock)
        {
            if(KeyType_ is null)
            {
                throw new ArgumentNullException(nameof(KeyType_));
            }

            if(Keys_ is null)
            {
                throw new ArgumentNullException(nameof(Keys_));
            }

            KeyType = (new byte[5]);
            Keys = (new int[5]);
            byte i = 0;
            do
            {
                KeyType[i] = KeyType_[i];
                Keys[i] = Keys_[i];
                checked
                {
                    i = (byte)unchecked((uint)(i + 1));
                }
            } while (i <= 4u);
            RequiredMiningSkill = ReqMining;
            RequiredLockingSkill = ReqLock;
        }
    }
}
