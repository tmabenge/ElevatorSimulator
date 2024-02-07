---

# Elevator Simulator

## Overview
The Elevator Simulator is a C# application designed to simulate the operation of elevators within a multi-floor building. It provides functionalities such as dispatching elevators to requested floors, loading and unloading passengers, and moving elevators between floors based on passenger requests.

**Please Note:** This project currently does not include different types of elevators, and the efficiency of unloading passengers can be increased by changing weights of the scan algorithm. Additionally, the Observer-Subscriber pattern is not implemented in the current version.

## Features
- Dispatching the nearest available elevator to a requested floor
- Loading passengers into elevators
- Unloading passengers from elevators at their destination floors
- Moving elevators between floors based on passenger requests
- Real-time logging of elevator operations

## Technologies Used
- C# programming language
- .NET Framework
- NUnit testing framework for unit testing
- Moq library for mocking dependencies in unit tests

## Structure
The project is structured as follows:
- **ElevatorSimulator**: Contains the main application logic and classes, including the `ElevatorService`, `Elevator`, `Passenger`, and other related classes.
- **ElevatorSimulator.DTOs**: Defines data transfer objects (DTOs) used for transferring data between different parts of the application.
- **ElevatorSimulator.Interfaces**: Contains interfaces used for dependency injection and loose coupling of components.
- **ElevatorSimulator.Models**: Defines models used to represent elevator-related entities.
- **ElevatorSimulator.Mappers**: Contains classes responsible for mapping between DTOs and models.
- **ElevatorSimulator.Utilities**: Contains utility classes and constants used throughout the application.
- **ElevatorSimulator.Tests**: Contains unit tests for testing the functionality of the application.

## Observer Pattern (Not Implemented)
The Observer pattern could be implemented to allow components of the system to subscribe to events related to elevator operations. For example, a Console component could subscribe to events such as "ElevatorArrived", "PassengerLoaded", and "PassengerUnloaded" to update its display in real-time.

```

With the Factory pattern, you can create different instances of elevators based on their types, allowing for more flexibility and scalability in your application.

## Usage
1. Clone the repository to your local machine.
2. Open the solution in Visual Studio or your preferred IDE.
3. Build the solution to ensure all dependencies are resolved.
4. Run the unit tests to verify the functionality of the application.
5. Run the application and interact with it to simulate elevator operations.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---
