namespace ElevatorSimulator.DTOs;

public class ElevatorDto
{
    public int ElevatorId { get; set; }
    public int CurrentFloor { get; set; }
    public ElevatorStatusDto? ElevatorStatus { get; set; }
    public ElevatorDirectionDto? ElevatorDirection { get; set; }
    public List<PassengerDto>? Passengers { get; set; }
}

