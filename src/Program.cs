using System;
using System.Windows.Forms;

namespace SC880
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            ThreeDLog.Clear();
            ThreeDLog.Write("app start args=" + string.Join(",", args));
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ThreeDLog.Write("appdomain exception: " + e.ExceptionObject);
            };
            bool startIn3D = false;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? "";
                if (arg == "--3d" || arg == "/3d" || arg == "-3d") startIn3D = true;
            }
            ThreeDLog.Write("startIn3D=" + startIn3D);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show(e.Exception.Message, "BRAUBERG SC-880",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            Application.Run(new Sc880Form(startIn3D));
        }
    }
}
