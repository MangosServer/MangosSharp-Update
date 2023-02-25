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
using Mangos.World.Objects;
using System;

namespace Mangos.World.AI;

public partial class WS_Creatures_AI
{
    public class WaypointAI : DefaultAI
    {
        public int CurrentWaypoint;

        public WaypointAI(ref WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            if(Creature is null)
            {
                throw new ArgumentNullException(nameof(Creature));
            }

            CurrentWaypoint = -1;
            IsWaypoint = true;
        }

        public override void DoMove()
        {
            var distanceToSpawn = WorldServiceLocator.WSCombat
                .GetDistance(
                    aiCreature.positionX,
                    aiCreature.SpawnX,
                    aiCreature.positionY,
                    aiCreature.SpawnY,
                    aiCreature.positionZ,
                    aiCreature.SpawnZ);
            checked
            {
                switch(aiTarget)
                {
                    case null:
                        if(WorldServiceLocator.WSDBCDatabase.CreatureMovement.ContainsKey(aiCreature.WaypointID))
                        {
                            try
                            {
                                CurrentWaypoint++;
                                if(!WorldServiceLocator.WSDBCDatabase.CreatureMovement[aiCreature.WaypointID].ContainsKey(
                                    CurrentWaypoint))
                                {
                                    CurrentWaypoint = 1;
                                }
                                var MovementPoint = WorldServiceLocator.WSDBCDatabase.CreatureMovement[
                                    aiCreature.WaypointID][CurrentWaypoint];
                                aiTimer = aiCreature.MoveTo(MovementPoint.x, MovementPoint.y, MovementPoint.z) +
                                    MovementPoint.waitTime;
                            } catch(Exception ex)
                            {
                                WorldServiceLocator.WorldServer.Log
                                    .WriteLine(
                                        LogType.CRITICAL,
                                        "Creature [{0:X}] waypoints are damaged.",
                                        ex,
                                        (aiCreature?.GUID) - WorldServiceLocator.GlobalConstants.GUID_UNIT);
                                aiCreature.ResetAI();
                            }

                            break;
                        }
                        WorldServiceLocator.WorldServer.Log
                            .WriteLine(
                                LogType.CRITICAL,
                                "Creature [{0:X}] is missing waypoints.",
                                (aiCreature?.GUID) - WorldServiceLocator.GlobalConstants.GUID_UNIT);
                        aiCreature.ResetAI();
                        return;

                    default:
                        base.DoMove();
                        break;
                }
            }
        }

        public override void Pause(int Time)
        {
            checked
            {
                CurrentWaypoint--;
                aiTimer = Time;
            }
        }
    }
}
