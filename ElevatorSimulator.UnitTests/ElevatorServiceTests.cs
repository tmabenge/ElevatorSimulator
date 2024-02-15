using NUnit.Framework;
using Moq;
using ElevatorSimulator.DTOs;
using ElevatorSimulator.Services;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Utilities;
using ElevatorSimulator.Models;
using ElevatorSimulator.Services.Interfaces;

namespace ElevatorSimulator.Tests
{
    [TestFixture]
    public class ElevatorServiceTests
    {
        private IElevatorService? _elevatorService;
        private Mock<IMapper>? _mapperMock;
        private Mock<ILogger>? _loggerMock;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger>();
            _elevatorService = new ElevatorService(_mapperMock.Object, _loggerMock.Object);
        }

        [Test]
        public void AddPassengerToQueue_ShouldAddPassengerToWaitingQueue()
        {
            // Arrange
            int floor = 1;
            var passengerDto = new PassengerDto { CurrentFloor = 1, DestinationFloor = 2 };

            // Act
            _elevatorService.AddPassengerToQueue(floor, passengerDto);

            // Assert
            Assert.That(_elevatorService.WaitingPassengerFloors[floor].Count, Is.EqualTo(1));
        }

        [Test]
        public void DispatchElevator_WhenNoElevatorAvailable_ShouldLogMessage()
        {
            // Arrange
            int requestedFloor = 1;

            // Act
            _elevatorService.DispatchElevator(requestedFloor);

            // Assert
            _loggerMock.Verify(logger => logger.Log("No available elevator to dispatch."), Times.Once);
        }

        [Test]
        public void Elevators_ShouldReturnListOfElevatorDtos()
        {
            // Arrange
            var elevators = new List<Elevator>
    {
        new Elevator(10), // Example elevators with capacity 10
        new Elevator(10),
        new Elevator(10)
    };

            _ = _mapperMock.Setup(mapper => mapper.MapList<Elevator, ElevatorDto>(It.IsAny<List<Elevator>>()))
                       .Returns(new List<ElevatorDto>
                       {
                   new ElevatorDto { ElevatorId = 1, CurrentFloor = 1 }, // Example elevator DTOs
                   new ElevatorDto { ElevatorId = 2, CurrentFloor = 1 },
                   new ElevatorDto { ElevatorId = 3, CurrentFloor = 1 }
                       });

            // Act
            List<ElevatorDto> result = _elevatorService.Elevators;

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].ElevatorId, Is.EqualTo(1));
            Assert.That(result[1].CurrentFloor, Is.EqualTo(1));
            Assert.That(result[2].ElevatorId, Is.EqualTo(3));
        }

    }
}
