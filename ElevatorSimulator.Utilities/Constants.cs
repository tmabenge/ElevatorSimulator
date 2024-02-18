namespace ElevatorSimulator.Utilities
{
    public static class Constants
	{
        public static int MinFloor { get; set; } = 1;
        public static int MaxFloor { get; set; } = 9;
        public static int ProximityThreshold { get; set; } = 2; // Floors within 2 of the elevator
        public static byte DensityCap { get; set; } = 8; // Limits excessive crowd influence 
        public static double WaitTimeWeight { get; set; } = 0.6;
        public static double DensityWeight { get; set; } = 0.3;
        public static int MaxWaitThreshold { get; set; } = 60; // Seconds 
        public static double LoadSensitivity { get; set; } = 0.3;
        public static int WaitTimePriority { get; set; } = 2;
        public static int Capacity { get; set; } = 10;
        public static int MaxElevators { get; set; } = 3;
        public static int CapacityOptimizationThreshold { get; set; }
    }
}

