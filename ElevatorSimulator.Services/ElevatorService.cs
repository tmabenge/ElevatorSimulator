using ElevatorSimulator.DTOs;
using ElevatorSimulator.Interfaces;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Models;
using ElevatorSimulator.Utilities;

namespace ElevatorSimulator.Services
{
    public class ElevatorService : IElevatorService
    {
        private readonly object _lock = new();
        private readonly List<Elevator> _elevators;
        private readonly Dictionary<int, Queue<Passenger>> _waitingPassengers;
        private IMapper _mapper;
        private ILogger _logger;

        public ElevatorService(IMapper mapper, ILogger logger)
        {
            // Initialize elevators based on the constant MaxFloor
            _elevators = new List<Elevator>();
            for (int i = 0; i < Constants.MaxFloor; i++)
            {
                _elevators.Add(new Elevator(Constants.MaxFloor));
            }
            _waitingPassengers = new Dictionary<int, Queue<Passenger>>();
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public object Lock => _lock;

        public List<ElevatorDto> Elevators
        {
            get
            {
                lock (_lock)
                {
                    var elevatorDtos = _mapper.MapList<Elevator, ElevatorDto>(_elevators);
                    return elevatorDtos;
                }
            }
        }

        public Dictionary<int, Queue<PassengerDto>> WaitingPassengerFloors
        {
            get
            {
                lock (_lock)
                {
                    var waitingPassengerFloors = _waitingPassengers.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new Queue<PassengerDto>(kvp.Value.Select(passenger => _mapper.Map<Passenger, PassengerDto>(passenger))));
                    return waitingPassengerFloors;
                }
            }
        }

        public void AddPassengerToQueue(int floor, PassengerDto passengerDto)
        {
            lock (_lock)
            {
                var passenger = _mapper.Map<PassengerDto, Passenger>(passengerDto);

                if (!_waitingPassengers.ContainsKey(floor))
                {
                    _waitingPassengers[floor] = new Queue<Passenger>();
                }
                _waitingPassengers[floor].Enqueue(passenger);
            }
        }

        private void DequeuePassengers(Elevator elevator)
        {
            lock (_lock)
            {

                if (_waitingPassengers.TryGetValue(elevator.CurrentFloor, out var queue))
                {
                    while (queue.Any() && elevator.Passengers.Count < elevator.Capacity)
                    {
                        var passenger = queue.Dequeue();
                        elevator.LoadPassenger(passenger);

                        _logger.Log($"Elevator ID: {elevator.Id}, Floor: {elevator.CurrentFloor}, " +
                        $"Status: {elevator.ElevatorStatus}, Direction: {elevator.ElevatorDirection}, " +
                        $"Passengers: {elevator.Passengers.Count}");
                    }
                }
            }
        }

        public void DispatchElevator(int requestedFloor)
        {
            var nearestElevator = FindNearestElevator(requestedFloor);
            if (nearestElevator != null)
            {
                MoveElevator(nearestElevator, requestedFloor);
                UpdateAndDequeue(nearestElevator);
            }
            else
            {
                _logger.Log("No available elevator to dispatch.");
            }
        }

        private void MoveElevator(Elevator elevator, int destinationFloor)
        {
            MoveElevatorToRequestedFloor(elevator, destinationFloor);
            UpdateElevatorPosition(elevator);

            // Check if there are passengers in the elevator
            if (elevator.Passengers.Any())
            {
                int nextFloor = DetermineNextFloor(elevator);
                MoveElevator(elevator, nextFloor); // Recursive call to move to the next floor
            }
            else
            {
                // If no passengers are left, stop the elevator
                elevator.ElevatorStatus = Elevator.Status.Stationary;
                elevator.ElevatorDirection = Elevator.Direction.None;
            }
        }


#pragma warning disable CA1822 // Mark members as static
        private void MoveElevatorToRequestedFloor(Elevator elevator, int requestedFloor)
#pragma warning restore CA1822 // Mark members as static
        {
            if (elevator.CurrentFloor != requestedFloor)
            {
                elevator.ElevatorStatus = Elevator.Status.Moving;
            }
            elevator.MoveToFloor(requestedFloor);
        }

        private void UpdateAndDequeue(Elevator elevator)
        {
            UpdateElevatorPosition(elevator);
            DequeuePassengers(elevator);
        }

        private Elevator? FindNearestElevator(int requestedFloor)
        {
            lock (_lock)
            {
                try
                {
                    var elevatorsUp = GetMovingElevators(Elevator.Direction.Up).OrderBy(e => e.CurrentFloor).ToList();
                    var elevatorsDown = GetMovingElevators(Elevator.Direction.Down).OrderByDescending(e => e.CurrentFloor).ToList();
                    var elevatorsStationary = GetStationaryElevators().OrderBy(e => Math.Abs(e.CurrentFloor - requestedFloor)).ToList();

                    return ElevatorScanAlgorithm(requestedFloor, elevatorsUp, elevatorsDown, elevatorsStationary);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error finding nearest elevator: {ex.Message}");
                    return null;
                }
            }
        }

        private IEnumerable<Elevator> GetMovingElevators(Elevator.Direction direction) =>
            _elevators.Where(e => e.ElevatorStatus == Elevator.Status.Moving && e.ElevatorDirection == direction);

        private IEnumerable<Elevator> GetStationaryElevators() =>
            _elevators.Where(e => e.ElevatorStatus == Elevator.Status.Stationary);

