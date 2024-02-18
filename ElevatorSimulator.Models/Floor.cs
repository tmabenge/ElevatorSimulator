using System;
using System.Collections.Concurrent;
namespace ElevatorSimulator.Models
{
    public class Floor
    {
        public int FloorNumber { get; private set; }
        public ConcurrentQueue<Passenger> WaitingPassengers { get; set; }

        public Floor(int floorNumber)
        {
            FloorNumber = floorNumber;
            WaitingPassengers = new ConcurrentQueue<Passenger>();
        }
    }

}

