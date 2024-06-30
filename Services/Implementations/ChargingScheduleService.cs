using System.Globalization;
using JedlixChargingProfileGenerator.Models;
using JedlixChargingProfileGenerator.Services.Abstractions;

namespace JedlixChargingProfileGenerator.Services.Implementations;

public class ChargingScheduleService : IChargingScheduleService
{
    /// <summary>
    ///     Get charging profile based on the request parameters
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ChargingProfileResponse CalculateChargingSchedule(ChargingProfileRequest request)
    {
        var schedules = new List<ChargingSchedule>();
        var startingTime = DateTime.Parse(request.StartingTime!).ToUniversalTime();
        var leavingTime = ParseLeavingTime(request.UserSettings!.LeavingTime, startingTime);

        var batteryCapacity = request.CarData!.BatteryCapacity;
        var currentBatteryLevel = request.CarData.CurrentBatteryLevel;
        var chargePower = request.CarData.ChargePower;
        var desiredStateOfCharge = request.UserSettings.DesiredStateOfCharge;
        var directChargingPercentage = request.UserSettings.DirectChargingPercentage;

        var minDirectChargingLevel = batteryCapacity * (directChargingPercentage / 100.0m);
        var desiredChargeLevel = batteryCapacity * (desiredStateOfCharge / 100.0m);

        // Direct charge if needed before anything else
        if (currentBatteryLevel < minDirectChargingLevel)
        {
            currentBatteryLevel = DirectCharge(schedules, startingTime, leavingTime, currentBatteryLevel, minDirectChargingLevel, chargePower, batteryCapacity);
            startingTime = schedules.Last().EndTime;
            
        }

        var availableTariffs = request.UserSettings.Tariffs
            .OrderBy(t => t.EnergyPrice)
            .ToList();
        
        var totalChargeHoursNeeded = (double)((desiredChargeLevel - currentBatteryLevel) / chargePower);
        var chargingHoursLeft = totalChargeHoursNeeded;
        
        // Divide the hours of charging needed between the cheapest possible tariff timespans
        while (chargingHoursLeft > 0 && availableTariffs.Count != 0)
        {
            var cheapestTariff = availableTariffs.MinBy(t => t.EnergyPrice);
            if (cheapestTariff is null) continue;
            
            var tariffDates = ParseTariffDates(cheapestTariff.StartTime, cheapestTariff.EndTime, startingTime);
            var tariffDuration = (tariffDates.endTime - tariffDates.startTime).TotalHours;
            
            var chargeHours = Math.Min(chargingHoursLeft, tariffDuration);
            
            var chargeEndTime = tariffDates.startTime.AddHours(chargeHours);
            
            if (chargeEndTime > leavingTime)
            {
                chargeEndTime = leavingTime;
                chargeHours = (chargeEndTime - tariffDates.startTime).TotalHours;
            }
            
            schedules.Add(new ChargingSchedule
            {
                StartTime = tariffDates.startTime,
                EndTime = chargeEndTime,
                IsCharging = true
            });
            
            currentBatteryLevel += (decimal)chargeHours * chargePower;
            chargingHoursLeft -= chargeHours;
            availableTariffs.Remove(cheapestTariff);
        }

        return new ChargingProfileResponse
        {
            ActualChargingPercentageAtLeavingTime = (int)Math.Round((currentBatteryLevel / batteryCapacity) * 100),
            ChargingSchedules = FillGapsWithNonChargingSchedules(schedules.OrderBy(x => x.StartTime).ToList(), startingTime, leavingTime)
        };
    }
    
    private decimal DirectCharge(List<ChargingSchedule> schedules, DateTime startingTime, DateTime leavingTime, decimal currentBatteryLevel, decimal minDirectChargingLevel, decimal chargePower, decimal batteryCapacity)
    {
        var chargingHours = (minDirectChargingLevel - currentBatteryLevel) / chargePower;
        var endTime = startingTime.AddHours((double)chargingHours);
        
        if (endTime >= leavingTime)
        {
            endTime = leavingTime;
            var hoursCharged = (decimal)(endTime - startingTime).TotalHours;
            currentBatteryLevel += hoursCharged * chargePower;
        }
        else
        {
            currentBatteryLevel = minDirectChargingLevel;
        }
        
        schedules.Add(new ChargingSchedule
        {
            StartTime = startingTime,
            EndTime = endTime,
            IsCharging = true
        });

        return currentBatteryLevel;
    }
    
