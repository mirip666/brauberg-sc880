using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace SC880
{
    internal sealed class ThreeDKeyVisual
    {
        public string Id;
        public TranslateTransform3D Press;
    }

    internal sealed partial class Sc8803DWindow : Window
    {
        private readonly Sc880Form form;
        private readonly ThreeDHost host;
        private readonly List<CalcKey> keys;

        private readonly Grid root;
        private readonly Viewport3D viewport;
        private readonly PerspectiveCamera camera;
        private readonly ModelVisual3D modelVisual;
        private readonly QuaternionRotation3D quaternionRotation;

        private readonly Dictionary<Model3D, string> modelIds = new Dictionary<Model3D, string>();
        private readonly Dictionary<string, ThreeDKeyVisual> keyVisuals = new Dictionary<string, ThreeDKeyVisual>();

        private Model3DGroup modelGroup;
        private TranslateTransform3D padPress;
        private DiffuseMaterial screenMaterial;
        private Quaternion orientation;
        private double distance = 1420;
        private const double DefaultDistance = 1420;

        private string pressedKey;
        private int pressedPad;
        private string hoverKey;
        private bool orbiting;
        private Point lastPoint;
        private Point pressStart;
        private bool pressMoved;
        private string lastScreenSignature;

        private DispatcherTimer syncTimer;
        private DispatcherTimer hintTimer;
        private bool closing;
        private bool interactionBlocked;

        public Sc8803DWindow(Sc880Form form, ThreeDHost host)
        {
            ThreeDLog.Write("window constructor begin");
            this.form = form;
            this.host = host;

            keys = host.ReadKeys( );
            if (keys == null) keys = new List<CalcKey>();
            ThreeDLog.Write("keys read: " + keys.Count);

            Title = "BRAUBERG SC-880 \u2014 3D";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = true;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            Rect area = SystemParameters.WorkArea;
            double h = Math.Min(1040, area.Height - 24);
            if (h < 520) h = Math.Max(420, area.Height - 8);
            double w = Math.Round(h * 0.80);
            if (w > area.Width - 24)
            {
                w = Math.Max(360, area.Width - 24);
                h = Math.Round(w / 0.80);
            }
            Width = w;
            Height = h;
            Left = area.Left + (area.Width - w) / 2;
            Top = area.Top + (area.Height - h) / 2;

            root = new Grid();
            root.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            viewport = new Viewport3D();
            viewport.ClipToBounds = true;
            root.Children.Add(viewport);

            orientation = InitialRotation();
            quaternionRotation = new QuaternionRotation3D(orientation);
            camera = new PerspectiveCamera(new Point3D(0, 0, distance), new Vector3D(0, 0, -1),
                new Vector3D(0, 1, 0), 42);
            viewport.Camera = camera;
            modelVisual = new ModelVisual3D();
            modelVisual.Transform = new RotateTransform3D(quaternionRotation);
            viewport.Children.Add(modelVisual);
            modelGroup = new Model3DGroup();
            modelVisual.Content = modelGroup;

            BuildLights();
            BuildBody();
            BuildScreen();
            BuildFaceOverlay();
            BuildBackPanel();
            BuildKeys();
            BuildPad();
            BuildBodyControls();
            ThreeDLog.Write("geometry built");
            SyncScreen();
            ThreeDLog.Write("screen synced");

            Content = root;
            BuildOverlay();
            AttachEvents();
            ThreeDLog.Write("overlay attached");

            Loaded += delegate
            {
                Activate();
                root.Focus();
                Keyboard.Focus(root);
                if (syncTimer != null) syncTimer.Start();
                if (hintTimer != null) hintTimer.Start();
                ThreeDLog.Write("window loaded, viewport=" + viewport.ActualWidth + "x" + viewport.ActualHeight);
                DispatcherTimer releaseTopmost = new DispatcherTimer();
                releaseTopmost.Interval = TimeSpan.FromMilliseconds(500);
                releaseTopmost.Tick += delegate
                {
                    releaseTopmost.Stop();
                    ReleaseTopmost();
                };
                releaseTopmost.Start();
                if (Environment.GetEnvironmentVariable("SC880_3D_DEBUG") == "1")
                {
                    DispatcherTimer debugTimer = new DispatcherTimer();
                    debugTimer.Interval = TimeSpan.FromSeconds(1.8);
                    debugTimer.Tick += delegate
                    {
                        SaveViewportDebug();
                    };
                    debugTimer.Start();
                }
            };
            Closed += delegate
            {
                ThreeDLog.Write("window Closed event");
                closing = true;
                if (syncTimer != null) syncTimer.Stop();
                if (hintTimer != null) hintTimer.Stop();
            };
            ThreeDLog.Write("window constructor end");
        }

        private static Quaternion InitialRotation()
        {
            Quaternion yaw = new Quaternion(new Vector3D(0, 1, 0), -18);
            Quaternion pitch = new Quaternion(new Vector3D(1, 0, 0), 7);
            Quaternion q = Quaternion.Multiply(yaw, pitch);
            q.Normalize();
            return q;
        }

        private void ReleaseTopmost()
        {
            if (closing || !Topmost) return;
            Topmost = false;
            ThreeDLog.Write("topmost released");
        }

        internal void SetDialogOpen(bool open)
        {
            interactionBlocked = open;
            if (open)
            {
                if (pressedKey != null || orbiting)
                {
                    pressedKey = null;
                    pressedPad = 0;
                    orbiting = false;
                    try { root.ReleaseMouseCapture(); }
                    catch { }
                    UpdateVisualState();
                }
                hoverKey = null;
                Cursor = Cursors.Arrow;
                HideHint();
                ThreeDLog.Write("interaction blocked by dialog");
            }
            else
            {
                ThreeDLog.Write("interaction restored after dialog");
                Activate();
                root.Focus();
                Keyboard.Focus(root);
            }
        }

        private void BuildLights()
        {
            Model3DGroup lights = new Model3DGroup();
            lights.Children.Add(new AmbientLight(Color.FromRgb(146, 143, 137)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(78, 76, 72), new Vector3D(0.35, 0.55, -0.9)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(38, 41, 46), new Vector3D(-0.65, -0.2, -0.75)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(28, 26, 24), new Vector3D(0.2, -0.6, 0.8)));
            viewport.Children.Add(new ModelVisual3D { Content = lights });
        }

        private void BuildBody()
        {
            double backZ = -32;
            double shoulderZ = 30;
            double frontZ = 32.5;
            Point[] outer = ThreeDGeometry.RoundedRect(460, 860, 38, 6);
            Point[] inner = ThreeDGeometry.RoundedRect(456, 856, 36, 6);

            Material front = SolidMaterial(ThreeDColors.BodyFront, 46, 30);
            Material bevel = SolidMaterial(ThreeDColors.BodyBevel, 56, 34);
            Material side = SolidMaterial(ThreeDColors.BodySide, 30, 22);
            Material back = SolidMaterial(ThreeDColors.BodyBack, 12, 18);

            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Face(inner, frontZ, true), front, front), "body", modelGroup);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Ring(outer, shoulderZ, inner, frontZ), bevel, bevel), "body", modelGroup);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Side(outer, backZ, shoulderZ), side, side), "body", modelGroup);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Face(outer, backZ, false), back, back), "body", modelGroup);
        }

        private void BuildScreen()
        {
            const double centerY = 310;
            Point[] outer = ThreeDGeometry.RoundedRect(400, 134, 7, 5);
            Point[] inner = ThreeDGeometry.RoundedRect(392, 126, 3, 5);

            Model3DGroup group = new Model3DGroup();
            group.Transform = new TranslateTransform3D(0, centerY, 0);

            Material frame = SolidMaterial(ThreeDColors.LcdFrame, 24, 26);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Ring(outer, 33.4, inner, 32.95), frame, frame), "body", group);

            screenMaterial = new DiffuseMaterial(ThreeDTextures.BrushFor(ThreeDTextures.Screen(null, 2.0)));
            GeometryModel3D screen = new GeometryModel3D(
                ThreeDGeometry.TexturedFrontQuad(-196, -63, 196, 63, 32.96), screenMaterial);
            modelIds[screen] = "body";
            group.Children.Add(screen);

            modelGroup.Children.Add(group);
        }

        private void BuildFaceOverlay()
        {
            BitmapSource bmp = ThreeDTextures.FaceOverlay(keys, 2.0);
            ImageBrush brush = ThreeDTextures.BrushFor(bmp);
            GeometryModel3D face = new GeometryModel3D(
                ThreeDGeometry.TexturedFrontQuad(-230, -430, 230, 430, 32.55),
                new DiffuseMaterial(brush));
            modelIds[face] = "body";
            modelGroup.Children.Add(face);
        }

        private void BuildBackPanel()
        {
            BitmapSource bmp = ThreeDTextures.Back(2.0);
            ImageBrush brush = ThreeDTextures.BrushFor(bmp);
            GeometryModel3D panel = new GeometryModel3D(
                ThreeDGeometry.TexturedBackQuad(-230, -430, 230, 430, -32.06),
                new DiffuseMaterial(brush));
            modelIds[panel] = "body";
            modelGroup.Children.Add(panel);
        }

        private void BuildKeys()
        {
            for (int i = 0; i < keys.Count; i++)
            {
                CalcKey k = keys[i];
                bool soft = k.Kind == KeyKind.Soft;
                double w = k.Rect.Width;
                double h = k.Rect.Height;
                double cx = k.Rect.X + w / 2.0 - 240.0;
                double cy = 430.0 - (k.Rect.Y + h / 2.0);
                double baseZ = 32.58;
                double capHeight = soft ? 5.2 : 6.6;
                double radius = soft ? 8.0 : 5.2;
                Point[] bottom = ThreeDGeometry.RoundedRect(w - 2.6, h - 2.6, radius, 4);
                Point[] top = ThreeDGeometry.RoundedRect(w - 4.2, h - 4.2, radius * 0.82, 4);
                double topZ = baseZ + capHeight;

                Material material = KeyMaterial(k.Kind);
                Model3DGroup positioned = new Model3DGroup();
                positioned.Transform = new TranslateTransform3D(cx, cy, 0);
                AddModel(ThreeDGeometry.Model(ThreeDGeometry.Side(bottom, baseZ, topZ), material, material), k.Id, positioned);
                AddModel(ThreeDGeometry.Model(ThreeDGeometry.Face(top, topZ, true), material, material), k.Id, positioned);

                BitmapSource texture = ThreeDTextures.Key(k, 2.0);
                GeometryModel3D face = new GeometryModel3D(
                    ThreeDGeometry.TexturedFrontQuad(-(w - 4.8) / 2.0, -(h - 4.8) / 2.0,
                        (w - 4.8) / 2.0, (h - 4.8) / 2.0, topZ + 0.06),
                    new DiffuseMaterial(ThreeDTextures.BrushFor(texture)));
                modelIds[face] = k.Id;
                positioned.Children.Add(face);

                TranslateTransform3D press = new TranslateTransform3D(0, 0, 0);
                Model3DGroup pressGroup = new Model3DGroup();
                pressGroup.Transform = press;
                pressGroup.Children.Add(positioned);
                modelGroup.Children.Add(pressGroup);

                ThreeDKeyVisual visual = new ThreeDKeyVisual();
                visual.Id = k.Id;
                visual.Press = press;
                keyVisuals[k.Id] = visual;
            }
        }

        private void BuildPad()
        {
            double baseZ = 32.58;
            double topZ = baseZ + 5.4;
            Point[] bottom = ThreeDGeometry.RoundedRect(106, 91, 15, 6);
            Point[] top = ThreeDGeometry.RoundedRect(103, 88, 13, 6);
            Material material = SolidMaterial(Color.FromRgb(219, 216, 208), 28, 20);

            Model3DGroup positioned = new Model3DGroup();
            positioned.Transform = new TranslateTransform3D(0, 88.5, 0);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Side(bottom, baseZ, topZ), material, material), "pad", positioned);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Face(top, topZ, true), material, material), "pad", positioned);

            BitmapSource texture = ThreeDTextures.Pad(2.0);
            GeometryModel3D face = new GeometryModel3D(
                ThreeDGeometry.TexturedFrontQuad(-50.5, -43, 50.5, 43, topZ + 0.06),
                new DiffuseMaterial(ThreeDTextures.BrushFor(texture)));
            modelIds[face] = "pad";
            positioned.Children.Add(face);

            padPress = new TranslateTransform3D(0, 0, 0);
            Model3DGroup pressGroup = new Model3DGroup();
            pressGroup.Transform = padPress;
            pressGroup.Children.Add(positioned);
            modelGroup.Children.Add(pressGroup);
        }

        private void BuildBodyControls()
        {
            AddBodyControl("ui2d", "2D", KeyKind.Function, 126, 212, 72, 32, 11.5, 4.4);
            AddBodyControl("uireset", "\u0421\u0431\u0440\u043e\u0441", KeyKind.Soft,
                206, 212, 64, 32, 9.2, 4.4);
            AddBodyControl("uiclose", "\u00d7", KeyKind.Red, 278, 212, 56, 32, 13.5, 4.4);
        }

        private void AddBodyControl(string id, string label, KeyKind kind,
            double x, double y, double w, double h, double size, double capHeight)
        {
            double cx = x + w / 2.0 - 240.0;
            double cy = 430.0 - (y + h / 2.0);
            double baseZ = 32.58;
            double topZ = baseZ + capHeight;
            double radius = 7.0;
            Point[] bottom = ThreeDGeometry.RoundedRect(w - 2.6, h - 2.6, radius, 4);
            Point[] top = ThreeDGeometry.RoundedRect(w - 4.2, h - 4.2, radius * 0.82, 4);

            Material material = KeyMaterial(kind);
            Model3DGroup positioned = new Model3DGroup();
            positioned.Transform = new TranslateTransform3D(cx, cy, 0);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Side(bottom, baseZ, topZ), material, material),
                id, positioned);
            AddModel(ThreeDGeometry.Model(ThreeDGeometry.Face(top, topZ, true), material, material),
                id, positioned);

            CalcKey key = new CalcKey();
            key.Id = id;
            key.Primary = label;
            key.Kind = kind;
            key.Rect = new System.Drawing.RectangleF((float)x, (float)y, (float)w, (float)h);
            key.PrimarySize = (float)size;
            GeometryModel3D face = new GeometryModel3D(
                ThreeDGeometry.TexturedFrontQuad(-(w - 4.8) / 2.0, -(h - 4.8) / 2.0,
                    (w - 4.8) / 2.0, (h - 4.8) / 2.0, topZ + 0.06),
                new DiffuseMaterial(ThreeDTextures.BrushFor(ThreeDTextures.Key(key, 2.0))));
            modelIds[face] = id;
            positioned.Children.Add(face);

            TranslateTransform3D press = new TranslateTransform3D(0, 0, 0);
            Model3DGroup pressGroup = new Model3DGroup();
            pressGroup.Transform = press;
            pressGroup.Children.Add(positioned);
            modelGroup.Children.Add(pressGroup);

            ThreeDKeyVisual visual = new ThreeDKeyVisual();
            visual.Id = id;
            visual.Press = press;
            keyVisuals[id] = visual;
        }

        private static Material KeyMaterial(KeyKind kind)
        {
            if (kind == KeyKind.Red) return SolidMaterial(Color.FromRgb(184, 53, 44), 32, 22);
            if (kind == KeyKind.Function) return SolidMaterial(Color.FromRgb(61, 61, 59), 30, 24);
            if (kind == KeyKind.Soft) return SolidMaterial(Color.FromRgb(215, 212, 204), 24, 20);
            return SolidMaterial(Color.FromRgb(240, 238, 232), 28, 24);
        }

        private static Material SolidMaterial(Color color, byte specular, double power)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            DiffuseMaterial diffuse = new DiffuseMaterial(brush);
            if (specular == 0)
            {
                diffuse.Freeze();
                return diffuse;
            }
            SolidColorBrush white = new SolidColorBrush(Color.FromArgb(specular, 255, 255, 255));
            white.Freeze();
            SpecularMaterial spec = new SpecularMaterial(white, power);
            MaterialGroup group = new MaterialGroup();
            group.Children.Add(diffuse);
            group.Children.Add(spec);
            group.Freeze();
            return group;
        }

        private void AddModel(GeometryModel3D model, string id, Model3DGroup parent)
        {
            if (id != null) modelIds[model] = id;
            parent.Children.Add(model);
        }

        private void SyncScreen()
        {
            Calc3DSnapshot snapshot = host.ReadSnapshot();
            if (snapshot == null) return;
            string signature = snapshot.Signature();
            if (signature == lastScreenSignature) return;
            lastScreenSignature = signature;
            ThreeDLog.Write("screen sync exp=\"" + snapshot.Expression + "\" res=\"" + snapshot.Result +
                "\" shown=" + snapshot.ShowingResult + " off=" + snapshot.PowerOff);
            ImageBrush brush = ThreeDTextures.BrushFor(ThreeDTextures.Screen(snapshot, 2.0));
            if (screenMaterial != null) screenMaterial.Brush = brush;
        }

        private void ClearScreen()
        {
            lastScreenSignature = null;
        }

        private void DumpHitMap()
        {
            try
            {
                for (int y = 320; y <= 840; y += 10)
                {
                    System.Text.StringBuilder line = new System.Text.StringBuilder();
                    string last = null;
                    int start = 0;
                    for (int x = 240; x <= 620; x += 10)
                    {
                        Point3D hp;
                        string id = HitIdAt(new Point(x, y), out hp);
                        string label = (id == null || id == "body") ? "." : id;
                        if (label != last)
                        {
                            if (last != null && last != ".") line.Append(start + "-" + x + ":" + last + " ");
                            last = label;
                            start = x;
                        }
                    }
                    if (last != null && last != ".") line.Append(start + "-620:" + last + " ");
                    if (line.Length > 0) ThreeDLog.Write("hitmap y=" + y + " " + line.ToString());
                }
            }
            catch (Exception ex)
            {
                ThreeDLog.Write("hitmap failed: " + ex);
            }
        }

        private void SaveViewportDebug()
        {
            try
            {
                int w = (int)Math.Round(viewport.ActualWidth);
                int h = (int)Math.Round(viewport.ActualHeight);
                ThreeDLog.Write("debug viewport size " + w + "x" + h + " children=" + viewport.Children.Count);
                if (w <= 0 || h <= 0) return;
                RenderTargetBitmap bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(viewport);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                string path = Path.Combine(Path.GetTempPath(), "SC880-viewport.png");
                using (FileStream stream = File.Create(path)) encoder.Save(stream);
                ThreeDLog.Write("debug viewport saved " + path);

                int rw = (int)Math.Round(root.ActualWidth);
                int rh = (int)Math.Round(root.ActualHeight);
                if (rw > 0 && rh > 0)
                {
                    RenderTargetBitmap whole = new RenderTargetBitmap(rw, rh, 96, 96, PixelFormats.Pbgra32);
                    whole.Render(root);
                    PngBitmapEncoder wholeEncoder = new PngBitmapEncoder();
                    wholeEncoder.Frames.Add(BitmapFrame.Create(whole));
                    string wholePath = Path.Combine(Path.GetTempPath(), "SC880-window.png");
                    using (FileStream stream = File.Create(wholePath)) wholeEncoder.Save(stream);
                    ThreeDLog.Write("debug window saved " + wholePath);
                }
                ImageBrush live = screenMaterial == null ? null : screenMaterial.Brush as ImageBrush;
                BitmapSource liveSource = live == null ? null : live.ImageSource as BitmapSource;
                if (liveSource != null)
                {
                    PngBitmapEncoder screenEncoder = new PngBitmapEncoder();
                    screenEncoder.Frames.Add(BitmapFrame.Create(liveSource));
                    string screenPath = Path.Combine(Path.GetTempPath(), "SC880-screen-live.png");
                    using (FileStream stream = File.Create(screenPath)) screenEncoder.Save(stream);
                    ThreeDLog.Write("debug screen saved " + screenPath);
                }
                DumpHitMap();
            }
            catch (Exception ex)
            {
                ThreeDLog.Write("debug viewport failed: " + ex);
            }
        }

        private void RotateBy(double dx, double dy)
        {
            if (Math.Abs(dx) + Math.Abs(dy) > 2)
                ThreeDLog.Write("rotate dx=" + dx.ToString("F1") + " dy=" + dy.ToString("F1"));
            Quaternion qy = new Quaternion(new Vector3D(0, 1, 0), dx * 0.38);
            Quaternion qx = new Quaternion(new Vector3D(1, 0, 0), dy * 0.38);
            orientation = Quaternion.Multiply(Quaternion.Multiply(qy, qx), orientation);
            orientation.Normalize();
            quaternionRotation.Quaternion = orientation;
        }

        private void ResetView()
        {
            orientation = InitialRotation();
            quaternionRotation.Quaternion = orientation;
            distance = DefaultDistance;
            camera.Position = new Point3D(0, 0, distance);
        }

        private void UpdateVisualState()
        {
            foreach (ThreeDKeyVisual visual in keyVisuals.Values)
            {
                double z = 0;
                if (pressedKey == visual.Id) z = -3.0;
                else if (hoverKey == visual.Id) z = 0.75;
                visual.Press.OffsetZ = z;
            }
            if (padPress != null)
            {
                if (pressedKey == "pad") padPress.OffsetZ = -2.2;
                else if (hoverKey == "pad") padPress.OffsetZ = 0.55;
                else padPress.OffsetZ = 0;
            }
        }
    }
}
