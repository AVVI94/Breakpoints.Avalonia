using Avalonia.Controls;
using AVVI94.Breakpoints.Avalonia.Collections;

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
                ("XXL", 1600)
            ];
            AVVI94.Breakpoints.Avalonia.Controls.Breakpoints.SetValues(this, bp);
        }
    }
}