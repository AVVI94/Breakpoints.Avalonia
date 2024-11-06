using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading;

namespace AVVI94.Breakpoints.Avalonia.Markup;

public class BreakpointExtension : MarkupExtension
{
    public BreakpointExtension(object def)
    {
        Default = def;
    }
    public BreakpointExtension()
    {
    }

    public object? XS { get; set; }
    public object? S { get; set; }
    public object? M { get; set; }
    public object? L { get; set; }
    public object? XL { get; set; }
    public object? XXL { get; set; }
    public object? Default { get; set; }

    public IValueConverter? Converter { get; set; }
    public object? ConverterParameter { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
        var targetProperty = target.TargetProperty as AvaloniaProperty;
        var targetType = (targetProperty?.PropertyType)
            ?? throw new InvalidOperationException("The target property is not an AvaloniaProperty.");

        if (target.TargetObject is not Visual visual
            || !Controls.Breakpoints.TryFindBreakpointProvider(visual, out var bpProv))
            return AvaloniaProperty.UnsetValue;

        string previousBreakpoint = "";
        object? previousValue = AvaloniaProperty.UnsetValue;

        return new Binding("Width", BindingMode.OneWay)
        {
            Source = bpProv,
            Converter = new FuncValueConverter<double, object?>(w =>
            {
                var prov = bpProv!;
                var bps = Controls.Breakpoints.GetValues(prov);
                var current = Controls.Breakpoints.GetCurrentBreakpoint(prov);
                if (!(bps?.Items.ContainsKey(current) ?? false))
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(prov, "The current breakpoint '{Current}' is not defined in the Breakpoints.Values on provider {Provider}.", current, prov);
                    return AvaloniaProperty.UnsetValue;
                }

                if (current == previousBreakpoint)
                    return previousValue;
                previousBreakpoint = current;
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
                    return previousValue = Converter is not null ? Converter?.Convert(value,
                                                                                      targetType,
                                                                                      ConverterParameter,
                                                                                      Dispatcher.UIThread.Invoke(() => Thread.CurrentThread.CurrentUICulture))
                    : AvaloniaProperty.UnsetValue;
                }


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


                return previousValue = Converter is not null ? Converter?.Convert(v,
                                                                                  targetType,
                                                                                  ConverterParameter,
                                                                                  Dispatcher.UIThread.Invoke(() => Thread.CurrentThread.CurrentUICulture))
                : v;
            })
        };
    }
}
