using ElevatorSimulator.DTOs;

namespace ElevatorSimulator.Interfaces
{

    public interface IElevatorService
    {
        object Lock { get; }
        List<ElevatorDto> Elevators { get; }
        Dictionary<int, Queue<PassengerDto>> WaitingPassengerFloors { get; }

        void AddPassengerToQueue(int floor, PassengerDto passengerDto);
        void DispatchElevator(int requestedFloor);
    }

}