        private Elevator ElevatorScanAlgorithm(int requestedFloor, List<Elevator> elevatorsUp, List<Elevator> elevatorsDown, List<Elevator> elevatorsStationary)
        {
            var weightedElevatorsUp = OrderByWeight(elevatorsUp, requestedFloor);
            var weightedElevatorsDown = OrderByWeight(elevatorsDown, requestedFloor);
            var weightedElevatorsStationary = OrderByWeight(elevatorsStationary, requestedFloor);

            var upGoingElevator = weightedElevatorsUp.FirstOrDefault(e => e.CurrentFloor <= requestedFloor);
            if (upGoingElevator != null)
            {
                return upGoingElevator;
            }

            var downGoingElevator = weightedElevatorsDown.FirstOrDefault(e => e.CurrentFloor >= requestedFloor);
            if (downGoingElevator != null)
            {
                return downGoingElevator;
            }

#pragma warning disable CS8603 // Possible null reference return.
            return weightedElevatorsStationary.FirstOrDefault();
#pragma warning restore CS8603 // Possible null reference return.
        }

        private IOrderedEnumerable<Elevator> OrderByWeight(List<Elevator> elevators, int requestedFloor) =>
            elevators.OrderBy(e => WeightedSum(Math.Abs(e.CurrentFloor - requestedFloor), e.Passengers.Count));

        private int WeightedSum(int floorDifference, int passengerCount)
        {
            double weightFloor = Constants.WeightFloor;
            double weightPassenger = Constants.WeightPassenger;
            return (int)(weightFloor * floorDifference + weightPassenger * passengerCount);
        }

        private void UpdateElevatorPosition(Elevator elevator)
        {
            lock (_lock)
            {

                elevator.ElevatorStatus = Elevator.Status.Moving;

                if (elevator.ElevatorStatus == Elevator.Status.Moving)
                {
                    // Determine the next floor based on passengers' destinations and current direction
                    int nextFloor = DetermineNextFloor(elevator);

                    // Move the elevator to the next floor
                    MoveElevatorToNextFloor(elevator, nextFloor);

                    // Check if the elevator has reached its destination floor and update status
                    if (elevator.Passengers.Any(p => p.DestinationFloor == elevator.CurrentFloor))
                    {
                        // Unload passengers whose destination floor is the current floor
                        var passengersToUnload = elevator.Passengers.Where(p => p.DestinationFloor == elevator.CurrentFloor).ToList();
                        foreach (var passenger in passengersToUnload)
                        {
                            elevator.UnloadPassenger(passenger);
                        }

                        Console.WriteLine($" {passengersToUnload.Count()} Passenger unloaded from Elevator {elevator.Id} at floor {elevator.CurrentFloor}");

                        // If there are no more passengers with destination floors, stop the elevator
                        if (!elevator.Passengers.Any(p => p.DestinationFloor != elevator.CurrentFloor))
                        {
                            elevator.ElevatorStatus = Elevator.Status.Stationary;
                            elevator.ElevatorDirection = Elevator.Direction.None;
                        }
                    }
                }
            }
        }

        private int DetermineNextFloor(Elevator elevator)
        {
            // Prioritize passengers' destinations over the next floor based on current direction
            if (elevator.ElevatorDirection == Elevator.Direction.Up)
            {
                var nextFloorsUp = elevator.Passengers.Where(p => p.DestinationFloor > elevator.CurrentFloor)
                                                      .OrderBy(p => p.DestinationFloor)
                                                      .Select(p => p.DestinationFloor)
                                                      .ToList();

                return nextFloorsUp.Any() ? nextFloorsUp.First() : elevator.CurrentFloor + 1;
            }
            else if (elevator.ElevatorDirection == Elevator.Direction.Down)
            {
                var nextFloorsDown = elevator.Passengers.Where(p => p.DestinationFloor < elevator.CurrentFloor)
                                                        .OrderByDescending(p => p.DestinationFloor)
                                                        .Select(p => p.DestinationFloor)
                                                        .ToList();

                return nextFloorsDown.Any() ? nextFloorsDown.First() : elevator.CurrentFloor - 1;
            }
            else
            {
                // If the elevator is stationary, choose the closest destination floor
                var nextFloors = elevator.Passengers.OrderBy(p => Math.Abs(p.DestinationFloor - elevator.CurrentFloor))
                                                    .Select(p => p.DestinationFloor)
                                                    .ToList();

                return nextFloors.Any() ? nextFloors.First() : elevator.CurrentFloor;
            }
        }

        private void MoveElevatorToNextFloor(Elevator elevator, int nextFloor)
        {
            // Check if the next floor is within the valid range
            if (nextFloor >= Constants.MinFloor && nextFloor <= Constants.MaxFloor)
            {
                // Update the elevator's current floor
                elevator.CurrentFloor = nextFloor;

                _logger.Log($"Elevator ID: {elevator.Id}, Moved to floor {elevator.CurrentFloor}");
            }
            else
            {
                // Stop the elevator if the next floor is outside the valid range
                elevator.ElevatorStatus = Elevator.Status.Stationary;
                elevator.ElevatorDirection = Elevator.Direction.None;

                _logger.Log($"Elevator ID: {elevator.Id}, Reached the top/bottom floor. Stopped.");
            }
        }

    }
}
