using ElevatorSimulator.DTOs;
using ElevatorSimulator.Event;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Services;
using ElevatorSimulator.Services.Interfaces;
using ElevatorSimulator.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using static ElevatorSimulator.Event.ElevatorEventArgs;
// Initialize ElevatorService
var elevatorService = InitializeElevatorService();
ILogger logger;

elevatorService.ElevatorStatusChanges.Subscribe(UpdateElevatorDisplay);
elevatorService.PassengerActivity.Subscribe(UpdatePassengerDisplay);

Dictionary<int, int> startingFloorWeights = new();
Dictionary<int, int> destinationFloorWeights = new();

DateTime simulationStartTime = new DateTime();
double timeScaleFactor = 60.0;  // One real second = 60 simulated seconds

Random random = new Random();

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
            RunSimulationAsync(elevatorService);
            break;

        case "3":
            Console.WriteLine("Exiting the program. Goodbye!");
            return;

        default:
            Console.WriteLine("Invalid choice. Please enter a valid option.");
            break;
    }
}

 void UpdateElevatorDisplay(ElevatorEventArgs args)
{
    switch (args.NewStatus)
    {
        case Status.Moving:
            logger.Log($"Elevator ID: {args.ElevatorId}, Moved to floor {args.CurrentFloor}");
            break;
        default:
            logger.Log($"Elevator {args.ElevatorId}: {args.NewStatus} (Floor {args.CurrentFloor})");
            break;
    }

}

