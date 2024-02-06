using System;
using ElevatorSimulator.DTOs;
using ElevatorSimulator.Interfaces;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Services;
using ElevatorSimulator.Utilities;
using Microsoft.Extensions.DependencyInjection;
// Initialize ElevatorService
var elevatorService = InitializeElevatorService();

// Run the console interface
Console.WriteLine("Elevator Simulator Console Interface");
Console.WriteLine("------------------------------------");

while (true)
{
    Console.WriteLine("\nOptions:");
    Console.WriteLine("1. Add Passenger to Waiting Queue & Elevator Dispatch");
    Console.WriteLine("2. Run Simulation");
    Console.WriteLine("3. Exit");

    Console.Write("Enter your choice (1-3): ");
    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            AddPassengerToQueueElevatorDispatch(elevatorService);
            break;

        case "2":
            RunSimulation(elevatorService);
            break;

        case "3":
            Console.WriteLine("Exiting the program. Goodbye!");
            return;

        default:
            Console.WriteLine("Invalid choice. Please enter a valid option.");
            break;
    }
}

IElevatorService InitializeElevatorService()
{
    var serviceProvider = new ServiceCollection()
                   .AddScoped<IMapper, Mapper>()
                   .AddScoped<ILogger, Logger>()
                   .AddScoped<IElevatorService, ElevatorService>()
                   .BuildServiceProvider();


    var mapper = serviceProvider.GetRequiredService<IMapper>();
    var logger = serviceProvider.GetRequiredService<ILogger>();


    return new ElevatorService(mapper, logger);
}

void AddPassengerToQueueElevatorDispatch(IElevatorService elevatorService)
{
    Console.Write("Enter the floor for the waiting passenger: ");
    if (int.TryParse(Console.ReadLine(), out int floor))
    {
        Console.Write("Enter the passenger's destination floor: ");
        if (int.TryParse(Console.ReadLine(), out int destinationFloor))
        {
            var passengerDto = new PassengerDto { DestinationFloor = destinationFloor };
            elevatorService.AddPassengerToQueue(floor, passengerDto);
            elevatorService.DispatchElevator(floor);
        }
        else
        {
            Console.WriteLine("Invalid destination floor. Please enter a valid number.");
        }
    }
    else
    {
        Console.WriteLine("Invalid floor. Please enter a valid number.");
    }
}

void RunSimulation(IElevatorService elevatorService)
{
    Console.WriteLine("Running Simulation...");

    // Simulate elevator activity for a certain duration
    while(true)
    {
        // Simulate time passing
        Thread.Sleep(1000);

        // Randomly generate passenger requests and elevator dispatches
        SimulateRandomElevatorActivity(elevatorService);
    }

    Console.WriteLine("Simulation completed.");
}

void SimulateRandomElevatorActivity(IElevatorService elevatorService)
{
    Random random = new Random();

    // Simulate adding passengers to waiting queues
    for (int i = 0; i < random.Next(1, 4); i++)
    {
        int floor = random.Next(Constants.MinFloor, Constants.MaxFloor + 1);
        int destinationFloor = random.Next(Constants.MinFloor, Constants.MaxFloor + 1);
        var passengerDto = new PassengerDto { DestinationFloor = destinationFloor };
        elevatorService.AddPassengerToQueue(floor, passengerDto);
        Console.WriteLine($"Passenger added to the waiting queue at floor {floor} with destination {destinationFloor}.");
    }

    // Simulate requesting elevator dispatch
    int requestedFloor = random.Next(Constants.MinFloor, Constants.MaxFloor + 1);
    elevatorService.DispatchElevator(requestedFloor);
    Console.WriteLine($"Elevator dispatch requested for floor {requestedFloor}.");
}
