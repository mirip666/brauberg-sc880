using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SC880
{
    internal enum KeyKind
    {
        Function,
        Number,
        Red,
        Soft
    }

    internal sealed class CalcKey
    {
        public string Id;
        public string Primary;
        public string Shift;
        public string Alpha;
        public string Tertiary;
        public Color ShiftColor = Palette.Orange;
        public Color AlphaColor = Palette.Pink;
        public Color TertiaryColor = Palette.Teal;
        public KeyKind Kind;
        public RectangleF Rect;
        public float PrimarySize = 13f;
        public bool IsBlank;
    }

    internal sealed class HistoryEntry
    {
        public string Expression;
        public string Result;
        public Cplx Value;
    }

    internal sealed class Calc3DSnapshot
    {
        public string Expression = "";
        public string Result = "";
        public string Error;
        public string Mode = "";
        public string Indicators = "";
        public string Toast;
        public bool PowerOff;
        public bool ShowingResult;
        public bool Blink;

        public string Signature()
        {
            return (PowerOff ? "1" : "0") + "|" + (ShowingResult ? "1" : "0") + "|" +
                   (Blink ? "1" : "0") + "|" + Expression + "|" + Result + "|" +
                   (Error == null ? "" : Error) + "|" + Mode + "|" + Indicators + "|" +
                   (Toast == null ? "" : Toast);
        }
    }

    internal sealed partial class Sc880Form : Form
    {
        internal const float BaseW = 480f;
        internal const float BaseH = 880f;

        private readonly CalcState state = new CalcState();
        private readonly List<CalcKey> keys = new List<CalcKey>();
        private readonly List<HistoryEntry> history = new List<HistoryEntry>();
        private readonly FontBank fonts = new FontBank();
        private readonly Timer blinkTimer;
        private readonly Timer toastTimer;
        private readonly Timer dialogWatch;
        private readonly Random random = new Random();
        private readonly List<StatPoint> statData = new List<StatPoint>();
        private readonly MatrixData[] matrices = new MatrixData[3];
        private readonly VectorData[] vectors = new VectorData[3];

        private float scale = 1f;
        private string expression = "";
        private int caret;
        private string resultText = "";
        private string errorText;
        private string toast;
        private bool showingResult;
        private bool shift;
        private bool alpha;
        private bool hyp;
        private bool powerOff;
        private bool insertMode = true;
        private bool engMode;
        private bool blink = true;
        private bool hasLastValue;
        private Cplx lastValue;
        private int historyIndex;
        private string savedExpression = "";
        private int savedCaret;
        private int baseValue = 10;

        private RectangleF padRect = new RectangleF(186, 295, 108, 93);
        private RectangleF threeDButton = new RectangleF(262, 27, 48, 30);
        private string pressedId;
        private int pressedPad;
        private string hoverId;
        private int hoverPad;
        private bool hover3D;
        private Label displayProbe;
        private ThreeDHost threeDView;
        private bool dialogRaised;
        private readonly bool startIn3D;

        internal Sc880Form() : this(false)
        {
        }

        internal Sc880Form(bool startIn3D)
        {
            this.startIn3D = startIn3D;
            Text = "BRAUBERG SC-880";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Palette.FormBack;
            AutoScaleMode = AutoScaleMode.None;
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ClientSize = new Size(480, 880);
            displayProbe = new Label();
            displayProbe.AutoSize = false;
            displayProbe.SetBounds(-40, -40, 10, 10);
            displayProbe.Name = "displayProbe";
            displayProbe.TabStop = false;
            displayProbe.Text = "";
            Controls.Add(displayProbe);
            BuildKeys();
            blinkTimer = new Timer();
            blinkTimer.Interval = 530;
            blinkTimer.Tick += delegate
            {
                blink = !blink;
                Invalidate();
            };
            blinkTimer.Start();
            toastTimer = new Timer();
            toastTimer.Interval = 1400;
            toastTimer.Tick += delegate
            {
                toast = null;
                toastTimer.Stop();
                Invalidate();
            };
            dialogWatch = new Timer();
            dialogWatch.Interval = 140;
            dialogWatch.Tick += delegate { KeepDialogsAbove3D(); };
        }

        internal CalcState State
        {
            get { return state; }
        }

        internal string ExpressionText
        {
            get { return expression; }
        }

        internal bool HasLastValue
        {
            get { return hasLastValue; }
        }

        internal Cplx LastValue
        {
            get { return lastValue; }
        }

        internal List<StatPoint> StatData
        {
            get { return statData; }
        }

        internal MatrixData[] Matrices
        {
            get { return matrices; }
        }

        internal VectorData[] Vectors
        {
            get { return vectors; }
        }

        internal int BaseValue
        {
            get { return baseValue; }
            set { baseValue = value; Invalidate(); }
        }

        internal List<CalcKey> KeyDefinitions
        {
            get { return keys; }
        }

        internal RectangleF PadBounds
        {
            get { return padRect; }
        }

        internal Calc3DSnapshot Create3DSnapshot()
        {
            Calc3DSnapshot s = new Calc3DSnapshot();
            s.PowerOff = powerOff;
            s.Expression = expression;
            s.Result = showingResult ? resultText : "";
            s.Error = errorText;
            s.ShowingResult = showingResult;
            s.Blink = blink;
            s.Toast = toast;

            StringBuilder indicators = new StringBuilder();
            if (state.Memory.Magnitude > 5e-14) indicators.Append("M ");
            if (shift) indicators.Append("S ");
            if (alpha) indicators.Append("A ");
            if (hyp) indicators.Append("hyp ");
            s.Indicators = indicators.ToString();

            string angle = state.Angle == AngleUnit.Degrees ? "D" : (state.Angle == AngleUnit.Radians ? "R" : "G");
            s.Mode = state.Mode == CalcMode.BaseN
                ? "BASE " + BaseName(baseValue)
                : state.ModeName + "  " + angle + "  Math";
            return s;
        }

        internal void PressKeyExternal(string id)
        {
            PressKey(id);
        }

        internal void PressPadExternal(int pad)
        {
            PressPad(pad);
        }

        internal void ProcessKeyExternal(Keys keyData)
        {
            ProcessKey(new KeyEventArgs(keyData));
        }

        private void Open3DView()
        {
            ThreeDLog.Write("open 3D requested, existing=" + (threeDView != null));
            if (threeDView != null)
            {
                threeDView.Activate();
                return;
            }
            ThreeDHost host = null;
            try
            {
                host = new ThreeDHost(this);
                threeDView = host;
                Hide();
                host.Start();
                dialogRaised = false;
                dialogWatch.Start();
            }
            catch (Exception ex)
            {
                ThreeDLog.Write("open 3D failed: " + ex);
                dialogWatch.Stop();
                threeDView = null;
                if (host != null) host.Shutdown();
                if (!Visible) Show();
                UiKit.Error(this, "Не удалось открыть 3D-вид: " + ex.Message);
            }
        }

        private void KeepDialogsAbove3D()
        {
            if (threeDView == null)
            {
                dialogWatch.Stop();
                return;
            }
            bool any = false;
            FormCollection forms = Application.OpenForms;
            for (int i = 0; i < forms.Count; i++)
            {
                Form f = forms[i];
                if (f == null || object.ReferenceEquals(f, this) || !f.Visible) continue;
                any = true;
                if (!f.TopMost)
                {
                    f.TopMost = true;
                    f.Activate();
                    ThreeDLog.Write("dialog raised above 3D: " + f.Text);
                }
            }
            if (any != dialogRaised)
            {
                dialogRaised = any;
                ThreeDLog.Write("dialog watch state=" + any);
                threeDView.SetDialogOpen(any);
                if (!any) threeDView.Activate();
            }
        }

        internal void OnThreeDClosed(bool exitApp)
        {
            ThreeDLog.Write("OnThreeDClosed exitApp=" + exitApp);
            dialogWatch.Stop();
            dialogRaised = false;
            threeDView = null;
            if (exitApp)
            {
                Close();
                return;
            }
            if (!Visible) Show();
            Activate();
            Invalidate();
        }

        internal void OnThreeDFailed(Exception ex)
        {
            ThreeDLog.Write("OnThreeDFailed: " + ex);
            dialogWatch.Stop();
            dialogRaised = false;
            ThreeDHost host = threeDView;
            threeDView = null;
            if (host != null) host.Shutdown();
            if (!Visible) Show();
            Activate();
            UiKit.Error(this, "Не удалось открыть 3D-вид: " + ex.Message);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyDpiScale();
            fonts.SetScale(scale);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ThreeDLog.Write("form shown startIn3D=" + startIn3D);
            if (!startIn3D) return;
            BeginInvoke(new Action(Open3DView));
        }

        private void ApplyDpiScale()
        {
            float dpi = 96f;
            using (Graphics g = CreateGraphics()) dpi = g.DpiX;
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            float byDpi = dpi / 96f;
            float byHeight = (wa.Height - 90f) / BaseH;
            float factor = Math.Min(byDpi, byHeight);
            if (factor < 0.7f) factor = 0.7f;
            int w = (int)Math.Round(BaseW * factor);
            int h = (int)Math.Round(BaseH * factor);
            ClientSize = new Size(w, h);
            scale = Math.Min(ClientSize.Width / BaseW, ClientSize.Height / BaseH);
            fonts.SetScale(scale);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            float s = Math.Min(ClientSize.Width / BaseW, ClientSize.Height / BaseH);
            if (Math.Abs(s - scale) > 0.0005f)
            {
                scale = s;
                fonts.SetScale(scale);
            }
            Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (threeDView != null)
            {
                ThreeDHost host = threeDView;
                threeDView = null;
                host.Shutdown();
            }
            blinkTimer.Stop();
            toastTimer.Stop();
            dialogWatch.Stop();
            blinkTimer.Dispose();
            toastTimer.Dispose();
            dialogWatch.Dispose();
            fonts.Dispose();
            base.OnFormClosed(e);
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new Sc880Accessible(this);
        }

        internal string AccessibilitySnapshot()
        {
            if (!string.IsNullOrEmpty(errorText)) return "ОШИБКА: " + errorText;
            if (showingResult && !string.IsNullOrEmpty(resultText))
                return expression + " = " + resultText;
            return expression;
        }

        private sealed class Sc880Accessible : System.Windows.Forms.Control.ControlAccessibleObject
        {
            private readonly Sc880Form owner;

            public Sc880Accessible(Sc880Form form) : base(form)
            {
                owner = form;
            }

            public override string Name
            {
                get
                {
                    string snapshot = owner.AccessibilitySnapshot();
                    return string.IsNullOrEmpty(snapshot) ? owner.Text : owner.Text + " | " + snapshot;
                }
                set { base.Name = value; }
            }
        }

        private float S(float value)
        {
            return value * scale;
        }

        private RectangleF Scaled(RectangleF r)
        {
            return new RectangleF(S(r.X), S(r.Y), S(r.Width), S(r.Height));
        }

        private PointF Unscale(Point p)
        {
            return new PointF(p.X / scale, p.Y / scale);
        }

        // ---------- key layout ----------

        private static float FCol(int i)
        {
            return 40f + i * 69.9f;
        }

        private static float FRow(int i)
        {
            return 354f + i * 62f;
        }

        private static float NCol(int i)
        {
            return 49f + i * 82.1f;
        }

        private static float NRow(int i)
        {
            return 620f + i * 62f;
        }

        private CalcKey AddKey(string id, string primary, string shiftText, string alphaText, string tertiary,
            KeyKind kind, float x, float y, float w, float h, float size)
        {
            CalcKey k = new CalcKey();
            k.Id = id;
            k.Primary = primary;
            k.Shift = shiftText;
            k.Alpha = alphaText;
            k.Tertiary = tertiary;
            k.Kind = kind;
            k.Rect = new RectangleF(x, y, w, h);
            k.PrimarySize = size;
            k.IsBlank = string.IsNullOrEmpty(primary);
            keys.Add(k);
            return k;
        }

        private CalcKey Key(string id)
        {
            for (int i = 0; i < keys.Count; i++) if (keys[i].Id == id) return keys[i];
            return null;
        }

        private void BuildKeys()
        {
            // top soft keys
            AddKey("shift", "", null, null, null, KeyKind.Soft, 44, 300, 56, 34, 10f).IsBlank = true;
            AddKey("alpha", "", null, null, null, KeyKind.Soft, 114, 300, 56, 34, 10f).IsBlank = true;
            AddKey("mode", "", null, null, null, KeyKind.Soft, 310, 304, 56, 34, 10f).IsBlank = true;
            AddKey("on", "", null, null, null, KeyKind.Soft, 386, 295, 56, 34, 10f).IsBlank = true;

            // row 1
            AddKey("calc", "CALC", "SOLVE", null, "=", KeyKind.Function, FCol(0), FRow(0), 63.5f, 46, 12.5f);
            AddKey("integral", "∫dx", "d/dx", null, ":", KeyKind.Function, FCol(1), FRow(0), 63.5f, 46, 15f);
            AddKey("xinv", "x⁻¹", "x!", null, null, KeyKind.Function, FCol(4), FRow(0), 63.5f, 46, 16f);
            AddKey("logbox", "log□", "Σ", null, null, KeyKind.Function, FCol(5), FRow(0), 63.5f, 46, 14f);

            // row 2
            AddKey("frac", "a b/c", "d/c", null, null, KeyKind.Function, FCol(0), FRow(1), 63.5f, 46, 12.5f);
            AddKey("sqrt", "√", "³√", null, null, KeyKind.Function, FCol(1), FRow(1), 63.5f, 46, 21f);
            AddKey("square", "x²", "x³", null, "DEC", KeyKind.Function, FCol(2), FRow(1), 63.5f, 46, 17f);
            AddKey("power", "xʸ", "ⁿ√", null, "HEX", KeyKind.Function, FCol(3), FRow(1), 63.5f, 46, 16f);
            AddKey("log", "log", "10ˣ", null, "BIN", KeyKind.Function, FCol(4), FRow(1), 63.5f, 46, 16f);
            AddKey("ln", "ln", "eˣ", null, "OCT", KeyKind.Function, FCol(5), FRow(1), 63.5f, 46, 17f);

            // row 3
            AddKey("neg", "(−)", "r∠θ", "A", null, KeyKind.Function, FCol(0), FRow(2), 63.5f, 46, 14f);
            AddKey("dms", "° ' \"", "←", "B", null, KeyKind.Function, FCol(1), FRow(2), 63.5f, 46, 14f);
            AddKey("hyp", "hyp", "Abs", "C", null, KeyKind.Function, FCol(2), FRow(2), 63.5f, 46, 14f);
            AddKey("sin", "sin", "sin⁻¹", "D", null, KeyKind.Function, FCol(3), FRow(2), 63.5f, 46, 15f);
            AddKey("cos", "cos", "cos⁻¹", "E", null, KeyKind.Function, FCol(4), FRow(2), 63.5f, 46, 15f);
            AddKey("tan", "tan", "tan⁻¹", "F", null, KeyKind.Function, FCol(5), FRow(2), 63.5f, 46, 15f);

            // row 4
            AddKey("rcl", "RCL", "STO", null, null, KeyKind.Function, FCol(0), FRow(3), 63.5f, 46, 13f);
            AddKey("eng", "ENG", "←", "i", null, KeyKind.Function, FCol(1), FRow(3), 63.5f, 46, 13f);
            AddKey("lparen", "(", "%", "X", null, KeyKind.Function, FCol(2), FRow(3), 63.5f, 46, 17f);
            AddKey("rparen", ")", ",", "Y", null, KeyKind.Function, FCol(3), FRow(3), 63.5f, 46, 17f);
            AddKey("sd", "S⇔D", "a b/c⇄d/c", null, null, KeyKind.Function, FCol(4), FRow(3), 63.5f, 46, 12.5f);
            AddKey("mplus", "M+", "M−", "M", null, KeyKind.Function, FCol(5), FRow(3), 63.5f, 46, 15f);

            // numpad row 1
            AddKey("7", "7", "CONST", null, null, KeyKind.Number, NCol(0), NRow(0), 74.8f, 46, 21f);
            AddKey("8", "8", "CONV", null, null, KeyKind.Number, NCol(1), NRow(0), 74.8f, 46, 21f);
            AddKey("9", "9", "CLR", null, null, KeyKind.Number, NCol(2), NRow(0), 74.8f, 46, 21f);
            AddKey("del", "DEL", "INS", null, null, KeyKind.Red, NCol(3), NRow(0), 74.8f, 46, 15f);
            AddKey("ac", "AC", "OFF", null, null, KeyKind.Red, NCol(4), NRow(0), 74.8f, 46, 15f);

            // numpad row 2
            AddKey("4", "4", "MATRIX", null, null, KeyKind.Number, NCol(0), NRow(1), 74.8f, 46, 21f);
            AddKey("5", "5", "VECTOR", null, null, KeyKind.Number, NCol(1), NRow(1), 74.8f, 46, 21f);
            AddKey("6", "6", null, null, null, KeyKind.Number, NCol(2), NRow(1), 74.8f, 46, 21f);
            AddKey("times", "×", "nPr", null, null, KeyKind.Number, NCol(3), NRow(1), 74.8f, 46, 22f);
            AddKey("div", "÷", "nCr", null, null, KeyKind.Number, NCol(4), NRow(1), 74.8f, 46, 22f);

            // numpad row 3
            AddKey("1", "1", "STAT", null, null, KeyKind.Number, NCol(0), NRow(2), 74.8f, 46, 21f);
            AddKey("2", "2", "CMPLX", null, null, KeyKind.Number, NCol(1), NRow(2), 74.8f, 46, 21f);
            AddKey("3", "3", "BASE", null, null, KeyKind.Number, NCol(2), NRow(2), 74.8f, 46, 21f);
            AddKey("plus", "+", "Pol", null, null, KeyKind.Number, NCol(3), NRow(2), 74.8f, 46, 23f);
            AddKey("minus", "−", "Rec", null, null, KeyKind.Number, NCol(4), NRow(2), 74.8f, 46, 23f);

            // numpad row 4
            AddKey("0", "0", "Rnd", null, null, KeyKind.Number, NCol(0), NRow(3), 74.8f, 46, 21f);
            CalcKey dot = AddKey("dot", ".", "Ran#", "RanInt", null, KeyKind.Number, NCol(1), NRow(3), 74.8f, 46, 23f);
            CalcKey exp = AddKey("exp", "×10", "π", "e", null, KeyKind.Number, NCol(2), NRow(3), 74.8f, 46, 15f);
            AddKey("ans", "Ans", "DRG▶", null, null, KeyKind.Number, NCol(3), NRow(3), 74.8f, 46, 16f);
            AddKey("equals", "=", null, null, null, KeyKind.Number, NCol(4), NRow(3), 74.8f, 46, 23f);

            // alpha colors
            CalcKey eng = Key("eng");
            eng.AlphaColor = Palette.Purple;
            CalcKey expKey = Key("exp");
            expKey.AlphaColor = Palette.Pink;
            CalcKey neg = Key("neg");
            neg.AlphaColor = Palette.Pink;
            CalcKey calcKey = Key("calc");
            calcKey.TertiaryColor = Palette.Pink;
            CalcKey integralKey = Key("integral");
            integralKey.TertiaryColor = Palette.Pink;
        }

        // ---------- public bridge for dialogs ----------

        internal void InsertTextExternal(string text)
        {
            Insert(text);
            Invalidate();
        }

        internal void ShowExternalResult(string label, Cplx value)
        {
            if (string.IsNullOrEmpty(label)) label = Parseable(value);
            expression = label;
            caret = expression.Length;
            lastValue = value;
            hasLastValue = true;
            state.Ans = value;
            resultText = FormatValue(value);
            errorText = null;
            showingResult = true;
            engMode = false;
            AddHistory(expression, resultText, value);
            Invalidate();
        }

        internal void InsertNumber(Cplx value)
        {
            Insert(Parseable(value));
            Invalidate();
        }

        internal Cplx CurrentValue()
        {
            if (hasLastValue) return lastValue;
            Cplx value;
            string display;
            string error;
            if (TryEvaluate(expression, out value, out display, out error)) return value;
            throw new CalcError("Нет значения");
        }

        internal Cplx CurrentValueFrom(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Trim().Length == 0) return CurrentValue();
            try
            {
                EvalResult r = Engine.Evaluate(text, state);
                return r.Value;
            }
            catch (CalcError ex)
            {
                throw new CalcError("«" + text + "»: " + ex.Message);
            }
        }

        internal void UseBaseResult(string exprText, long value, int numberBase)
        {
            baseValue = numberBase;
            state.Mode = CalcMode.BaseN;
            expression = exprText;
            caret = expression.Length;
            lastValue = Cplx.Real(value);
            hasLastValue = true;
            state.Ans = lastValue;
            resultText = BaseN.Format(value, numberBase);
            errorText = null;
            showingResult = true;
            engMode = false;
            AddHistory(expression, resultText, lastValue);
            Invalidate();
        }

        internal void Toast(string text)
        {
            toast = text;
            toastTimer.Stop();
            toastTimer.Start();
            Invalidate();
        }

        internal void ShowError(string text)
        {
            errorText = text;
            showingResult = false;
            resultText = "";
            Invalidate();
        }

        // ---------- input ----------

        private static bool IsBinaryOperator(string t)
        {
            return t == "+" || t == "×" || t == "÷" || t == "^" || t == "∠" || t == "×10^(";
        }

        private static bool IsPostfix(string t)
        {
            return t == "!" || t == "²" || t == "³" || t == "⁻¹" || t == "%";
        }

        private void Insert(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (showingResult)
            {
                if (IsBinaryOperator(text) || IsPostfix(text))
                {
                    expression = "Ans";
                    caret = 3;
                }
                else if (text == ")" || text == ",")
                {
                    showingResult = false;
                    resultText = "";
                }
                else
                {
                    expression = "";
                    caret = 0;
                    resultText = "";
                    showingResult = false;
                }
            }
            if (errorText != null)
            {
                errorText = null;
                if (!IsBinaryOperator(text))
                {
                    expression = "";
                    caret = 0;
                }
            }
            if (expression.Length == 0 && IsBinaryOperator(text)) expression = "Ans";
            if (caret > expression.Length) caret = expression.Length;
            if (!state.InsertMode && caret < expression.Length && text.Length > 0)
            {
                int remove = 1;
                if (caret + 1 < expression.Length && expression[caret] == '⁻' && expression[caret + 1] == '¹') remove = 2;
                expression = expression.Remove(caret, Math.Min(remove, expression.Length - caret));
            }
            if (text == "." && caret > 0)
            {
                int start = caret - 1;
                while (start >= 0 && (char.IsDigit(expression[start]) || expression[start] == '.')) start--;
                string part = expression.Substring(start + 1, caret - start - 1);
                if (part.IndexOf('.') >= 0) return;
            }
            expression = expression.Insert(caret, text);
            caret += text.Length;
            showingResult = false;
            resultText = "";
            engMode = false;
            historyIndex = history.Count;
            blink = true;
            UpdateInsertIndicator();
            Invalidate();
        }

        private void UpdateInsertIndicator()
        {
            insertMode = state.InsertMode;
        }

        private void Backspace()
        {
            if (showingResult)
            {
                showingResult = false;
                resultText = "";
            }
            if (errorText != null) errorText = null;
            if (caret == 0) { Invalidate(); return; }
            int remove = 1;
            if (caret >= 2 && expression[caret - 2] == '⁻' && expression[caret - 1] == '¹') remove = 2;
            expression = expression.Remove(caret - remove, remove);
            caret -= remove;
            historyIndex = history.Count;
            Invalidate();
        }

        private void DeleteAtCaret()
        {
            if (showingResult) { showingResult = false; resultText = ""; }
            if (caret >= expression.Length) { Invalidate(); return; }
            int remove = 1;
            if (caret + 1 < expression.Length && expression[caret] == '⁻' && expression[caret + 1] == '¹') remove = 2;
            expression = expression.Remove(caret, remove);
            Invalidate();
        }

        private void MoveCaret(int delta)
        {
            if (showingResult)
            {
                showingResult = false;
                resultText = "";
            }
            caret += delta;
            if (caret < 0) caret = 0;
            if (caret > expression.Length) caret = expression.Length;
            blink = true;
            Invalidate();
        }

        private void ClearAll()
        {
            expression = "";
            caret = 0;
            resultText = "";
            errorText = null;
            showingResult = false;
            engMode = false;
            hasLastValue = false;
            historyIndex = history.Count;
            blink = true;
            Invalidate();
        }

        private void BeginEdit()
        {
            if (!showingResult) return;
            showingResult = false;
            resultText = "";
        }

        // ---------- evaluation ----------

        private bool TryEvaluate(string expr, out Cplx value, out string display, out string error)
        {
            value = Cplx.Zero;
            display = "";
            error = null;
            if (expr == null || expr.Trim().Length == 0)
            {
                error = "Пустое выражение";
                return false;
            }
            try
            {
                if (state.Mode == CalcMode.BaseN)
                {
                    BaseNResult br = BaseN.Evaluate(expr, baseValue);
                    value = Cplx.Real(br.Value);
                    display = br.Display;
                }
                else
                {
                    EvalResult r = Engine.Evaluate(expr, state);
                    value = r.Value;
                    display = FormatValue(value);
                }
                if (!value.IsFinite) throw new CalcError("Результат вне диапазона");
                return true;
            }
            catch (CalcError ex)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private string FormatValue(Cplx value)
        {
            if (engMode && Math.Abs(value.Im) < 1e-12)
                return Engineering(value.Re);
            return NumberText.Format(value, state);
        }

        private static string Engineering(double value)
        {
            if (value == 0) return "0";
            int exp = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            int e3 = (int)(Math.Floor(exp / 3.0) * 3);
            double mantissa = value / Math.Pow(10, e3);
            string m = mantissa.ToString("G10", CultureInfo.InvariantCulture).Replace('-', '−');
            if (e3 == 0) return m;
            return m + "×10" + NumberText.Superscript(e3);
        }

        private void Evaluate()
        {
            if (expression.Trim().Length == 0)
            {
                if (hasLastValue)
                {
                    resultText = FormatValue(lastValue);
                    showingResult = true;
                    Invalidate();
                }
                return;
            }
            Cplx value;
            string display;
            string error;
            if (TryEvaluate(expression, out value, out display, out error))
            {
                lastValue = value;
                hasLastValue = true;
                state.Ans = value;
                resultText = display;
                errorText = null;
                showingResult = true;
                engMode = false;
                AddHistory(expression, resultText, value);
                historyIndex = history.Count;
                blink = false;
            }
            else
            {
                errorText = error;
                resultText = "";
                showingResult = false;
            }
            Invalidate();
        }

        private void AddHistory(string expr, string result, Cplx value)
        {
            if (expr == null || expr.Trim().Length == 0) return;
            if (history.Count > 0)
            {
                HistoryEntry last = history[history.Count - 1];
                if (last.Expression == expr && last.Result == result) return;
            }
            HistoryEntry e = new HistoryEntry();
            e.Expression = expr;
            e.Result = result;
            e.Value = value;
            history.Add(e);
            if (history.Count > 80) history.RemoveAt(0);
            historyIndex = history.Count;
        }

        private void NavigateHistory(int direction)
        {
            if (history.Count == 0) return;
            if (historyIndex == history.Count && direction < 0)
            {
                savedExpression = expression;
                savedCaret = caret;
            }
            int next = historyIndex + direction;
            if (next < 0) next = 0;
            if (next > history.Count) next = history.Count;
            historyIndex = next;
            if (historyIndex == history.Count)
            {
                expression = savedExpression;
                caret = Math.Min(savedCaret, expression.Length);
                showingResult = false;
                resultText = "";
            }
            else
            {
                HistoryEntry e = history[historyIndex];
                expression = e.Expression;
                caret = expression.Length;
                resultText = e.Result;
                lastValue = e.Value;
                hasLastValue = true;
                showingResult = true;
                errorText = null;
            }
            Invalidate();
        }

        private string Parseable(Cplx value)
        {
            if (Math.Abs(value.Im) < 5e-13)
            {
                double re = value.Re;
                if (Math.Abs(re) < 5e-13) re = 0;
                return re.ToString("G15", CultureInfo.InvariantCulture).Replace('-', '−');
            }
            string reText = value.Re.ToString("G15", CultureInfo.InvariantCulture).Replace('-', '−');
            string imText = Math.Abs(value.Im).ToString("G15", CultureInfo.InvariantCulture);
            return "(" + reText + (value.Im < 0 ? "−" : "+") + imText + "i)";
        }

        // ---------- memory & variables ----------

        private void MemoryAdd(int sign)
        {
            Cplx value;
            if (!hasLastValue)
            {
                string display, error;
                if (!TryEvaluate(expression, out value, out display, out error))
                {
                    ShowError("Нет значения для памяти");
                    return;
                }
            }
            else value = lastValue;
            state.Memory = sign > 0 ? state.Memory + value : state.Memory - value;
            Toast((sign > 0 ? "M+ = " : "M− = ") + NumberText.Format(state.Memory, state));
            Invalidate();
        }

        private void MemorySubtract()
        {
            MemoryAdd(-1);
        }

        private void ShowRecall()
        {
            string[] labels = new string[9];
            for (int i = 0; i < 6; i++)
            {
                char c = (char)('A' + i);
                labels[i] = c + " = " + ValueText(state.Vars[c]);
            }
            labels[6] = "X = " + ValueText(state.Vars['X']);
            labels[7] = "Y = " + ValueText(state.Vars['Y']);
            labels[8] = "M = " + ValueText(state.Memory) + "   (Ans = " + ValueText(state.Ans) + ")";
            int idx = ListChoiceForm.Choose(this, "ВЫЗОВ ПЕРЕМЕННОЙ", "RCL: выберите переменную", labels, 0);
            if (idx < 0) return;
            Cplx v;
            if (idx < 6) v = state.Vars[(char)('A' + idx)];
            else if (idx == 6) v = state.Vars['X'];
            else if (idx == 7) v = state.Vars['Y'];
            else v = state.Memory;
            Insert(Parseable(v));
        }

        private void ShowStore()
        {
            string[] labels = new string[10];
            for (int i = 0; i < 6; i++)
            {
                char c = (char)('A' + i);
                labels[i] = "A…F: " + c + " = " + ValueText(state.Vars[c]);
            }
            labels[6] = "X";
            labels[7] = "Y";
            labels[8] = "M (память)";
            labels[9] = "Ans";
            int idx = ListChoiceForm.Choose(this, "ЗАПИСЬ В ПАМЯТЬ", "STO: выберите, куда записать результат", labels, 0);
            if (idx < 0) return;
            Cplx value;
            if (!hasLastValue)
            {
                string display, error;
                if (!TryEvaluate(expression, out value, out display, out error))
                {
                    ShowError("Нет значения для STO");
                    return;
                }
            }
            else value = lastValue;
            if (idx < 6) state.Vars[(char)('A' + idx)] = value;
            else if (idx == 6) state.Vars['X'] = value;
            else if (idx == 7) state.Vars['Y'] = value;
            else if (idx == 8) state.Memory = value;
            else state.Ans = value;
            Toast("STO → " + (idx < 6 ? ((char)('A' + idx)).ToString() : idx == 6 ? "X" : idx == 7 ? "Y" : idx == 8 ? "M" : "Ans"));
        }

        private string ValueText(Cplx v)
        {
            try { return NumberText.Format(v, state); }
            catch { return Parseable(v); }
        }

        private List<char> FindVariables()
        {
            string stripped = Regex.Replace(expression,
                "(?i)\\b(asin|acos|atan|sinh|cosh|tanh|asinh|acosh|atanh|sin|cos|tan|logb|log2|log|ln|exp|sqrt|cbrt|nroot|abs|arg|conj|re|im|int|frac|round|rnd|gcd|lcm|npr|ncr|ranint|pol|rec|min|max|ans|ran)\\b",
                " ");
            List<char> found = new List<char>();
            for (int i = 0; i < stripped.Length; i++)
            {
                char c = stripped[i];
                if ("ABCDEFXYM".IndexOf(c) >= 0 && !found.Contains(c)) found.Add(c);
            }
            return found;
        }

        private void RunCalc()
        {
            List<char> vars = FindVariables();
            if (vars.Count == 0)
            {
                Evaluate();
                return;
            }
            string[] labels = new string[vars.Count];
            string[] defaults = new string[vars.Count];
            for (int i = 0; i < vars.Count; i++)
            {
                labels[i] = vars[i] + " =";
                defaults[i] = UiKit.FormatNumber(state.Vars[vars[i]].Re);
            }
            string[] values = FieldsForm.Ask(this, "CALC — ЗНАЧЕНИЯ ПЕРЕМЕННЫХ",
                "Введите значения переменных для вычисления выражения", labels, defaults);
            if (values == null) return;
            try
            {
                for (int i = 0; i < vars.Count; i++)
                    state.Vars[vars[i]] = Cplx.Real(UiKit.ParseNumber(values[i]));
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
                return;
            }
            Evaluate();
        }

        // ---------- tools ----------

        private void OpenSetup()
        {
            using (SetupForm f = new SetupForm(state))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    insertMode = state.InsertMode;
                    Invalidate();
                }
            }
        }

        private void OpenModes()
        {
            CalcMode chosen;
            if (!ModeForm.Choose(this, state.Mode, out chosen)) return;
            state.Mode = chosen;
            ClearAll();
            OpenModeTool();
        }

        private void OpenModeTool()
        {
            switch (state.Mode)
            {
                case CalcMode.Stat:
                    using (StatForm f = new StatForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.Eqn:
                    using (EqnForm f = new EqnForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.Matrix:
                    using (MatrixForm f = new MatrixForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.Vector:
                    using (VectorForm f = new VectorForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.Table:
                    using (TableForm f = new TableForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.BaseN:
                    using (BaseForm f = new BaseForm(this, baseValue)) f.ShowDialog(this);
                    break;
                case CalcMode.Verif:
                    using (VerifyForm f = new VerifyForm(this)) f.ShowDialog(this);
                    break;
                case CalcMode.Cmplx:
                    state.Mode = CalcMode.Cmplx;
                    Toast("CMPLX: доступны i, ∠, arg, conj, Re, Im");
                    break;
            }
            Invalidate();
        }

        private void OpenSolve()
        {
            using (SolveForm f = new SolveForm(this)) f.ShowDialog(this);
        }

        private void OpenIntegral()
        {
            using (IntegralForm f = new IntegralForm(this)) f.ShowDialog(this);
        }

        private void OpenDerivative()
        {
            using (DerivativeForm f = new DerivativeForm(this)) f.ShowDialog(this);
        }

        private void OpenSum()
        {
            using (SumForm f = new SumForm(this)) f.ShowDialog(this);
        }

        private void OpenConstants()
        {
            using (ConstantsForm f = new ConstantsForm(this)) f.ShowDialog(this);
        }

        private void OpenConversion()
        {
            using (ConversionForm f = new ConversionForm(this)) f.ShowDialog(this);
        }

        private void OpenBase()
        {
            using (BaseForm f = new BaseForm(this, baseValue)) f.ShowDialog(this);
        }

        private void OpenAngleConvert()
        {
            using (AngleConvertForm f = new AngleConvertForm(this)) f.ShowDialog(this);
        }

        private void OpenClearMenu()
        {
            string[] options =
            {
                "Всё (кроме настроек)",
                "Память M",
                "Переменные A–F, X, Y и Ans",
                "История вычислений",
                "Данные STAT",
                "Матрицы и векторы",
                "Настройки (DEG, формат)",
                "Отмена"
            };
            int idx = ListChoiceForm.Choose(this, "ОЧИСТКА (CLR)", "Что очистить?", options, 0);
            switch (idx)
            {
                case 0:
                    ClearAll();
                    state.Memory = Cplx.Zero;
                    state.Ans = Cplx.Zero;
                    for (char c = 'A'; c <= 'F'; c++) state.Vars[c] = Cplx.Zero;
                    state.Vars['X'] = Cplx.Zero;
                    state.Vars['Y'] = Cplx.Zero;
                    history.Clear();
                    statData.Clear();
                    for (int i = 0; i < 3; i++) { matrices[i] = null; vectors[i] = null; }
                    Toast("Очищено");
                    break;
                case 1:
                    state.Memory = Cplx.Zero;
                    Toast("Память очищена");
                    break;
                case 2:
                    state.Ans = Cplx.Zero;
                    for (char c = 'A'; c <= 'F'; c++) state.Vars[c] = Cplx.Zero;
                    state.Vars['X'] = Cplx.Zero;
                    state.Vars['Y'] = Cplx.Zero;
                    Toast("Переменные очищены");
                    break;
                case 3:
                    history.Clear();
                    historyIndex = 0;
                    Toast("История очищена");
                    break;
                case 4:
                    statData.Clear();
                    Toast("STAT очищен");
                    break;
                case 5:
                    for (int i = 0; i < 3; i++) { matrices[i] = null; vectors[i] = null; }
                    Toast("Матрицы и векторы очищены");
                    break;
                case 6:
                    state.Angle = AngleUnit.Degrees;
                    state.Format = NumFormat.Norm2;
                    state.Digits = 2;
                    state.FractionResult = false;
                    state.ComplexDisplay = ComplexView.Rectangular;
                    state.StatFrequency = false;
                    state.InsertMode = true;
                    insertMode = true;
                    Toast("Настройки сброшены");
                    break;
            }
            Invalidate();
        }

        private void OpenHelp()
        {
            string help =
                "BRAUBERG SC-880 — инженерный калькулятор\r\n" +
                "========================================\r\n\r\n" +
                "КЛАВИАТУРА КОМПЬЮТЕРА\r\n" +
                "  0–9, + − * / ^ ( ) .   — ввод\r\n" +
                "  Enter или =             — вычислить\r\n" +
                "  Backspace                — удалить символ\r\n" +
                "  Esc                      — AC (очистить)\r\n" +
                "  ← →                      — курсор\r\n" +
                "  ↑ ↓                      — история\r\n" +
                "  Ctrl+C                   — копировать\r\n" +
                "  F1                       — эта справка\r\n" +
                "  F2                       — режимы\r\n" +
                "  F3                       — настройка\r\n" +
                "  a–f, x, y, m, i          — переменные и i\r\n" +
                "  p                        — π\r\n\r\n" +
                "SHIFT и ALPHA\r\n" +
                "  Оранжевые надписи — SHIFT, розовые — ALPHA.\r\n" +
                "  SHIFT+7 CONST    — физические постоянные\r\n" +
                "  SHIFT+8 CONV     — перевод единиц\r\n" +
                "  SHIFT+9 CLR      — очистка\r\n" +
                "  SHIFT+1 STAT     — статистика\r\n" +
                "  SHIFT+2 CMPLX    — комплексный режим\r\n" +
                "  SHIFT+3 BASE     — системы счисления\r\n" +
                "  SHIFT+4 MATRIX   — матрицы\r\n" +
                "  SHIFT+5 VECTOR   — векторы\r\n" +
                "  SHIFT+× nPr, SHIFT+÷ nCr\r\n" +
                "  SHIFT++ Pol, SHIFT+− Rec\r\n" +
                "  SHIFT+CALC       — SOLVE (численное решение)\r\n" +
                "  SHIFT+∫dx        — производная d/dx\r\n" +
                "  SHIFT+log□       — сумма Σ\r\n" +
                "  SHIFT+RCL        — STO (запись в переменную)\r\n" +
                "  SHIFT+M+         — M−\r\n" +
                "  SHIFT+Ans        — DRG▶ (перевод углов)\r\n" +
                "  SHIFT+AC         — OFF\r\n\r\n" +
                "ФУНКЦИИ\r\n" +
                "  sin cos tan + SHIFT (sin⁻¹ cos⁻¹ tan⁻¹), hyp\r\n" +
                "  ln, log (10), log□ (произвольное основание), 10ˣ, eˣ\r\n" +
                "  √, ∛, x², x³, xʸ, ⁿ√, x⁻¹, x!, |x|, 1/x\r\n" +
                "  π, e, Ans, M, A–F, X, Y\r\n" +
                "  ∠ — полярная форма (r∠θ), r∠θ в CMPLX\r\n" +
                "  ° ′ ″ — градусы/минуты/секунды\r\n" +
                "  % — процент (деление на 100)\r\n\r\n" +
                "РЕЖИМЫ\r\n" +
                "  COMP  — обычные вычисления\r\n" +
                "  CMPLX — комплексные числа, i, arg, conj, Re, Im\r\n" +
                "  STAT  — 1- и 2-переменная статистика, регрессия\r\n" +
                "  BASE  — DEC/HEX/BIN/OCT, AND OR XOR NOT, сдвиги\r\n" +
                "  EQN   — системы 2×2, 3×3, квадратные и кубические\r\n" +
                "  MATRIX— A/B/C, det, обратная, A+B, A×B, транспонирование\r\n" +
                "  VECTOR— VctA/B/C, скалярное, векторное, угол, |v|\r\n" +
                "  TABLE — таблица f(x) с шагом\r\n" +
                "  VERIF — проверка равенств и неравенств\r\n\r\n" +
                "ПОДСКАЗКИ\r\n" +
                "  Кнопки функций вставляют функцию с открывающей скобкой.\r\n" +
                "  Скобки можно не закрывать — они закроются автоматически.\r\n" +
                "  После результата оператор продолжает вычисление с Ans,\r\n" +
                "  а цифра начинает новое выражение.\r\n" +
                "  S⇔D переключает десятичную дробь и обыкновенную.\r\n" +
                "  ENG переключает инженерный формат (×10³).\r\n" +
                "  Нажмите на индикатор режима на дисплее, чтобы открыть\r\n" +
                "  инструмент текущего режима.";
            InfoForm.Show(this, "СПРАВКА — SC-880", help, 620, 640);
        }

        private void ToggleFraction()
        {
            Cplx v;
            try { v = CurrentValue(); }
            catch { ShowError("Нет результата для S⇔D"); return; }
            state.FractionResult = !state.FractionResult;
            resultText = FormatValue(v);
            showingResult = true;
            errorText = null;
            Invalidate();
        }

        private void ToggleEngineering()
        {
            Cplx v;
            try { v = CurrentValue(); }
            catch { ShowError("Нет результата для ENG"); return; }
            engMode = !engMode;
            resultText = FormatValue(v);
            showingResult = true;
            errorText = null;
            Invalidate();
        }

        private void RoundValue()
        {
            Cplx v;
            try { v = CurrentValue(); }
            catch { ShowError("Нет значения для Rnd"); return; }
            v = new Cplx(Math.Round(v.Re, 10), Math.Round(v.Im, 10));
            lastValue = v;
            hasLastValue = true;
            resultText = FormatValue(v);
            showingResult = true;
            Toast("Rnd");
            Invalidate();
        }

        private void InsertRandom()
        {
            Insert(random.NextDouble().ToString("G12", CultureInfo.InvariantCulture));
        }

        private void OpenRanInt()
        {
            string[] values = FieldsForm.Ask(this, "RanInt", "Случайное целое в диапазоне", new string[] { "От", "До" }, new string[] { "1", "6" });
            if (values == null) return;
            try
            {
                long a = (long)Math.Round(UiKit.ParseNumber(values[0]));
                long b = (long)Math.Round(UiKit.ParseNumber(values[1]));
                if (a > b) { long t = a; a = b; b = t; }
                if (b - a > int.MaxValue) throw new CalcError("Диапазон слишком велик");
                Insert(random.Next((int)a, (int)b + 1).ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }

        private void InsertVariable(char c)
        {
            Insert(c.ToString());
        }

        private void InsertConstant(string text)
        {
            Insert(text);
        }

        // ---------- key dispatch ----------

        private void PressKey(string id)
        {
            if (powerOff)
            {
                if (id == "on")
                {
                    powerOff = false;
                    Invalidate();
                }
                return;
            }
            if (id == "shift")
            {
                shift = !shift;
                alpha = false;
                Invalidate();
                return;
            }
            if (id == "alpha")
            {
                alpha = !alpha;
                shift = false;
                Invalidate();
                return;
            }
            bool useShift = shift;
            bool useAlpha = alpha;
            shift = false;
            alpha = false;

            if (useShift && ShiftAction(id))
            {
                Invalidate();
                return;
            }
            if (useAlpha && AlphaAction(id))
            {
                Invalidate();
                return;
            }
            PrimaryAction(id);
            Invalidate();
        }

        private bool ShiftAction(string id)
        {
            switch (id)
            {
                case "mode": OpenSetup(); return true;
                case "ac": powerOff = true; Invalidate(); return true;
                case "del": state.InsertMode = !state.InsertMode; insertMode = state.InsertMode; Toast(state.InsertMode ? "INS" : "OVR"); return true;
                case "calc": OpenSolve(); return true;
                case "integral": OpenDerivative(); return true;
                case "xinv": Insert("!"); return true;
                case "logbox": OpenSum(); return true;
                case "frac": ToggleFraction(); return true;
                case "sqrt": Insert("∛("); return true;
                case "square": Insert("³"); return true;
                case "power": Insert("nroot("); return true;
                case "log": Insert("10^("); return true;
                case "ln": Insert("e^("); return true;
                case "neg": Insert("∠"); return true;
                case "dms": MoveCaret(-1); return true;
                case "hyp": Insert("Abs("); return true;
                case "sin": Insert("asin("); return true;
                case "cos": Insert("acos("); return true;
                case "tan": Insert("atan("); return true;
                case "rcl": ShowStore(); return true;
                case "eng": MoveCaret(-1); return true;
                case "lparen": Insert("%"); return true;
                case "rparen": Insert(","); return true;
                case "sd": ToggleFraction(); return true;
                case "mplus": MemorySubtract(); return true;
                case "7": OpenConstants(); return true;
                case "8": OpenConversion(); return true;
                case "9": OpenClearMenu(); return true;
                case "4":
                    state.Mode = CalcMode.Matrix;
                    using (MatrixForm f = new MatrixForm(this)) f.ShowDialog(this);
                    return true;
                case "5":
                    state.Mode = CalcMode.Vector;
                    using (VectorForm f = new VectorForm(this)) f.ShowDialog(this);
                    return true;
                case "1":
                    state.Mode = CalcMode.Stat;
                    using (StatForm f = new StatForm(this)) f.ShowDialog(this);
                    return true;
                case "2":
                    state.Mode = CalcMode.Cmplx;
                    Toast("CMPLX включён");
                    return true;
                case "3":
                    state.Mode = CalcMode.BaseN;
                    OpenBase();
                    return true;
                case "times": Insert("npr("); return true;
                case "div": Insert("ncr("); return true;
                case "plus": Insert("pol("); return true;
                case "minus": Insert("rec("); return true;
                case "0": RoundValue(); return true;
                case "dot": InsertRandom(); return true;
                case "exp": Insert("π"); return true;
                case "ans": OpenAngleConvert(); return true;
            }
            return false;
        }

        private bool AlphaAction(string id)
        {
            switch (id)
            {
                case "neg": InsertVariable('A'); return true;
                case "dms": InsertVariable('B'); return true;
                case "hyp": InsertVariable('C'); return true;
                case "sin": InsertVariable('D'); return true;
                case "cos": InsertVariable('E'); return true;
                case "tan": InsertVariable('F'); return true;
                case "eng": Insert("i"); return true;
                case "lparen": InsertVariable('X'); return true;
                case "rparen": InsertVariable('Y'); return true;
                case "mplus": InsertVariable('M'); return true;
                case "dot": OpenRanInt(); return true;
                case "exp": Insert("e"); return true;
                case "calc": OpenHelp(); return true;
            }
            return false;
        }

        private void PrimaryAction(string id)
        {
            switch (id)
            {
                case "0": Insert("0"); break;
                case "1": Insert("1"); break;
                case "2": Insert("2"); break;
                case "3": Insert("3"); break;
                case "4": Insert("4"); break;
                case "5": Insert("5"); break;
                case "6": Insert("6"); break;
                case "7": Insert("7"); break;
                case "8": Insert("8"); break;
                case "9": Insert("9"); break;
                case "dot": Insert("."); break;
                case "plus": Insert("+"); break;
                case "minus": Insert("−"); break;
                case "times": Insert("×"); break;
                case "div": Insert("÷"); break;
                case "lparen": Insert("("); break;
                case "rparen": Insert(")"); break;
                case "power": Insert("^"); break;
                case "equals": Evaluate(); break;
                case "ac": ClearAll(); break;
                case "del": Backspace(); break;
                case "on": ClearAll(); break;
                case "mode": OpenModes(); break;
                case "calc": RunCalc(); break;
                case "integral": OpenIntegral(); break;
                case "xinv": Insert("⁻¹"); break;
                case "logbox": Insert("logb("); break;
                case "frac": Insert("/"); break;
                case "sqrt": Insert("√("); break;
                case "square": Insert("²"); break;
                case "log": Insert("log("); break;
                case "ln": Insert("ln("); break;
                case "neg": Insert("−"); break;
                case "dms": InsertDms(); break;
                case "hyp": hyp = !hyp; Toast(hyp ? "hyp включён" : "hyp выключен"); break;
                case "sin": Insert(hyp ? "sinh(" : "sin("); hyp = false; break;
                case "cos": Insert(hyp ? "cosh(" : "cos("); hyp = false; break;
                case "tan": Insert(hyp ? "tanh(" : "tan("); hyp = false; break;
                case "rcl": ShowRecall(); break;
                case "eng": ToggleEngineering(); break;
                case "sd": ToggleFraction(); break;
                case "mplus": MemoryAdd(1); break;
                case "exp": Insert("×10^("); break;
                case "ans": Insert("Ans"); break;
            }
        }

        private void InsertDms()
        {
            if (caret > 0 && caret == expression.Length)
            {
                char last = expression[caret - 1];
                if (last == '°')
                {
                    expression = expression.Substring(0, caret - 1) + "′";
                    Invalidate();
                    return;
                }
                if (last == '′')
                {
                    expression = expression.Substring(0, caret - 1) + "″";
                    Invalidate();
                    return;
                }
            }
            Insert("°");
        }

        private void CopyDisplay()
        {
            try
            {
                string text = showingResult && !string.IsNullOrEmpty(resultText) ? resultText : expression;
                if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
                Toast("Скопировано");
            }
            catch
            {
            }
        }

        // ---------- mouse ----------

        private bool Hit(PointF p, RectangleF r)
        {
            return p.X >= r.X && p.X <= r.Right && p.Y >= r.Y && p.Y <= r.Bottom;
        }

        private int HitPad(PointF p)
        {
            if (!Hit(p, padRect)) return 0;
            float cx = padRect.X + padRect.Width / 2f;
            float cy = padRect.Y + padRect.Height / 2f;
            if (p.Y < cy - 13) return 1;
            if (p.Y > cy + 13) return 2;
            if (p.X < cx - 13) return 3;
            if (p.X > cx + 13) return 4;
            return 5;
        }

        private string HitKey(PointF p)
        {
            for (int i = keys.Count - 1; i >= 0; i--)
                if (Hit(p, keys[i].Rect)) return keys[i].Id;
            return null;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF p = Unscale(e.Location);
            if (e.Button != MouseButtons.Left) return;
            if (Hit(p, threeDButton))
            {
                Open3DView();
                return;
            }
            if (powerOff)
            {
                if (HitKey(p) == "on") PressKey("on");
                return;
            }
            int pad = HitPad(p);
            if (pad > 0)
            {
                pressedPad = pad;
                PressPad(pad);
                Invalidate();
                return;
            }
            string id = HitKey(p);
            if (id != null)
            {
                pressedId = id;
                PressKey(id);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (pressedId != null || pressedPad != 0)
            {
                pressedId = null;
                pressedPad = 0;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF p = Unscale(e.Location);
            bool over3D = Hit(p, threeDButton);
            if (over3D)
            {
                if (!hover3D || hoverId != null || hoverPad != 0)
                {
                    hover3D = true;
                    hoverId = null;
                    hoverPad = 0;
                    Cursor = Cursors.Hand;
                    Invalidate();
                }
                return;
            }
            if (hover3D)
            {
                hover3D = false;
                Invalidate();
            }
            string id = HitKey(p);
            int pad = HitPad(p);
            if (id != hoverId || pad != hoverPad)
            {
                hoverId = id;
                hoverPad = pad;
                Cursor = id != null || pad > 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverId = null;
            hoverPad = 0;
            hover3D = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        private void PressPad(int pad)
        {
            switch (pad)
            {
                case 1: NavigateHistory(-1); break;
                case 2: NavigateHistory(1); break;
                case 3: MoveCaret(-1); break;
                case 4: MoveCaret(1); break;
                case 5: Evaluate(); break;
            }
        }

        // ---------- keyboard ----------

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            ProcessKey(e);
        }

        private void ProcessKey(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyDisplay();
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.F1) { OpenHelp(); return; }
            if (e.KeyCode == Keys.F2) { OpenModes(); return; }
            if (e.KeyCode == Keys.F3) { OpenSetup(); return; }
            if (e.KeyCode == Keys.F4) { Open3DView(); return; }
            if (powerOff)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.O) powerOff = false;
                return;
            }
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    Evaluate();
                    e.SuppressKeyPress = true;
                    return;
                case Keys.Escape:
                    ClearAll();
                    return;
                case Keys.Back:
                    Backspace();
                    return;
                case Keys.Delete:
                    DeleteAtCaret();
                    return;
                case Keys.Left:
                    MoveCaret(-1);
                    return;
                case Keys.Right:
                    MoveCaret(1);
                    return;
                case Keys.Up:
                    NavigateHistory(-1);
                    return;
                case Keys.Down:
                    NavigateHistory(1);
                    return;
                case Keys.Oemplus:
                    if (e.Shift) Insert("+"); else Insert("=");
                    return;
                case Keys.OemMinus:
                    Insert("−");
                    return;
                case Keys.D0: Insert(e.Shift ? ")" : "0"); return;
                case Keys.D1: Insert(e.Shift ? "!" : "1"); return;
                case Keys.D2: Insert(e.Shift ? "@" : "2"); return;
                case Keys.D3: Insert(e.Shift ? "#" : "3"); return;
                case Keys.D4: Insert(e.Shift ? "$" : "4"); return;
                case Keys.D5: Insert(e.Shift ? "%" : "5"); return;
                case Keys.D6: Insert(e.Shift ? "^" : "6"); return;
                case Keys.D7: Insert(e.Shift ? "&" : "7"); return;
                case Keys.D8: Insert(e.Shift ? "*" : "8"); return;
                case Keys.D9: Insert(e.Shift ? "(" : "9"); return;
                case Keys.Decimal:
                case Keys.OemPeriod:
                    Insert(".");
                    return;
                case Keys.Multiply:
                    Insert("×");
                    return;
                case Keys.Divide:
                    Insert("÷");
                    return;
                case Keys.Add:
                    Insert("+");
                    return;
                case Keys.Subtract:
                    Insert("−");
                    return;
                case Keys.OemOpenBrackets:
                    Insert(e.Shift ? "{" : "(");
                    return;
                case Keys.OemCloseBrackets:
                    Insert(e.Shift ? "}" : ")");
                    return;
                case Keys.OemQuestion:
                    if (e.Shift) Insert("?"); else Insert("/");
                    return;
                case Keys.A: Insert(e.Shift ? "A" : "a"); return;
                case Keys.B: Insert(e.Shift ? "B" : "b"); return;
                case Keys.C: Insert(e.Shift ? "C" : "c"); return;
                case Keys.D: Insert(e.Shift ? "D" : "d"); return;
                case Keys.E: Insert(e.Shift ? "E" : "e"); return;
                case Keys.F: Insert(e.Shift ? "F" : "f"); return;
                case Keys.X: Insert(e.Shift ? "X" : "x"); return;
                case Keys.Y: Insert(e.Shift ? "Y" : "y"); return;
                case Keys.M: Insert(e.Shift ? "M" : "m"); return;
                case Keys.I: Insert("i"); return;
                case Keys.P: Insert("π"); return;
            }
        }
    }
}
