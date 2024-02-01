using ElevatorSimulator.Interfaces;
using ElevatorSimulator.Models;

namespace ElevatorSimulator.Services
{
    public class ElevatorService : IElevatorService
    {
        private List<Elevator> _elevators;

        private Dictionary<int, Queue<Passenger>> _waitingPassengers;

        public ElevatorService(List<Elevator> elevators)
        {
            _elevators = elevators ?? throw new ArgumentNullException(nameof(elevators));
            _waitingPassengers = new Dictionary<int, Queue<Passenger>>();
        }

        public void AddPassengerToQueue(int floor, Passenger passenger)
        {
            if (!_waitingPassengers.ContainsKey(floor))
            {
                _waitingPassengers[floor] = new Queue<Passenger>();
            }
            _waitingPassengers[floor].Enqueue(passenger);
        }

        private void DequeuePassengers(Elevator elevator)
        {
            if (_waitingPassengers.ContainsKey(elevator.CurrentFloor))
            {
                var queue = _waitingPassengers[elevator.CurrentFloor];
                while (queue.Any() && elevator.Passengers.Count < elevator.Capacity)
                {
                    var passenger = queue.Dequeue();
                    elevator.LoadPassenger(passenger);
                }
            }
        }

        // Display real-time status of all elevators
        public void DisplayElevatorStatuses()
        {
            foreach (var elevator in _elevators)
            {
                Console.WriteLine($"Elevator ID: {elevator.Id}, Floor: {elevator.CurrentFloor}, " +
                                  $"Status: {elevator.ElevatorStatus}, Direction: {elevator.ElevatorDirection}, " +
                                  $"Passengers: {elevator.Passengers.Count}");
            }
        }

        // Efficiently dispatch the nearest available elevator to a floor request
        public void DispatchElevator(int requestedFloor)
        {
            var nearestElevator = FindNearestElevator(requestedFloor);

            if (nearestElevator != null)
            {
                nearestElevator.MoveToFloor(requestedFloor);
            }
            else
            {
                Console.WriteLine("No available elevator to dispatch.");
            }

        }

        public void LoadPassenger(Elevator elevator, Passenger passenger)
        {
            if (elevator.Passengers.Count < elevator.Capacity)
            {
                elevator.LoadPassenger(passenger);
            }
            else
            {
                Console.WriteLine("Elevator is at full capacity.");
                AddPassengerToQueue(elevator.CurrentFloor, passenger);
            }
        }


        // Helper method to find the nearest available elevator
        private Elevator FindNearestElevator(int requestedFloor)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return _elevators
                .Where(e => e.ElevatorStatus == Elevator.Status.Stationary ||
                            (e.ElevatorStatus == Elevator.Status.Moving &&
                             ((e.ElevatorDirection == Elevator.Direction.Up && e.CurrentFloor <= requestedFloor) ||
                              (e.ElevatorDirection == Elevator.Direction.Down && e.CurrentFloor >= requestedFloor))))
                .OrderBy(e => Math.Abs(e.CurrentFloor - requestedFloor) + e.Passengers.Count)
                .FirstOrDefault();
#pragma warning restore CS8603 // Possible null reference return.
        }

        public void StartSimulation()
        {
            Console.WriteLine("Starting elevator simulation...");

            while (true)
            {
                Thread.Sleep(1000); // Simulate time passing

                // Simulate calls to elevators from different floors
                SimulateFloorCalls();

                // Update elevators' positions and load/unload passengers
                foreach (var elevator in _elevators)
                {
                    UpdateElevatorPosition(elevator);
                    DequeuePassengers(elevator);
                }

                // Display the current status of all elevators
                DisplayElevatorStatuses();
            }
        }

        private void SimulateFloorCalls()
        {
            // Example: Randomly generate floor calls
            Random rnd = new Random();
            int floorCall = rnd.Next(1, 10); // Assuming floors 1-10
            Console.WriteLine($"Simulating call to floor {floorCall}");
            DispatchElevator(floorCall);
        }

        private void UpdateElevatorPosition(Elevator elevator)
        {
            Console.WriteLine($"Elevator on floor {elevator.Id} - {elevator.CurrentFloor}");
        }


    }
}
