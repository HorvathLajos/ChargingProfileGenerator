using JedlixChargingProfileGenerator.Models;
using JedlixChargingProfileGenerator.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace JedlixChargingProfileGenerator.Controllers;

[ApiController]
[Route("[controller]")]
public class ChargingProfileController(IChargingScheduleService chargingScheduleService) : ControllerBase
{
    [HttpPost]
    public ActionResult<ChargingProfileResponse> GenerateChargingProfile([FromBody] ChargingProfileRequest request)
    {
        var validationResult = chargingScheduleService.ValidateChargingProfileRequest(request);
        if (!validationResult.Success) return BadRequest(validationResult.Message);
        
        var response = chargingScheduleService.CalculateChargingSchedule(request);
        // Return exactly the expected format
        return Ok(response.ChargingSchedules);
    }
}