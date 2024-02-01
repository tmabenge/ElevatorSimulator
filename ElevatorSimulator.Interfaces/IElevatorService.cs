using System.Collections.Generic;
using ElevatorSimulator.Models;

namespace ElevatorSimulator.Interfaces
{

    public interface IElevatorService
    {
        void AddPassengerToQueue(int floor, Passenger passenger);
        void DispatchElevator(int requestedFloor);
        void DisplayElevatorStatuses();
        void LoadPassenger(Elevator elevator, Passenger passenger);
        void StartSimulation();
    }

}

