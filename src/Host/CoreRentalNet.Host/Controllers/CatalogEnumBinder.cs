using CoreRentalNet.Host.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// Reads one catalogue filter from the query string, accepting only the words the catalogue publishes.
/// </summary>
/// <remarks>
/// A model binder rather than a check inside the action, so a mistyped filter is a model-state error
/// and the framework answers it: <c>[ApiController]</c> refuses the request with a problem-details
/// <c>400</c>, and the action is never entered. The action therefore has no failure branch to write
/// and none to forget.
/// </remarks>
internal sealed class CatalogEnumBinder<TEnum> : IModelBinder
    where TEnum : struct, Enum
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var sent = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

        if (string.IsNullOrWhiteSpace(sent))
        {
            // Left out, which asks for the whole catalogue: bound as nothing, not as the first member
            // of the enum, which is what a default value would silently mean.
            bindingContext.Result = ModelBindingResult.Success(null);

            return Task.CompletedTask;
        }

        if (CatalogApiParameters.TryMatch<TEnum>(sent, out var matched))
        {
            bindingContext.Result = ModelBindingResult.Success(matched);

            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            CatalogApiParameters.Refusal(bindingContext.ModelName, sent, CatalogApiParameters.Names<TEnum>()));

        // Named, so the refusal that reaches the caller says which filter was wrong rather than only
        // that the request was refused.
        ApiErrorCode.Set(bindingContext.HttpContext, ApiErrorCode.UnknownFilter);

        return Task.CompletedTask;
    }
}
