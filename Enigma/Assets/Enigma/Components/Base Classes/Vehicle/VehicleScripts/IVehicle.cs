using Assets.Enigma.Components.Base_Classes.Player;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Vehicle.VehicleScripts
{
    public interface IVehicle
    {
        void SetPlayerOccupant(IPlayer player);
    }
}
