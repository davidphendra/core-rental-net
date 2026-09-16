using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CoreRentalNet.Host.Controllers;

/// <summary>
/// Supplies the catalogue's own binder for the catalogue's own filter types, and for nothing else.
/// </summary>
/// <remarks>
/// The two types are named rather than matched by shape ("every enum in the application"). An enum
/// added elsewhere has not been published as a filter, and binding it here would answer a caller with
/// a refusal message about a vocabulary that type does not have.
/// </remarks>
internal sealed class CatalogEnumBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        if (type != typeof(CatalogCategory) && type != typeof(CatalogSubCategory))
        {
            return null;
        }

        return (IModelBinder)Activator.CreateInstance(typeof(CatalogEnumBinder<>).MakeGenericType(type))!;
    }
}
