using System;
namespace ElevatorSimulator.Models
{
    public class Floor
    {
        public int FloorNumber { get; private set; }
        public List<Passenger> WaitingPassengers { get; private set; }

        public Floor(int floorNumber)
        {
            FloorNumber = floorNumber;
            WaitingPassengers = new List<Passenger>();
        }

        public void AddPassenger(Passenger passenger)
        {
            WaitingPassengers.Add(passenger);
        }

        public void RemovePassenger(Passenger passenger)
        {
            if (WaitingPassengers.Contains(passenger))
            {
                WaitingPassengers.Remove(passenger);
            }
            else
            {
                throw new InvalidOperationException("Passenger is not on this floor.");
            }
        }
    }

}

