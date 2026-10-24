using OrderManagement.Api.Features.Components;

namespace OrderManagement.Api.Data;

/// <summary>
/// Demo presets inserted by an EF Core migration, so a fresh database is ready for a presentation.
/// Seeded rows behave like any other data: once edited or deleted, they are not restored.
/// </summary>
public static class SeedData
{
    public static readonly Component[] Components =
    [
        new() { Id = 1, Name = "Resistor 10 kOhm", Description = "0603 thick-film chip resistor, 1 %, 0.1 W", Quantity = 24 },
        new() { Id = 2, Name = "Ceramic capacitor 100 nF", Description = "0402 MLCC, X7R, 16 V, decoupling", Quantity = 32 },
        new() { Id = 3, Name = "LED green", Description = "0805 SMD LED, 2.1 V forward voltage", Quantity = 4 },
        new() { Id = 4, Name = "Microcontroller STM32F103", Description = "LQFP-48, ARM Cortex-M3, 72 MHz", Quantity = 1 },
        new() { Id = 5, Name = "Voltage regulator AMS1117-3.3", Description = "SOT-223 LDO, 3.3 V, 1 A", Quantity = 2 },
    ];
}
