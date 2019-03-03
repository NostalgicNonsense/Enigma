using System.Collections.Generic;
using Enigma.Components.Base_Classes.Player;

namespace Enigma.Components.Base_Classes.Vehicle.ComponentScripts
{
    public class Passengers
    {
        private readonly IPlayer[] _players;
        private int _seatIndex;
        private readonly Dictionary<int, int> _playerHashCodeLookUp;

        public Passengers(int numberOfSeats)
        {
            _seatIndex = 0;
            _players = new IPlayer[numberOfSeats];
            _playerHashCodeLookUp = new Dictionary<int, int>(numberOfSeats);
        }

        public IPlayer GetDriver()
        {
            return _players[0];
        }

        public void DisembarkPlayer(IPlayer player)
        {
            var seatLocation = _playerHashCodeLookUp[player.GetHashCode()];
            RemovePlayer(seatLocation);
            RemoveFromHashLookUp(player);
        }

        private void RemoveFromHashLookUp(IPlayer player)
        {
            _playerHashCodeLookUp.Remove(player.GetHashCode());
        }

        public void EmbarkPlayer(IPlayer player)
        {
            if (!VehicleFull())
            {
                AddPassenger(player);
            }
        }

        private void AddPassenger(IPlayer player)
        {
            _players[_seatIndex] = player;
            _playerHashCodeLookUp.Add(player.GetHashCode(), _seatIndex);
            HandleSeatIndexEmbark();
        }

        private void HandleSeatIndexEmbark()
        {
            for (int i = _seatIndex + 1; i < _players.Length; i++)
            {
                if (_players[i] == null)
                {
                    _seatIndex = i;
                }
            }
        }

        private bool VehicleFull()
        {
            return _seatIndex == _players.Length;
        } 

        private void RemovePlayer(int index)
        {
            _players[index] = null;
            HandleSeatIndexDisembark(index);
        }

        private void HandleSeatIndexDisembark(int index)
        {
            if (_seatIndex > index)
            {
                _seatIndex = index;
            }
        }
    }
}
