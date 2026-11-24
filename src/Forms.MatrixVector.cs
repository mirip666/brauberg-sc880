using System;
using System.Drawing;
using System.Windows.Forms;

namespace SC880
{
    internal sealed class MatrixForm : Form
    {
        private readonly Sc880Form owner;
        private readonly DataGridView grid;
        private readonly DataGridView resultGrid;
        private readonly NumericUpDown rows;
        private readonly NumericUpDown cols;
        private readonly Label info;
        private readonly Label activeLabel;
        private readonly Button[] matrixButtons = new Button[3];
        private int activeIndex;
        private MatrixData result;

        private static readonly string[] Names = { "A", "B", "C" };

        public MatrixForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "MATRIX — МАТРИЦЫ", 800, 640);
            Controls.Add(UiKit.Caption("Матрица", 16, 12, 70));
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button b = UiKit.Button(Names[i], 90 + i * 54, 8, 48, 30);
                b.Click += delegate { SelectMatrix(idx); };
                matrixButtons[i] = b;
                Controls.Add(b);
            }
            Controls.Add(UiKit.Label("Строк", 270, 14, 40));
            rows = new NumericUpDown();
            rows.Minimum = 1;
            rows.Maximum = 6;
            rows.Value = 2;
            rows.Location = new Point(312, 10);
            rows.Size = new Size(48, 24);
            Controls.Add(rows);
            Controls.Add(UiKit.Label("Столбцов", 372, 14, 66));
            cols = new NumericUpDown();
            cols.Minimum = 1;
            cols.Maximum = 6;
            cols.Value = 2;
            cols.Location = new Point(442, 10);
            cols.Size = new Size(48, 24);
            Controls.Add(cols);
            Button resize = UiKit.Button("Размер", 502, 8, 80, 28);
            resize.Click += delegate { ResizeGrid(); };
            Controls.Add(resize);
            activeLabel = UiKit.Label("Матрица A", 600, 14, 160);
            Controls.Add(activeLabel);

            grid = MakeGrid(16, 48, 372, 350);
            Controls.Add(grid);
            Controls.Add(UiKit.Label("Результат", 408, 14, 80));
            resultGrid = MakeGrid(408, 48, 372, 350);
            resultGrid.ReadOnly = true;
            Controls.Add(resultGrid);

            info = new Label();
            info.Location = new Point(16, 404);
            info.Size = new Size(370, 60);
            info.Font = new Font("Consolas", 10F, FontStyle.Bold);
            info.ForeColor = UiKit.Ink;
            info.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(info);

            int bx = 16, by = 474;
            AddOp("Определитель", bx, by, 120, delegate { DoDet(); });
            AddOp("Обратная", bx + 128, by, 110, delegate { DoInverse(); });
            AddOp("Транспонировать", bx + 246, by, 134, delegate { DoTranspose(); });
            AddOp("A + B", bx + 388, by, 78, delegate { DoAdd(); });
            AddOp("A − B", bx + 474, by, 78, delegate { DoSub(); });
            AddOp("A × B", bx + 560, by, 78, delegate { DoMul(); });
            AddOp("A × k", bx, by + 40, 100, delegate { DoScalar(); });
            AddOp("Скопировать в A", bx + 108, by + 40, 140, delegate { CopyResultTo(0); });
            AddOp("в B", bx + 256, by + 40, 72, delegate { CopyResultTo(1); });
            AddOp("в C", bx + 336, by + 40, 72, delegate { CopyResultTo(2); });
            AddOp("Записать", bx + 416, by + 40, 100, delegate { SaveGrid(); });
            AddOp("Очистить", bx + 524, by + 40, 114, delegate { ClearGrid(); });

            Button close = UiKit.Button("Закрыть", 684, 574, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;

            SelectMatrix(0);
        }

        private DataGridView MakeGrid(int x, int y, int w, int h)
        {
            DataGridView g = new DataGridView();
            g.Location = new Point(x, y);
            g.Size = new Size(w, h);
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToResizeColumns = false;
            g.RowHeadersVisible = false;
            g.ColumnHeadersVisible = false;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.FixedSingle;
            g.GridColor = Color.FromArgb(220, 217, 209);
            g.Font = new Font("Consolas", 10F, FontStyle.Regular);
            g.EditMode = DataGridViewEditMode.EditOnEnter;
            g.RowTemplate.Height = 34;
            g.SelectionMode = DataGridViewSelectionMode.CellSelect;
            return g;
        }

        private void AddOp(string text, int x, int y, int w, EventHandler handler)
        {
            Button b = UiKit.Button(text, x, y, w, 32);
            b.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Regular);
            b.Click += handler;
            Controls.Add(b);
        }

        private void SelectMatrix(int idx)
        {
            if (grid.RowCount > 0 && grid.ColumnCount > 0)
            {
                try { CaptureGrid(); }
                catch { }
            }
            activeIndex = idx;
            activeLabel.Text = "Матрица " + Names[idx];
            for (int i = 0; i < 3; i++)
                matrixButtons[i].BackColor = i == idx ? Color.FromArgb(230, 226, 217) : Color.FromArgb(246, 245, 241);
            MatrixData m = owner.Matrices[idx];
            LoadGrid(m);
        }

        private void LoadGrid(MatrixData m)
        {
            if (m == null)
            {
                rows.Value = 2;
                cols.Value = 2;
                grid.Rows.Clear();
                grid.Columns.Clear();
                for (int j = 0; j < 2; j++)
                {
                    grid.Columns.Add("c" + j, "");
                    grid.Columns[j].Width = 52;
                }
                grid.Rows.Add(2);
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++) grid.Rows[i].Cells[j].Value = "0";
                return;
            }
            rows.Value = m.Rows;
            cols.Value = m.Cols;
            grid.Rows.Clear();
            grid.Columns.Clear();
            for (int j = 0; j < m.Cols; j++)
            {
                grid.Columns.Add("c" + j, "");
                grid.Columns[j].Width = 52;
            }
            grid.Rows.Add(m.Rows);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Cols; j++)
                    grid.Rows[i].Cells[j].Value = UiKit.FormatNumber(m.Data[i, j]);
        }

        private void ResizeGrid()
        {
            int r = (int)rows.Value;
            int c = (int)cols.Value;
            string[,] old = new string[grid.Rows.Count, grid.Columns.Count];
            for (int i = 0; i < grid.Rows.Count; i++)
                for (int j = 0; j < grid.Columns.Count; j++)
                    old[i, j] = Convert.ToString(grid.Rows[i].Cells[j].Value);
            grid.Rows.Clear();
            grid.Columns.Clear();
            for (int j = 0; j < c; j++)
            {
                grid.Columns.Add("c" + j, "");
                grid.Columns[j].Width = 52;
            }
            grid.Rows.Add(r);
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                    grid.Rows[i].Cells[j].Value = i < old.GetLength(0) && j < old.GetLength(1) ? old[i, j] : "0";
        }

        private MatrixData CaptureGrid()
        {
            int r = grid.Rows.Count;
            int c = grid.Columns.Count;
            if (r == 0 || c == 0) throw new CalcError("Пустая матрица");
            MatrixData m = new MatrixData(r, c);
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                {
                    string text = Convert.ToString(grid.Rows[i].Cells[j].Value);
                    if (string.IsNullOrWhiteSpace(text)) text = "0";
                    m.Data[i, j] = UiKit.ParseNumber(text);
                }
            owner.Matrices[activeIndex] = m;
            return m;
        }

        private void SaveGrid()
        {
            try
            {
                CaptureGrid();
                info.Text = "Матрица " + Names[activeIndex] + " записана (" + grid.Rows.Count + "×" + grid.Columns.Count + ")";
            }
            catch (Exception ex)
            {
                UiKit.Error(this, ex.Message);
            }
        }

        private void ClearGrid()
        {
            for (int i = 0; i < grid.Rows.Count; i++)
                for (int j = 0; j < grid.Columns.Count; j++) grid.Rows[i].Cells[j].Value = "0";
            owner.Matrices[activeIndex] = null;
            result = null;
            resultGrid.Rows.Clear();
            resultGrid.Columns.Clear();
            info.Text = "Матрица очищена";
        }

        private void ShowResult(MatrixData m, string label)
        {
            result = m;
            resultGrid.Rows.Clear();
            resultGrid.Columns.Clear();
            for (int j = 0; j < m.Cols; j++)
            {
                resultGrid.Columns.Add("r" + j, "");
                resultGrid.Columns[j].Width = 52;
            }
            resultGrid.Rows.Add(m.Rows);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Cols; j++)
                    resultGrid.Rows[i].Cells[j].Value = UiKit.FormatNumber(m.Data[i, j]);
            info.Text = label;
        }

        private void DoDet()
        {
            try
            {
                MatrixData m = CaptureGrid();
                double det = m.Determinant();
                result = null;
                resultGrid.Rows.Clear();
                resultGrid.Columns.Clear();
                info.Text = "det(" + Names[activeIndex] + ") = " + UiKit.FormatNumber(det);
            }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoInverse()
        {
            try { ShowResult(CaptureGrid().Inverse(), "Обратная матрица " + Names[activeIndex] + "⁻¹"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoTranspose()
        {
            try { ShowResult(CaptureGrid().Transpose(), "Транспонированная матрица"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private MatrixData GetStored(int idx)
        {
            if (idx == activeIndex) return CaptureGrid();
            if (owner.Matrices[idx] == null) throw new CalcError("Сначала зап+ишите матрицу " + Names[idx]);
            return owner.Matrices[idx];
        }

        private void DoAdd()
        {
            try { ShowResult(MatrixData.Add(GetStored(0), GetStored(1)), "A + B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoSub()
        {
            try { ShowResult(MatrixData.Sub(GetStored(0), GetStored(1)), "A − B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoMul()
        {
            try { ShowResult(MatrixData.Mul(GetStored(0), GetStored(1)), "A × B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoScalar()
        {
            try
            {
                string[] v = FieldsForm.Ask(this, "A × k", "Умножить матрицу на число", new string[] { "k" }, new string[] { "2" });
                if (v == null) return;
                double k = UiKit.ParseNumber(v[0]);
                ShowResult(MatrixData.Scalar(CaptureGrid(), k), "A × " + UiKit.FormatNumber(k));
            }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void CopyResultTo(int idx)
        {
            if (result == null) { UiKit.Error(this, "Сначала выполните операцию с матрицей."); return; }
            owner.Matrices[idx] = result.Clone();
            if (idx != activeIndex)
            {
                SelectMatrix(idx);
            }
            else
            {
                LoadGrid(result);
            }
            info.Text = "Результат скопирован в " + Names[idx];
        }
    }

    internal sealed class VectorForm : Form
    {
        private readonly Sc880Form owner;
        private readonly TextBox[] comps = new TextBox[3];
        private readonly RadioButton twoD;
        private readonly RadioButton threeD;
        private readonly Label result;
        private readonly Label info;
        private readonly Button[] vectorButtons = new Button[3];
        private int activeIndex;
        private VectorData resultVec;
        private double resultScalar;
        private bool hasScalar;

        private static readonly string[] Names = { "A", "B", "C" };

        public VectorForm(Sc880Form owner)
        {
            this.owner = owner;
            UiKit.Style(this, "VECTOR — ВЕКТОРЫ", 640, 500);
            Controls.Add(UiKit.Caption("Вектор", 16, 12, 60));
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button b = UiKit.Button("Vct" + Names[i], 80 + i * 76, 8, 68, 30);
                b.Click += delegate { SelectVector(idx); };
                vectorButtons[i] = b;
                Controls.Add(b);
            }
            twoD = new RadioButton();
            twoD.Text = "2D";
            twoD.Location = new Point(330, 14);
            twoD.AutoSize = true;
            twoD.Checked = true;
            twoD.CheckedChanged += delegate { UpdateDim(); };
            Controls.Add(twoD);
            threeD = new RadioButton();
            threeD.Text = "3D";
            threeD.Location = new Point(384, 14);
            threeD.AutoSize = true;
            Controls.Add(threeD);

            string[] axis = { "x", "y", "z" };
            for (int i = 0; i < 3; i++)
            {
                Controls.Add(UiKit.Label(axis[i] + " =", 20, 62 + i * 40, 34));
                comps[i] = UiKit.TextBox("0", 60, 58 + i * 40, 160);
                Controls.Add(comps[i]);
            }
            Button save = UiKit.Button("Записать", 20, 190, 100, 30);
            save.Click += delegate { SaveVector(); };
            Controls.Add(save);
            Button clear = UiKit.Button("Очистить", 128, 190, 92, 30);
            clear.Click += delegate
            {
                comps[0].Text = comps[1].Text = comps[2].Text = "0";
                owner.Vectors[activeIndex] = null;
                result.Text = "";
            };
            Controls.Add(clear);

            int x = 260, y = 58;
            AddOp("A + B", x, y, 90, delegate { DoAdd(); });
            AddOp("A − B", x + 98, y, 90, delegate { DoSub(); });
            AddOp("A · B", x + 196, y, 90, delegate { DoDot(); });
            AddOp("A × B", x, y + 38, 90, delegate { DoCross(); });
            AddOp("Угол A,B", x + 98, y + 38, 90, delegate { DoAngle(); });
            AddOp("|A|", x + 196, y + 38, 90, delegate { DoNorm(); });
            AddOp("Ед. вектор A", x, y + 76, 120, delegate { DoUnit(); });
            AddOp("A + k", x + 128, y + 76, 70, delegate { DoAddScalar(); });
            AddOp("A × k", x + 206, y + 76, 70, delegate { DoMulScalar(); });

            result = new Label();
            result.Location = new Point(260, 190);
            result.Size = new Size(360, 58);
            result.Font = new Font("Consolas", 12F, FontStyle.Bold);
            result.TextAlign = ContentAlignment.MiddleLeft;
            result.BorderStyle = BorderStyle.FixedSingle;
            result.BackColor = Color.White;
            result.ForeColor = UiKit.Ink;
            Controls.Add(result);
            Label resultCap = UiKit.MutedLabel("Результат", 260, 172, 160);
            Controls.Add(resultCap);
            info = UiKit.MutedLabel("", 260, 252, 360);
            Controls.Add(info);

            Button insert = UiKit.Button("Вставить скаляр", 260, 300, 130, 30);
            insert.Click += delegate { InsertScalar(); };
            Controls.Add(insert);
            Button close = UiKit.Button("Закрыть", 524, 428, 96, 28);
            close.DialogResult = DialogResult.Cancel;
            Controls.Add(close);
            CancelButton = close;

            SelectVector(0);
        }

        private void AddOp(string text, int x, int y, int w, EventHandler handler)
        {
            Button b = UiKit.Button(text, x, y, w, 32);
            b.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Regular);
            b.Click += handler;
            Controls.Add(b);
        }

        private void SelectVector(int idx)
        {
            try { SaveVector(); } catch { }
            activeIndex = idx;
            for (int i = 0; i < 3; i++)
                vectorButtons[i].BackColor = i == idx ? Color.FromArgb(230, 226, 217) : Color.FromArgb(246, 245, 241);
            VectorData v = owner.Vectors[idx];
            if (v == null)
            {
                comps[0].Text = comps[1].Text = comps[2].Text = "0";
                twoD.Checked = true;
            }
            else
            {
                comps[0].Text = UiKit.FormatNumber(v.Values[0]);
                comps[1].Text = UiKit.FormatNumber(v.Values[1]);
                comps[2].Text = v.Dimension > 2 ? UiKit.FormatNumber(v.Values[2]) : "0";
                twoD.Checked = v.Dimension == 2;
                threeD.Checked = v.Dimension == 3;
            }
            UpdateDim();
            result.Text = "";
            info.Text = "";
        }

        private void UpdateDim()
        {
            comps[2].Enabled = threeD.Checked;
        }

        private VectorData ReadVector()
        {
            int dim = threeD.Checked ? 3 : 2;
            double[] v = new double[dim];
            v[0] = UiKit.ParseNumber(comps[0].Text);
            v[1] = UiKit.ParseNumber(comps[1].Text);
            if (dim == 3) v[2] = UiKit.ParseNumber(comps[2].Text);
            return new VectorData(v);
        }

        private void SaveVector()
        {
            owner.Vectors[activeIndex] = ReadVector();
        }

        private VectorData Get(int idx)
        {
            if (idx == activeIndex) return ReadVector();
            if (owner.Vectors[idx] == null) throw new CalcError("Сначала запишите Vct" + Names[idx]);
            return owner.Vectors[idx];
        }

        private void ShowVec(VectorData v, string label)
        {
            resultVec = v;
            hasScalar = false;
            result.Text = v.Format();
            info.Text = label;
        }

        private void ShowScalar(double v, string label)
        {
            resultScalar = v;
            hasScalar = true;
            resultVec = null;
            result.Text = UiKit.FormatNumber(v);
            info.Text = label;
        }

        private void DoAdd()
        {
            try { ShowVec(VectorData.Add(Get(0), Get(1)), "A + B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoSub()
        {
            try { ShowVec(VectorData.Sub(Get(0), Get(1)), "A − B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoDot()
        {
            try { ShowScalar(VectorData.Dot(Get(0), Get(1)), "A · B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoCross()
        {
            try { ShowVec(VectorData.Cross(Get(0), Get(1)), "A × B"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoAngle()
        {
            try
            {
                double rad = VectorData.Angle(Get(0), Get(1));
                double inUnit = Numerics.ConvertAngle(rad, AngleUnit.Radians, owner.State.Angle);
                string unit = owner.State.Angle == AngleUnit.Degrees ? "°" : (owner.State.Angle == AngleUnit.Gradians ? " grad" : " rad");
                ShowScalar(inUnit, "Угол между A и B" + unit);
            }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoNorm()
        {
            try { ShowScalar(ReadVector().Norm(), "|A|"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoUnit()
        {
            try { ShowVec(ReadVector().Unit(), "Единичный вектор A"); }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoAddScalar()
        {
            try
            {
                string[] v = FieldsForm.Ask(this, "A + k", "Прибавить число к каждому компоненту", new string[] { "k" }, new string[] { "1" });
                if (v == null) return;
                double k = UiKit.ParseNumber(v[0]);
                VectorData a = ReadVector();
                double[] vals = new double[a.Dimension];
                for (int i = 0; i < vals.Length; i++) vals[i] = a.Values[i] + k;
                ShowVec(new VectorData(vals), "A + " + UiKit.FormatNumber(k));
            }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void DoMulScalar()
        {
            try
            {
                string[] v = FieldsForm.Ask(this, "A × k", "Умножить вектор на число", new string[] { "k" }, new string[] { "2" });
                if (v == null) return;
                double k = UiKit.ParseNumber(v[0]);
                VectorData a = ReadVector();
                double[] vals = new double[a.Dimension];
                for (int i = 0; i < vals.Length; i++) vals[i] = a.Values[i] * k;
                ShowVec(new VectorData(vals), "A × " + UiKit.FormatNumber(k));
            }
            catch (Exception ex) { UiKit.Error(this, ex.Message); }
        }

        private void InsertScalar()
        {
            if (hasScalar)
            {
                owner.InsertTextExternal(UiKit.FormatNumber(resultScalar));
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (resultVec != null)
            {
                double n = resultVec.Norm();
                owner.InsertTextExternal(UiKit.FormatNumber(n));
                DialogResult = DialogResult.OK;
                Close();
            }
            else UiKit.Error(this, "Сначала выполните операцию.");
        }
    }
}
