using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.XInput;
using System.Threading.Tasks;
using System.Threading;

namespace GamepadTester
{
    public class XboxController
    {
        readonly Controller controller = null;



        public XboxController()
        {
            foreach (var index in Enum.GetValues(typeof(UserIndex)).Cast<UserIndex>())
            {
                controller = new Controller(index);
                if (controller.IsConnected)
                    break;
            }
            if (!controller?.IsConnected ?? false)
                throw new Exception("No XInput controller found!");


            foreach (var i in Enum.GetValues(typeof(Buttons)).Cast<Buttons>())
            {
                pressedButtons.Add(i, false);
                newPressedButtons.Add(i, false);
            }
            foreach (var pressed in controller.GetState().Gamepad.Buttons.GetButtons().Select(b => b.GetButton()))
            {
                pressedButtons[pressed] = true;
                newPressedButtons[pressed] = true;
            }




        }




        readonly Dictionary<Buttons, bool> pressedButtons = new Dictionary<Buttons, bool>();
        readonly Dictionary<Buttons, bool> newPressedButtons = new Dictionary<Buttons, bool>();
        short lx = 0, ly = 0, rx = 0, ry = 0;
        byte lt = 0, rt = 0;

        public void StopVibration()
        {
            controller.SetVibration(new Vibration()
            {
                LeftMotorSpeed = 0,
                RightMotorSpeed = 0
            });
        }
        public async Task Vibrate(double left, double right, TimeSpan time, CancellationToken token = default)
        {
            if (left > 1.0) left = 1.0;
            if (left < 0.0) left = 0.0;
            if (right > 1.0) right = 1.0;
            if (right < 0.0) right = 0.0;
            ushort l = (ushort)Math.Round(ushort.MaxValue * left);
            ushort r = (ushort)Math.Round(ushort.MaxValue * right);

            controller.SetVibration(new Vibration()
            {
                LeftMotorSpeed = l,
                RightMotorSpeed = r
            });
            try { await Task.Delay(time, token); } catch { }
            controller.SetVibration(new Vibration()
            {
                LeftMotorSpeed = 0,
                RightMotorSpeed = 0
            });
        }
        public async Task Vibrate(Func<TimeSpan, double> left, Func<TimeSpan, double> right, TimeSpan max, CancellationToken token = default)
        {
            var start = DateTime.Now;
            var end = start + max;
            var now = start;

            while (now < end)
            {
                ushort l = (ushort)Math.Round(ushort.MaxValue * left(now - start));
                ushort r = (ushort)Math.Round(ushort.MaxValue * right(now - start));
                controller.SetVibration(new Vibration()
                {
                    LeftMotorSpeed = l,
                    RightMotorSpeed = r
                });
                now = DateTime.Now;
                try { await Task.Delay(1, token); } catch {}
                if (token.IsCancellationRequested)
                    break;
            }
            controller.SetVibration(new Vibration()
            {
                LeftMotorSpeed = 0,
                RightMotorSpeed = 0
            });
        }

        public void PollOnce()
        {
            var state = controller.GetState();
            var g = state.Gamepad;


            foreach (var i in Enum.GetValues(typeof(Buttons)).Cast<Buttons>())
            {
                newPressedButtons[i] = false;
            }
            foreach (var pressed in g.Buttons.GetButtons().Select(b => b.GetButton()))
                newPressedButtons[pressed] = true;

            if (lt != g.LeftTrigger)
            {
                var old = lt;
                lt = g.LeftTrigger;
                var oldd = old / 255.0;
                var nowd = lt / 255.0;

                TriggerChanged?.Invoke(this, new TriggerEventArgs(oldd, nowd, Triggers.Left));
                StateChanged?.Invoke(this);

                if (nowd >= 0.4)
                    newPressedButtons[Buttons.LT] = true;
                else
                    newPressedButtons[Buttons.LT] = false;
            }
            else
                newPressedButtons[Buttons.LT] = pressedButtons[Buttons.LT];

            if (rt != g.RightTrigger)
            {
                var old = rt;
                rt = g.RightTrigger;
                var oldd = old / 255.0;
                var nowd = rt / 255.0;

                TriggerChanged?.Invoke(this, new TriggerEventArgs(oldd, nowd, Triggers.Right));
                StateChanged?.Invoke(this);

                if (nowd >= 0.4)
                    newPressedButtons[Buttons.RT] = true;
                else
                    newPressedButtons[Buttons.RT] = false;
            }
            else
                newPressedButtons[Buttons.RT] = pressedButtons[Buttons.RT];




            foreach (var i in Enum.GetValues(typeof(Buttons)).Cast<Buttons>())
            {
                var old = pressedButtons[i];
                var now = newPressedButtons[i];

                if (old != now)
                {
                    pressedButtons[i] = now;

                    if (old == true && now == false)
                    {
                        ButtonPressed?.Invoke(this, i);
                        ButtonUp?.Invoke(this, i);
                        StateChanged?.Invoke(this);
                    }
                    else if (old == false && now == true)
                    {
                        ButtonDown?.Invoke(this, i);
                        StateChanged?.Invoke(this);
                    }
                }
            }

            if (lx != g.LeftThumbX || ly != g.LeftThumbY)
            {
                var oldx = lx;
                lx = g.LeftThumbX;

                var oldy = ly;
                ly = g.LeftThumbY;

                StickChanged?.Invoke(this, new StickEventArgs((oldx.ToDouble(), oldy.ToDouble()), (lx.ToDouble(), ly.ToDouble()), Stick.Left));
                StateChanged?.Invoke(this);
            }

            if (rx != g.RightThumbX || ry != g.RightThumbY)
            {
                var oldx = rx;
                rx = g.RightThumbX;

                var oldy = ry;
                ry = g.RightThumbY;

                StickChanged?.Invoke(this, new StickEventArgs((oldx.ToDouble(), oldy.ToDouble()), (rx.ToDouble(), ry.ToDouble()), Stick.Right));
                StateChanged?.Invoke(this);
            }

        }


