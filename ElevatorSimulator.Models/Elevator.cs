

namespace ElevatorSimulator.Models;

public class Elevator
{
    public enum Status
    {
        Moving,
        Stationary,
        DoorsOpen
    }

    public enum Direction
    {
        Up,
        Down,
        None
    }

    private static int _nextId = 1; // Static variable to generate unique IDs

    public int Id { get; private set; }

    public int CurrentFloor { get; set; }
    public Status ElevatorStatus { get;  set; }
    public Direction ElevatorDirection { get; set; }
    public int Capacity { get; private set; }
    public List<Passenger> Passengers { get; private set; }
    public bool IsAvailable => ElevatorStatus == Status.Stationary && ElevatorDirection == Direction.None;

    public Elevator(int capacity)
    {
        Id = _nextId++;
        CurrentFloor = 1;
        ElevatorStatus = Status.Stationary;
        ElevatorDirection = Direction.None;
        Capacity = capacity;
        Passengers = new List<Passenger>();
    }


    public void MoveToFloor(int floor)
    {
        if (floor > CurrentFloor)
        {
            ElevatorDirection = Direction.Up;
        }
        else if (floor < CurrentFloor)
        {
            ElevatorDirection = Direction.Down;
        }
        else
        {
            ElevatorDirection = Direction.None;
        }

        CurrentFloor = Math.Clamp(floor, 1, 9);
        ElevatorStatus = Status.Stationary;
    }

    public void LoadPassenger(Passenger passenger)
    {
        if (Passengers.Count < Capacity)
        {
            Passengers.Add(passenger);
        }
        else
        {
            
        }
    }

    public void UnloadPassenger(Passenger passenger)
    {
        if (Passengers.Contains(passenger))
        {
            Passengers.Remove(passenger);
        }
        else
        {
            throw new InvalidOperationException("Passenger is not in the elevator.");
        }
    }

}


