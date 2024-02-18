using ElevatorSimulator.DTOs;
using ElevatorSimulator.Event;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Models;
using ElevatorSimulator.Services.Interfaces;
using ElevatorSimulator.Utilities;
using System.Collections.Concurrent;
using System.Drawing;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static ElevatorSimulator.Models.Elevator;

namespace ElevatorSimulator.Services
{
    public class ElevatorService : IElevatorService
    {
        private readonly object _lock = new();

        private readonly ConcurrentQueue<int> _requests;
        private readonly ConcurrentBag<Elevator> _elevators;
        private readonly ConcurrentDictionary<int, Floor> _floors;

        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        private readonly Subject<ElevatorEventArgs> _elevatorStatusChangedSubject = new();
        public IObservable<ElevatorEventArgs> ElevatorStatusChanges => _elevatorStatusChangedSubject.AsObservable();
        private readonly Subject<PassengerEventArgs> _passengerChangedSubject = new();

        public IObservable<PassengerEventArgs> PassengerActivity => _passengerChangedSubject.AsObservable();


        public ElevatorService(IMapper mapper, ILogger logger)
        {
            _requests = new ConcurrentQueue<int>();
            _elevators = new ConcurrentBag<Elevator>(); 
            _floors = new ConcurrentDictionary<int, Floor>();

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeElevators();
            InitializeFloors();

            PassengerActivity.Subscribe(async passengerArgs =>
            {
                if (passengerArgs.Status == PassengerEventArgs.PassengerStatus.AddedToQueue)
                {
                    _requests.Enqueue(passengerArgs.CurrentFloor); // Add the request to the queue
                    await ManageElevators();
                }
            });
        }

        private void InitializeElevators()
        {
            for (int i = 1; i <= Constants.MaxElevators; i++)
            {
                _elevators.Add(new Elevator(Constants.Capacity));
            }
        }

        private void InitializeFloors()
        {
            for (int i = Constants.MinFloor; i <= Constants.MaxFloor; i++)
            {
                _floors[i] = new Floor(i);
            }
        }


        public void AddPassengerToQueue(int floor, PassengerDto passengerDto)
        {
            lock (_lock)
            {
                var passenger = _mapper.Map<PassengerDto, Passenger>(passengerDto);

                if (!_floors.ContainsKey(floor))
                {
                    _floors[floor] = new Floor(floor); // Fixed: Initialize the floor if not exist
                }
                _floors[floor].WaitingPassengers.Enqueue(passenger);

                _passengerChangedSubject.OnNext(new PassengerEventArgs()
                {
                    CurrentFloor = floor,
                    ElevatorId = -1,
                    PassengersCount = -1,
                    Status = PassengerEventArgs.PassengerStatus.AddedToQueue
                });
            }
        }


        private async Task DispatchElevator(Elevator elevator, int bestFloor)
        {
            elevator.ElevatorDirection = _getDirection(elevator, bestFloor);
            elevator.ElevatorStatus = Status.Moving;
            await _simulateMovement(elevator, bestFloor);
        }

        private Elevator.Direction _getDirection(Elevator elevator, int bestFloor)
        {
            if (elevator.CurrentFloor < bestFloor)
            {
                return Direction.Up;
            }
            else if (elevator.CurrentFloor > bestFloor)
            {
                return Direction.Down;
            }
            else
            {
                return Direction.None; // Handle the case where they are already on the correct floor
            }
        }

        private async Task _simulateMovement(Elevator elevator, int bestFloor)
        {
            int endFloor = 0;
            int startFloor = elevator.CurrentFloor;

            if (elevator.Passengers.Any())
            {
                endFloor = elevator.ElevatorDirection == Direction.Up ?
                    elevator.Passengers.Max(p => p.DestinationFloor) :
                    elevator.Passengers.Min(p => p.DestinationFloor);
            }
            else
            {
                endFloor = bestFloor;
            }

            // Simple Linear Movement Simulation (adjust speed as needed)
            int floorsPerSecond = 1;
            double timePerFloor = 1.0 / floorsPerSecond;

            for (int currentFloor = startFloor; currentFloor != endFloor; currentFloor += Math.Sign(endFloor - startFloor)) // Simulate moving floor-by-floor
            {
                elevator.CurrentFloor = currentFloor;

                await Task.Delay(TimeSpan.FromSeconds(timePerFloor));
                NotifyElevatorStatusChange(elevator);
            }

            // Arrival: 
            elevator.CurrentFloor = endFloor; // Ensure exact arrival 
            _handleArrival(elevator); // Perform actions on arrival 

            await DispatchElevatorToFloorWithLongestWaitingPassenger(elevator, endFloor);
        }

