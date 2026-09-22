using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;
using CoreRentalNet.Modules.Rentals.Application.Rules;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Host.Extentions;

/// <summary>The orders, the invoices and the run that moves them forward with time.</summary>
internal static class RentalsRegistrationExtentions
{
    public static void AddRentals(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RegisterPersistence(builder);
        RegisterSettings(builder);
        RegisterRules(builder);
        RegisterUseCases(builder);
        RegisterScheduler(builder);
    }

    private static void RegisterPersistence(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<RentalsContext>((provider, options) =>
        {
            RentalsPersistence.Configure(options, provider.GetRequiredService<SqliteDatabaseSettings>());
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
        });

        builder.Services.AddScoped<IRentalRepository, RentalRepository>();
        builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        builder.Services.AddScoped<IUnitOfWork, RentalsUnitOfWork>();
        builder.Services.AddScoped<INumberSequence, SqliteNumberSequence>();
    }

    private static void RegisterSettings(WebApplicationBuilder builder)
        => builder.Services.AddSingleton(new RentalsSettings(
            new Money(builder.Configuration.GetValue("Rentals:DeliveryFeeAmount", 750_000m), Currencies.Idr),
            builder.Configuration.GetValue("Rentals:TaxRate", 0m)));

    private static void RegisterRules(WebApplicationBuilder builder)
    {
        // The order's rules, in services. The records hold data; these hold everything
        // that used to live on the aggregates and the static policies.
        builder.Services.AddScoped<IRenewalPolicyService, RenewalPolicyService>();
        builder.Services.AddScoped<IDeliveryPolicyService, DeliveryPolicyService>();
        builder.Services.AddScoped<ICancellationPolicyService, CancellationPolicyService>();
        builder.Services.AddScoped<IRentalLifecycleService, RentalLifecycleService>();
        builder.Services.AddScoped<IInvoiceService, InvoiceService>();
        builder.Services.AddScoped<IRenewalInvoiceIssuer, RenewalInvoiceIssuer>();
    }

    private static void RegisterUseCases(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IPlaceOrder, PlaceOrderService>();
        builder.Services.AddScoped<IConvertWorkspaceToOrder, ConvertWorkspaceToOrder>();
        builder.Services.AddScoped<ICheckoutConfirmation, CheckoutConfirmation>();
        builder.Services.AddScoped<ICheckoutCommandHandler, CheckoutCommandHandler>();

        // By the operation, not by the handler's own type: the page asks for an order and its
        // invoices, and how they are assembled is the application's business.
        builder.Services.AddScoped<IGetRentalByTokenHandler, GetRentalByTokenHandler>();
        builder.Services.AddScoped<IGetInvoicesByTokenHandler, GetInvoicesByTokenHandler>();
    }

    private static void RegisterScheduler(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IRunRentalSchedule, RentalScheduler>();
        builder.Services.AddSingleton(SchedulerSettings.FromMinutes(
            builder.Configuration.GetValue("Rentals:SchedulerIntervalMinutes", 60)));
        builder.Services.AddHostedService<RenewalSchedulerHostedService>();
    }
}
