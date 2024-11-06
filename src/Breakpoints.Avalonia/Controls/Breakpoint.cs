using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.VisualTree;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AVVI94.Breakpoints.Avalonia.Controls;

/// <summary>
/// A control that can be used to hide or show content based on the current breakpoint.
/// </summary>
public class Breakpoint : ContentControl
{
    /// <summary>
    /// Enabled StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<bool> EnabledProperty =
        AvaloniaProperty.Register<Breakpoint, bool>(nameof(Enabled), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the Enabled property.
    /// </summary>
    public bool Enabled
    {
        get => GetValue(EnabledProperty);
        set => SetValue(EnabledProperty, value);
    }

    /// <summary>
    /// For StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<string> ForProperty =
        AvaloniaProperty.Register<Breakpoint, string>(nameof(For));

    /// <summary>
    /// Gets or sets the For property.
    /// </summary>
    public string For
    {
        get => GetValue(ForProperty);
        set => SetValue(ForProperty, value);
    }

    /// <summary>
    /// UpperBound StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<string?> UpperBoundProperty =
        AvaloniaProperty.Register<Breakpoint, string?>(nameof(UpperBound));

    /// <summary>
    /// Gets or sets the UpperBound property.
    /// </summary>
    public string? UpperBound
    {
        get => GetValue(UpperBoundProperty);
        set => SetValue(UpperBoundProperty, value);
    }

    /// <summary>
    /// IsExclusive StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<bool> IsExclusiveProperty =
        AvaloniaProperty.Register<Breakpoint, bool>(nameof(IsExclusive));

    /// <summary>
    /// Gets or sets the IsExclusive property.
    /// </summary>
    public bool IsExclusive
    {
        get => GetValue(IsExclusiveProperty);
        set => SetValue(IsExclusiveProperty, value);
    }

    Layoutable? _breakpointProvider;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (string.IsNullOrEmpty(For))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "The For property must be set to a valid value.");
            throw new InvalidOperationException("The For property must be set to a valid breakpoint value.");
        }

        if (!Breakpoints.TryFindBreakpoints(this, out _, out _breakpointProvider))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Breakpoint not found, setting the Enabled property to true so this element is always visible.");
            Enabled = true;
            return;
        }

        Debug.Assert(_breakpointProvider is not null);

        _breakpointProvider!.PropertyChanged += OnParentPropertyChanged;
        PropertyChanged += Breakpoint_PropertyChanged;
    }

    private void Breakpoint_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Enabled))
        {
            UpdateBreakpoint(Width);
        }
    }

    private void OnParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(Width))
        {
            return;
        }

        UpdateBreakpoint((double)e.NewValue!);
    }

    private void UpdateBreakpoint(double width)
    {
        if (!Enabled)
        {
            IsVisible = true;
            InvalidateVisual();
            return;
        }
        if (!string.IsNullOrEmpty(UpperBound))
        {
            IsVisible = Breakpoints.IsBetween(this, For, UpperBound!);
            return;
        }
        IsVisible = Breakpoints.ShouldBeVisible(this, For, IsExclusive);
    }
}