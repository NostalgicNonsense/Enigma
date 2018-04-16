using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Enigma.Components.Base_Classes.TeamSettings.Resources
{
    public class ResourceManager
    {
        public Money money;
        public Oil oil;

        public ResourceManager()
        {
            money = new Money();
            oil = new Oil();
        }
    }
}
