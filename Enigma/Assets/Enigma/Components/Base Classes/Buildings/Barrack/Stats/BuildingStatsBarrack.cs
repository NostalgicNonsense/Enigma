using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Barrack.Stats
{
    public class BuildingStatsBarrack : BuildingStats
    {
        public BuildingStatsBarrack()
            : base("Barrack", "B", "The barrack acts as a spawn point for the faction.", 300, 200, 0)
        {

        }
    }
}
