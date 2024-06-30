namespace JedlixChargingProfileGenerator.Models;

public class ChargingProfileRequest
{
    public string? StartingTime { get; set; }
    public UserSettings? UserSettings { get; set; }
    public CarData? CarData { get; set; }
}