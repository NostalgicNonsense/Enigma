using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings
{
    public class BuildingStats : MonoBehaviour
    {
        public string Name;
        public string Hotkey;
        public string Description;

        public float healthMax;

        public int costMoney;
        public int costOil;

        //public BuildingStats(string name, string hotkey, string description, float healthMax, int costMoney, int costOil)
        //{
        //    Name = name;
        //    Hotkey = hotkey;
        //    Description = description;
        //    this.healthMax = healthMax;
        //    this.costMoney = costMoney;
        //    this.costOil = costOil;
        //}
    }
}
