using System;
using UnityEngine;

namespace LumberJack.WorldTime
{
    /// <summary>
    /// Estado do tempo do mundo: só as horas desde o começo (vai para o save e, no multiplayer, vem do servidor).
    /// </summary>
    [Serializable]
    public class WorldTimeState
    {
        [SerializeField] private double _totalHours;

        public WorldTimeState(double totalHours)
        {
            _totalHours = totalHours;
        }

        public double TotalHours => _totalHours;

        public void Advance(double hours)
        {
            if (hours > 0.0) _totalHours += hours;
        }
    }
}
