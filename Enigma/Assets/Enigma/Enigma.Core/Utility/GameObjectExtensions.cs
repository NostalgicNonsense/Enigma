using System.Collections.Generic;
using UnityEngine;

namespace Utility
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
