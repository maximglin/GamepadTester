using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GamepadTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XboxController controller;
        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                controller = new XboxController();

                controller.ButtonDown += Controller_ButtonDown;
                controller.ButtonUp += Controller_ButtonUp;
                controller.StickChanged += Controller_StickChanged;

                timer.Tick += (s, e) =>
                {
                    controller.PollOnce();
                };
                timer.Start();
            }
            catch
            {
                MessageBox.Show("No XInput controllers found!");
            }
        }

        private void Controller_StickChanged(object? sender, StickEventArgs e)
        {
            var tr = e.Stick == Stick.Left ? this.LSTransform : this.RSTransform;
            var margin = e.Stick == Stick.Left ? this.LS.Margin : this.RS.Margin;
            
            if(e.NewValue.X >= 0.0)
                tr.X = e.NewValue.X * margin.Left;
            else
                tr.X = e.NewValue.X * margin.Right;

            if (e.NewValue.Y >= 0.0)
                tr.Y = -e.NewValue.Y * margin.Top;
            else
                tr.Y = -e.NewValue.Y * margin.Bottom;
        }

        private UIElement? GetControl(Buttons b)
        {
            return b switch
            {
                Buttons.A => this.A,
                Buttons.Y => this.Y,
                Buttons.X => this.X,
                Buttons.B => this.B,
                Buttons.DPadDown => this.DDown,
                Buttons.DPadUp => this.DUp,
                Buttons.DPadLeft => this.DLeft,
                Buttons.DPadRight => this.DRight,
                Buttons.LeftThumb => this.LS,
                Buttons.RightThumb => this.RS,

                _ => null
            };
        }

        private void Controller_ButtonUp(object? sender, Buttons e)
        {
            var control = GetControl(e);
            if (control is null) return;


            control.Effect = null;
        }

        private void Controller_ButtonDown(object? sender, Buttons e)
        {
            var control = GetControl(e);
            if (control is null) return;


            control.Effect = new InvertEffect();
        }
    }
}
