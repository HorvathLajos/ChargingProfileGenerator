namespace JedlixChargingProfileGenerator.Models;

public class UserSettings
{
    public int DesiredStateOfCharge { get; set; }
    public string LeavingTime { get; set; }
    public int DirectChargingPercentage { get; set; }
    public List<Tariff> Tariffs { get; set; }
}