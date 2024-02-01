namespace ElevatorSimulator.Models
{
    public class Passenger
    {
        public int CurrentFloor { get; private set; }
        public int DestinationFloor { get; private set; }

        public Passenger(int currentFloor, int destinationFloor)
        {
            CurrentFloor = currentFloor;
            DestinationFloor = destinationFloor;
        }
    }

}

