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

namespace AVVI94.Breakpoints.Avalonia.Controls;

/// <summary>
/// Breakpoints AttachedProperty definition and helper methods.
/// </summary>
public class Breakpoints
{
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
        element.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(element.Width))
            {
                var vals = GetValues(element);
                if(vals is null)
                {
                    return;
                }
                if (vals.FindPrevious((double)e.NewValue!) is KeyValuePair<string, double> current)
                {
                    SetCurrentBreakpoint(element, current.Key);
                    return;
                }
                if (vals.FindNext((double)e.NewValue!) is KeyValuePair<string, double> next)
                {
                    SetCurrentBreakpoint(element, next.Key);
                    return;
                }
            }
        };

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
    public static bool TryFindBreakpointProvider(Visual? element, out Layoutable? provider)
    {
        if (element is null)
        {
            provider = null;
            return false;
        }

        Visual? parentV = element.GetLogicalParent() as Visual;
        while ((parentV = parentV?.GetLogicalParent() as Visual) is not null)
        {
            if (parentV is Layoutable layoutable && GetIsBreakpointProvider(layoutable))
            {
                break;
            }
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
    public static bool TryFindBreakpoints(Visual? element, out BreakpointList? breakpoints, out Layoutable? provider)
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
            if (parentV is Layoutable layoutable && GetIsBreakpointProvider(layoutable))
            {
                break;
            }
        }
        Debug.Assert(parentV is Layoutable);

        Layoutable parent = (Layoutable)parentV!;
        if (parent is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Visual)?.Log(element, "No breakpoint provider found.");
            breakpoints = null;
            provider = null;
            return false;
        }

        breakpoints = GetValues(parent);
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
        if (!TryFindBreakpoints(element, out var breakpoints, out var provider))
        {
            return true;
        }
        var bps = GetValues(provider!);
        if (!bps.TryGetValue(breakpoint, out var value))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(element, "Breakpoint value for '{For}' not found at breakpoint provider {Provider}.", breakpoint, provider);
            return true;
        }
        var width = provider!.Bounds.Width;
        if (width <= value && bps.FindPrevious(width) is null && bps.FindPrevious(value) is null)
        {
            return true;
        }

        if (width <= value)
        {
            return false;
        }

        if (exclusive)
        {
            var bigger = bps.FindNext(value);
            if (bigger is not null)
            {
                return width < bigger.Value.Value;
            }
        }

        return true;
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
        if (!TryFindBreakpoints(element, out var breakpoints, out var provider))
        {
            return true;
        }
        var bps = GetValues(provider!);
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
        var width = provider!.Bounds.Width;
        //return width >= lowerValue && width <= upperValue;
        return ShouldBeVisible(element, lower) && width <= (bps.FindNext(upperValue)?.Value ?? upperValue);
    }
}