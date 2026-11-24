using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace SC880
{
    internal sealed partial class Sc8803DWindow
    {
        private Grid overlay;
        private Border hint;

        private void BuildOverlay()
        {
            overlay = new Grid();
            overlay.Background = null;
            root.Children.Add(overlay);

            Border pill = new Border();
            pill.Background = new SolidColorBrush(Color.FromArgb(214, 34, 34, 32));
            pill.CornerRadius = new CornerRadius(14);
            pill.Padding = new Thickness(9, 5, 7, 5);
            pill.HorizontalAlignment = HorizontalAlignment.Left;
            pill.VerticalAlignment = VerticalAlignment.Top;
            pill.Margin = new Thickness(16, 12, 0, 0);
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 18;
            shadow.ShadowDepth = 2;
            shadow.Opacity = 0.32;
            shadow.Color = Colors.Black;
            pill.Effect = shadow;

            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.VerticalAlignment = VerticalAlignment.Center;
            pill.Child = row;

            TextBlock drag = new TextBlock();
            drag.Text = "BRAUBERG SC-880  \u00b7  3D";
            drag.Foreground = new SolidColorBrush(Color.FromRgb(246, 243, 235));
            drag.FontSize = 12.5;
            drag.FontWeight = FontWeights.SemiBold;
            drag.VerticalAlignment = VerticalAlignment.Center;
            drag.Margin = new Thickness(6, 1, 13, 1);
            drag.Cursor = Cursors.SizeAll;
            drag.ToolTip = "\u041f\u0435\u0440\u0435\u0442\u0430\u0449\u0438\u0442\u0435, \u0447\u0442\u043e\u0431\u044b \u043f\u0435\u0440\u0435\u043c\u0435\u0441\u0442\u0438\u0442\u044c \u043e\u043a\u043d\u043e";
            drag.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                try { DragMove(); }
                catch { }
                e.Handled = true;
            };
            row.Children.Add(drag);
            overlay.Children.Add(pill);

            hint = new Border();
            hint.Background = new SolidColorBrush(Color.FromArgb(185, 34, 34, 32));
            hint.CornerRadius = new CornerRadius(10);
            hint.Padding = new Thickness(11, 4, 11, 4);
            hint.HorizontalAlignment = HorizontalAlignment.Center;
            hint.VerticalAlignment = VerticalAlignment.Bottom;
            hint.Margin = new Thickness(0, 0, 0, 13);
            hint.IsHitTestVisible = false;
            TextBlock hintText = new TextBlock();
            hintText.Text = "\u041a\u043e\u0440\u043f\u0443\u0441 \u2014 \u0432\u0440\u0430\u0449\u0435\u043d\u0438\u0435  \u00b7  \u043a\u043e\u043b\u0435\u0441\u043e \u2014 \u043c\u0430\u0441\u0448\u0442\u0430\u0431  \u00b7  2D, \u0421\u0431\u0440\u043e\u0441 \u0438 \u00d7 \u2014 \u043d\u0430 \u043a\u043e\u0440\u043f\u0443\u0441\u0435";
            hintText.Foreground = new SolidColorBrush(Color.FromRgb(225, 221, 212));
            hintText.FontSize = 11.2;
            hintText.VerticalAlignment = VerticalAlignment.Center;
            hint.Child = hintText;
            overlay.Children.Add(hint);

            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromMilliseconds(70);
            syncTimer.Tick += delegate
            {
                if (!closing) SyncScreen();
            };
            hintTimer = new DispatcherTimer();
            hintTimer.Interval = TimeSpan.FromSeconds(12);
            hintTimer.Tick += delegate
            {
                HideHint();
            };
        }

        private void HideHint()
        {
            if (hint != null) hint.Visibility = Visibility.Collapsed;
            if (hintTimer != null) hintTimer.Stop();
        }

        private void AttachEvents()
        {
            root.Focusable = true;
            root.PreviewMouseLeftButtonDown += OnMouseDown;
            root.PreviewMouseLeftButtonUp += OnMouseUp;
            root.PreviewMouseMove += OnMouseMove;
            root.PreviewMouseWheel += OnMouseWheel;
            root.LostMouseCapture += OnLostCapture;
            root.MouseLeave += delegate
            {
                if (orbiting || pressedKey != null) return;
                if (hoverKey != null)
                {
                    hoverKey = null;
                    UpdateVisualState();
                }
                Cursor = Cursors.Arrow;
            };
            PreviewKeyDown += OnKeyDown;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (interactionBlocked) return;
            ReleaseTopmost();
            if (IsOverlaySource(e.OriginalSource)) return;
            root.Focus();
            Keyboard.Focus(root);
            Point p = e.GetPosition(viewport);
            Point3D hitPoint;
            string id = HitIdAt(p, out hitPoint);
            ThreeDLog.Write("mouse down " + p.X.ToString("F0") + "," + p.Y.ToString("F0") + " id=" + (id ?? "null"));
            pressedPad = 0;
            pressStart = p;
            pressMoved = false;
            if (id != null && id != "body")
            {
                pressedKey = id;
                if (id == "pad")
                {
                    pressedPad = PadZone(hitPoint);
                    if (pressedPad <= 0) pressedPad = 5;
                }
                hoverKey = id;
                UpdateVisualState();
                root.CaptureMouse();
                Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }

            orbiting = true;
            lastPoint = p;
            root.CaptureMouse();
            Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (interactionBlocked)
            {
                pressedKey = null;
                pressedPad = 0;
                orbiting = false;
                return;
            }
            if (pressedKey == null && !orbiting) return;

            string oldKey = pressedKey;
            int oldPad = pressedPad;
            bool wasOrbiting = orbiting;
            pressedKey = null;
            pressedPad = 0;
            orbiting = false;

            string activate = null;
            int padToSend = 0;
            if (oldKey != null)
            {
                Point p = e.GetPosition(viewport);
                double dx = p.X - pressStart.X;
                double dy = p.Y - pressStart.Y;
                bool nearPress = (dx * dx + dy * dy) <= 64;
                Point3D hitPoint;
                string id = HitIdAt(p, out hitPoint);
                if (oldKey == "pad")
                {
                    int zone = id == "pad" ? PadZone(hitPoint) : 0;
                    if (zone == oldPad && zone > 0)
                    {
                        activate = "pad";
                        padToSend = zone;
                    }
                    else if (nearPress && oldPad > 0)
                    {
                        activate = "pad";
                        padToSend = oldPad;
                    }
                }
                else if (id == oldKey || (nearPress && !pressMoved))
                {
                    activate = oldKey;
                }
            }

            root.ReleaseMouseCapture();
            UpdateVisualState();
            Cursor = Cursors.Arrow;
            HideHint();

            if (activate != null)
            {
                ThreeDLog.Write("mouse up activate=" + activate + (activate == "pad" ? " zone=" + padToSend : ""));
                if (activate == "pad") host.PressPad(padToSend);
                else if (!ActivateBodyControl(activate)) host.PressKey(activate);
            }
            else
            {
                ThreeDLog.Write("mouse up no activation, oldKey=" + (oldKey ?? "null") + " orbiting=" + wasOrbiting);
            }
            e.Handled = oldKey != null || wasOrbiting;
        }

        private bool ActivateBodyControl(string id)
        {
            if (id == "ui2d")
            {
                ThreeDLog.Write("body control 2D");
                host.ReturnToFlat();
                return true;
            }
            if (id == "uireset")
            {
                ThreeDLog.Write("body control reset");
                ResetView();
                HideHint();
                return true;
            }
            if (id == "uiclose")
            {
                ThreeDLog.Write("body control close");
                Close();
                return true;
            }
            return false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (interactionBlocked) return;
            if (orbiting)
            {
                Point p = e.GetPosition(viewport);
                double dx = p.X - lastPoint.X;
                double dy = p.Y - lastPoint.Y;
                lastPoint = p;
                RotateBy(dx, dy);
                HideHint();
                e.Handled = true;
                return;
            }
            if (IsOverlaySource(e.OriginalSource))
            {
                if (hoverKey != null)
                {
                    hoverKey = null;
                    UpdateVisualState();
                }
                return;
            }

            Point pos = e.GetPosition(viewport);
            if (pressedKey != null && !pressMoved)
            {
                double dx = pos.X - pressStart.X;
                double dy = pos.Y - pressStart.Y;
                if (dx * dx + dy * dy > 64) pressMoved = true;
            }
            Point3D hitPoint;
            string id = HitIdAt(pos, out hitPoint);
            string newHover = (id != null && id != "body") ? id : null;
            if (newHover != hoverKey)
            {
                hoverKey = newHover;
                UpdateVisualState();
            }
            if (pressedKey != null || newHover != null) Cursor = Cursors.Hand;
            else if (id == "body") Cursor = Cursors.SizeAll;
            else Cursor = Cursors.Arrow;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (interactionBlocked) return;
            ReleaseTopmost();
            if (e.Delta == 0) return;
            double factor = Math.Pow(1.11, -e.Delta / 120.0);
            distance *= factor;
            if (distance < 1180) distance = 1180;
            if (distance > 2250) distance = 2250;
            camera.Position = new Point3D(0, 0, distance);
            HideHint();
            e.Handled = true;
        }

        private void OnLostCapture(object sender, MouseEventArgs e)
        {
            if (pressedKey == null && !orbiting) return;
            pressedKey = null;
            pressedPad = 0;
            orbiting = false;
            UpdateVisualState();
            Cursor = Cursors.Arrow;
        }

        private string HitIdAt(Point point, out Point3D hitPoint)
        {
            string found = null;
            Point3D hp = new Point3D();
            HitTestResultCallback callback = delegate(HitTestResult result)
            {
                RayMeshGeometry3DHitTestResult ray = result as RayMeshGeometry3DHitTestResult;
                if (ray != null)
                {
                    string id;
                    if (modelIds.TryGetValue(ray.ModelHit, out id))
                    {
                        found = id;
                        hp = ray.PointHit;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };
            VisualTreeHelper.HitTest(viewport, null, callback, new PointHitTestParameters(point));
            hitPoint = hp;
            return found;
        }

        private int PadZone(Point3D worldPoint)
        {
            try
            {
                Matrix3D matrix = modelVisual.Transform.Value;
                matrix.Invert();
                Point3D local = matrix.Transform(worldPoint);
                double flatX = local.X + 240;
                double flatY = 430 - local.Y;
                System.Drawing.RectangleF pad = form.PadBounds;
                double cx = pad.X + pad.Width / 2.0;
                double cy = pad.Y + pad.Height / 2.0;
                int zone = 5;
                if (flatY < cy - 13) zone = 1;
                else if (flatY > cy + 13) zone = 2;
                else if (flatX < cx - 13) zone = 3;
                else if (flatX > cx + 13) zone = 4;
                ThreeDLog.Write("pad local=" + local.X.ToString("F1") + "," + local.Y.ToString("F1") +
                    " flat=" + flatX.ToString("F1") + "," + flatY.ToString("F1") +
                    " center=" + cx.ToString("F1") + "," + cy.ToString("F1") + " zone=" + zone);
                return zone;
            }
            catch
            {
                return 0;
            }
        }

        private bool IsOverlaySource(object source)
        {
            DependencyObject d = source as DependencyObject;
            while (d != null)
            {
                if (object.ReferenceEquals(d, overlay)) return true;
                if (d is Visual)
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(d);
                    if (parent == null) parent = LogicalTreeHelper.GetParent(d);
                    d = parent;
                }
                else
                {
                    d = LogicalTreeHelper.GetParent(d);
                }
            }
            return false;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (interactionBlocked) return;
            ReleaseTopmost();
            ThreeDLog.Write("key " + e.Key + " mods=" + Keyboard.Modifiers);
            if (e.Key == Key.F4)
            {
                host.ReturnToFlat();
                e.Handled = true;
                return;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) return;
            Forms.Keys key = MapKey(e.Key, Keyboard.Modifiers);
            if (key != Forms.Keys.None)
            {
                host.SendKey(key);
                e.Handled = true;
                HideHint();
            }
        }

        private static Forms.Keys MapKey(Key key, ModifierKeys modifiers)
        {
            Forms.Keys result = Forms.Keys.None;
            if (key >= Key.D0 && key <= Key.D9)
                result = (Forms.Keys)((int)Forms.Keys.D0 + ((int)key - (int)Key.D0));
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
                result = (Forms.Keys)((int)Forms.Keys.NumPad0 + ((int)key - (int)Key.NumPad0));
            else if (key >= Key.A && key <= Key.Z)
                result = (Forms.Keys)((int)Forms.Keys.A + ((int)key - (int)Key.A));
            else
            {
                switch (key)
                {
                    case Key.Enter: result = Forms.Keys.Enter; break;
                    case Key.Back: result = Forms.Keys.Back; break;
                    case Key.Delete: result = Forms.Keys.Delete; break;
                    case Key.Escape: result = Forms.Keys.Escape; break;
                    case Key.Left: result = Forms.Keys.Left; break;
                    case Key.Right: result = Forms.Keys.Right; break;
                    case Key.Up: result = Forms.Keys.Up; break;
                    case Key.Down: result = Forms.Keys.Down; break;
                    case Key.OemPlus: result = Forms.Keys.Oemplus; break;
                    case Key.OemMinus: result = Forms.Keys.OemMinus; break;
                    case Key.OemPeriod: result = Forms.Keys.OemPeriod; break;
                    case Key.OemOpenBrackets: result = Forms.Keys.OemOpenBrackets; break;
                    case Key.OemCloseBrackets: result = Forms.Keys.OemCloseBrackets; break;
                    case Key.OemQuestion: result = Forms.Keys.OemQuestion; break;
                    case Key.Add: result = Forms.Keys.Add; break;
                    case Key.Subtract: result = Forms.Keys.Subtract; break;
                    case Key.Multiply: result = Forms.Keys.Multiply; break;
                    case Key.Divide: result = Forms.Keys.Divide; break;
                    case Key.Decimal: result = Forms.Keys.Decimal; break;
                    case Key.F1: result = Forms.Keys.F1; break;
                    case Key.F2: result = Forms.Keys.F2; break;
                    case Key.F3: result = Forms.Keys.F3; break;
                }
            }
            if (result != Forms.Keys.None)
            {
                if ((modifiers & ModifierKeys.Shift) != 0) result |= Forms.Keys.Shift;
                if ((modifiers & ModifierKeys.Control) != 0) result |= Forms.Keys.Control;
            }
            return result;
        }
    }
}
