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
using Mangos.Common.Enums.Map;
using Mangos.DataStores;
using Microsoft.VisualBasic;
using System;
using System.IO;

namespace Mangos.World.Maps;

public partial class WS_Maps
{
    public class TMap : IDisposable
    {
        private bool _disposedValue;
        public int ID;

        public string Name;

        public TMapTile[,] Tiles;

        public bool[,] TileUsed;

        public MapTypes Type;

        public TMap(int Map, DataStore mapDataStore)
        {
            if(mapDataStore is null)
            {
                throw new ArgumentNullException(nameof(mapDataStore));
            }

            Type = MapTypes.MAP_COMMON;
            Name = string.Empty;
            TileUsed = (new bool[64, 64]);
            Tiles = (new TMapTile[64, 64]);
            checked
            {
                if(WorldServiceLocator.WSMaps.Maps.ContainsKey((uint)Map))
                {
                    return;
                }
                WorldServiceLocator.WSMaps.Maps.Add((uint)Map, this);
                var x = 0;
                do
                {
                    var y = 0;
                    do
                    {
                        TileUsed[x, y] = false;
                        y++;
                    } while (y <= 63);
                    x++;
                } while (x <= 63);
                try
                {
                    for(var i = 0; i <= (mapDataStore.Rows - 1); i++)
                    {
                        if(mapDataStore.ReadInt(i, 0) == Map)
                        {
                            ID = Map;
                            Type = (MapTypes)mapDataStore.ReadInt(i, 2);
                            Name = mapDataStore.ReadString(i, 4);
                            break;
                        }
                    }
                    WorldServiceLocator.WorldServer.Log
                        .WriteLine(LogType.INFORMATION, "DBC: 1 Map initialized.", mapDataStore.Rows - 1);
                } catch(DirectoryNotFoundException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("DBC File : Map missing.", ex);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        void IDisposable.Dispose()
        {
            //ILSpy generated this explicit interface implementation from .override directive in Dispose
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            checked
            {
                if(!_disposedValue)
                {
                    var i = 0;
                    do
                    {
                        var j = 0;
                        do
                        {
                            Tiles[i, j]?.Dispose();
                            j++;
                        } while (j <= 63);
                        i++;
                    } while (i <= 63);
                    WorldServiceLocator.WSMaps.Maps.Remove((uint)ID);
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool IsBattleGround => Type == MapTypes.MAP_BATTLEGROUND;

        public bool IsDungeon => Type is MapTypes.MAP_INSTANCE or MapTypes.MAP_RAID;

        public bool IsRaid => Type == MapTypes.MAP_RAID;

        public int ResetTime
        {
            get
            {
                checked
                {
                    switch(Type)
                    {
                        case MapTypes.MAP_BATTLEGROUND:
                            return WorldServiceLocator.GlobalConstants.DEFAULT_BATTLEFIELD_EXPIRE_TIME;

                        case MapTypes.MAP_INSTANCE:
                        case MapTypes.MAP_RAID:
                            switch(ID)
                            {
                                case 249:
                                    return (int)Math.Round(
                                        WorldServiceLocator.Functions.GetNextDate(5, 3).Subtract(DateAndTime.Now)
                                            .TotalSeconds);

                                case 309:
                                case 509:
                                    return (int)Math.Round(
                                        WorldServiceLocator.Functions.GetNextDate(3, 3).Subtract(DateAndTime.Now)
                                            .TotalSeconds);

                                case 409:
                                case 469:
                                case 531:
                                case 533:
                                    return (int)Math.Round(
                                        WorldServiceLocator.Functions
                                            .GetNextDay(DayOfWeek.Tuesday, 3)
                                            .Subtract(DateAndTime.Now)
                                            .TotalSeconds);
                            }
                            break;

                        default:
                            break;
                    }
                    return WorldServiceLocator.GlobalConstants.DEFAULT_INSTANCE_EXPIRE_TIME;
                }
            }
        }
    }
}
