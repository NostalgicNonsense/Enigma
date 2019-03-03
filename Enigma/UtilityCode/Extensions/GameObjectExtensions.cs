using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UtilityCode.Extensions
{
    public static class GameObjectExtensions
    {
        public static IEnumerable<Component> GetAllComponents(this GameObject gameObject)
        {
            return gameObject.GetComponents<Component>();
        }

        public static IEnumerable<Component> GetAllComponents(this MonoBehaviour gameObject)
        {
            return gameObject.GetComponents<Component>();
        }
    }
}
