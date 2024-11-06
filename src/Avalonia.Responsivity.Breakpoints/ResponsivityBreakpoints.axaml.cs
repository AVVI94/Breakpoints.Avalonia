using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Responsivity.Breakpoints;

public class ResponsivityBreakpoints : Styles
{
    public ResponsivityBreakpoints()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public ResponsivityBreakpoints(IResourceHost owner) : base(owner)
    {
        AvaloniaXamlLoader.Load(this);
    }
}
