using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Metadata;
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
public partial class Breakpoint : ContentControl, IDisposable
{
    private bool _isDisposed;
    Layoutable? _breakpointProvider;

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

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (string.IsNullOrEmpty(For))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "The For property must be set to a valid value.");
            throw new InvalidOperationException("The For property must be set to a valid breakpoint value.");
        }

        if (Design.IsDesignMode)
        {
            var src = Breakpoints.FindDesignTimeParentWithDesignBreakpoint(this);
            if (src is null)
            {
                IsVisible = Enabled = true;
                return;
            }
            IsVisible = Breakpoints.GetDesignCurrentBreakpoint(src) == For;
            return;
        }

        if (!Breakpoints.TryFindBreakpoints(this, out _, out _breakpointProvider))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Breakpoint not found, setting the Enabled property to true so this element is always visible.");
            Enabled = true;
            return;
        }

        Debug.Assert(_breakpointProvider is not null);

        _breakpointProvider!.PropertyChanged += BreakpointProvider_PropertyChanged;
        UpdateBreakpoint();
    }

    private void BreakpointProvider_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Breakpoints.CurrentBreakpointProperty)
        {
            UpdateBreakpoint();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property.Name == nameof(Enabled))
        {
            UpdateBreakpoint();
        }
    }

    private void UpdateBreakpoint()
    {
        if (!Enabled)
        {
            IsVisible = false;
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

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Breakpoint"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // dispose managed resources here
            if (_breakpointProvider != null)
            {
                _breakpointProvider.PropertyChanged -= BreakpointProvider_PropertyChanged;
            }
        }

        // dispose unmanaged resources here
        _breakpointProvider = null;
        _isDisposed = true;
    }
}