        private void _handleArrival(Elevator elevator)
        {
            // Step 1: Change Elevator State
            elevator.ElevatorStatus = Status.DoorsOpen; // Simulate doors opening

            // Step 2: Unload Passengers
            var exitingPassengers = elevator.Passengers.Where(p => p.DestinationFloor == elevator.CurrentFloor).ToList();
            foreach (var passenger in exitingPassengers)
            {
                elevator.UnloadPassenger(passenger);
            }

            if (exitingPassengers.Any())
            {
                NotifyPassengerStatusChange(elevator, PassengerEventArgs.PassengerStatus.DepartedElevator, exitingPassengers.Count);
            }

            // Step 3: Load Waiting Passengers
            if (_floors.TryGetValue(elevator.CurrentFloor, out Floor? value)) // Check for presence of a queue
            {
                var queue = value;
                while (queue.WaitingPassengers.Any() && elevator.Passengers.Count < elevator.Capacity)
                {
                    queue.WaitingPassengers.TryDequeue(out var passenger);
                    if (passenger != null)
                        elevator.LoadPassenger(passenger);
                }

                if (elevator.Passengers.Any())
                {
                    NotifyPassengerStatusChange(elevator, PassengerEventArgs.PassengerStatus.BoardedElevator, elevator.Passengers.Count);
                }

            }

            // Step 4: Determine Next State
            if (elevator.Passengers.Any())
            {
                elevator.ElevatorDirection = DetermineDirection(elevator);
                elevator.ElevatorStatus = Status.Moving;
            }
            else
            {
                // All passengers departed: Recalculate direction from scratch
                elevator.ElevatorDirection = DetermineDirection(elevator);

                // Determine if there are waiting passengers anywhere
                if (_floors.Values.Any(f => f.WaitingPassengers.Any()))
                {
                    elevator.ElevatorStatus = Status.Moving; // There's more work to do 
                }
                else
                {
                    elevator.ElevatorStatus = Status.Stationary;
                    NotifyElevatorStatusChange(elevator);

                }
            }
        }

        private async Task DispatchElevatorToFloorWithLongestWaitingPassenger(Elevator elevator, int floor)
        {
            if (elevator.Passengers.Any())
            {
                await DispatchElevator(elevator, floor);
            }
        }

        private void NotifyElevatorStatusChange(Elevator elevator)
        {
            _elevatorStatusChangedSubject.OnNext(new ElevatorEventArgs()
            {
                ElevatorId = elevator.Id,
                NewStatus = (ElevatorEventArgs.Status)elevator.ElevatorStatus,
                CurrentFloor = elevator.CurrentFloor
            });
        }

        private void NotifyPassengerStatusChange(Elevator elevator, Event.PassengerEventArgs.PassengerStatus passengerStatus, int passengersCount)
        {
            _passengerChangedSubject.OnNext(new PassengerEventArgs()
            {
                CurrentFloor = elevator.CurrentFloor,
                ElevatorId = elevator.Id,
                PassengersCount = passengersCount,
                Status = passengerStatus
            });
        }

        private async Task ManageElevators()
        {
            foreach (var elevator in _elevators.Where(e => e.ElevatorStatus == Elevator.Status.Stationary))
            {

               await DetermineAndExecuteNextMove(elevator);
            }

        }


        private async Task DetermineAndExecuteNextMove(Elevator elevator)
        {
            // Step 1: Get all floors with waiting passengers
            List<int> floorsWithWaiting = GetFloorsWithWaitingPassengers(elevator);

            // Step 2: Determine the best choice
            int bestFloor = DetermineBestNextFloor(elevator, floorsWithWaiting);

            // Step 3: Dispatch!
            await DispatchElevator(elevator, bestFloor);
        }

        private List<int> GetFloorsWithWaitingPassengers(Elevator elevator)
        {
            var query = _floors.Where(kvp => kvp.Value.WaitingPassengers.Any());

            if (elevator.ElevatorDirection == Direction.Up)
            {
                query = query.Where(kvp => kvp.Key > elevator.CurrentFloor);  // Above current position
            }
            else if (elevator.ElevatorDirection == Direction.Down)
            {
                query = query.Where(kvp => kvp.Key < elevator.CurrentFloor); // Below current position
            }

            // Convert to just floor numbers 
            return query.Select(kvp => kvp.Key).ToList();
        }

