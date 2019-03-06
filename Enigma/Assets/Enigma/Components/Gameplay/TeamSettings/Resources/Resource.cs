namespace Enigma.Components.Gameplay.TeamSettings.Resources
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
