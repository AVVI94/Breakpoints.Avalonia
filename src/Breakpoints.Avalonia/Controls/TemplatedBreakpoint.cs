using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Metadata;
using AVVI94.Breakpoints.Avalonia.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AVVI94.Breakpoints.Avalonia.Controls;

/// <summary>
/// A control that can change its content based on the current breakpoint
/// </summary>
public class TemplatedBreakpoint : ContentControl, IObserver<object?>, IAddChild<BreakpointTemplate>
{
    /// <summary>
    /// Breakpoint provider resolved for this control.
    /// </summary>
    protected Layoutable? _breakpointProvider;
    IDisposable? _bindingDisposable;

    /// <summary>
    /// BreakpointTemplates StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<IList<BreakpointTemplate>> BreakpointTemplatesProperty =
        AvaloniaProperty.Register<TemplatedBreakpoint, IList<BreakpointTemplate>>(nameof(BreakpointTemplates), []);

    /// <summary>
    /// Gets or sets the BreakpointTemplates property.
    /// </summary>
    [Content]
    public IList<BreakpointTemplate> BreakpointTemplates
    {
        get => this.GetValue(BreakpointTemplatesProperty);
        set => SetValue(BreakpointTemplatesProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (Design.IsDesignMode)
        {
            var src = Breakpoints.FindDesignTimeParentWithDesignBreakpoint(this);
            BreakpointTemplate? template = BreakpointTemplates.First();
            if (src is not null)
            {
                var bp = Breakpoints.GetDesignCurrentBreakpoint(src);
                foreach (var item in BreakpointTemplates)             
                {
                    if (item.For == bp)
                    {
                        template = item;
                        break;
                    }
                }
            }
            Content = template?.ContentTemplate?.Build(null);
            return;
        }

        if (!Breakpoints.TryFindBreakpoints(this, out _, out _breakpointProvider))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Breakpoint not found, setting the Enabled property to true so this element is always visible.");
            return;
        }

        Debug.Assert(_breakpointProvider is not null);

        var binding = Controls.Breakpoints.CurrentBreakpointProperty.Bind().WithMode(BindingMode.OneWay);
        binding.Source = _breakpointProvider;
        _bindingDisposable = binding.Subscribe(this);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _bindingDisposable?.Dispose();
        _bindingDisposable = null;
    }

    /// <summary>
    /// Updates the currently visible breakpoint content based on the current breakpoint and the available templates in <see cref="BreakpointTemplates"/>.
    /// </summary>
    protected virtual void UpdateBreakpoint()
    {
        if (!IsEnabled || _breakpointProvider is null)
        {
            IsVisible = false;
            Content = null;
            InvalidateVisual();
            return;
        }

        var bps = Breakpoints.GetValues(_breakpointProvider);
        var current = Breakpoints.GetCurrentBreakpoint(_breakpointProvider);
        if (string.IsNullOrEmpty(current))
        {
            return;
        }
        double currentBp = bps.TryGetValue(current, out var val) ? val : 0;

        BreakpointTemplate? selectedTemplate = null;
        double highestBp = -1;
        double smallestBp = double.MaxValue;
        BreakpointTemplate? smallestTemplate = null;

        foreach (var template in BreakpointTemplates)
        {
            if (bps.Get(template.For) is double bp and not -1)
            {
                if (bp < smallestBp)
                {
                    smallestBp = bp;
                    smallestTemplate = template;
                }

                if (bp <= currentBp && bp > highestBp)
                {
                    selectedTemplate = template;
                    highestBp = bp;
                }
            }
        }

        selectedTemplate ??= smallestTemplate;

        Content = selectedTemplate?.ContentTemplate?.Build(null);
    }
    private Control? _current;

    /// <inheritdoc/>
    public void OnCompleted()
    {
        _bindingDisposable?.Dispose();
        _bindingDisposable = null;
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Error during breakpoint subscription. {Error}", error);
    }

    /// <inheritdoc/>
    public void OnNext(object? value)
    {
        UpdateBreakpoint();
    }

    /// <inheritdoc/>
    public void AddChild(BreakpointTemplate child)
    {
        this.BreakpointTemplates.Add(child);
    }
}

/// <summary>
/// A template for a breakpoint in a <see cref="TemplatedBreakpoint"/> control that can change its content based on the current breakpoint.
/// </summary>
public class BreakpointTemplate : AvaloniaObject
{
    /// <summary>
    /// For DirectProperty definition
    /// </summary>
    public static readonly DirectProperty<BreakpointTemplate, string> ForProperty =
        AvaloniaProperty.RegisterDirect<BreakpointTemplate, string>(nameof(For),
            o => o.For,
            (o, v) => o.For = v);

    private string _For = "";
    /// <summary>
    /// Gets or sets the For property. This DirectProperty 
    /// indicates the brakpoint that this template is for.
    /// </summary>
    public string For
    {
        get => _For;
        set => SetAndRaise(ForProperty, ref _For, value);
    }

    /// <summary>
    /// ContentTemplate StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<BreakpointTemplate, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>
    /// Gets or sets the ContentTemplate property.
    /// </summary>
    [Content]
    public IDataTemplate? ContentTemplate
    {
        get => this.GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }
}

static class Ext
{
    public static double Get(this BreakpointList b, string key)
    {
        return b.TryGetValue(key, out var value) ? value : -1;
    }
}
