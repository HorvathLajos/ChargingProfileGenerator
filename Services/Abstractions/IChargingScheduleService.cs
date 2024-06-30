using JedlixChargingProfileGenerator.Models;

namespace JedlixChargingProfileGenerator.Services.Abstractions;

public interface IChargingScheduleService
{
    ChargingProfileResponse CalculateChargingSchedule(ChargingProfileRequest request);
    ChargingProfileRequestValidationResult ValidateChargingProfileRequest(ChargingProfileRequest request);
}