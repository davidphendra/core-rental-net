using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.Host.Composition;

/// <summary>The shared building-block services.</summary>
internal static class BuildingBlocksRegistration
{
    public static void AddBuildingBlocks(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IOpaqueTokenService, OpaqueTokenService>();
        builder.Services.AddSingleton<IMoneyService, MoneyService>();
    }
}
