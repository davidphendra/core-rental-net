using CoreRentalNet.Host.Infrastructure;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The one error shape the API answers with.
/// </summary>
/// <remarks>
/// Registering the problem-details service is what lets the exception handler and the status-code
/// middleware write a body at all, and it is where the members every problem carries are added, in
/// <see cref="ApiProblemDetails"/>. Where the middleware that uses it is mounted - and why it is
/// mounted for the API alone - is stated in <see cref="ApplicationPipeline"/>.
/// </remarks>
internal static class ApiResponseRegistration
{
    public static void AddApiResponses(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ApiProblemDetails.Customize);
    }
}
