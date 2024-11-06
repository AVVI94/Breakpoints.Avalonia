using Avalonia.Controls;
using Avalonia.Responsivity.Breakpoints.Collections;

namespace Avalonia.Responsivity.Breakpoints.TestApp
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
                ("XXL", 1600)
            ];
            Controls.Breakpoints.SetValues(this, bp);
        }
    }
}