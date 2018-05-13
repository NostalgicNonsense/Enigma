using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Barrack.Stats
{
    public class BuildingStatsVehicleFactory : BuildingStats
    {
        public BuildingStatsVehicleFactory()
            : base("Factory", "F", "The factory produces all vehicles for the faction.", 400, 400, 0)
        {

        }
    }
}
