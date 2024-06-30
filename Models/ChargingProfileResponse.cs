namespace JedlixChargingProfileGenerator.Models;

public class ChargingProfileResponse
{
    public int ActualChargingPercentageAtLeavingTime { get; set; }
    public List<ChargingSchedule>? ChargingSchedules { get; set; }
}