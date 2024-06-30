namespace JedlixChargingProfileGenerator.Models;

public class ChargingSchedule
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsCharging { get; set; }
}