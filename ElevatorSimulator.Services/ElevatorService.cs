using System;
using System.Collections.Generic;
using System.Linq;
using ElevatorSimulator.DTOs;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Models;
using ElevatorSimulator.Services.Interfaces;
using ElevatorSimulator.Utilities;

namespace ElevatorSimulator.Services
{
    public class ElevatorService : IElevatorService
    {
        private readonly object _lock = new();
        private readonly List<Elevator> _elevators;
        private readonly Dictionary<int, Queue<Passenger>> _waitingPassengers;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public ElevatorService(IMapper mapper, ILogger logger)
        {
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
                    return _mapper.MapList<Elevator, ElevatorDto>(_elevators);
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

        public void DispatchElevator(int requestedFloor)
        {
            lock (_lock)
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
        }

        private Elevator? FindNearestElevator(int requestedFloor)
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

        private IEnumerable<Elevator> GetMovingElevators(Elevator.Direction direction) =>
            _elevators.Where(e => e.ElevatorStatus == Elevator.Status.Moving && e.ElevatorDirection == direction);

        private IEnumerable<Elevator> GetStationaryElevators() =>
            _elevators.Where(e => e.ElevatorStatus == Elevator.Status.Stationary);

        private Elevator? ElevatorScanAlgorithm(int requestedFloor, List<Elevator> elevatorsUp, List<Elevator> elevatorsDown, List<Elevator> elevatorsStationary)
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

            return weightedElevatorsStationary.FirstOrDefault();
        }

        private IOrderedEnumerable<Elevator> OrderByWeight(List<Elevator> elevators, int requestedFloor) =>
            elevators.OrderBy(e => WeightedSum(Math.Abs(e.CurrentFloor - requestedFloor), e.Passengers.Count));

        private int WeightedSum(int floorDifference, int passengerCount)
        {
            double weightFloor = Constants.WeightFloor;
            double weightPassenger = Constants.WeightPassenger;
            return (int)(weightFloor * floorDifference + weightPassenger * passengerCount);
        }

        private void MoveElevator(Elevator elevator, int destinationFloor)
        {
            MoveElevatorToRequestedFloor(elevator, destinationFloor);
            UpdateElevatorPosition(elevator);
        }

        private void MoveElevatorToRequestedFloor(Elevator elevator, int requestedFloor)
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

        // Inside the ElevatorService class

        private void UpdateElevatorPosition(Elevator elevator)
        {
            lock (_lock)
            {
                if (elevator.ElevatorStatus == Elevator.Status.Moving)
                {
                    int nextFloor = DetermineNextFloor(elevator);
                    MoveElevatorToNextFloor(elevator, nextFloor);

                    // Log elevator movement only if it has moved to a different floor
                    if (elevator.CurrentFloor != nextFloor)
                    {
                        _logger.Log($"Elevator ID: {elevator.Id}, Moved from floor {elevator.CurrentFloor} to floor {nextFloor}");
                    }

                    // Check if passengers need to be unloaded
                    if (elevator.Passengers.Any(p => p.DestinationFloor == elevator.CurrentFloor))
                    {
                        var passengersToUnload = elevator.Passengers.Where(p => p.DestinationFloor == elevator.CurrentFloor).ToList();
                        foreach (var passenger in passengersToUnload)
                        {
                            elevator.UnloadPassenger(passenger);
                            _logger.Log($"Passenger unloaded from Elevator {elevator.Id} at floor {elevator.CurrentFloor}");
                        }

                        // Log the number of passengers unloaded
                        _logger.Log($" {passengersToUnload.Count} Passenger(s) unloaded from Elevator {elevator.Id} at floor {elevator.CurrentFloor}");

                        // Check if elevator is now empty
                        if (!elevator.Passengers.Any())
                        {
                            elevator.ElevatorStatus = Elevator.Status.Stationary;
                            elevator.ElevatorDirection = Elevator.Direction.None;
                            _logger.Log($"Elevator {elevator.Id} is now empty and stationary at floor {elevator.CurrentFloor}");
                        }
                    }
                }
                else if (elevator.ElevatorStatus == Elevator.Status.Stationary)
                {
                    // Log elevator status only if it's not already logged
                    if (elevator.ElevatorDirection != Elevator.Direction.None)
                    {
                        _logger.Log($"Elevator ID: {elevator.Id}, Stopped at floor {elevator.CurrentFloor}, Direction: {elevator.ElevatorDirection}");
                    }

                    // Check if there are passengers in the elevator
                    if (elevator.Passengers.Any())
                    {
                        // Determine the direction based on passengers' destinations
                        elevator.ElevatorDirection = DetermineDirection(elevator);
                    }
                    else
                    {
                        elevator.ElevatorDirection = Elevator.Direction.None;
                    }
                }
            }
        }


        private Elevator.Direction DetermineDirection(Elevator elevator)
        {
            // Determine direction based on passengers' destinations
            int maxDestination = elevator.Passengers.Max(p => p.DestinationFloor);
            int minDestination = elevator.Passengers.Min(p => p.DestinationFloor);

            if (maxDestination > elevator.CurrentFloor)
            {
                return Elevator.Direction.Up;
            }
            else if (minDestination < elevator.CurrentFloor)
            {
                return Elevator.Direction.Down;
            }
            else
            {
                return Elevator.Direction.None;
            }
        }

        private void MoveElevatorToNextFloor(Elevator elevator, int nextFloor)
        {
            if (nextFloor >= Constants.MinFloor && nextFloor <= Constants.MaxFloor)
            {
                elevator.CurrentFloor = nextFloor;

                _logger.Log($"Elevator ID: {elevator.Id}, Moved to floor {elevator.CurrentFloor}");

                elevator.ElevatorStatus = Elevator.Status.Moving; // Set status again after moving
            }
            else
            {
                elevator.ElevatorStatus = Elevator.Status.Stationary;
                elevator.ElevatorDirection = Elevator.Direction.None;

                _logger.Log($"Elevator ID: {elevator.Id}, Reached the top/bottom floor. Stopped.");
            }
        }


        private int DetermineNextFloor(Elevator elevator)
        {
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
                var nextFloors = elevator.Passengers.OrderBy(p => Math.Abs(p.DestinationFloor - elevator.CurrentFloor))
                                                    .Select(p => p.DestinationFloor)
                                                    .ToList();

                return nextFloors.Any() ? nextFloors.First() : elevator.CurrentFloor;
            }
        }



        private void DequeuePassengers(Elevator elevator)
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
}
