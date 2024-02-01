using System;
using ElevatorSimulator.Interfaces;
using ElevatorSimulator.Models;
using ElevatorSimulator.Services;
using ElevatorSimulator.Utilities;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Initialize elevators and floors
            List<Elevator> elevators = new();
            for (int i = 0; i < 2; i++)
            {
                elevators.Add(new Elevator(10));
            }

            List<Floor> floors = new();
            for (int i = 0; i < 3; i++)
            {
                floors.Add(new Floor(i));
            }

            // Initialize the elevator service
            IElevatorService elevatorService = new ElevatorService(elevators);

            // Start the elevator simulation
            //elevatorService.StartSimulation();

            // Run the simulation until the user decides to stop
            while (true)
            {
                // Display the current status of the elevators
                elevatorService.DisplayElevatorStatuses();

                // Handle user input for elevator requests
                HandleUserInput(elevatorService);
            }
        }
        catch (Exception ex)
        {
            // Log any unhandled exceptions
            Logger.Log(ex);
        }
    }

    private static void HandleUserInput(IElevatorService elevatorService)
    {
        Console.WriteLine("Enter 1 to request an elevator, 2 to load a passenger, or any other key to exit:");
        string userInput = Console.ReadLine();

        switch (userInput)
        {
            case "1":
                RequestElevator(elevatorService);
                break;
            default:
                Environment.Exit(0);
                break;
        }
    }

    private static void RequestElevator(IElevatorService elevatorService)
    {
        Console.WriteLine("Enter the floor to request an elevator:");
        if (int.TryParse(Console.ReadLine(), out int requestedFloor))
        {
            elevatorService.DispatchElevator(requestedFloor);
            elevatorService.DisplayElevatorStatuses();
        }
        else
        {
            Console.WriteLine("Invalid floor input.");
        }
    }


}
