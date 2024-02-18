namespace ElevatorSimulator.Event
{
    public class PassengerEventArgs
    {
        public enum PassengerStatus
        {
            AddedToQueue,
            BoardedElevator,
            DepartedElevator,
            RequestFailed
        }

        public int ElevatorId { get; set; }
        public int CurrentFloor { get; set; }
        public int PassengersCount { get; set; }
        public PassengerStatus Status { get; set; }
    }


}