void UpdatePassengerDisplay(PassengerEventArgs args)
{
    switch (args.Status)
    {
        case PassengerEventArgs.PassengerStatus.DepartedElevator:
            logger.Log($" {args.PassengersCount} passengers departed onto Elevator {args.ElevatorId} at floor {args.CurrentFloor}");
            break;
        case PassengerEventArgs.PassengerStatus.BoardedElevator:
            logger.Log($" {args.PassengersCount} passengers boarded onto Elevator {args.ElevatorId} at floor {args.CurrentFloor}");
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
        logger = serviceProvider.GetRequiredService<ILogger>();

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
            if (destinationFloor == floor)
            {
                Console.WriteLine("The destination floor cannot be the same as the starting floor.");
            }
            else if (destinationFloor < Constants.MinFloor || destinationFloor > Constants.MaxFloor)
            {
                Console.WriteLine("Invalid destination floor. Please enter a floor within the building's range.");
            }
            else
            {
                var passengerDto = new PassengerDto { DestinationFloor = destinationFloor };
                elevatorService.AddPassengerToQueue(floor, passengerDto);
            }
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

 async Task RunSimulationAsync(IElevatorService elevatorService)
{
    Console.WriteLine("Running Simulation...");

    // Set up a timer to trigger the simulation every second
    var timer = new Timer(
        async _ =>
        {
            SimulateRandomElevatorActivity(elevatorService);
            await Task.Yield(); // Ensure asynchronous operation
        },
        null,
        TimeSpan.Zero,
        TimeSpan.FromSeconds(30));

    // Wait indefinitely
    await Task.Delay(Timeout.Infinite);
}

void SimulateRandomElevatorActivity(IElevatorService elevatorService)
{
    Random random = new();

    var timer = new System.Timers.Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
    timer.AutoReset = true;
    timer.Elapsed += (sender, e) => AdjustSimulationParameters(random);
    AdjustSimulationParameters(random);
    timer.Start();


    while (true)
    {
        // Wait a random amount of time between passenger calls (for variability)
        int timeUntilNextRequest = random.Next((int)TimeSpan.FromSeconds(30).TotalMilliseconds, (int)TimeSpan.FromSeconds(60).TotalMilliseconds);
        Thread.Sleep(timeUntilNextRequest);

        // 1. Determine "activity level" - low, normal, high based on factors
        ActivityLevel currentActivity = DetermineActivityLevel(random);

        // 2. Generate Passengers with Biases 
        for (int i = 0; i < GetPassengerCount(currentActivity); i++)
        {
            int floor = GetStartingFloor(random);
            int destinationFloor = GetDestinationFloor(random, floor);
            var passengerDto = new PassengerDto { DestinationFloor = destinationFloor };
            elevatorService.AddPassengerToQueue(floor, passengerDto);
        }
    }
}

// Helper Functions
void AdjustSimulationParameters(Random random)
{
    DateTime simulatedTime = GetSimulatedTime();
    int rushHourIntensity = 70; // Range 0-100, Higher means stronger rush hour effect

    int midBuildingFloor = (Constants.MaxFloor - Constants.MinFloor) / 2;

    // Change factors based on simulated time 
    if (IsRushHour(simulatedTime))
    {
        // Starting Floors (Strong downward bias)
        startingFloorWeights.Clear();
        for (int i = Constants.MinFloor; i <= Constants.MaxFloor; i++)
        {
            // Decrease drastically the higher the floor
            startingFloorWeights[i] = rushHourIntensity - (i * 2);
        }

        // Destinations (Ground floor heavily favored)
        destinationFloorWeights.Clear();
        destinationFloorWeights[Constants.MinFloor] = rushHourIntensity;

        // Mid floors get a slight favor
        int midFloor = (Constants.MaxFloor + Constants.MinFloor) / 2;
        destinationFloorWeights[midFloor] = rushHourIntensity / 3;
    }
    else
    {
        startingFloorWeights.Clear(); // Or reset an existing dictionary
        startingFloorWeights[Constants.MinFloor] = 40; // Higher chance for lobby

        // Give mid-range floors a mild bonus
        startingFloorWeights[midBuildingFloor - 1] = 15;
        startingFloorWeights[midBuildingFloor] = 15;
        startingFloorWeights[midBuildingFloor + 1] = 15;

        // Destinations (Ground floor heavily favored)
        destinationFloorWeights.Clear();
        destinationFloorWeights[Constants.MinFloor] = rushHourIntensity;

        // Mid floors get a slight favor
        int midFloor = (Constants.MaxFloor + Constants.MinFloor) / 2;
        destinationFloorWeights[midFloor] = rushHourIntensity / 3;

        // Very slightly nudge remaining weights upward compared to totally uniform
        for (int i = Constants.MinFloor + 1; i < Constants.MaxFloor; i++)
        {
            if (!startingFloorWeights.ContainsKey(i))
            {
                startingFloorWeights[i] = 5;
            }
        }


    }
}

ActivityLevel DetermineActivityLevel(Random random)
{
    DateTime simulatedTime = GetSimulatedTime(); // Retrieve current simulated time 

    // 1. Base Traffic Levels based on Time
    if (IsNightTime(simulatedTime))
    {
        return ActivityLevel.Low;
    }
    else if (IsRushHour(simulatedTime))
    {
        return ActivityLevel.High;
    }
    else // Normal hours
    {
        // 2. Variability (within Normal Hours)
        int baseActivityValue = random.Next(30, 70); // Example: Range from 30-70% within normal hours

        // 3. Special Events or Dynamic Adjustment (Optional)
        if (IsSpecialEvent())
        {
            baseActivityValue += 20; // Boost if simulating a special event
        }

        // 4. Map Value to Activity Level 
        if (baseActivityValue <= 40)
        {
            return ActivityLevel.Low;
        }
        else if (baseActivityValue <= 80)
        {
            return ActivityLevel.Medium;
        }
        else
        {
            return ActivityLevel.High;
        }
    }
}


bool IsSpecialEvent()
{
    return random.NextDouble() < 0.02; // 2% chance of an event
}

bool IsRushHour(DateTime simulatedTime)
{
    // Morning Rush Configuration
    int morningRushStartHour = 7;
    int morningRushEndHour = 9;

    // Evening Rush Configuration
    int eveningRushStartHour = 16; // 4 PM
    int eveningRushEndHour = 18; // 6 PM

    return (simulatedTime.Hour >= morningRushStartHour && simulatedTime.Hour < morningRushEndHour) ||
           (simulatedTime.Hour >= eveningRushStartHour && simulatedTime.Hour < eveningRushEndHour);
}


bool IsNightTime(DateTime simulatedTime)
{
    int nightStartHour = 21; // 9 PM
    int nightEndHour = 5; // 5 AM

    // We'll consider nighttime as being either late in the evening *or* very early
    return (simulatedTime.Hour >= nightStartHour || simulatedTime.Hour < nightEndHour);
}

DateTime GetSimulatedTime()
{
    if (simulationStartTime == DateTime.MinValue) // Initialize on first call
    {
        simulationStartTime = DateTime.Now;
    }

    TimeSpan elapsedRealTime = DateTime.Now - simulationStartTime;
    TimeSpan simulatedTime = TimeSpan.FromSeconds(elapsedRealTime.TotalSeconds * timeScaleFactor);

    return simulationStartTime + simulatedTime; // Adjust by the scaled-up timeframe
}


int GetPassengerCount(ActivityLevel activityLevel)
{
    Random random = new Random(); // Instance for randomness within ranges

    switch (activityLevel)
    {
        case ActivityLevel.Low:
            return random.Next(1, 3); // Smaller groups or individuals

        case ActivityLevel.Medium:
            return random.Next(2, 5); // Typical small group sizes

        case ActivityLevel.High:
            return random.Next(3, 7); // Larger potential groups when it's busy

        default:
            return 1; // Safe default in case of unexpected input 
    }
}


int GetStartingFloor(Random random)
{
    // Ensure you have initialized startingFloorWeights elsewhere (likely in AdjustSimulationParameters)
    if (startingFloorWeights.Count == 0)
    {
        throw new InvalidOperationException("Floor weights have not been configured.");
    }

    int totalWeight = startingFloorWeights.Values.Sum(); // Get the sum of all the weights

    int randomValue = random.Next(1, totalWeight + 1); // 1 to inclusive of totalWeight
    int currentWeight = 0;

    // Find the floor associated with the random value:
    foreach (var floorAndWeight in startingFloorWeights)
    {
        currentWeight += floorAndWeight.Value;
        if (randomValue <= currentWeight)
        {
            return floorAndWeight.Key; // Return the floor number
        }
    }

    // This should be rarely hit - If due to rounding issues, return a random valid floor
    return startingFloorWeights.Keys.ElementAt(random.Next(startingFloorWeights.Count));
}


int GetDestinationFloor(Random random, int startingFloor)
{
    // Similar to starting floors, ensure destinationFloorWeights is initialized elsewhere
    if (destinationFloorWeights.Count == 0)
    {
        throw new InvalidOperationException("Destination floor weights not configured.");
    }

    // We'll repeat the same weighted random selection as used for GetStartingFloor

    int candidateFloor = 0;
    do
    {
        int totalWeight = destinationFloorWeights.Values.Sum();
        int randomValue = random.Next(1, totalWeight + 1);
        int currentWeight = 0;

        foreach (var floorAndWeight in destinationFloorWeights)
        {
            currentWeight += floorAndWeight.Value;
            if (randomValue <= currentWeight)
            {
                candidateFloor = floorAndWeight.Key;
                break; // We have a potential floor
            }
        }
    } while (candidateFloor == startingFloor); // Ensure it's not the starting floor itself

    return candidateFloor;
}


