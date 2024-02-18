using ElevatorSimulator.DTOs;
using ElevatorSimulator.Event;
using ElevatorSimulator.Models;

namespace ElevatorSimulator.Services.Interfaces
{

    public interface IElevatorService
    {
        IObservable<ElevatorEventArgs> ElevatorStatusChanges { get; }
        IObservable<PassengerEventArgs> PassengerActivity { get; }

        void AddPassengerToQueue(int floor, PassengerDto passengerDto);
    }

}

