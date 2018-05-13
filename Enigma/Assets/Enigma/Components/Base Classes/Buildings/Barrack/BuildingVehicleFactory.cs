using Assets.Enigma.Components.Base_Classes.Buildings.Barrack.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingVehicleFactory : Building
    {
        void Start()
        {
            BuildingStats = new BuildingStatsVehicleFactory();
        }
    }
}