        public event EventHandler<Buttons> ButtonPressed;
        public event EventHandler<Buttons> ButtonDown;
        public event EventHandler<Buttons> ButtonUp;

        public event EventHandler<TriggerEventArgs> TriggerChanged;
        public event EventHandler<StickEventArgs> StickChanged;

        public event Action<XboxController> StateChanged;


        public (double X, double Y) LStick => (lx.ToDouble(), ly.ToDouble());
        public (double X, double Y) RStick => (rx.ToDouble(), ry.ToDouble());
        public double LTrigger => lt / 255.0;
        public double RTrigger => rt / 255.0;

        public IReadOnlyDictionary<Buttons, bool> IsButtonDown => pressedButtons;
    }


    public class XboxControllerAutoPoll : XboxController, IDisposable
    {
        readonly Task pollTask;
        readonly CancellationTokenSource cts = new CancellationTokenSource();
        public XboxControllerAutoPoll()
        {
            pollTask = Task.Run(Poll, cts.Token);
        }

        public void Dispose()
        {
            cts.Cancel();
        }

        void Poll()
        {
            while (!cts.IsCancellationRequested)
                base.PollOnce();
        }

    }

    public enum Buttons
    {
        None = 0,
        DPadUp = 1,
        DPadDown = 2,
        DPadLeft = 3,
        DPadRight = 4,
        Start = 5,
        Back = 6,
        LeftThumb = 7,
        RightThumb = 8,
        LeftShoulder = 9,
        RightShoulder = 10,
        A = 11,
        B = 12,
        X = 13,
        Y = 14,
        LT = 15,
        RT = 16
    };


    public enum Triggers { Left, Right };
    public enum Stick { Left, Right };
    public record TriggerEventArgs(double OldValue, double NewValue, Triggers Trigger);
    public record StickEventArgs((double X, double Y) OldValue, (double X, double Y) NewValue, Stick Stick);

    internal static class ButtonExtensions
    {
        public static IEnumerable<GamepadButtonFlags> GetButtons(this GamepadButtonFlags item)
        {
            foreach (var i in Enum.GetValues(typeof(GamepadButtonFlags)).Cast<GamepadButtonFlags>())
                if (item.HasFlag(i))
                    yield return i;
        }
        public static Buttons GetButton(this GamepadButtonFlags item)
        {
            return item switch
            {
                GamepadButtonFlags.None => Buttons.None,
                GamepadButtonFlags.DPadUp => Buttons.DPadUp,
                GamepadButtonFlags.DPadDown => Buttons.DPadDown,
                GamepadButtonFlags.DPadLeft => Buttons.DPadLeft,
                GamepadButtonFlags.DPadRight => Buttons.DPadRight,
                GamepadButtonFlags.Start => Buttons.Start,
                GamepadButtonFlags.Back => Buttons.Back,
                GamepadButtonFlags.LeftThumb => Buttons.LeftThumb,
                GamepadButtonFlags.RightThumb => Buttons.RightThumb,
                GamepadButtonFlags.LeftShoulder => Buttons.LeftShoulder,
                GamepadButtonFlags.RightShoulder => Buttons.RightShoulder,
                GamepadButtonFlags.A => Buttons.A,
                GamepadButtonFlags.B => Buttons.B,
                GamepadButtonFlags.X => Buttons.X,
                GamepadButtonFlags.Y => Buttons.Y,

                _ => Buttons.None
            };
        }
        public static double ToDouble(this short val)
        {
            if (val == 0)
                return 0.0;
            else if (val > 0)
                return val / (double)short.MaxValue;
            else
                return val / -(double)short.MinValue;
        }
    }
}