# Breakpoints.Avalonia

[![Nuget](https://img.shields.io/nuget/v/AVVI94.Breakpoints.Avalonia)](https://www.nuget.org/packages/AVVI94.Breakpoints.Avalonia) [![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](https://raw.githubusercontent.com/AVVI94/Breakpoints.Avalonia/master/LICENSE)

`Breakpoints.Avalonia` is a library for [Avalonia UI](https://avaloniaui.net/) that provides responsive design capabilities using breakpoints. It allows you to define different UI layouts and behaviors based on the size of the application window (or any other element!).

![](https://github.com/AVVI94/Breakpoints.Avalonia/raw/master/gif.gif)

The package should be compatible with Avalonia >= 11.0.10.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Setup](#setup)
- [Defining Breakpoints and Breakpoint Providers](#defining-breakpoints-and-breakpoint-providers)
- [Using Breakpoints](#using-breakpoints)
  - [Using the Breakpoint Control](#using-the-breakpoint-control)
  - [Using the Breakpoint Markup Extension](#using-the-breakpoint-markup-extension)
- [Edge Cases and errors](#edge-cases-and-errors)
- [License](#license)

## Features

- User-defined breakpoints for different screen sizes.
- Use markup extensions to apply different values based on the current breakpoint - predefined breakpoint names only.
- Support for exclusive breakpoints and upper bounds.

## Installation

NuGet package:

```
dotnet add package AVVI94.Breakpoints.Avalonia
```

[Link](https://www.nuget.org/packages/AVVI94.Breakpoints.Avalonia)

## Setup

I tried to make using this library as simple as possible. All you have to do is the following:

1) [Setup App.xaml](#appxaml)
2) [Define and set breakpoints](#defining-breakpoints-and-breakpoint-providers)
3) [Define breakpoints provider](#defining-breakpoints-and-breakpoint-providers)
4) [Use them](#using-breakpoints)

### App.xaml

In your `App.axaml` file, define namespace and add the `ResponsivityBreakpoints` resource:

(If you also reference the AVVI94.Breakpoints.Avalonia.Xmlns package, you can omit the xaml namespace definition)

```xml
xmlns:avvi="using:AVVI94.Breakpoints.Avalonia"
```

```xml
<Application.Styles>
    <FluentTheme />
	<avvi:ResponsivityBreakpoints />
</Application.Styles>	
```

Full `App.axaml` example:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:avvi="https://github.com/AVVI94"
             x:Class="Breakpoints.Avalonia.TestApp.App"
             RequestedThemeVariant="Default">
             <!-- "Default" ThemeVariant follows system theme variant. "Dark" or "Light" are other available options. -->

    <Application.Styles>
        <FluentTheme />
		<avvi:ResponsivityBreakpoints />
    </Application.Styles>	
</Application>
```

### Defining breakpoints and breakpoint providers

The library searches for breakpoints in the logical tree of your view, looking for the first element with the attached property `Breakpoints.IsBreakpointProvider` set to `True`. This element must also have the `Breakpoints.Values` property set. The `Values` property specifies the provided breakpoints. `Breakpoints.IsBreakpointProvider` can be set in XAML directly or with binding, or you can set it from code-behind. The `Breakpoints.Values` property can only be set with binding or from code-behind because the type of the property is a custom breakpoint collection which cannot be instantiated and initialized in XAML.

The logical tree is searched upwards from the element that requested the breakpoint. The breakpoint provider can be any element that inherits from `Layoutable` (basically anything that has width and height properties and will be in the logical tree), e.g., Window, UserControl, Border, Grid, etc.

#### Set up breakpoints

To set up the breakpoint values, create a new instance of `AVVI94.Breakpoints.Avalonia.Collections.BreakpointList` and add your desired breakpoint values:

```cs
BreakpointList bp = [
    ("XS", 600),
    ("S", 800),
    ("M", 1000),
    ("L", 1200),
    ("XXL", 1600)
];
```

Then get the element you want to provide the breakpoints and set `Breakpoints.Values` to the collection instance:

```cs
var provider = (MainWindow)this;
AVVI94.Breakpoints.Avalonia.Controls.Breakpoints.SetValues(provider, bp);
```

Full MainWindow code-behind:

```cs
using Avalonia.Controls;
using AVVI94.Breakpoints.Avalonia.Collections;

namespace Breakpoints.Avalonia.TestApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Here you can also add your custom breakpoints that can be used with
            // the Breakpoint control
            BreakpointList bp = [
                ("XS", 600),
                ("S", 800),
                ("M", 1000),
                ("L", 1200),
                ("XXL", 1600)
            ];
            Controls.Breakpoints.SetValues(this, bp);
        }
    }
}
```

In the XAML of your desired breakpoint provider, set the `Breakpoints.IsBreakpointProvider` to `True`:

(MainWindow in this example)

```XML
Breakpoints.IsBreakpointProvider="True"
```

If you choose to use MainWindow as a provider, your XAML could look like this:

```XML
<Window x:Class="Breakpoints.Avalonia.TestApp.MainWindow"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:avvi="https://github.com/AVVI94"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        Title="Breakpoints.Avalonia.TestApp"
        d:DesignHeight="450"
        d:DesignWidth="800"
        avvi:Breakpoints.IsBreakpointProvider="true"
        mc:Ignorable="d">
    ...
</Window>
```

## Using breakpoints

You have two options for using breakpoints.

### Using the Breakpoint control

You can use the `Breakpoint` custom control, which hides itself when the breakpoint gets hit. This control works similarly to Panel controls:

```xml
<avvi:Breakpoint For=S>
    <Grid>
        ...
    </Grid>
</avvi:Breakpoint>
```

Control parameters:

* `For`: string specifying the breakpoint name. If the specified name is not found in the provided breakpoints, it will always be visible.
* `UpperBound`: string specifying the breakpoint name that is used as the upper bound. When the upper bound breakpoint is hit, the Breakpoint control gets hidden.
* `IsExclusive`: bool flag indicating whether the `For` breakpoint is exclusive. This means the control is only visible in the range for the specified breakpoint. For example, if you set `For="S"` and the breakpoint provider's width gets bigger than breakpoint `S + 1` (so breakpoint `M`), this control will be hidden.
* `Enabled`: bool flag indicating whether this control's visibility can be altered by breakpoints. This flag has the highest priority. The default value is `True`. If you set this to `False`, this control will **always** be hidden.

### Using the Breakpoint markup extension

You can also use the markup extension with predefined breakpoint names (not values, just names). This extension usage is similar to the official OnPlatform extension. Names of the breakpoints in the BreakpointList must match properties of the Breakpoint markup extension class (Default property is excluded).

```XML
 <StackPanel Orientation="{avvi:Breakpoint Vertical, M=Horizontal}">
     <Button Content="Button" />
     <Button Content="Button" />
     <Button Content="Button" />
     <Button Content="Button" />
     <Button Content="Button" />
     <Button Content="Button" />
 </StackPanel>
```

You can specify a default value that will be used when no other smaller breakpoint has a specified value. In the example above, the stack panel will have vertical orientation for anything smaller than the breakpoint value specified for `M`.

This markup extension also supports Converters (IValueConverter). It may also support bindings for values, although this has not been tested.

The markup extension implementation does not support upper bounds or exclusive breakpoints.

#### Fixed grid columns for different breakpoints

```XML
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{a:Breakpoint 50, M=100, L=150, XL=200, XXL=250}" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="{a:Breakpoint 50, M=100, L=150, XL=200, XXL=250}" />
    </Grid.ColumnDefinitions>
    <Rectangle Height="50" Fill="Red" Grid.Column="0" />
    <Rectangle MinWidth="50" Height="50" Fill="Green" Grid.Column="1" />
    <Rectangle Height="50" Fill="Blue" Grid.Column="2" />
</Grid>
```

### Using TemplatedBreakpoint control

!TODO

```xml
<a:TemplatedBreakpoint>
    <a:BreakpointTemplate For="XS">
        <DataTemplate>
            <TextBlock Text="Templated XS" />
        </DataTemplate>
    </a:BreakpointTemplate>
    <a:BreakpointTemplate For="S">
        <DataTemplate>
            <TextBlock Text="Templated S" />
        </DataTemplate>
    </a:BreakpointTemplate>
    <a:BreakpointTemplate For="M">
        <DataTemplate>
            <TextBlock Text="Templated M" />
        </DataTemplate>
    </a:BreakpointTemplate>
    <a:BreakpointTemplate For="L">
        <DataTemplate>
            <TextBlock Text="Templated L" />
        </DataTemplate>
    </a:BreakpointTemplate>            
    <a:BreakpointTemplate For="XL">
        <DataTemplate>
            <TextBlock Text="Templated XL" />
        </DataTemplate>
    </a:BreakpointTemplate>            
    <a:BreakpointTemplate For="XXL">
        <DataTemplate>
            <TextBlock Text="Templated XXL" />
        </DataTemplate>
    </a:BreakpointTemplate>
</a:TemplatedBreakpoint>
```

## Edge cases and errors

#### Breakpoint control visibility

- If you set the `For` parameter of the Breakpoint control to a breakpoint that is not in the provided breakpoints, the control will always be visible.
- If you set the `UpperBound` parameter of the Breakpoint control to a breakpoint that is not in the provided breakpoints, the control will always be visible.

#### Breakpoints not working for controls created outside of XAML or attached later to the logical tree (for version <= 1.0.6)

- If you instantiate controls that rely on breakpoints (either the Breakpoint control or Breakpoint markup extension) outside of XAML - such as through a dependency injection container or in the code-behind - you need to set up a breakpoint provider proxy using binding. This ensures that the breakpoint system is notified about breakpoint changes.

    The reason for this setup is that when a control isn't yet attached to the logical tree, the system won't automatically search for a breakpoint provider. For example, imagine you have a window acting as the breakpoint provider, and this window contains dynamic content that you set in code-behind or bind from a ViewModel. If you then instantiate a control to use within this dynamic content and use breakpoints in that control, the breakpoint provider won't be found because the control isn't yet in the logical tree.

    To resolve this, you should bind the control's XAML to the breakpoint provider in the window. You can do so like this:

     ```xml
    r:Breakpoints.IsBreakpointProvider="True"
    r:Breakpoints.CurrentBreakpoint="{Binding $parent[Window].(r:Breakpoints.CurrentBreakpoint)}"
    r:Breakpoints.Values="{Binding $parent[Window].(r:Breakpoints.Values)}"
     ```

    Yes, the control used as content must be marked as a breakpoint provider. The current breakpoint is provided by the binding. Unfortunately, the breakpoint values must also be bound to the source object.

- #### Breakpoints not working for controls created outside of XAML or attached later to the logical tree (for version >= 1.0.7)

- If you instantiate controls that rely on breakpoints (either the Breakpoint control or Breakpoint markup extension) outside of XAML - such as through a dependency injection container or in the code-behind - you need to set up a breakpoint provider proxy using binding. This ensures that the breakpoint system is notified about breakpoint changes.

    The reason for this setup is that when a control isn't yet attached to the logical tree, the system won't automatically search for a breakpoint provider. For example, imagine you have a window acting as the breakpoint provider, and this window contains dynamic content that you set in code-behind or bind from a ViewModel. If you then instantiate a control to use within this dynamic content and use breakpoints in that control, the breakpoint provider won't be found because the control isn't yet in the logical tree.

    To resolve this, you should bind the control's XAML to the breakpoint provider in the window. You can do so like this:

     ```xml
    r:Breakpoints.IsProxy="True"
    r:Breakpoints.CurrentBreakpoint="{Binding $parent[Window].(r:Breakpoints.CurrentBreakpoint)}"
    r:Breakpoints.Values="{Binding $parent[Window].(r:Breakpoints.Values)}"
     ```

    Yes, the control used as content must be marked as a breakpoint provider proxy. The current breakpoint is provided by the binding. Unfortunately, the breakpoint values must also be bound to the source object.

## License

[MIT](https://github.com/AVVI94/Breakpoints.Avalonia/blob/master/LICENSE)
