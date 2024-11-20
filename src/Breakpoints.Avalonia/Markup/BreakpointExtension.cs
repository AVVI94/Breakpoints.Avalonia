using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Threading;

namespace AVVI94.Breakpoints.Avalonia.Markup;

/// <summary>
/// Markup extension to provide values based on the current breakpoint
/// </summary>
public class BreakpointExtension : MarkupExtension
{
    /// <summary>
    /// Create a new instance of BreakpointExtension
    /// </summary>
    /// <param name="def">Smalles possible default value</param>
    public BreakpointExtension(object def)
    {
        Default = def;
    }
    /// <summary>
    /// Create a new instance of BreakpointExtension
    /// </summary>
    public BreakpointExtension()
    {
    }

    /// <summary>
    /// Value for breakpoint XS
    /// </summary>
    public object? XS { get; set; }
    /// <summary>
    /// Value for breakpoint S
    /// </summary>
    public object? S { get; set; }
    /// <summary>
    /// Value for breakpoint M
    /// </summary>
    public object? M { get; set; }
    /// <summary>
    /// Value for breakpoint L
    /// </summary>
    public object? L { get; set; }
    /// <summary>
    /// Value for breakpoint XL
    /// </summary>
    public object? XL { get; set; }
    /// <summary>
    /// Value for breakpoint XXL
    /// </summary>
    public object? XXL { get; set; }
    /// <summary>
    /// Smalles possible value when no other breakpoint is defined
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// Value converter
    /// </summary>
    public IValueConverter? Converter { get; set; }
    /// <summary>
    /// Value converter parameter
    /// </summary>
    public object? ConverterParameter { get; set; }

    object? _previousValue = AvaloniaProperty.UnsetValue;
    string _previousBreakpoint = "";

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
        var targetProperty = target.TargetProperty as AvaloniaProperty;
        var targetType = (targetProperty?.PropertyType)
            ?? throw new InvalidOperationException("The target property is not an AvaloniaProperty.");

        if (Design.IsDesignMode)
        {
            return ConvertValue(targetType, XXL ?? XL ?? L ?? M ?? S ?? XS ?? Default!)!;
        }

        if (target.TargetObject is not Visual visual
            || !Controls.Breakpoints.TryFindBreakpointProvider(visual, out var bpProv))
        {
            // If the target object is not a Visual, to find first parent Visual that is a BreakpointProvider
            // If this fails I have no idea what to do, so I return UnsetValue
            var parents = (IAvaloniaXamlIlParentStackProvider)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider))!;
            if (parents.Parents.Any())
            {
                foreach (var p in parents.Parents)
                {
                    if (p is Visual v && Controls.Breakpoints.TryFindBreakpointProvider(v, out bpProv))
                    {
                        visual = v;
                        goto BindingSetup;
                    }
                }
            }
            return AvaloniaProperty.UnsetValue;
        }

    BindingSetup:
        var valuesBinding = Controls.Breakpoints.ValuesProperty.Bind();
        valuesBinding.Source = bpProv;
        var currentBpBinding = Controls.Breakpoints.CurrentBreakpointProperty.Bind();
        currentBpBinding.Source = bpProv;
        return new MultiBinding()
        {
            Bindings =
            {
                valuesBinding.ToBinding(),
                currentBpBinding.ToBinding(),
                new Binding("Width") { Source = bpProv }
            },
            Converter = new FuncMultiValueConverter<object, object?>(w =>
            {
                var prov = bpProv!;
                var bps = Controls.Breakpoints.GetValuesActual(prov);
                var current = Controls.Breakpoints.GetCurrentBreakpoint(prov);
                if (current is null || !(bps?.Items.ContainsKey(current) ?? false))
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(prov, "The current breakpoint '{Current}' is not defined in the Breakpoints.Values on provider {Provider}.", current, prov);
                    return AvaloniaProperty.UnsetValue;
                }

                if (current == _previousBreakpoint)
                    return _previousValue;
                _previousBreakpoint = current;
                var value = current switch
                {
                    "XS" => XS ?? Default,
                    "S" => S ?? XS ?? Default,
                    "M" => M ?? S ?? XS ?? Default,
                    "L" => L ?? M ?? S ?? XS ?? Default,
                    "XL" => XL ?? L ?? M ?? S ?? XS ?? Default,
                    "XXL" => XXL ?? XL ?? L ?? M ?? S ?? XS ?? Default,
                    _ => AvaloniaProperty.UnsetValue
                };

                if (value == AvaloniaProperty.UnsetValue || value is null)
                {
                    return _previousValue = Converter is not null ? Converter?.Convert(value,
                                                                                      targetType,
                                                                                      ConverterParameter,
                                                                                      Dispatcher.UIThread.Invoke(() => Thread.CurrentThread.CurrentUICulture))
                    : AvaloniaProperty.UnsetValue;
                }

                return ConvertValue(targetType, value);
            })
        };
    }

    private object? ConvertValue(Type targetType, object value)
    {
        object? v;
        if (targetType == typeof(double))
        {
            _ = double.TryParse(value as string ?? value.ToString(), out var d) ? v = d : v = 0;
        }
        else if (targetType == typeof(int))
        {
            _ = int.TryParse(value as string ?? value.ToString(), out var i) ? v = i : v = 0;
        }
        else if (targetType == typeof(float))
        {
            _ = float.TryParse(value as string ?? value.ToString(), out var f) ? v = f : v = 0;
        }
        else if (targetType == typeof(bool))
        {
            _ = bool.TryParse(value as string ?? value.ToString(), out var b) ? v = b : v = false;
        }
        else if (targetType == typeof(GridLength))
        {
            v = GridLength.Parse(value as string ?? value.ToString() ?? "*");
        }
        else if (targetType.IsEnum)
        {
            v = Enum.Parse(targetType, value as string ?? value.ToString());
        }
        else
        {
            v = value;
        }


        return _previousValue = Converter is not null ? Converter?.Convert(v,
                                                                          targetType,
                                                                          ConverterParameter,
                                                                          Dispatcher.UIThread.Invoke(() => Thread.CurrentThread.CurrentUICulture))
        : v;
    }
}
