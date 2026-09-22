using CoreRentalNet.Host.Infrastructure;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The one error shape the API answers with.
/// </summary>
/// <remarks>
/// Registering the problem-details service is what lets the exception handler and the status-code
/// middleware write a body at all, and it is where the members every problem carries are added, in
/// <see cref="ApiProblemDetails"/>. Where the middleware that uses it is mounted - and why it is
/// mounted for the API alone - is stated in <see cref="ApplicationPipelineExtentions"/>.
/// </remarks>
internal static class ApiResponseRegistrationExtentions
{
    public static void AddApiResponses(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ApiProblemDetails.Customize);

        // The one failure the API answers with a status of its own choosing: the vectors behind a similarityService
        // search are derived data, so their absence is a 503 a caller may retry rather than a 500.
        builder.Services.AddExceptionHandler<ProductSimilarityUnavailableHandler>();
    }
}
