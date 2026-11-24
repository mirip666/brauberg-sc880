using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace SC880
{
    internal static class ThreeDLog
    {
        private static readonly object Gate = new object();
        private static readonly string PathName =
            Path.Combine(Path.GetTempPath(), "SC880-3D.log");
        private static readonly bool Enabled =
            Environment.GetEnvironmentVariable("SC880_3D_DEBUG") == "1";

        public static void Write(string message)
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                    File.AppendAllText(PathName,
                        DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void Clear()
        {
            if (!Enabled) return;
            try
            {
                lock (Gate)
                    if (File.Exists(PathName)) File.Delete(PathName);
            }
            catch
            {
            }
        }
    }

    internal sealed class ThreeDHost
    {
        private readonly Sc880Form owner;
        private readonly Thread thread;
        private Dispatcher dispatcher;
        private Sc8803DWindow window;
        private volatile bool returningToFlat;
        private volatile bool shuttingDown;
        private bool closedNotified;

        public ThreeDHost(Sc880Form owner)
        {
            this.owner = owner;
            thread = new Thread(Run);
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "SC-880 3D";
        }

        public void Start()
        {
            thread.Start();
        }

        private void Run()
        {
            try
            {
                ThreeDLog.Write("thread start");
                dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.UnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    ThreeDLog.Write("dispatcher exception: " + e.Exception);
                };
                if (shuttingDown) return;
                ThreeDLog.Write("creating window");
                window = new Sc8803DWindow(owner, this);
                ThreeDLog.Write("window created");
                window.Closed += OnWindowClosed;
                if (shuttingDown)
                {
                    window.Close();
                    return;
                }
                ThreeDLog.Write("showing window");
                window.Show();
                ThreeDLog.Write("window shown, entering dispatcher");
                Dispatcher.Run();
                ThreeDLog.Write("dispatcher exited");
            }
            catch (Exception ex)
            {
                ThreeDLog.Write("run failed: " + ex);
                try
                {
                    owner.BeginInvoke(new Action(delegate { owner.OnThreeDFailed(ex); }));
                }
                catch
                {
                }
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (closedNotified) return;
            closedNotified = true;
            bool back = returningToFlat;
            bool shutting = shuttingDown;
            ThreeDLog.Write("window closed back=" + back + " shutting=" + shutting);
            try
            {
                owner.BeginInvoke(new Action(delegate
                {
                    if (back) owner.OnThreeDClosed(false);
                    else if (!shutting) owner.OnThreeDClosed(true);
                }));
            }
            catch
            {
            }
            try
            {
                Dispatcher d = dispatcher;
                if (d != null) d.BeginInvokeShutdown(DispatcherPriority.Background);
            }
            catch
            {
            }
        }

        public void Activate()
        {
            ThreeDLog.Write("activate requested");
            try
            {
                Dispatcher d = dispatcher;
                if (d == null) return;
                d.BeginInvoke(new Action(delegate
                {
                    if (window == null) return;
                    if (window.WindowState == System.Windows.WindowState.Minimized)
                        window.WindowState = System.Windows.WindowState.Normal;
                    window.Activate();
                }));
            }
            catch
            {
            }
        }

        public void SetDialogOpen(bool open)
        {
            try
            {
                Dispatcher d = dispatcher;
                if (d == null) return;
                d.BeginInvoke(new Action(delegate
                {
                    if (window != null) window.SetDialogOpen(open);
                }));
            }
            catch
            {
            }
        }

        public void ReturnToFlat()
        {
            returningToFlat = true;
            try
            {
                Dispatcher d = dispatcher;
                if (d == null) return;
                d.BeginInvoke(new Action(delegate
                {
                    if (window != null) window.Close();
                }));
            }
            catch
            {
            }
        }

        public void Shutdown()
        {
            shuttingDown = true;
            try
            {
                Dispatcher d = dispatcher;
                if (d == null) return;
                d.BeginInvoke(new Action(delegate
                {
                    if (window != null) window.Close();
                }));
            }
            catch
            {
            }
        }

        public void PressKey(string id)
        {
            try
            {
                owner.BeginInvoke(new Action(delegate { owner.PressKeyExternal(id); }));
            }
            catch
            {
            }
        }

        public void PressPad(int pad)
        {
            try
            {
                owner.BeginInvoke(new Action(delegate { owner.PressPadExternal(pad); }));
            }
            catch
            {
            }
        }

        public void SendKey(Forms.Keys keyData)
        {
            try
            {
                owner.BeginInvoke(new Action(delegate { owner.ProcessKeyExternal(keyData); }));
            }
            catch
            {
            }
        }

        public Calc3DSnapshot ReadSnapshot()
        {
            try
            {
                if (owner.InvokeRequired)
                    return (Calc3DSnapshot)owner.Invoke(new Func<Calc3DSnapshot>(owner.Create3DSnapshot));
                return owner.Create3DSnapshot();
            }
            catch
            {
                return null;
            }
        }

        public List<CalcKey> ReadKeys()
        {
            try
            {
                if (owner.InvokeRequired)
                    return (List<CalcKey>)owner.Invoke(new Func<List<CalcKey>>(delegate { return owner.KeyDefinitions; }));
                return owner.KeyDefinitions;
            }
            catch
            {
                return new List<CalcKey>();
            }
        }
    }
}
