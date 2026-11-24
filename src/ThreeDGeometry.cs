using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SC880
{
    internal static class ThreeDColors
    {
        public static readonly Color BodyFront = Color.FromRgb(247, 244, 237);
        public static readonly Color BodyBevel = Color.FromRgb(255, 253, 248);
        public static readonly Color BodySide = Color.FromRgb(207, 202, 192);
        public static readonly Color BodyBack = Color.FromRgb(222, 218, 209);
        public static readonly Color Ink = Color.FromRgb(35, 35, 33);
        public static readonly Color MutedInk = Color.FromRgb(112, 109, 104);
        public static readonly Color Orange = Color.FromRgb(220, 112, 38);
        public static readonly Color Pink = Color.FromRgb(194, 43, 108);
        public static readonly Color Teal = Color.FromRgb(33, 127, 122);
        public static readonly Color Purple = Color.FromRgb(110, 78, 160);
        public static readonly Color LcdFrame = Color.FromRgb(97, 104, 91);
        public static readonly Color LcdTop = Color.FromRgb(179, 190, 171);
        public static readonly Color LcdBottom = Color.FromRgb(154, 168, 144);
        public static readonly Color LcdInk = Color.FromRgb(28, 55, 91);
        public static readonly Color LcdFaint = Color.FromRgb(70, 92, 84);
        public static readonly Color KeyWhiteTop = Color.FromRgb(253, 252, 248);
        public static readonly Color KeyWhiteBottom = Color.FromRgb(226, 223, 215);
        public static readonly Color KeyWhiteLine = Color.FromRgb(201, 197, 188);
        public static readonly Color KeyDarkTop = Color.FromRgb(79, 78, 75);
        public static readonly Color KeyDarkBottom = Color.FromRgb(49, 49, 47);
        public static readonly Color KeyDarkLine = Color.FromRgb(35, 35, 34);
        public static readonly Color KeyRedTop = Color.FromRgb(219, 84, 71);
        public static readonly Color KeyRedBottom = Color.FromRgb(174, 44, 37);
        public static readonly Color KeyRedLine = Color.FromRgb(143, 35, 29);
        public static readonly Color KeySoftTop = Color.FromRgb(240, 238, 233);
        public static readonly Color KeySoftBottom = Color.FromRgb(211, 208, 200);
        public static readonly Color KeySoftLine = Color.FromRgb(187, 183, 175);
        public static readonly Color White = Color.FromRgb(255, 255, 255);
    }

    internal static class ThreeDGeometry
    {
        public static Point[] RoundedRect(double width, double height, double radius, int segments)
        {
            double r = Math.Max(0.1, Math.Min(radius, Math.Min(width, height) / 2.0));
            double hw = width / 2.0;
            double hh = height / 2.0;
            List<Point> p = new List<Point>();
            AddCorner(p, hw - r, -hh + r, r, -90, 0, segments);
            AddCorner(p, hw - r, hh - r, r, 0, 90, segments);
            AddCorner(p, -hw + r, hh - r, r, 90, 180, segments);
            AddCorner(p, -hw + r, -hh + r, r, 180, 270, segments);
            return p.ToArray();
        }

        private static void AddCorner(List<Point> points, double cx, double cy, double r,
            double startAngle, double endAngle, int segments)
        {
            int n = Math.Max(1, segments);
            for (int i = 0; i <= n; i++)
            {
                double a = (startAngle + (endAngle - startAngle) * i / n) * Math.PI / 180.0;
                points.Add(new Point(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r));
            }
        }

        public static MeshGeometry3D Face(Point[] outline, double z, bool front)
        {
            MeshGeometry3D m = new MeshGeometry3D();
            m.Positions.Add(new Point3D(0, 0, z));
            for (int i = 0; i < outline.Length; i++)
                m.Positions.Add(new Point3D(outline[i].X, outline[i].Y, z));
            int n = outline.Length;
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i;
                int b = 1 + ((i + 1) % n);
                if (front)
                {
                    m.TriangleIndices.Add(0);
                    m.TriangleIndices.Add(a);
                    m.TriangleIndices.Add(b);
                }
                else
                {
                    m.TriangleIndices.Add(0);
                    m.TriangleIndices.Add(b);
                    m.TriangleIndices.Add(a);
                }
            }
            return m;
        }

        public static MeshGeometry3D Ring(Point[] outer, double outerZ, Point[] inner, double innerZ)
        {
            MeshGeometry3D m = new MeshGeometry3D();
            int n = Math.Min(outer.Length, inner.Length);
            for (int i = 0; i < n; i++)
            {
                Point3D po0 = new Point3D(outer[i].X, outer[i].Y, outerZ);
                Point3D pi0 = new Point3D(inner[i].X, inner[i].Y, innerZ);
                Point3D po1 = new Point3D(outer[(i + 1) % n].X, outer[(i + 1) % n].Y, outerZ);
                Point3D pi1 = new Point3D(inner[(i + 1) % n].X, inner[(i + 1) % n].Y, innerZ);
                int b = m.Positions.Count;
                m.Positions.Add(po0);
                m.Positions.Add(po1);
                m.Positions.Add(pi1);
                m.Positions.Add(pi0);
                m.TriangleIndices.Add(b);
                m.TriangleIndices.Add(b + 1);
                m.TriangleIndices.Add(b + 2);
                m.TriangleIndices.Add(b);
                m.TriangleIndices.Add(b + 2);
                m.TriangleIndices.Add(b + 3);
            }
            return m;
        }

        public static MeshGeometry3D Side(Point[] outline, double backZ, double frontZ)
        {
            MeshGeometry3D m = new MeshGeometry3D();
            int n = outline.Length;
            for (int i = 0; i < n; i++)
            {
                Point3D b0 = new Point3D(outline[i].X, outline[i].Y, backZ);
                Point3D f0 = new Point3D(outline[i].X, outline[i].Y, frontZ);
                Point3D b1 = new Point3D(outline[(i + 1) % n].X, outline[(i + 1) % n].Y, backZ);
                Point3D f1 = new Point3D(outline[(i + 1) % n].X, outline[(i + 1) % n].Y, frontZ);
                int p = m.Positions.Count;
                m.Positions.Add(b0);
                m.Positions.Add(b1);
                m.Positions.Add(f1);
                m.Positions.Add(f0);
                m.TriangleIndices.Add(p);
                m.TriangleIndices.Add(p + 1);
                m.TriangleIndices.Add(p + 2);
                m.TriangleIndices.Add(p);
                m.TriangleIndices.Add(p + 2);
                m.TriangleIndices.Add(p + 3);
            }
            return m;
        }

        public static MeshGeometry3D TexturedFrontQuad(double x0, double y0, double x1, double y1, double z)
        {
            MeshGeometry3D m = new MeshGeometry3D();
            m.Positions.Add(new Point3D(x0, y1, z));
            m.Positions.Add(new Point3D(x1, y1, z));
            m.Positions.Add(new Point3D(x1, y0, z));
            m.Positions.Add(new Point3D(x0, y0, z));
            m.TextureCoordinates.Add(new Point(0, 0));
            m.TextureCoordinates.Add(new Point(1, 0));
            m.TextureCoordinates.Add(new Point(1, 1));
            m.TextureCoordinates.Add(new Point(0, 1));
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(3); m.TriangleIndices.Add(2);
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(2); m.TriangleIndices.Add(1);
            return m;
        }

        public static MeshGeometry3D TexturedBackQuad(double x0, double y0, double x1, double y1, double z)
        {
            MeshGeometry3D m = new MeshGeometry3D();
            m.Positions.Add(new Point3D(x1, y1, z));
            m.Positions.Add(new Point3D(x0, y1, z));
            m.Positions.Add(new Point3D(x0, y0, z));
            m.Positions.Add(new Point3D(x1, y0, z));
            m.TextureCoordinates.Add(new Point(0, 0));
            m.TextureCoordinates.Add(new Point(1, 0));
            m.TextureCoordinates.Add(new Point(1, 1));
            m.TextureCoordinates.Add(new Point(0, 1));
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(3); m.TriangleIndices.Add(2);
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(2); m.TriangleIndices.Add(1);
            return m;
        }

        public static GeometryModel3D Model(MeshGeometry3D mesh, Material material, Material back)
        {
            GeometryModel3D model = new GeometryModel3D(mesh, material);
            model.BackMaterial = back;
            return model;
        }
    }
}
