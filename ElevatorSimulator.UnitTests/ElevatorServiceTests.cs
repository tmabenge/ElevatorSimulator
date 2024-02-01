using ElevatorSimulator.Models;
using ElevatorSimulator.Services;
using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class ElevatorServiceTests
{
    private ElevatorService _elevatorService;
    private List<Elevator> _elevators;

    [SetUp]
    public void Setup()
    {
        // Initialize with a set of elevators for testing
        _elevators = new List<Elevator>
        {
            new Elevator(10), // Elevator ID 1, Capacity 10
            new Elevator(10)  // Elevator ID 2, Capacity 10
        };
        _elevatorService = new ElevatorService(_elevators);
    }


    [Test]
    public void LoadPassenger_WhenAtCapacity_ThrowsInvalidOperationException()
    {
        // Arrange
        var elevator = _elevators[0];
        // Fill the elevator to capacity
        for (int i = 0; i < elevator.Capacity; i++)
        {
            elevator.Passengers.Add(new Passenger(1, 5));
        }
        var newPassenger = new Passenger(2, 6);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _elevatorService.LoadPassenger(elevator, newPassenger));
    }

    [Test]
    public void MoveElevator_WhenCalled_MovesElevatorToTargetFloor()
    {
        // Arrange
        var targetFloor = 5;
        var elevator = _elevators[0];

        // Act
        _elevatorService.DispatchElevator(targetFloor);

        // Assert
        Assert.Equals(targetFloor, elevator.CurrentFloor);
    }

    // Additional tests for unloading passengers, elevator status updates, etc., can be added here
}
