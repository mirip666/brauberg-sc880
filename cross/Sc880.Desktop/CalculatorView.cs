using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace SC880.Desktop
{
    internal sealed partial class CalculatorView : Control
    {
        internal const double BaseW = 480;
        internal const double BaseH = 880;

        private readonly CalcState state = new CalcState();
        private readonly List<CalcKey> keys = new List<CalcKey>();
        private readonly List<HistoryEntry> history = new List<HistoryEntry>();
        private readonly DispatcherTimer blinkTimer;
        private readonly DispatcherTimer toastTimer;
        private readonly Random random = new Random();
        private readonly List<StatPoint> statData = new List<StatPoint>();
        private readonly MatrixData[] matrices = new MatrixData[3];
        private readonly VectorData[] vectors = new VectorData[3];

        private double scale = 1;
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

        private Rect padRect = new Rect(186, 295, 108, 93);
        private string pressedId;
        private int pressedPad;
        private string hoverId;
        private int hoverPad;

        public CalculatorView()
        {
            Focusable = true;
            BuildKeys();

            blinkTimer = new DispatcherTimer();
            blinkTimer.Interval = TimeSpan.FromMilliseconds(530);
            blinkTimer.Tick += delegate
            {
                blink = !blink;
                InvalidateVisual();
            };
            blinkTimer.Start();

            toastTimer = new DispatcherTimer();
            toastTimer.Interval = TimeSpan.FromMilliseconds(1400);
            toastTimer.Tick += delegate
            {
                toast = null;
                toastTimer.Stop();
                InvalidateVisual();
            };
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(BaseW, BaseH);
        }

        private Window OwnerWindow
        {
            get { return TopLevel.GetTopLevel(this) as Window; }
        }

        internal CalcState State
        {
            get { return state; }
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
            set { baseValue = value; }
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

        // ---------- public bridge for dialogs ----------

        internal void InsertTextExternal(string text)
        {
            Insert(text);
            InvalidateVisual();
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
            InvalidateVisual();
        }

        internal void InsertNumber(Cplx value)
        {
            Insert(Parseable(value));
            InvalidateVisual();
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
            InvalidateVisual();
        }

        internal void Toast(string text)
        {
            toast = text;
            toastTimer.Stop();
            toastTimer.Start();
            InvalidateVisual();
        }

        internal void ShowError(string text)
        {
            errorText = text;
            showingResult = false;
            resultText = "";
            InvalidateVisual();
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
            InvalidateVisual();
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
            if (caret == 0) { InvalidateVisual(); return; }
            int remove = 1;
            if (caret >= 2 && expression[caret - 2] == '⁻' && expression[caret - 1] == '¹') remove = 2;
            expression = expression.Remove(caret - remove, remove);
            caret -= remove;
            historyIndex = history.Count;
            InvalidateVisual();
        }

        private void DeleteAtCaret()
        {
            if (showingResult) { showingResult = false; resultText = ""; }
            if (caret >= expression.Length) { InvalidateVisual(); return; }
            int remove = 1;
            if (caret + 1 < expression.Length && expression[caret] == '⁻' && expression[caret + 1] == '¹') remove = 2;
            expression = expression.Remove(caret, remove);
            InvalidateVisual();
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
            InvalidateVisual();
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
            InvalidateVisual();
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
                    InvalidateVisual();
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
            InvalidateVisual();
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
            InvalidateVisual();
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
            InvalidateVisual();
        }

        private void MemorySubtract()
        {
            MemoryAdd(-1);
        }

        private async Task ShowRecallAsync()
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
            int idx = await Dialogs.ListAsync(OwnerWindow, "ВЫЗОВ ПЕРЕМЕННОЙ", "RCL: выберите переменную", labels, 0);
            if (idx < 0) return;
            Cplx v;
            if (idx < 6) v = state.Vars[(char)('A' + idx)];
            else if (idx == 6) v = state.Vars['X'];
            else if (idx == 7) v = state.Vars['Y'];
            else v = state.Memory;
            Insert(Parseable(v));
        }

        private async Task ShowStoreAsync()
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
            int idx = await Dialogs.ListAsync(OwnerWindow, "ЗАПИСЬ В ПАМЯТЬ", "STO: выберите, куда записать результат", labels, 0);
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

        private async Task RunCalcAsync()
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
                defaults[i] = Numbers.Format(state.Vars[vars[i]].Re);
            }
            string[] values = await Dialogs.FieldsAsync(OwnerWindow, "CALC — ЗНАЧЕНИЯ ПЕРЕМЕННЫХ",
                "Введите значения переменных для вычисления выражения", labels, defaults);
            if (values == null) return;
            try
            {
                for (int i = 0; i < vars.Count; i++)
                    state.Vars[vars[i]] = Cplx.Real(Numbers.Parse(values[i]));
            }
            catch (Exception ex)
            {
                await Dialogs.InfoAsync(OwnerWindow, "Ошибка", ex.Message, 420, 220);
                return;
            }
            Evaluate();
        }

        // ---------- tools ----------

        private async Task OpenSetupAsync()
        {
            if (await Dialogs.SetupAsync(OwnerWindow, state))
            {
                insertMode = state.InsertMode;
                InvalidateVisual();
            }
        }

        private async Task OpenModesAsync()
        {
            CalcMode? chosen = await Dialogs.ModeAsync(OwnerWindow, state.Mode);
            if (chosen == null) return;
            state.Mode = chosen.Value;
            ClearAll();
            await OpenModeToolAsync();
            InvalidateVisual();
        }

        private async Task OpenModeToolAsync()
        {
            switch (state.Mode)
            {
                case CalcMode.Stat:
                    await FeatureDialogs.StatAsync(OwnerWindow, this);
                    break;
                case CalcMode.Eqn:
                    await FeatureDialogs.EqnAsync(OwnerWindow, this);
                    break;
                case CalcMode.Matrix:
                    await FeatureDialogs.MatrixAsync(OwnerWindow, this);
                    break;
                case CalcMode.Vector:
                    await FeatureDialogs.VectorAsync(OwnerWindow, this);
                    break;
                case CalcMode.Table:
                    await FeatureDialogs.TableAsync(OwnerWindow, this);
                    break;
                case CalcMode.BaseN:
                    await FeatureDialogs.BaseAsync(OwnerWindow, this);
                    break;
                case CalcMode.Verif:
                    await FeatureDialogs.VerifyAsync(OwnerWindow, this);
                    break;
                case CalcMode.Cmplx:
                    state.Mode = CalcMode.Cmplx;
                    Toast("CMPLX: доступны i, ∠, arg, conj, Re, Im");
                    break;
            }
            InvalidateVisual();
        }

        private async Task OpenSolveAsync()
        {
            await FeatureDialogs.SolveAsync(OwnerWindow, this);
        }

        private async Task OpenIntegralAsync()
        {
            await FeatureDialogs.IntegralAsync(OwnerWindow, this);
        }

        private async Task OpenDerivativeAsync()
        {
            await FeatureDialogs.DerivativeAsync(OwnerWindow, this);
        }

        private async Task OpenSumAsync()
        {
            await FeatureDialogs.SumAsync(OwnerWindow, this);
        }

        private async Task OpenConstantsAsync()
        {
            await FeatureDialogs.ConstantsAsync(OwnerWindow, this);
        }

        private async Task OpenConversionAsync()
        {
            await FeatureDialogs.ConversionAsync(OwnerWindow, this);
        }

        private async Task OpenBaseAsync()
        {
            await FeatureDialogs.BaseAsync(OwnerWindow, this);
        }

        private async Task OpenAngleConvertAsync()
        {
            await FeatureDialogs.AngleConvertAsync(OwnerWindow, this);
        }

        private async Task OpenClearMenuAsync()
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
            int idx = await Dialogs.ListAsync(OwnerWindow, "ОЧИСТКА (CLR)", "Что очистить?", options, 0);
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
            InvalidateVisual();
        }

        private async Task OpenHelpAsync()
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
            await Dialogs.InfoAsync(OwnerWindow, "СПРАВКА — SC-880", help, 620, 640);
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
            InvalidateVisual();
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
            InvalidateVisual();
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
            InvalidateVisual();
        }

        private void InsertRandom()
        {
            Insert(random.NextDouble().ToString("G12", CultureInfo.InvariantCulture));
        }

        private async Task OpenRanIntAsync()
        {
            string[] values = await Dialogs.FieldsAsync(OwnerWindow, "RanInt", "Случайное целое в диапазоне",
                new string[] { "От", "До" }, new string[] { "1", "6" });
            if (values == null) return;
            try
            {
                long a = (long)Math.Round(Numbers.Parse(values[0]));
                long b = (long)Math.Round(Numbers.Parse(values[1]));
                if (a > b) { long t = a; a = b; b = t; }
                if (b - a > int.MaxValue) throw new CalcError("Диапазон слишком велик");
                Insert(random.Next((int)a, (int)b + 1).ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                await Dialogs.InfoAsync(OwnerWindow, "Ошибка", ex.Message, 420, 220);
            }
        }

        private void InsertVariable(char c)
        {
            Insert(c.ToString());
        }

        // ---------- key dispatch ----------

        private async Task PressKeyAsync(string id)
        {
            if (powerOff)
            {
                if (id == "on")
                {
                    powerOff = false;
                    InvalidateVisual();
                }
                return;
            }
            if (id == "shift")
            {
                shift = !shift;
                alpha = false;
                InvalidateVisual();
                return;
            }
            if (id == "alpha")
            {
                alpha = !alpha;
                shift = false;
                InvalidateVisual();
                return;
            }
            bool useShift = shift;
            bool useAlpha = alpha;
            shift = false;
            alpha = false;

            if (useShift && await ShiftActionAsync(id))
            {
                InvalidateVisual();
                return;
            }
            if (useAlpha && await AlphaActionAsync(id))
            {
                InvalidateVisual();
                return;
            }
            await PrimaryActionAsync(id);
            InvalidateVisual();
        }

        private async Task<bool> ShiftActionAsync(string id)
        {
            switch (id)
            {
                case "mode": await OpenSetupAsync(); return true;
                case "ac": powerOff = true; InvalidateVisual(); return true;
                case "del":
                    state.InsertMode = !state.InsertMode;
                    insertMode = state.InsertMode;
                    Toast(state.InsertMode ? "INS" : "OVR");
                    return true;
                case "calc": await OpenSolveAsync(); return true;
                case "integral": await OpenDerivativeAsync(); return true;
                case "xinv": Insert("!"); return true;
                case "logbox": await OpenSumAsync(); return true;
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
                case "rcl": await ShowStoreAsync(); return true;
                case "eng": MoveCaret(-1); return true;
                case "lparen": Insert("%"); return true;
                case "rparen": Insert(","); return true;
                case "sd": ToggleFraction(); return true;
                case "mplus": MemorySubtract(); return true;
                case "7": await OpenConstantsAsync(); return true;
                case "8": await OpenConversionAsync(); return true;
                case "9": await OpenClearMenuAsync(); return true;
                case "4":
                    state.Mode = CalcMode.Matrix;
                    await FeatureDialogs.MatrixAsync(OwnerWindow, this);
                    return true;
                case "5":
                    state.Mode = CalcMode.Vector;
                    await FeatureDialogs.VectorAsync(OwnerWindow, this);
                    return true;
                case "1":
                    state.Mode = CalcMode.Stat;
                    await FeatureDialogs.StatAsync(OwnerWindow, this);
                    return true;
                case "2":
                    state.Mode = CalcMode.Cmplx;
                    Toast("CMPLX включён");
                    return true;
                case "3":
                    state.Mode = CalcMode.BaseN;
                    await OpenBaseAsync();
                    return true;
                case "times": Insert("npr("); return true;
                case "div": Insert("ncr("); return true;
                case "plus": Insert("pol("); return true;
                case "minus": Insert("rec("); return true;
                case "0": RoundValue(); return true;
                case "dot": InsertRandom(); return true;
                case "exp": Insert("π"); return true;
                case "ans": await OpenAngleConvertAsync(); return true;
            }
            return false;
        }

        private async Task<bool> AlphaActionAsync(string id)
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
                case "dot": await OpenRanIntAsync(); return true;
                case "exp": Insert("e"); return true;
                case "calc": await OpenHelpAsync(); return true;
            }
            return false;
        }

        private async Task PrimaryActionAsync(string id)
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
                case "mode": await OpenModesAsync(); break;
                case "calc": await RunCalcAsync(); break;
                case "integral": await OpenIntegralAsync(); break;
                case "xinv": Insert("⁻¹"); break;
                case "logbox": Insert("logb("); break;
                case "frac": Insert("/"); break;
                case "sqrt": Insert("√("); break;
                case "square": Insert("²"); break;
                case "log": Insert("log("); break;
                case "ln": Insert("ln("); break;
                case "neg": Insert("−"); break;
                case "dms": InsertDms(); break;
                case "hyp":
                    hyp = !hyp;
                    Toast(hyp ? "hyp включён" : "hyp выключен");
                    break;
                case "sin": Insert(hyp ? "sinh(" : "sin("); hyp = false; break;
                case "cos": Insert(hyp ? "cosh(" : "cos("); hyp = false; break;
                case "tan": Insert(hyp ? "tanh(" : "tan("); hyp = false; break;
                case "rcl": await ShowRecallAsync(); break;
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
                    InvalidateVisual();
                    return;
                }
                if (last == '′')
                {
                    expression = expression.Substring(0, caret - 1) + "″";
                    InvalidateVisual();
                    return;
                }
            }
            Insert("°");
        }

        private async void CopyDisplay()
        {
            try
            {
                string text = showingResult && !string.IsNullOrEmpty(resultText) ? resultText : expression;
                if (!string.IsNullOrEmpty(text))
                {
                    TopLevel top = TopLevel.GetTopLevel(this);
                    if (top != null && top.Clipboard != null) await top.Clipboard.SetTextAsync(text);
                }
                Toast("Скопировано");
            }
            catch
            {
            }
        }

        // ---------- mouse ----------

        private static bool Hit(Point p, Rect r)
        {
            return p.X >= r.X && p.X <= r.Right && p.Y >= r.Y && p.Y <= r.Bottom;
        }

        private int HitPad(Point p)
        {
            if (!Hit(p, padRect)) return 0;
            double cx = padRect.X + padRect.Width / 2;
            double cy = padRect.Y + padRect.Height / 2;
            if (p.Y < cy - 13) return 1;
            if (p.Y > cy + 13) return 2;
            if (p.X < cx - 13) return 3;
            if (p.X > cx + 13) return 4;
            return 5;
        }

        private string HitKey(Point p)
        {
            for (int i = keys.Count - 1; i >= 0; i--)
                if (Hit(p, keys[i].Rect)) return keys[i].Id;
            return null;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            Point p = e.GetPosition(this);
            if (powerOff)
            {
                if (HitKey(p) == "on") _ = PressKeyAsync("on");
                return;
            }
            int pad = HitPad(p);
            if (pad > 0)
            {
                pressedPad = pad;
                PressPad(pad);
                InvalidateVisual();
                return;
            }
            string id = HitKey(p);
            if (id != null)
            {
                pressedId = id;
                _ = PressKeyAsync(id);
                InvalidateVisual();
            }
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (pressedId != null || pressedPad != 0)
            {
                pressedId = null;
                pressedPad = 0;
                InvalidateVisual();
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Point p = e.GetPosition(this);
            string id = HitKey(p);
            int pad = HitPad(p);
            if (id != hoverId || pad != hoverPad)
            {
                hoverId = id;
                hoverPad = pad;
                Cursor = id != null || pad > 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
                InvalidateVisual();
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            hoverId = null;
            hoverPad = 0;
            Cursor = Cursor.Default;
            InvalidateVisual();
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

        internal async Task ProcessKeyAsync(KeyEventArgs e)
        {
            bool shiftDown = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.C)
            {
                CopyDisplay();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.F1) { await OpenHelpAsync(); e.Handled = true; return; }
            if (e.Key == Key.F2) { await OpenModesAsync(); e.Handled = true; return; }
            if (e.Key == Key.F3) { await OpenSetupAsync(); e.Handled = true; return; }
            if (powerOff)
            {
                if (e.Key == Key.Enter || e.Key == Key.O) powerOff = false;
                return;
            }
            switch (e.Key)
            {
                case Key.Enter:
                    Evaluate();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    ClearAll();
                    return;
                case Key.Back:
                    Backspace();
                    return;
                case Key.Delete:
                    DeleteAtCaret();
                    return;
                case Key.Left:
                    MoveCaret(-1);
                    return;
                case Key.Right:
                    MoveCaret(1);
                    return;
                case Key.Up:
                    NavigateHistory(-1);
                    return;
                case Key.Down:
                    NavigateHistory(1);
                    return;
                case Key.OemPlus:
                    Insert(shiftDown ? "+" : "=");
                    return;
                case Key.OemMinus:
                    Insert("−");
                    return;
                case Key.D0:
                case Key.NumPad0:
                    Insert(shiftDown ? ")" : "0");
                    return;
                case Key.D1:
                case Key.NumPad1:
                    Insert(shiftDown ? "!" : "1");
                    return;
                case Key.D2:
                case Key.NumPad2:
                    Insert(shiftDown ? "@" : "2");
                    return;
                case Key.D3:
                case Key.NumPad3:
                    Insert(shiftDown ? "#" : "3");
                    return;
                case Key.D4:
                case Key.NumPad4:
                    Insert(shiftDown ? "$" : "4");
                    return;
                case Key.D5:
                case Key.NumPad5:
                    Insert(shiftDown ? "%" : "5");
                    return;
                case Key.D6:
                case Key.NumPad6:
                    Insert(shiftDown ? "^" : "6");
                    return;
                case Key.D7:
                case Key.NumPad7:
                    Insert(shiftDown ? "&" : "7");
                    return;
                case Key.D8:
                case Key.NumPad8:
                    Insert(shiftDown ? "*" : "8");
                    return;
                case Key.D9:
                case Key.NumPad9:
                    Insert(shiftDown ? "(" : "9");
                    return;
                case Key.Decimal:
                case Key.OemPeriod:
                    Insert(".");
                    return;
                case Key.Multiply:
                    Insert("×");
                    return;
                case Key.Divide:
                    Insert("÷");
                    return;
                case Key.Add:
                    Insert("+");
                    return;
                case Key.Subtract:
                    Insert("−");
                    return;
                case Key.OemOpenBrackets:
                    Insert(shiftDown ? "{" : "(");
                    return;
                case Key.OemCloseBrackets:
                    Insert(shiftDown ? "}" : ")");
                    return;
                case Key.OemQuestion:
                    Insert(shiftDown ? "?" : "/");
                    return;
                case Key.A: Insert(shiftDown ? "A" : "a"); return;
                case Key.B: Insert(shiftDown ? "B" : "b"); return;
                case Key.C: Insert(shiftDown ? "C" : "c"); return;
                case Key.D: Insert(shiftDown ? "D" : "d"); return;
                case Key.E: Insert(shiftDown ? "E" : "e"); return;
                case Key.F: Insert(shiftDown ? "F" : "f"); return;
                case Key.X: Insert(shiftDown ? "X" : "x"); return;
                case Key.Y: Insert(shiftDown ? "Y" : "y"); return;
                case Key.M: Insert(shiftDown ? "M" : "m"); return;
                case Key.I: Insert("i"); return;
                case Key.P: Insert("π"); return;
            }
        }
    }
}
