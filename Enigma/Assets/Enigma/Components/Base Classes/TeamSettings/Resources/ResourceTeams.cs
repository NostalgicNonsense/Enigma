using Enigma.Components.Base_Classes.TeamSettings.Enums;
using Enigma.Components.HelpClasses;
using UnityEngine;

namespace Enigma.Components.Base_Classes.TeamSettings.Resources
{
    public class ResourceTeams : MonoBehaviour
    {
        public Money MoneyTeam1 { get; private set; }
        public Oil OilTeam1 { get; private set; }

        public Money MoneyTeam2 { get; private set; }
        public Oil OilTeam2 { get; private set; }

        void Start()
        {
            MoneyTeam1 = new Money();
            OilTeam1 = new Oil();

            MoneyTeam1.Add(653);
            OilTeam1.Add(55);

            MoneyTeam2 = new Money();
            OilTeam2 = new Oil();

            MoneyTeam2.Add(653);
            OilTeam2.Add(55);
        }

        public void Add(int money, int oil, TeamName teamName)
        {
            switch (teamName)
            {
                case TeamName.Team1:
                    MoneyTeam1.Add(money);
                    OilTeam1.Add(oil);
                    break;

                case TeamName.Team2:
                    MoneyTeam2.Add(money);
                    OilTeam2.Add(oil);
                    break;
            }
        }

        public void Reduce(int money, int oil, TeamName teamName)
        {
            switch (teamName)
            {
                case TeamName.Team1:
                    MoneyTeam1.Reduce(money);
                    OilTeam1.Reduce(oil);
                    break;

                case TeamName.Team2:
                    MoneyTeam2.Reduce(money);
                    OilTeam2.Reduce(oil);
                    break;
            }
        }

        public Tuple<Money, Oil> GetResources(TeamName teamName)
        {
            switch (teamName)
            {
                case TeamName.Team1:
                    return new Tuple<Money, Oil>(MoneyTeam1, OilTeam1);

                case TeamName.Team2:
                    return new Tuple<Money, Oil>(MoneyTeam2, OilTeam2);
            }
            return null;
        }
    }
}
