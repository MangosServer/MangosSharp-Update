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

using Mangos.World.Objects;
using Mangos.World.Player;
using System;
using System.Threading;

namespace Mangos.World.AI;

public partial class WS_Creatures_AI
{
    public class BossAI : DefaultAI
    {
        public BossAI(ref WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            if(Creature is null)
            {
                throw new ArgumentNullException(nameof(Creature));
            }
        }

        public override void DoThink()
        {
            base.DoThink();
            new Thread(OnThink) { Name = "Boss Thinking" }.Start();
        }

        public override void OnEnterCombat()
        {
            base.OnEnterCombat();
            foreach(var Unit in aiHateTable)
            {
                if(Unit.Key is not WS_PlayerData.CharacterObject)
                {
                    continue;
                }
                var characterObject = (WS_PlayerData.CharacterObject)Unit.Key;
                if(characterObject.IsInGroup)
                {
                    foreach(var member in characterObject.Group.LocalMembers.ToArray())
                    {
                        if(WorldServiceLocator.WorldServer.CHARACTERs.ContainsKey(member) &&
                            (WorldServiceLocator.WorldServer.CHARACTERs[member].MapID == characterObject.MapID) &&
                            (WorldServiceLocator.WorldServer.CHARACTERs[member].instance == characterObject.instance))
                        {
                            aiHateTable.Add(WorldServiceLocator.WorldServer.CHARACTERs[member], 0);
                        }
                    }
                    break;
                }
            }
        }

        public virtual void OnThink()
        {
        }
    }
}
