using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Enigma.Components.HelpClasses.Builders
{
    public static class InaccurateRayBuilder
    {
        public static Ray GetInaccurateRay(Vector3 origin, Vector3 targetToMakeInaccurate, float howBadDoYouWantIt)
        {
            targetToMakeInaccurate.x += Random.Range(howBadDoYouWantIt * -1, howBadDoYouWantIt);
            targetToMakeInaccurate.y += Random.Range(howBadDoYouWantIt * -1, howBadDoYouWantIt);
            targetToMakeInaccurate.z += Random.Range(howBadDoYouWantIt * -1, howBadDoYouWantIt);
            Debug.DrawRay(origin, new Vector3(targetToMakeInaccurate.x, targetToMakeInaccurate.y, targetToMakeInaccurate.z));
            return new Ray(origin, targetToMakeInaccurate);
        }
    }
}
