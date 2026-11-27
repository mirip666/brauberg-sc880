using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace SC880.Desktop
{
    internal static partial class FeatureDialogs
    {
        private sealed class MatrixEditor
        {
            private readonly Canvas host;
            private TextBox[,] cells;
            public int Rows;
            public int Cols;

            public MatrixEditor(Canvas host)
            {
                this.host = host;
            }

            public void Build(int rows, int cols, MatrixData data, string[,] old)
            {
                Rows = rows;
                Cols = cols;
                host.Children.Clear();
                cells = new TextBox[rows, cols];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string text;
                        if (old != null && i < old.GetLength(0) && j < old.GetLength(1)) text = old[i, j];
                        else if (data != null) text = Dlg.Format(data.Data[i, j]);
                        else text = "0";
                        TextBox t = Dlg.MakeText(text, 52, 30);
                        t.Width = 52;
                        t.Height = 30;
                        Canvas.SetLeft(t, j * 56);
                        Canvas.SetTop(t, i * 36);
                        host.Children.Add(t);
                        cells[i, j] = t;
                    }
                }
            }

            public string[,] Snapshot()
            {
                string[,] data = new string[Rows, Cols];
                for (int i = 0; i < Rows; i++)
                    for (int j = 0; j < Cols; j++)
                        data[i, j] = cells[i, j].Text;
                return data;
            }

            public MatrixData Read()
            {
                if (Rows <= 0 || Cols <= 0) throw new CalcError("Пустая матрица");
                MatrixData m = new MatrixData(Rows, Cols);
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols; j++)
                    {
                        string text = cells[i, j].Text;
                        if (string.IsNullOrWhiteSpace(text)) text = "0";
                        m.Data[i, j] = Dlg.Parse(text);
                    }
                }
                return m;
            }

            public void Fill(string value)
            {
                for (int i = 0; i < Rows; i++)
                    for (int j = 0; j < Cols; j++)
                        cells[i, j].Text = value;
            }
        }

        // ---------------- MATRIX ----------------

        public static async Task MatrixAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("MATRIX — МАТРИЦЫ", 800, 650);
            string[] names = { "A", "B", "C" };
            Button[] matrixButtons = new Button[3];
            int activeIndex = 0;
            MatrixData result = null;

            Dlg.Caption(w, "Матрица", 16, 10, 70);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button b = Dlg.Button(w, names[i], 90 + i * 54, 8, 48, 30);
                b.Click += delegate { };
                matrixButtons[i] = b;
            }
            TextBlock activeLabel = Dlg.Label(w, "Матрица A", 258, 14, 150);
            Dlg.Label(w, "Результат", 600, 14, 90);

            Border gridFrame = new Border();
            gridFrame.Background = Dlg.FieldBack;
            gridFrame.BorderBrush = Dlg.Line;
            gridFrame.BorderThickness = new Thickness(1);
            gridFrame.Width = 374;
            gridFrame.Height = 352;
            Canvas.SetLeft(gridFrame, 16);
            Canvas.SetTop(gridFrame, 48);
            Canvas root = (Canvas)w.Content;
            root.Children.Add(gridFrame);
            Canvas inputHost = new Canvas();
            inputHost.Width = 372;
            inputHost.Height = 350;
            gridFrame.Child = inputHost;
            MatrixEditor editor = new MatrixEditor(inputHost);

            Border resultFrame = new Border();
            resultFrame.Background = Dlg.FieldBack;
            resultFrame.BorderBrush = Dlg.Line;
            resultFrame.BorderThickness = new Thickness(1);
            resultFrame.Width = 374;
            resultFrame.Height = 352;
            Canvas.SetLeft(resultFrame, 408);
            Canvas.SetTop(resultFrame, 48);
            root.Children.Add(resultFrame);
            Canvas resultHost = new Canvas();
            resultHost.Width = 372;
            resultHost.Height = 350;
            resultFrame.Child = resultHost;
            MatrixEditor resultEditor = new MatrixEditor(resultHost);

            Dlg.Label(w, "Строк", 16, 414, 46);
            NumericUpDown rows = Dlg.Number(w, 2, 1, 6, 66, 410, 108);
            Dlg.Label(w, "Столбцов", 190, 414, 66);
            NumericUpDown cols = Dlg.Number(w, 2, 1, 6, 258, 410, 108);
            Button resize = Dlg.Button(w, "Размер", 380, 408, 86, 30);
            TextBlock info = Dlg.Label(w, "", 480, 408, 302, 46);
            info.FontFamily = Dlg.MonoFont;
            info.FontSize = 13;
            info.FontWeight = FontWeight.Bold;
            info.VerticalAlignment = VerticalAlignment.Center;

            Action<MatrixData, string> showResult = delegate(MatrixData m, string label)
            {
                result = m;
                resultEditor.Build(m.Rows, m.Cols, m, null);
                info.Text = label;
            };

            Func<MatrixData> capture = delegate
            {
                MatrixData m = editor.Read();
                view.Matrices[activeIndex] = m;
                return m;
            };

            Func<int, MatrixData> getStored = delegate(int idx)
            {
                if (idx == activeIndex) return capture();
                if (view.Matrices[idx] == null) throw new CalcError("Сначала запишите матрицу " + names[idx]);
                return view.Matrices[idx];
            };

            Action<int> loadMatrix = delegate(int idx)
            {
                activeIndex = idx;
                activeLabel.Text = "Матрица " + names[idx];
                for (int i = 0; i < 3; i++)
                {
                    matrixButtons[i].Background = i == idx
                        ? new SolidColorBrush(Color.FromRgb(230, 226, 217))
                        : Dlg.ButtonBack;
                }
                MatrixData m = view.Matrices[idx];
                if (m == null)
                {
                    rows.Value = 2;
                    cols.Value = 2;
                    editor.Build(2, 2, null, null);
                }
                else
                {
                    rows.Value = (decimal)m.Rows;
                    cols.Value = (decimal)m.Cols;
                    editor.Build(m.Rows, m.Cols, m, null);
                }
                result = null;
                resultEditor.Build(1, 1, null, null);
                resultHost.Children.Clear();
                info.Text = "";
            };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                matrixButtons[i].Click += delegate
                {
                    try { if (editor.Rows > 0) capture(); } catch { }
                    loadMatrix(idx);
                };
            }

            resize.Click += delegate
            {
                int r = (int)(rows.Value ?? 2);
                int c = (int)(cols.Value ?? 2);
                string[,] old = editor.Rows > 0 ? editor.Snapshot() : null;
                editor.Build(r, c, null, old);
            };

            Button det = Dlg.Button(w, "Определитель", 16, 476, 120, 32);
            det.Click += delegate
            {
                try
                {
                    MatrixData m = capture();
                    double d = m.Determinant();
                    result = null;
                    resultHost.Children.Clear();
                    info.Text = "det(" + names[activeIndex] + ") = " + Dlg.Format(d);
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button inverse = Dlg.Button(w, "Обратная", 144, 476, 110, 32);
            inverse.Click += delegate
            {
                try { showResult(capture().Inverse(), "Обратная матрица " + names[activeIndex] + "⁻¹"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button transpose = Dlg.Button(w, "Транспонировать", 262, 476, 134, 32);
            transpose.Click += delegate
            {
                try { showResult(capture().Transpose(), "Транспонированная матрица"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button add = Dlg.Button(w, "A + B", 404, 476, 78, 32);
            add.Click += delegate
            {
                try { showResult(MatrixData.Add(getStored(0), getStored(1)), "A + B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button sub = Dlg.Button(w, "A − B", 490, 476, 78, 32);
            sub.Click += delegate
            {
                try { showResult(MatrixData.Sub(getStored(0), getStored(1)), "A − B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button mul = Dlg.Button(w, "A × B", 576, 476, 78, 32);
            mul.Click += delegate
            {
                try { showResult(MatrixData.Mul(getStored(0), getStored(1)), "A × B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button scalar = Dlg.Button(w, "A × k", 16, 516, 100, 32);
            scalar.Click += async delegate
            {
                try
                {
                    string[] v = await Dlg.FieldsAsync(w, "A × k", "Умножить матрицу на число",
                        new string[] { "k" }, new string[] { "2" });
                    if (v == null) return;
                    double k = Dlg.Parse(v[0]);
                    showResult(MatrixData.Scalar(capture(), k), "A × " + Dlg.Format(k));
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };

            Action<int> copyResultTo = delegate(int idx)
            {
                if (result == null)
                {
                    _ = Dlg.ErrorAsync(w, "Сначала выполните операцию с матрицей.");
                    return;
                }
                view.Matrices[idx] = result.Clone();
                if (idx != activeIndex)
                {
                    loadMatrix(idx);
                    info.Text = "Результат скопирован в " + names[idx];
                }
                else
                {
                    editor.Build(result.Rows, result.Cols, result, null);
                    info.Text = "Результат скопирован в " + names[idx];
                }
            };
            Button copyA = Dlg.Button(w, "Скопировать в A", 124, 516, 140, 32);
            copyA.Click += delegate { copyResultTo(0); };
            Button copyB = Dlg.Button(w, "в B", 272, 516, 72, 32);
            copyB.Click += delegate { copyResultTo(1); };
            Button copyC = Dlg.Button(w, "в C", 352, 516, 72, 32);
            copyC.Click += delegate { copyResultTo(2); };

            Button save = Dlg.Button(w, "Записать", 432, 516, 100, 32);
            save.Click += delegate
            {
                try
                {
                    capture();
                    info.Text = "Матрица " + names[activeIndex] + " записана (" + editor.Rows + "×" + editor.Cols + ")";
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button clear = Dlg.Button(w, "Очистить", 540, 516, 114, 32);
            clear.Click += delegate
            {
                editor.Fill("0");
                view.Matrices[activeIndex] = null;
                result = null;
                resultHost.Children.Clear();
                info.Text = "Матрица очищена";
            };

            Button close = Dlg.Button(w, "Закрыть", 684, 584, 96, 28);
            close.Click += delegate
            {
                try { if (editor.Rows > 0) capture(); } catch { }
                w.Close();
            };

            loadMatrix(0);
            await w.ShowDialog(owner);
        }

        // ---------------- VECTOR ----------------

        public static async Task VectorAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("VECTOR — ВЕКТОРЫ", 640, 510);
            string[] names = { "A", "B", "C" };
            Button[] vectorButtons = new Button[3];
            int activeIndex = 0;
            VectorData resultVec = null;
            double resultScalar = 0;
            bool hasScalar = false;

            Dlg.Caption(w, "Вектор", 16, 10, 60);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button b = Dlg.Button(w, "Vct" + names[i], 80 + i * 76, 8, 68, 30);
                b.Click += delegate { };
                vectorButtons[i] = b;
            }
            RadioButton twoD = Dlg.Radio(w, "2D", true, 330, 14, 54, "vectorDim");
            RadioButton threeD = Dlg.Radio(w, "3D", false, 384, 14, 54, "vectorDim");

            string[] axis = { "x", "y", "z" };
            TextBox[] comps = new TextBox[3];
            for (int i = 0; i < 3; i++)
            {
                Dlg.Label(w, axis[i] + " =", 20, 62 + i * 40, 34);
                comps[i] = Dlg.Text(w, "0", 60, 58 + i * 40, 160);
            }
            Button save = Dlg.Button(w, "Записать", 20, 190, 100, 30);
            Button clear = Dlg.Button(w, "Очистить", 128, 190, 92, 30);

            TextBlock result = Dlg.Label(w, "", 260, 188, 360, 60);
            result.FontFamily = Dlg.MonoFont;
            result.FontSize = 15;
            result.FontWeight = FontWeight.Bold;
            result.Background = Dlg.FieldBack;
            result.VerticalAlignment = VerticalAlignment.Center;
            Dlg.MutedLabel(w, "Результат", 260, 172, 160);
            TextBlock info = Dlg.MutedLabel(w, "", 260, 256, 360, 34);
            info.TextWrapping = TextWrapping.Wrap;

            Action updateDim = delegate
            {
                comps[2].IsEnabled = threeD.IsChecked == true;
            };
            twoD.IsCheckedChanged += delegate { updateDim(); };
            threeD.IsCheckedChanged += delegate { updateDim(); };

            Func<VectorData> readVector = delegate
            {
                int dim = threeD.IsChecked == true ? 3 : 2;
                double[] v = new double[dim];
                v[0] = Dlg.Parse(comps[0].Text);
                v[1] = Dlg.Parse(comps[1].Text);
                if (dim == 3) v[2] = Dlg.Parse(comps[2].Text);
                return new VectorData(v);
            };
            Action saveVector = delegate
            {
                view.Vectors[activeIndex] = readVector();
            };
            Func<int, VectorData> get = delegate(int idx)
            {
                if (idx == activeIndex) return readVector();
                if (view.Vectors[idx] == null) throw new CalcError("Сначала запишите Vct" + names[idx]);
                return view.Vectors[idx];
            };
            Action<VectorData, string> showVec = delegate(VectorData v, string label)
            {
                resultVec = v;
                hasScalar = false;
                result.Text = v.Format();
                info.Text = label;
            };
            Action<double, string> showScalar = delegate(double v, string label)
            {
                resultScalar = v;
                hasScalar = true;
                resultVec = null;
                result.Text = Dlg.Format(v);
                info.Text = label;
            };

            Action<int> loadVector = delegate(int idx)
            {
                try { saveVector(); } catch { }
                activeIndex = idx;
                for (int i = 0; i < 3; i++)
                {
                    vectorButtons[i].Background = i == idx
                        ? new SolidColorBrush(Color.FromRgb(230, 226, 217))
                        : Dlg.ButtonBack;
                }
                VectorData v = view.Vectors[idx];
                if (v == null)
                {
                    comps[0].Text = comps[1].Text = comps[2].Text = "0";
                    twoD.IsChecked = true;
                }
                else
                {
                    comps[0].Text = Dlg.Format(v.Values[0]);
                    comps[1].Text = Dlg.Format(v.Values[1]);
                    comps[2].Text = v.Dimension > 2 ? Dlg.Format(v.Values[2]) : "0";
                    twoD.IsChecked = v.Dimension == 2;
                    threeD.IsChecked = v.Dimension == 3;
                }
                updateDim();
                result.Text = "";
                info.Text = "";
            };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                vectorButtons[i].Click += delegate { loadVector(idx); };
            }

            save.Click += delegate
            {
                try
                {
                    saveVector();
                    info.Text = "Вектор Vct" + names[activeIndex] + " записан";
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            clear.Click += delegate
            {
                comps[0].Text = comps[1].Text = comps[2].Text = "0";
                view.Vectors[activeIndex] = null;
                result.Text = "";
            };

            Button add = Dlg.Button(w, "A + B", 260, 58, 90, 32);
            add.Click += delegate
            {
                try { showVec(VectorData.Add(get(0), get(1)), "A + B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button sub = Dlg.Button(w, "A − B", 358, 58, 90, 32);
            sub.Click += delegate
            {
                try { showVec(VectorData.Sub(get(0), get(1)), "A − B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button dot = Dlg.Button(w, "A · B", 456, 58, 90, 32);
            dot.Click += delegate
            {
                try { showScalar(VectorData.Dot(get(0), get(1)), "A · B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button cross = Dlg.Button(w, "A × B", 260, 96, 90, 32);
            cross.Click += delegate
            {
                try { showVec(VectorData.Cross(get(0), get(1)), "A × B"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button angle = Dlg.Button(w, "Угол A,B", 358, 96, 90, 32);
            angle.Click += delegate
            {
                try
                {
                    double rad = VectorData.Angle(get(0), get(1));
                    double inUnit = Numerics.ConvertAngle(rad, AngleUnit.Radians, view.State.Angle);
                    string unit = view.State.Angle == AngleUnit.Degrees ? "°" : (view.State.Angle == AngleUnit.Gradians ? " grad" : " rad");
                    showScalar(inUnit, "Угол между A и B" + unit);
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button norm = Dlg.Button(w, "|A|", 456, 96, 90, 32);
            norm.Click += delegate
            {
                try { showScalar(readVector().Norm(), "|A|"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button unit = Dlg.Button(w, "Ед. вектор A", 260, 134, 120, 32);
            unit.Click += delegate
            {
                try { showVec(readVector().Unit(), "Единичный вектор A"); }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button addScalar = Dlg.Button(w, "A + k", 388, 134, 70, 32);
            addScalar.Click += async delegate
            {
                try
                {
                    string[] v = await Dlg.FieldsAsync(w, "A + k", "Прибавить число к каждому компоненту",
                        new string[] { "k" }, new string[] { "1" });
                    if (v == null) return;
                    double k = Dlg.Parse(v[0]);
                    VectorData a = readVector();
                    double[] vals = new double[a.Dimension];
                    for (int i = 0; i < vals.Length; i++) vals[i] = a.Values[i] + k;
                    showVec(new VectorData(vals), "A + " + Dlg.Format(k));
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };
            Button mulScalar = Dlg.Button(w, "A × k", 466, 134, 70, 32);
            mulScalar.Click += async delegate
            {
                try
                {
                    string[] v = await Dlg.FieldsAsync(w, "A × k", "Умножить вектор на число",
                        new string[] { "k" }, new string[] { "2" });
                    if (v == null) return;
                    double k = Dlg.Parse(v[0]);
                    VectorData a = readVector();
                    double[] vals = new double[a.Dimension];
                    for (int i = 0; i < vals.Length; i++) vals[i] = a.Values[i] * k;
                    showVec(new VectorData(vals), "A × " + Dlg.Format(k));
                }
                catch (Exception ex) { _ = Dlg.ErrorAsync(w, ex.Message); }
            };

            Button insertScalar = Dlg.Button(w, "Вставить скаляр", 260, 306, 130, 30, true);
            insertScalar.Click += delegate
            {
                if (hasScalar)
                {
                    view.InsertTextExternal(Dlg.Format(resultScalar));
                    w.Close();
                }
                else if (resultVec != null)
                {
                    double n = resultVec.Norm();
                    view.InsertTextExternal(Dlg.Format(n));
                    w.Close();
                }
                else _ = Dlg.ErrorAsync(w, "Сначала выполните операцию.");
            };
            Button close = Dlg.Button(w, "Закрыть", 524, 438, 96, 28);
            close.Click += delegate
            {
                try { saveVector(); } catch { }
                w.Close();
            };

            loadVector(0);
            await w.ShowDialog(owner);
        }

        // ---------------- BASE-N ----------------

        public static async Task BaseAsync(Window owner, CalculatorView view)
        {
            Window w = Dlg.Make("BASE-N — СИСТЕМЫ СЧИСЛЕНИЯ", 620, 530);
            Dlg.Caption(w, "Система счисления", 16, 10, 180);
            int selectedBase = view.BaseValue;
            ComboBox baseBox = Dlg.Combo(w, new string[] { "DEC (10)", "HEX (16)", "BIN (2)", "OCT (8)" },
                BaseIndex(selectedBase), 170, 8, 170);
            Dlg.MutedLabel(w, "AND, OR, XOR, NOT, <<, >>, скобки, A–F в HEX.", 350, 14, 250);

            TextBox expr = Dlg.Text(w, "", 16, 48, 588, 30);
            expr.FontSize = 15;
            TextBox output = Dlg.Text(w, "", 16, 90, 588, 74, true, true, true);
            ScrollViewer.SetVerticalScrollBarVisibility(output, ScrollBarVisibility.Auto);

            baseBox.SelectionChanged += delegate
            {
                selectedBase = BaseValue(baseBox.SelectedIndex);
                view.BaseValue = selectedBase;
                output.Text = "Основание: " + BaseLabel(selectedBase);
            };

            string[] labels =
            {
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F",
                "(", ")", "+", "−", "×", "÷", "AND", "OR", "XOR", "NOT", "<<", ">>", "DEL", "AC"
            };
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 6;
                int row = i / 6;
                string text = labels[i];
                Button b = Dlg.Button(w, text, 16 + col * 94, 176 + row * 40, 88, 34);
                b.FontSize = text.Length > 1 ? 10 : 13;
                b.Click += delegate { PressBaseToken(expr, text); };
            }

            Action compute = delegate
            {
                try
                {
                    BaseNResult r = BaseN.Evaluate(expr.Text, selectedBase);
                    output.Text = "DEC " + r.Dec + "\r\nHEX " + r.Hex + "\r\nBIN " + r.Bin + "\r\nOCT " + r.Oct;
                }
                catch (Exception ex)
                {
                    output.Text = "Ошибка: " + ex.Message;
                }
            };

            Button equals = Dlg.Button(w, "=", 16, 422, 180, 34, true);
            equals.Click += delegate { compute(); };
            Button insert = Dlg.Button(w, "Вставить в калькулятор", 204, 422, 190, 34);
            insert.Click += delegate
            {
                try
                {
                    BaseNResult r = BaseN.Evaluate(expr.Text, selectedBase);
                    view.UseBaseResult(expr.Text, r.Value, selectedBase);
                    w.Close();
                }
                catch (Exception ex)
                {
                    _ = Dlg.ErrorAsync(w, ex.Message);
                }
            };
            Button close = Dlg.Button(w, "Закрыть", 516, 476, 88, 28);
            close.Click += delegate { w.Close(); };
            w.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) compute();
            };

            output.Text = "Основание: " + BaseLabel(selectedBase) + "\r\nВведите выражение, например FF+1";
            await w.ShowDialog(owner);
        }

        private static void PressBaseToken(TextBox expr, string token)
        {
            int caret = Math.Max(0, Math.Min(expr.CaretIndex, expr.Text != null ? expr.Text.Length : 0));
            string current = expr.Text ?? "";
            if (token == "DEL")
            {
                if (caret > 0)
                {
                    expr.Text = current.Remove(caret - 1, 1);
                    expr.CaretIndex = caret - 1;
                }
                return;
            }
            if (token == "AC")
            {
                expr.Text = "";
                return;
            }
            string insert = token;
            if (token == "AND" || token == "OR" || token == "XOR" || token == "NOT" || token == "<<" || token == ">>")
                insert = " " + token + " ";
            expr.Text = current.Insert(caret, insert);
            expr.CaretIndex = caret + insert.Length;
            expr.Focus();
        }

        private static int BaseIndex(int b)
        {
            if (b == 16) return 1;
            if (b == 2) return 2;
            if (b == 8) return 3;
            return 0;
        }

        private static int BaseValue(int idx)
        {
            if (idx == 1) return 16;
            if (idx == 2) return 2;
            if (idx == 3) return 8;
            return 10;
        }

        private static string BaseLabel(int b)
        {
            if (b == 16) return "HEX";
            if (b == 2) return "BIN";
            if (b == 8) return "OCT";
            return "DEC";
        }
    }
}
