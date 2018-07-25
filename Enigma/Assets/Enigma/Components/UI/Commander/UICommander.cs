using Assets.Enigma.Components.UI.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Enigma.Components.Base_Classes.Commander;
using Assets.Enigma.Components.UI.Commander;
using Assets.Enigma.Components.Base_Classes.TeamSettings;
using Assets.Enigma.Components.Base_Classes.TeamSettings.Resources;

namespace Assets.Enigma.Components.UI
{
    public class UICommander : MonoBehaviour
    {
        public Tooltip Tooltip { get; private set; }
        public BuildingPlacement BuildingPlacement { get; private set; }
        public ResourceTeams ResourceManager;

        void Start()
        {
            BuildingPlacement = GetComponentInChildren<BuildingPlacement>();
            Tooltip = GetComponentInChildren<Tooltip>();
        }

        void Update()
        {

        }
    }
}
