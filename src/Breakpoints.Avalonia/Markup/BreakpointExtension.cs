using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Avalonia = global::Avalonia;
using BP = AVVI94.Breakpoints.Avalonia.Controls.Breakpoints;
using NsBreakpoints = AVVI94.Breakpoints.Avalonia;

namespace AVVI94.Breakpoints.Avalonia.Markup;

public class BreakpointExtension : MarkupExtension, IObservable<object>, IDisposable
{
    private readonly Subject<object> _subject = new();
    private Visual? _target;
    private Type? _targetType;
    private Visual? _provider;
    private string? _previousBreakpoint;
    private object? _previousValue;

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

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget))!;
        if (target.TargetProperty is not AvaloniaProperty ap)
        {
            throw new InvalidOperationException("Target property is not an AvaloniaProperty.");
        }
        _targetType = ap.PropertyType;

        SetTarget(serviceProvider, target);
        if (_target is null)
        {
            throw new InvalidOperationException("No Visual parent found for the target object.");
        }

        if (_target.IsAttachedToVisualTree())
        {
            _provider = BP.TryFindBreakpoints(_target, out _, out var breakpointProvider) ? breakpointProvider : null;
            if (_provider is null)
            {
                // If no provider is found, we set the default value to the target property
                Dispose();
                return AvaloniaProperty.UnsetValue;
            }

            _provider.PropertyChanged += Provider_PropertyChanged;
        }
        else
        {
            // If not attached, we use a TreeAttachmentNotifier to handle the attachment later
            _target.AttachedToVisualTree += Target_AttachedToVisualTree;
        }

        return this.ToBinding();
    }

    private void Target_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (_target is null)
        {
            return;
        }
        _target.AttachedToVisualTree -= Target_AttachedToVisualTree;
        _target.DetachedFromVisualTree += Target_DetachedFromVisualTree;

        if (Design.IsDesignMode)
        {
            // In design mode, we do not need to subscribe to the provider
            // We just set the default value based on the current design breakpoint
            var src = BP.FindDesignTimeParentWithDesignBreakpoint(_target);
            if (src is null)
            {
                NextValue("XS");
                Dispose();
                return;
            }
            NextValue(BP.GetDesignCurrentBreakpoint(src));
            Dispose();
            return;
        }

        _provider = BP.TryFindBreakpoints(_target, out _, out var breakpointProvider) ? breakpointProvider : null;

        if (_provider is null)
        {
            // If no provider is found, we set the default value to the target property
            _subject.OnNext(Default ?? AvaloniaProperty.UnsetValue);
            Dispose();
            return;
        }

        _provider.PropertyChanged += Provider_PropertyChanged;
        NextValue();
    }

    private void Target_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        Dispose();
    }

    private void Provider_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_provider is null)
        {
            return;
        }

        if (e.Property == BP.CurrentBreakpointProperty)
        {
            NextValue();
            return;
        }
    }

    protected virtual void NextValue()
    {
        Debug.Assert(_provider is not null);
        Debug.Assert(_target is not null);

        var currentBreakpoint = _provider!.GetValue(BP.CurrentBreakpointProperty);
        if (currentBreakpoint is null || currentBreakpoint == _previousBreakpoint)
        {
            _subject.OnNext(_previousValue ?? AvaloniaProperty.UnsetValue);
            return;
        }

        NextValue(currentBreakpoint);
    }

    protected void NextValue(string currentBreakpoint)
    {
        _previousBreakpoint = currentBreakpoint;
        var value = currentBreakpoint switch
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
            _subject.OnNext(AvaloniaProperty.UnsetValue);
            return;
        }

        _subject.OnNext(_previousValue = ConvertValue(_targetType!, value) ?? AvaloniaProperty.UnsetValue);
    }

    private void SetTarget(IServiceProvider serviceProvider, IProvideValueTarget target)
    {
        if (target.TargetObject is Visual visualTarget)
        {
            _target = visualTarget;
        }
        else
        {
            var parentStack = (IAvaloniaXamlIlParentStackProvider)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider))!;
            foreach (var parent in parentStack.Parents)
            {
                if (parent is Visual visualParent)
                {
                    _target = visualParent;
                    break;
                }
            }
        }
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

        return Converter is null ? v
        : Converter.Convert(v,
                            targetType,
                            ConverterParameter,
                            Dispatcher.UIThread.Invoke(() => Thread.CurrentThread.CurrentUICulture));
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<object> observer)
    {
        return _subject.Subscribe(observer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Design.IsDesignMode)
        {
            return;
        }
        _subject.OnCompleted();
        _subject.Dispose();
        if (_provider is not null)
        {
            _provider.PropertyChanged -= Provider_PropertyChanged;
            _provider = null;
        }
        if (_target is not null)
        {
            _target.AttachedToVisualTree -= Target_AttachedToVisualTree;
            _target.DetachedFromVisualTree -= Target_DetachedFromVisualTree;
            _target = null;
        }
    }
}
