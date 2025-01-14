using Avalonia;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AVVI94.Breakpoints.Avalonia.Collections;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using System.Xml.Linq;

namespace AVVI94.Breakpoints.Avalonia.Controls;

/// <summary>
/// Breakpoints AttachedProperty definition and helper methods.
/// </summary>
public class Breakpoints
{
    static Breakpoints()
    {
        ValuesProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<BreakpointList>>(static x =>
        {
            if (x.Sender is not StyledElement s)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(x.Sender, "Breakpoints.Values can only be set on StyledElement.");
                return;
            }
        }));

        IsBreakpointProviderProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(static x =>
        {
            if (x.Sender is not Layoutable l)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(x.Sender, "Breakpoints.IsBreakpointProvider can only be set on Layoutable.");
                return;
            }
            if (l.GetValue(IsProxyProperty))
            {
                return;
            }
            l.PropertyChanged += TargetPropertyChanged;
        }));

        IsProxyProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(static p =>
        {
            if (p.Sender is not Layoutable l)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(p.Sender, "Breakpoints.IsProxy can only be set on Layoutable.");
                return;
            }
            if (p.NewValue.Value is true)
            {
                l.PropertyChanged -= TargetPropertyChanged;
            }
            else
            {
                l.PropertyChanged += TargetPropertyChanged;
            }
        }));
    }



    /// <summary>
    /// Breakpoints AttachedProperty definition
    /// indicates the breakpoints dictionary.
    /// </summary>
    public static readonly AttachedProperty<BreakpointList> ValuesProperty =
        AvaloniaProperty.RegisterAttached<Breakpoints, Layoutable, BreakpointList>("Values");

    /// <summary>
    /// Accessor for Attached property <see cref="ValuesProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    /// <param name="value">The value to set  <see cref="ValuesProperty"/>.</param>
    public static void SetValues(Layoutable element, BreakpointList value) =>
        element.SetValue(ValuesProperty, value);

    /// <summary>
    /// Accessor for Attached property <see cref="ValuesProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    public static BreakpointList GetValues(Layoutable element) =>
        element.GetValue(ValuesProperty);


    /// <summary>
    /// IsBreakpointProvider AttachedProperty definition
    /// indicates whether the element should provide breakpoints or not.
    /// </summary>
    public static readonly AttachedProperty<bool> IsBreakpointProviderProperty =
        AvaloniaProperty.RegisterAttached<Breakpoints, Layoutable, bool>("IsBreakpointProvider");

    /// <summary>
    /// Accessor for Attached property <see cref="IsBreakpointProviderProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    /// <param name="value">The value to set  <see cref="IsBreakpointProviderProperty"/>.</param>
    public static void SetIsBreakpointProvider(Layoutable element, bool value)
    {
        element.SetValue(IsBreakpointProviderProperty, value);
        if (value)
        {
            if (GetValues(element) is null)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "Element is set to be a breakpoint provider but does not have set the Breakpoints.Values.");
            }
        }
    }

    /// <summary>
    /// Accessor for Attached property <see cref="IsBreakpointProviderProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    public static bool GetIsBreakpointProvider(Layoutable element) =>
        element.GetValue(IsBreakpointProviderProperty);


    /// <summary>
    /// CurrentBreakpoint AttachedProperty definition
    /// indicates the current breakpoint of the breakpoint provider.
    /// </summary>
    public static readonly AttachedProperty<string> CurrentBreakpointProperty =
        AvaloniaProperty.RegisterAttached<Breakpoints, Layoutable, string>("CurrentBreakpoint");

    /// <summary>
    /// Accessor for Attached property <see cref="CurrentBreakpointProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    /// <param name="value">The value to set  <see cref="CurrentBreakpointProperty"/>.</param>
    public static void SetCurrentBreakpoint(Layoutable element, string value) =>
        element.SetValue(CurrentBreakpointProperty, value);

    /// <summary>
    /// Accessor for Attached property <see cref="CurrentBreakpointProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    public static string GetCurrentBreakpoint(Layoutable element) =>
        element.GetValue(CurrentBreakpointProperty);


    /// <summary>
    /// IsProxy AttachedProperty definition
    /// indicates ....
    /// </summary>
    public static readonly AttachedProperty<bool> IsProxyProperty =
        AvaloniaProperty.RegisterAttached<Breakpoints, Layoutable, bool>("IsProxy");

    /// <summary>
    /// Accessor for Attached property <see cref="IsProxyProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    /// <param name="value">The value to set  <see cref="IsProxyProperty"/>.</param>
    public static void SetIsProxy(Layoutable element, bool value) =>
        element.SetValue(IsProxyProperty, value);

    /// <summary>
    /// Accessor for Attached property <see cref="IsProxyProperty"/>.
    /// </summary>
    /// <param name="element">Target element</param>
    public static bool GetIsProxy(Layoutable element) =>
        element.GetValue(IsProxyProperty);

    /// <summary>
    /// Try to find the breakpoint provider of the specified element.
    /// </summary>
    /// <param name="element">
    /// The element to find the breakpoint provider for.
    /// </param>
    /// <param name="provider">
    /// The breakpoint provider of the specified element.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint provider was found, otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryFindBreakpointProvider([NotNullWhen(true)] Visual? element, [NotNullWhen(true)] out Layoutable? provider)
    {
        if (element is null)
        {
            provider = null;
            return false;
        }

        Visual? parentV = element.GetLogicalParent() as Visual;
        do
        {
            if (parentV is Layoutable layoutable && (layoutable.GetValue(IsBreakpointProviderProperty) || layoutable.GetValue(IsProxyProperty)))// GetIsBreakpointProvider(layoutable))
            {
                break;
            }

        }
        while ((parentV = parentV?.GetLogicalParent() as Visual) is not null);


        if (parentV is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "No breakpoint provider found.");
            provider = null;
            return false;
        }
        Debug.Assert(parentV is Layoutable);

        Layoutable parent = (Layoutable)parentV!;
        if (parent is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "No breakpoint provider found.");
            provider = null;
            return false;
        }

        provider = parent;
        return true;
    }

    /// <summary>
    /// Try to find the breakpoints of the specified element.
    /// </summary>
    /// <param name="element">
    /// The element to find the breakpoints for.
    /// </param>
    /// <param name="breakpoints">
    /// The breakpoints of the specified element.
    /// </param>
    /// <param name="provider">
    /// [OUT] The breakpoint provider of the specified element.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the breakpoints were found, otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryFindBreakpoints([NotNullWhen(true)] Visual? element, [NotNullWhen(true)] out BreakpointList? breakpoints, [NotNullWhen(true)] out Layoutable? provider)
    {
        if (element is null)
        {
            breakpoints = null;
            provider = null;
            return false;
        }

        if (!element.IsAttachedToVisualTree())
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(element, "Element is not attached to the visual tree.");
            breakpoints = null;
            provider = null;
            return false;
        }

        Visual? parentV = element.GetLogicalParent() as Visual;
        while ((parentV = parentV?.GetLogicalParent() as Visual) is not null)
        {
            if (parentV is Layoutable layoutable && (layoutable.GetValue(IsBreakpointProviderProperty) || layoutable.GetValue(IsProxyProperty)))
            {
                break;
            }
        }
        //Debug.Assert(parentV is Layoutable);
        if (parentV is not Layoutable parent)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "No breakpoint provider found.");
            breakpoints = null;
            provider = null;
            return false;
        }

        if (parent is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "No breakpoint provider found.");
            breakpoints = null;
            provider = null;
            return false;
        }

        breakpoints = parent.GetValue(ValuesProperty);
        if (breakpoints is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(parent, "Element is set to be a breakpoint provider but does not have set the Breakpoints.Values.");
            provider = null;
            return false;
        }
        provider = parent;
        return true;
    }

    /// <summary>
    /// Check if the element should be visible at the specified breakpoint.
    /// </summary>
    /// <param name="element">
    /// The element to check the visibility for.
    /// </param>
    /// <param name="breakpoint">
    /// The breakpoint to check the visibility for.
    /// </param>
    /// <param name="exclusive">
    /// If <see langword="true"/> the element should be visible only at the specified breakpoint.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element should be visible, otherwise <see langword="false"/>.
    /// </returns>
    public static bool ShouldBeVisible(Visual element, string breakpoint, bool exclusive = false)
    {
        if (!TryFindBreakpoints(element, out var bps, out var provider))
        {
            return true;
        }
        if (!bps.TryGetValue(breakpoint, out var value))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(element, "Breakpoint value for '{For}' not found at breakpoint provider {Provider}.", breakpoint, provider);
            return true;
        }

        var current = provider.GetValue(CurrentBreakpointProperty);
        if (current == breakpoint || current is null)
        {
            return true;
        }

        //if(value < bps[current] && bps.FindPrevious(value) is null)
        //{
        //    return true;
        //}

        if (bps[current] > value)
        {
            var bigger = bps.FindNext(value);
            if (bigger is not null && exclusive)
            {
                return bps[current] < bigger.Value.Value;
            }
            return true;
        }

        //var width = provider!.Bounds.Width;
        //if (width <= value && bps.FindPrevious(width) is null && bps.FindPrevious(value) is null)
        //{
        //    return true;
        //}

        //if (width <= value)
        //{
        //    return false;
        //}

        //if (exclusive)
        //{
        //    var bigger = bps.FindNext(value);
        //    if (bigger is not null)
        //    {
        //        return width < bigger.Value.Value;
        //    }
        //}

        return false;
    }

    /// <summary>
    /// Check if the element should be visible between the specified breakpoints.
    /// </summary>
    /// <param name="element">
    /// The element to check the visibility for.
    /// </param>
    /// <param name="lower">
    /// The lower breakpoint.
    /// </param>
    /// <param name="upper">
    /// The upper breakpoint.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element should be visible, otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsBetween(Visual element, string lower, string upper)
    {
        if (!TryFindBreakpoints(element, out _, out var provider) || provider.GetValue(CurrentBreakpointProperty) is not string current)
        {
            return true;
        }
        var bps = provider.GetValue(ValuesProperty);
        if (!bps.TryGetValue(lower, out var lowerValue))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(element, "Breakpoint value for '{For}' not found at breakpoint provider {Provider}.", lower, provider);
            return true;
        }
        if (!bps.TryGetValue(upper, out var upperValue))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(element, "Breakpoint value for '{For}' not found at breakpoint provider {Provider}.", upper, provider);
            return true;
        }
        var width = bps[current];
        //return width >= lowerValue && width <= upperValue;
        //return ShouldBeVisible(element, lower) && width <= (bps.FindNext(upperValue)?.Value ?? upperValue);
        var lowerVisible = ShouldBeVisible(element, lower);
        var upperVisible = width < (bps.FindNext(upperValue)?.Value ?? upperValue);
        return lowerVisible && upperVisible;
    }

    private static void TargetPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Layoutable element)
        {
            return;
        }
        if (e.Property.Name == nameof(element.Width) && e.NewValue is not null)
        {
            UpdateCurrentBreakpoint(element, (double)e.NewValue!);
        }
        if (e.Property.Name == nameof(element.Bounds) && e.NewValue is not null)
        {
            UpdateCurrentBreakpoint(element, ((Rect)e.NewValue!).Width);
        }

        static void UpdateCurrentBreakpoint(Layoutable element, double newValue)
        {
            var vals = element.GetValue(ValuesProperty);
            if (vals is null)
            {
                return;
            }

            if (vals.FindCurrent(newValue) is KeyValuePair<string, double> crnt)
            {
                SetCurrentBreakpoint(element, crnt.Key);
                return;
            }
            //fallback
            if (vals.FindPrevious(newValue) is KeyValuePair<string, double> current)
            {
                SetCurrentBreakpoint(element, current.Key);
                return;
            }
            if (element.GetValue(CurrentBreakpointProperty) is string bp && vals[bp] == newValue && vals.FindPrevious(newValue) is null)
            {
                return;
            }
            if (vals.FindNext(newValue) is KeyValuePair<string, double> next)
            {
                SetCurrentBreakpoint(element, next.Key);
                return;
            }

        }
    }
}