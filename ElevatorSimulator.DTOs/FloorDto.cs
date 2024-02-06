using System;
namespace ElevatorSimulator.DTOs
{
	public class FloorDto
	{
        public int FloorNumber { get; set; }
        public List<PassengerDto> WaitingPassengers { get; set; }
    }
}