        private int CalculateAttractivenessScore(Elevator elevator, int floor)
        {
            int baseScore = 20;
            int densityBonus = CalculateDensityBonus(floor, elevator);
            int destinationBonus = CalculateDestinationBonus(elevator, floor);
            double distanceFactor = GetDistanceFactor(elevator, floor);
            double competitionFactor = GetCompetitionFactor(floor);

            // Wait Time Integration
            int waitSeverityScore = CalculateWaitSeverityScore(elevator, floor);
            double waitFactor = waitSeverityScore * Constants.WaitTimePriority;

            int adjustedScore = (int)(baseScore + densityBonus + destinationBonus +
                                      distanceFactor + competitionFactor + waitFactor);

            return adjustedScore;
        }

        private int CalculateWaitSeverityScore(Elevator elevator, int floor)
        {
            double avgWaitTime = _floors[floor].WaitingPassengers.Average(p => (DateTime.Now - p.TimeAddedToQueue).TotalSeconds);
            int numPassengers = _floors[floor].WaitingPassengers.Count;

            double normalizedWaitTime = avgWaitTime / GetDynamicWaitThreshold(elevator); // Value above 1 means exceeding average threshold 
            int densityFactor = Math.Min(numPassengers, Constants.DensityCap); // Limit to prevent overly skewed weighting for huge crowds

            int severityScore = (int)(normalizedWaitTime * Constants.WaitTimeWeight + densityFactor * Constants.DensityWeight);
            return severityScore;
        }

        private double GetDynamicWaitThreshold(Elevator elevator)
        {
            double loadFactor = (double)elevator.Passengers.Count / elevator.Capacity;
            double adjustedThreshold = Constants.MaxWaitThreshold - (loadFactor * Constants.LoadSensitivity);
            return adjustedThreshold;
        }

        private int CalculateDensityBonus(int floor, Elevator elevator)
        {
            int numberOfWaitingPassengers = _floors[floor].WaitingPassengers.Count;

            int availableSpaces = elevator.Capacity - elevator.Passengers.Count;
            int adjustedBonus = Math.Min(numberOfWaitingPassengers, availableSpaces) * 5;

            return adjustedBonus;
        }

        private int CalculateDestinationBonus(Elevator elevator, int floor)
        {
            if (elevator.Passengers.Any(p => p.DestinationFloor == floor))
            {
                return 15;
            }

            int totalDistanceDifference = elevator.Passengers
                                              .Select(p => Math.Abs(p.DestinationFloor - floor))
                                              .Sum();

            int averageDifference = 0;
            try
            {
                averageDifference = totalDistanceDifference / elevator.Passengers.Count;
            }
            catch (DivideByZeroException ex)
            {
                // Handle division by zero
            }

            int proximityBonus = 20 - averageDifference;

            return proximityBonus;
        }

        private double GetDistanceFactor(Elevator elevator, int floor)
        {
            double distance = Math.Abs(elevator.CurrentFloor - floor);
            double maxDistance = Constants.MaxFloor - Constants.MinFloor;

            double normalizedDistance = distance / maxDistance; // Value between 0 and 1

            // Use normalizedDistance to calculate a factor (could be linear or nonlinear)
            double distanceFactor = -50 * normalizedDistance; // Simple linear, favors closer floors

            return distanceFactor;
        }

        private double GetCompetitionFactor(int floor)
        {
            // Determine how many other elevators are 'close' to this floor
            int nearbyElevators = _elevators.Count(e =>
                                     Math.Abs(e.CurrentFloor - floor) <= Constants.ProximityThreshold);

            // Could be linear or nonlinear based on how heavily you want to penalize competition
            double competitionFactor = -10 * nearbyElevators;

            return competitionFactor;
        }

        private int DetermineBestNextFloor(Elevator elevator, List<int> floorsWithWaiting)
        {
            if (floorsWithWaiting.Count == 0)  // Handle case of no waiting passengers
            {
                return elevator.CurrentFloor; // Default: Stay put if truly nothing to do
            }

            int bestFloor = floorsWithWaiting.First(); // Initial candidate
            int highestScore = 0;

            foreach (var floor in floorsWithWaiting)
            {
                int score = CalculateAttractivenessScore(elevator, floor); // Modified calculation

                if (score > highestScore)
                {
                    highestScore = score;
                    bestFloor = floor;
                }
            }

            return bestFloor;
        }

        private Elevator.Direction DetermineDirection(Elevator elevator)
        {
            if (elevator.Passengers.Any())
            {
                // Determine direction based on passengers' destinations
                int maxDestination = elevator.Passengers.Max(p => p.DestinationFloor);
                int minDestination = elevator.Passengers.Min(p => p.DestinationFloor);


                if (maxDestination > elevator.CurrentFloor)
                {
                    return Direction.Up;
                }
                else if (minDestination < elevator.CurrentFloor)
                {
                    return Direction.Down;
                }
                else
                {
                    return Direction.None;
                }
            }
            return Direction.None;
        }

    }
}
