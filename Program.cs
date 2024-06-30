using JedlixChargingProfileGenerator.Models;
using JedlixChargingProfileGenerator.Services.Abstractions;
using JedlixChargingProfileGenerator.Services.Implementations;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddTransient<IChargingScheduleService, ChargingScheduleService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Charging Profile Generator API",
        Description = "This API provides an endpoint for charging profile generation."
    });
    c.MapType<ChargingProfileRequest>(() =>
        new OpenApiSchema
        {
            Example = new OpenApiString(@"{
                ""startingTime"": ""2024-06-30T08:00:00Z"",
                ""userSettings"": {
                    ""desiredStateOfCharge"": 100,
                    ""leavingTime"": ""07:00"",
                    ""directChargingPercentage"": 20,
                    ""tariffs"": [
                        {
                            ""startTime"": ""19:15"",
                            ""endTime"": ""10:00"",
                            ""energyPrice"": 0.22
                        },
                        {
                            ""startTime"": ""13:15"",
                            ""endTime"": ""19:15"",
                            ""energyPrice"": 0.35
                        },
                        {
                            ""startTime"": ""08:00"",
                            ""endTime"": ""13:15"",
                            ""energyPrice"": 0.25
                        }
                    ]
                },
                ""carData"": {
                    ""chargePower"": 10,
                    ""batteryCapacity"": 220,
                    ""currentBatteryLevel"": 0
                }
            }")
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Charging Profile Generator API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();