    private List<ChargingSchedule> FillGapsWithNonChargingSchedules(List<ChargingSchedule> schedules, DateTime startingTime, DateTime leavingTime)
    {
        var filledSchedules = new List<ChargingSchedule>();
        var currentTime = startingTime;

        foreach (var schedule in schedules)
        {
            if (currentTime < schedule.StartTime)
            {
                filledSchedules.Add(new ChargingSchedule
                {
                    StartTime = currentTime,
                    EndTime = schedule.StartTime,
                    IsCharging = false
                });
            }

            filledSchedules.Add(schedule);
            currentTime = schedule.EndTime;
        }

        if (currentTime < leavingTime)
        {
            filledSchedules.Add(new ChargingSchedule
            {
                StartTime = currentTime,
                EndTime = leavingTime,
                IsCharging = false
            });
        }

        return filledSchedules;
    }

    private (DateTime startTime, DateTime endTime) ParseTariffDates(string tariffStartTime, string tariffEndTime, DateTime startingTime)
    {
        var startTimeOfDay = TimeSpan.ParseExact(tariffStartTime, "h\\:mm", CultureInfo.InvariantCulture);
        var startTimeCandidate = startingTime.Date.Add(startTimeOfDay);

        var endTimeOfDay = TimeSpan.ParseExact(tariffEndTime, "h\\:mm", CultureInfo.InvariantCulture);
        var endTimeCandidate = startingTime.Date.Add(endTimeOfDay);

        if (endTimeCandidate <= startTimeCandidate)
        {
            endTimeCandidate = endTimeCandidate.AddDays(1);
        }
        if (startTimeCandidate < startingTime)
        {
            startTimeCandidate = startingTime;
        }

        return (startTimeCandidate, endTimeCandidate);
    }
    
    private DateTime ParseLeavingTime(string time, DateTime startingTime)
    {
        var timeOfDay = DateTime.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
        var parsedDateTime = startingTime.Date.Add(timeOfDay);
        
        if (parsedDateTime <= startingTime)
        {
            parsedDateTime = parsedDateTime.AddDays(1);
        }
        
        return parsedDateTime;
    }
    
    /// <summary>
    ///     Validates the incoming request and communicates any error towards the user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ChargingProfileRequestValidationResult ValidateChargingProfileRequest(ChargingProfileRequest request)
    {
        if (!IsValidDatetime(request.StartingTime))
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Starting time must be provided and valid." };
        
        if (request.CarData is null)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Car Data must be provided." };
        
        if (request.CarData.BatteryCapacity <= 0)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Battery capacity must be greater than zero." };

        if (request.CarData.CurrentBatteryLevel < 0 || request.CarData.CurrentBatteryLevel > request.CarData.BatteryCapacity)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Current battery level must be within valid range." };

        if (request.CarData.ChargePower <= 0)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Charge power must be greater than zero." };

        if (request.UserSettings is null)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "User Settings must be provided." };
        
        if (request.UserSettings.DesiredStateOfCharge is < 0 or > 100)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Desired state of charge must be between 0 and 100." };

        if (request.UserSettings.DirectChargingPercentage is < 0 or > 100)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Direct charging percentage must be between 0 and 100." };

        if (request.UserSettings.Tariffs.Count == 0)
            return new ChargingProfileRequestValidationResult { Success = false, Message = "At least one tariff must be provided." };

        if (request.UserSettings.Tariffs.Any(x => !IsValidDatetime(x.StartTime)))
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Tariff StartTimes must must be provided and valid." };
        
        if (request.UserSettings.Tariffs.Any(x => !IsValidDatetime(x.EndTime)))
            return new ChargingProfileRequestValidationResult { Success = false, Message = "Tariff EndTimes must must be provided and valid." };

        return new ChargingProfileRequestValidationResult { Success = true, Message = "Validation successful." };
    }
    
    private bool IsValidDatetime(string? dateToCheck)
    {
        return !string.IsNullOrEmpty(dateToCheck) && DateTime.TryParse(dateToCheck, out var parsedDateTime) && parsedDateTime != default;
    }
}