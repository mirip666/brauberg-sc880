using System;
using System.Collections.Generic;
using System.Globalization;
using SC880;

internal static class Program
{
    private static int passed;
    private static int failed;

    private static void Check(string name, double actual, double expected, double eps)
    {
        if (Math.Abs(actual - expected) <= eps)
        {
            passed++;
            Console.WriteLine("PASS " + name);
        }
        else
        {
            failed++;
            Console.WriteLine("FAIL " + name + " -> " + actual.ToString("G12", CultureInfo.InvariantCulture) + " expected " + expected.ToString("G12", CultureInfo.InvariantCulture));
        }
    }

    private static void Check(string name, bool condition, string details)
    {
        if (condition) { passed++; Console.WriteLine("PASS " + name); }
        else { failed++; Console.WriteLine("FAIL " + name + " -> " + details); }
    }

    private static double Eval(string expr, CalcState state)
    {
        return Engine.Evaluate(expr, state).Value.Re;
    }

    private static void Main()
    {
        CalcState s = new CalcState();
        Check("2+3*4", Eval("2+3*4", s), 14, 1e-12);
        Check("sin30", Eval("sin(30)", s), 0.5, 1e-12);
        s.Angle = AngleUnit.Radians;
        Check("sin(pi)", Eval("sin(π)", s), 0, 1e-12);
        s.Angle = AngleUnit.Degrees;
        Check("sqrt9", Eval("√(9)", s), 3, 1e-12);
        Check("cbrt-27", Eval("∛(-27)", s), -3, 1e-12);
        Check("fact5", Eval("5!", s), 120, 1e-12);
        Check("pow", Eval("2^10", s), 1024, 1e-12);
        Check("inv", Eval("4⁻¹", s), 0.25, 1e-12);
        Check("dms", Eval("1°30′", s), 1.5, 1e-12);
        Check("implicit", Eval("2(3+4)", s), 14, 1e-12);
        Check("logb", Eval("logb(2,8)", s), 3, 1e-12);
        Check("nroot", Eval("nroot(3,27)", s), 3, 1e-12);
        Check("ncr", Eval("ncr(5,2)", s), 10, 1e-12);
        Check("gcd", Eval("gcd(48,18)", s), 6, 1e-12);
        Check("percent", Eval("50%", s), 0.5, 1e-12);

        s.Mode = CalcMode.Cmplx;
        EvalResult polar = Engine.Evaluate("1∠30", s);
        Check("polar re", polar.Value.Re, Math.Sqrt(3) / 2, 1e-12);
        Check("polar im", polar.Value.Im, 0.5, 1e-12);
        EvalResult c = Engine.Evaluate("(2+3i)*(4-5i)", s);
        Check("complex mul re", c.Value.Re, 23, 1e-12);
        Check("complex mul im", c.Value.Im, 2, 1e-12);
        Check("complex display", c.Display, "23+2i", "");
        s.Mode = CalcMode.Comp;

        bool divError = false;
        try { Eval("1/0", s); } catch (CalcError) { divError = true; }
        Check("division by zero", divError, "no error");

        List<StatPoint> data = new List<StatPoint>();
        data.Add(new StatPoint(1, 2, 1));
        data.Add(new StatPoint(2, 4, 1));
        data.Add(new StatPoint(3, 6, 1));
        StatResult st = Statistics.Compute(data, true, false, RegressionType.Linear);
        Check("stat mean", st.MeanX, 2, 1e-12);
        Check("stat sd", st.SampleSdX, 1, 1e-12);
        Check("stat regression b", st.B, 2, 1e-12);
        Check("stat regression r", st.R, 1, 1e-12);
        Check("stat median", st.Median, 2, 1e-12);

        MatrixData a = new MatrixData(new double[,] { { 1, 2 }, { 3, 4 } });
        Check("matrix det", a.Determinant(), -2, 1e-12);
        MatrixData inv = a.Inverse();
        Check("matrix inverse 00", inv.Data[0, 0], -2, 1e-12);
        Check("matrix inverse 01", inv.Data[0, 1], 1, 1e-12);
        MatrixData id = MatrixData.Mul(a, inv);
        Check("matrix A*A^-1", id.Data[1, 1], 1, 1e-12);

        SolveResult sol = Numerics.Solve("x²-2", s, 'X', 1);
        Check("solve sqrt2", sol.Root, Math.Sqrt(2), 1e-9);
        s.Angle = AngleUnit.Radians;
        Check("integral x^2", Numerics.Integral("x²", s, 'X', 0, 1, 1000), 1.0 / 3.0, 1e-9);
        Check("derivative sin", Numerics.Derivative("sin(x)", s, 'X', 0), 1, 1e-7);
        s.Angle = AngleUnit.Degrees;
        Check("sum 1..100", Numerics.Sum("x", s, 'X', 1, 100), 5050, 1e-9);

        Cplx[] roots = Numerics.PolynomialRoots(new double[] { 1, -3, 2 });
        Check("quadratic roots", Math.Abs(roots[0].Re - 1) < 1e-9 && Math.Abs(roots[1].Re - 2) < 1e-9, "got " + roots[0].ToString() + ", " + roots[1].ToString());
        Cplx[] cubic = Numerics.PolynomialRoots(new double[] { 1, -6, 11, -6 });
        Check("cubic roots", Math.Abs(cubic[0].Re - 1) < 1e-7 && Math.Abs(cubic[1].Re - 2) < 1e-7 && Math.Abs(cubic[2].Re - 3) < 1e-7, "got " + cubic[0].ToString() + " " + cubic[1].ToString() + " " + cubic[2].ToString());

        double[] lin = Numerics.SolveLinear(new double[,] { { 2, 1 }, { 1, -1 } }, new double[] { 5, 1 });
        Check("linear x", lin[0], 2, 1e-12);
        Check("linear y", lin[1], 1, 1e-12);

        BaseNResult b = BaseN.Evaluate("FF+1", 16);
        Check("hex FF+1", b.Display, "100", "");
        Check("hex bin", b.Bin, "100000000", "");
        Check("base and", BaseN.Evaluate("12 AND 10", 10).Value, 8, 1e-12);
        Check("base shl", BaseN.Evaluate("1 SHL 4", 10).Value, 16, 1e-12);

        UnitCategory km = UnitsData.Categories[0];
        Check("mile to km", UnitsData.Convert(km, 10, 1, 1), 1.609344, 1e-12);
        UnitCategory temp = null;
        for (int i = 0; i < UnitsData.Categories.Count; i++) if (UnitsData.Categories[i].Temperature) temp = UnitsData.Categories[i];
        Check("C to F", UnitsData.Convert(temp, 0, 1, 0), 32, 1e-12);

        Check("constants", ConstantsData.Items.Count >= 40, "count " + ConstantsData.Items.Count);
        VerifyResult v = Numerics.Verify("2+2=4", s);
        Check("verify true", v.IsTrue, "false");

        NumberText.FormatReal(0.25, s);
        s.FractionResult = true;
        Check("fraction 1/4", NumberText.Format(Cplx.Real(0.25), s), "1/4", "");

        Console.WriteLine();
        Console.WriteLine("TOTAL: " + passed + " passed, " + failed + " failed");
        Environment.Exit(failed == 0 ? 0 : 1);
    }

    private static void Check(string name, string actual, string expected, string dummy)
    {
        if (actual == expected) { passed++; Console.WriteLine("PASS " + name); }
        else { failed++; Console.WriteLine("FAIL " + name + " -> [" + actual + "] expected [" + expected + "]"); }
    }
}
