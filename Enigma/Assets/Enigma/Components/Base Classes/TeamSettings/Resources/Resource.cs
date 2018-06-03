using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Enigma.Components.Base_Classes.TeamSettings.Resources
{
    public abstract class Resource
    {
        public int Current { get; private set; }

        public Resource()
        {
            Current = 0;
        }

        public void Add(int add)
        {
            Current += add;
        }

        public void Reduce(int reduce)
        {
            Current -= reduce;
        }
    }
}
