using System;
namespace ElevatorSimulator.DTOs
{
	public class PassengerQueueInfoDto
	{
        public int Floor { get; set; }
        public int PassengerCount { get; set; }
        public List<PassengerDto>? Passengers { get; set; }
    }
}

