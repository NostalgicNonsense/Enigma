using Assets.Enigma.Components.Base_Classes.Buildings.Barrack.Stats;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingBarracks : Building
    {
        void Start()
        {
            Debug.Log("BARRACK STARTED");
            
        }
        public override void Init()
        {
            BuildingStats = new BuildingStatsBarrack();

            base.Init();
        }
    }
}
