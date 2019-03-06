﻿using Enigma.Components.Gameplay.Command;
using Enigma.Components.Gameplay.TeamSettings.Resources;
using UnityEngine;

namespace Enigma.Components.UI.Commander
{
    public class UICommander : MonoBehaviour
    {
        public Tooltip Tooltip { get; private set; }
        public BuildingPlacement BuildingPlacement { get; private set; }

        public ResourceTeams ResourceManager { get; private set; }

        void Start()
        {
            BuildingPlacement = GetComponentInChildren<BuildingPlacement>();
            Tooltip = GetComponentInChildren<Tooltip>();
            ResourceManager = GameObject.FindObjectOfType<ResourceTeams>();
        }

        void Update()
        {

        }
    }
}
