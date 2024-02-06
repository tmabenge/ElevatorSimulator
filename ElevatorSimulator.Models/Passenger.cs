namespace ElevatorSimulator.Models
{
    public class Passenger
    {
        public int CurrentFloor { get; set; }
        public int DestinationFloor { get; set; }

        public Passenger(int currentFloor, int destinationFloor)
        {
            CurrentFloor = currentFloor;
            DestinationFloor = destinationFloor;
        }
    }

}

