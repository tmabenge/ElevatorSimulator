using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorSimulator.Event
{
    public class ElevatorEventArgs
    {
        public enum Status
        {
            Moving,
            Stationary,
            DoorsOpen
        }

        public int ElevatorId { get; set; }
        public Status NewStatus { get; set; }
        public int CurrentFloor { get; set; }
    }

}
