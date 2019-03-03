using UnityEngine;

namespace Enigma.Components.HelpClasses.ExtensionMethods
{
    public static class Vector3Extensions
    {
        public static Vector3 GetPlayerAdjustedVector3(this Vector3 vector)
        {
            return new Vector3(vector.x, vector.y + 1, vector.z);
        }
    }
}
