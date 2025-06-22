using Avalonia.Controls;
using AVVI94.Breakpoints.Avalonia.Collections;
using System;
using System.Diagnostics;

namespace Breakpoints.Avalonia.TestApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            BreakpointList bp = [
                ("XS", 600),
                ("S", 800),
                ("M", 1000),
                ("L", 1200),
                ("XL", 1400),
                ("XXL", 1600)
            ];
            AVVI94.Breakpoints.Avalonia.Controls.Breakpoints.SetValues(this, bp);

            GridInfoTextBox.Text = """
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
                """;
        }

        bool flag = false;
        private void Button_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            var button = (Button)sender!;
            if(flag)
            {
                ((StackPanel)button.Parent!).Children.RemoveAt(((StackPanel)button.Parent).Children.Count - 1);
                flag = false;
            }
            else
            {
                ((StackPanel)button.Parent!).Children.Add(new UserControl1());
                flag = true;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}