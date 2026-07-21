using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GVC.Web.ModelBinding;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        ValueProviderResult valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        string value = valueResult.FirstValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(value))
        {
            bindingContext.Result = ModelBindingResult.Success(
                Nullable.GetUnderlyingType(bindingContext.ModelMetadata.ModelType) is not null ? null : 0m);
            return Task.CompletedTask;
        }

        value = value.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        // Com vírgula, respeita o formato brasileiro: 1.234,56.
        // Apenas com ponto, trata o ponto como decimal: 89.00 = 89,00.
        CultureInfo culture = value.Contains(',') ? PtBr : CultureInfo.InvariantCulture;
        if (decimal.TryParse(value, NumberStyles.Number, culture, out decimal parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "Informe um valor monetário válido, por exemplo 89,00.");
        return Task.CompletedTask;
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    private static readonly IModelBinder Binder = new FlexibleDecimalModelBinder();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Type modelType = context.Metadata.ModelType;
        return modelType == typeof(decimal) || modelType == typeof(decimal?) ? Binder : null;
    }
}
