using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
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
