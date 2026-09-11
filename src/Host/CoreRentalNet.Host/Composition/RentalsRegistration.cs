using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Queries;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using CoreRentalNet.Modules.Rentals.Domain;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Composition;

/// <summary>The orders, the invoices and the run that moves them forward with time.</summary>
internal static class RentalsRegistration
{
    public static void AddRentals(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<RentalsContext>((provider, options) =>
        {
            RentalsPersistence.Configure(options, provider.GetRequiredService<SqliteDatabaseSettings>());
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
        });

        builder.Services.AddSingleton(new RentalsSettings(
            Money.Idr(builder.Configuration.GetValue("Rentals:DeliveryFeeAmount", 750_000m)),
            builder.Configuration.GetValue("Rentals:TaxRate", 0m)));

        builder.Services.AddScoped<IRentalRepository, RentalRepository>();
        builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        builder.Services.AddScoped<IUnitOfWork, RentalsUnitOfWork>();
        builder.Services.AddScoped<INumberSequence, SqliteNumberSequence>();

        builder.Services.AddScoped<IPlaceOrder, PlaceOrderService>();
        builder.Services.AddScoped<IConvertWorkspaceToOrder, ConvertWorkspaceToOrder>();
        builder.Services.AddScoped<ICheckout, CheckoutService>();

        builder.Services.AddScoped<IRunRentalSchedule, RentalScheduler>();
        builder.Services.AddSingleton(SchedulerSettings.FromMinutes(
            builder.Configuration.GetValue("Rentals:SchedulerIntervalMinutes", 60)));
        builder.Services.AddHostedService<RenewalSchedulerHostedService>();

        builder.Services.AddScoped<GetRentalByTokenHandler>();
        builder.Services.AddScoped<GetInvoicesByTokenHandler>();
    }
}
