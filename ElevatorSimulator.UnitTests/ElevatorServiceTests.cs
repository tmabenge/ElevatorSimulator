using NUnit.Framework;
using Moq;
using ElevatorSimulator.DTOs;
using ElevatorSimulator.Services;
using ElevatorSimulator.Mappers;
using ElevatorSimulator.Utilities;
using ElevatorSimulator.Models;
using ElevatorSimulator.Services.Interfaces;
using AutoMapper;
using NUnit.Framework.Internal;
using ILogger = ElevatorSimulator.Utilities.ILogger;
using IMapper = ElevatorSimulator.Mappers.IMapper;
using Xunit;

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

        [Fact]
        public void AddPassengerToQueue_TriggersStatusChange()
        {
            // Arrange - Set up test data and mock event handler
            bool eventTriggered = false;
            _elevatorService?.PassengerActivity.Subscribe(_ => eventTriggered = true);

            // Act
            _elevatorService?.AddPassengerToQueue(3, new PassengerDto { DestinationFloor = 5});

            //Verify
            _mapperMock?.Verify(m => m.Map<PassengerDto, Passenger>(It.IsAny<PassengerDto>()), Times.Once);

            // Assert
            Assert.That(eventTriggered);
        }

    }
}
