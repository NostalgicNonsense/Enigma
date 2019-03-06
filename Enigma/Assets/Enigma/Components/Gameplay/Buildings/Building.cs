using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings
{
    public class Building : MonoBehaviour
    {
        public BuildingStats BuildingStats { get { return GetComponentInChildren<BuildingStats>(); } }

        void Start()
        {
            //BuildingStats = GetComponentInChildren<BuildingStats>();
        }
    }
}
