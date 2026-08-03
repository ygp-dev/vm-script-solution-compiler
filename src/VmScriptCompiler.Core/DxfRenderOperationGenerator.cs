namespace VmScriptCompiler.Core;

internal static class DxfRenderOperationGenerator
{
    public static string Invocation(OperationRequirement operation)
    {
        var path = Id(operation.PathInput, "pathInput");
        var width = Id(operation.WidthInput, "widthInput");
        var height = Id(operation.HeightInput, "heightInput");
        var image = Id(operation.ImageOutput, "imageOutput");
        var success = Id(operation.SuccessOutput, "successOutput");
        var error = Id(operation.ErrorOutput, "errorOutput");
        var entityCount = Id(operation.EntityCountOutput, "entityCountOutput");
        var renderedCount = Id(operation.RenderedCountOutput, "renderedCountOutput");
        return "ImageData vmDxfImage; string vmDxfError; int vmDxfEntityCount; int vmDxfRenderedCount; " +
            "bool vmDxfOk = VmDxfRenderer.TryRender(" + path + ", " + width + ", " + height + ", out vmDxfImage, out vmDxfError, out vmDxfEntityCount, out vmDxfRenderedCount); " +
            image + " = vmDxfImage; " + success + " = vmDxfOk ? 1 : 0; " + error + " = vmDxfError; " +
            entityCount + " = vmDxfEntityCount; " + renderedCount + " = vmDxfRenderedCount; " +
            "if (!vmDxfOk) { ConsoleWrite(\"DXF_RENDER_FAILED: \" + vmDxfError); return false; }";
    }

    private static string Id(string? value, string field) =>
        !string.IsNullOrWhiteSpace(value) && value.All(x => char.IsLetterOrDigit(x) || x == '_') && !char.IsDigit(value[0])
            ? value
            : throw new CompilerException("IDENTIFIER_INVALID", "Invalid dxfRender " + field + ": " + value);

    public const string SupportSource = """

internal sealed class VmDxfPath
{
    public readonly System.Collections.Generic.List<System.Drawing.PointF> Points = new System.Collections.Generic.List<System.Drawing.PointF>();
    public bool Closed;
}

internal static class VmDxfRenderer
{
    private const int MinimumDimension = 64;
    private const int MaximumDimension = 8192;
    private const long MaximumPixels = 33554432L;

    public static bool TryRender(string file, int width, int height, out Script.Methods.ImageData image, out string error, out int entityCount, out int renderedCount)
    {
        image = null;
        error = string.Empty;
        entityCount = 0;
        renderedCount = 0;
        try
        {
            if (string.IsNullOrWhiteSpace(file)) return Fail("DXF_PATH_EMPTY: DXF path is empty.", out error);
            if (!System.IO.File.Exists(file)) return Fail("DXF_FILE_NOT_FOUND: " + file, out error);
            if (width < MinimumDimension || height < MinimumDimension || width > MaximumDimension || height > MaximumDimension || (long)width * height > MaximumPixels)
                return Fail("DXF_RENDER_SIZE_INVALID: dimensions must be 64..8192 and at most 33554432 pixels.", out error);

            netDxf.DxfDocument document = netDxf.DxfDocument.Load(file);
            if (document == null) return Fail("DXF_LOAD_FAILED: netDxf returned no document.", out error);
            System.Collections.IEnumerable all = document.Entities.All;
            var paths = new System.Collections.Generic.List<VmDxfPath>();
            foreach (object entity in all)
            {
                entityCount++;
                AddEntity(entity, paths, 0);
            }
            renderedCount = paths.Count;
            if (renderedCount == 0) return Fail("DXF_NO_DRAWABLE_ENTITIES: file contains no supported drawable entities.", out error);

            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            foreach (VmDxfPath path in paths)
                foreach (System.Drawing.PointF point in path.Points)
                {
                    if (point.X < minX) minX = point.X;
                    if (point.X > maxX) maxX = point.X;
                    if (point.Y < minY) minY = point.Y;
                    if (point.Y > maxY) maxY = point.Y;
                }
            if (double.IsInfinity(minX) || double.IsInfinity(minY)) return Fail("DXF_NO_DRAWABLE_ENTITIES: supported entities contain no finite points.", out error);

            double rangeX = System.Math.Max(maxX - minX, 0.000001);
            double rangeY = System.Math.Max(maxY - minY, 0.000001);
            float margin = System.Math.Max(8f, System.Math.Min(width, height) * 0.03f);
            double scale = System.Math.Min((width - margin * 2) / rangeX, (height - margin * 2) / rangeY);
            double offsetX = (width - rangeX * scale) * 0.5;
            double offsetY = (height - rangeY * scale) * 0.5;

            using (var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1f))
                {
                    graphics.Clear(System.Drawing.Color.White);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    foreach (VmDxfPath path in paths)
                    {
                        if (path.Points.Count == 0) continue;
                        var points = new System.Drawing.PointF[path.Points.Count];
                        for (int i = 0; i < points.Length; i++)
                        {
                            System.Drawing.PointF source = path.Points[i];
                            points[i] = new System.Drawing.PointF(
                                (float)(offsetX + (source.X - minX) * scale),
                                (float)(height - offsetY - (source.Y - minY) * scale));
                        }
                        if (points.Length == 1) graphics.FillEllipse(System.Drawing.Brushes.Black, points[0].X - 1, points[0].Y - 1, 3, 3);
                        else if (path.Closed && points.Length > 2) graphics.DrawPolygon(pen, points);
                        else graphics.DrawLines(pen, points);
                    }
                }

                System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(0, 0, width, height);
                System.Drawing.Imaging.BitmapData data = bitmap.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                try
                {
                    int rowBytes = checked(width * 3);
                    byte[] rgb = new byte[checked(rowBytes * height)];
                    byte[] row = new byte[rowBytes];
                    bool hasInk = false;
                    for (int y = 0; y < height; y++)
                    {
                        System.IntPtr address = System.IntPtr.Add(data.Scan0, y * data.Stride);
                        System.Runtime.InteropServices.Marshal.Copy(address, row, 0, rowBytes);
                        int target = y * rowBytes;
                        for (int x = 0; x < rowBytes; x += 3)
                        {
                            byte b = row[x], g = row[x + 1], r = row[x + 2];
                            rgb[target + x] = r;
                            rgb[target + x + 1] = g;
                            rgb[target + x + 2] = b;
                            if (r != 255 || g != 255 || b != 255) hasInk = true;
                        }
                    }
                    if (!hasInk) return Fail("DXF_RENDER_WHITE_IMAGE: supported entities produced no visible pixels.", out error);
                    image = new Script.Methods.ImageData { Buffer = rgb, Width = width, Height = height, PixelFormat = Script.Methods.ImagePixelFormate.RGB24 };
                }
                finally { bitmap.UnlockBits(data); }
            }
            return true;
        }
        catch (System.Exception exception)
        {
            error = "DXF_RENDER_EXCEPTION: " + exception.GetType().Name + ": " + exception.Message;
            image = null;
            return false;
        }
    }

    private static bool Fail(string message, out string error) { error = message; return false; }

    private static void AddEntity(object entity, System.Collections.Generic.List<VmDxfPath> paths, int depth)
    {
        if (entity == null || depth > 8) return;
        string name = entity.GetType().Name;
        if (name == "Insert")
        {
            object exploded = Invoke(entity, "Explode", null);
            var enumerable = exploded as System.Collections.IEnumerable;
            if (enumerable != null) foreach (object child in enumerable) AddEntity(child, paths, depth + 1);
            return;
        }
        if (name == "Line")
        {
            AddPath(paths, false, Get(entity, "StartPoint"), Get(entity, "EndPoint"));
            return;
        }
        if (name == "Circle")
        {
            object center = Get(entity, "Center");
            double radius = Number(Get(entity, "Radius"));
            AddArc(paths, center, radius, 0, 360, true);
            return;
        }
        if (name == "Arc")
        {
            AddArc(paths, Get(entity, "Center"), Number(Get(entity, "Radius")), Number(Get(entity, "StartAngle")), Number(Get(entity, "EndAngle")), false);
            return;
        }
        if (name == "Ellipse" || name == "Polyline2D" || name == "Polyline3D" || name == "Spline")
        {
            int precision = name == "Spline" ? 128 : 96;
            object values = Invoke(entity, "PolygonalVertexes", new object[] { precision });
            bool closed = name == "Ellipse" || Boolean(Get(entity, "IsClosed"));
            AddEnumerablePath(paths, values as System.Collections.IEnumerable, closed);
            return;
        }
        if (name == "Point") AddPath(paths, false, Get(entity, "Position"));
    }

    private static void AddArc(System.Collections.Generic.List<VmDxfPath> paths, object center, double radius, double startDegrees, double endDegrees, bool closed)
    {
        double cx, cy;
        if (!Coordinates(center, out cx, out cy) || radius <= 0) return;
        double sweep = closed ? 360 : endDegrees - startDegrees;
        while (sweep <= 0) sweep += 360;
        int steps = System.Math.Max(16, (int)System.Math.Ceiling(System.Math.Abs(sweep) / 4.0));
        var path = new VmDxfPath { Closed = closed };
        for (int i = 0; i <= steps; i++)
        {
            double angle = (startDegrees + sweep * i / steps) * System.Math.PI / 180.0;
            path.Points.Add(new System.Drawing.PointF((float)(cx + radius * System.Math.Cos(angle)), (float)(cy + radius * System.Math.Sin(angle))));
        }
        paths.Add(path);
    }

    private static void AddPath(System.Collections.Generic.List<VmDxfPath> paths, bool closed, params object[] values)
    {
        var path = new VmDxfPath { Closed = closed };
        foreach (object value in values)
        {
            double x, y;
            if (Coordinates(value, out x, out y) && IsFinite(x) && IsFinite(y)) path.Points.Add(new System.Drawing.PointF((float)x, (float)y));
        }
        if (path.Points.Count > 0) paths.Add(path);
    }

    private static void AddEnumerablePath(System.Collections.Generic.List<VmDxfPath> paths, System.Collections.IEnumerable values, bool closed)
    {
        if (values == null) return;
        var path = new VmDxfPath { Closed = closed };
        foreach (object value in values)
        {
            double x, y;
            if (Coordinates(value, out x, out y) && IsFinite(x) && IsFinite(y)) path.Points.Add(new System.Drawing.PointF((float)x, (float)y));
        }
        if (path.Points.Count > 0) paths.Add(path);
    }

    private static bool Coordinates(object value, out double x, out double y)
    {
        x = y = 0;
        if (value == null) return false;
        object position = Get(value, "Position");
        if (position != null && !object.ReferenceEquals(position, value)) value = position;
        object xValue = Get(value, "X"), yValue = Get(value, "Y");
        if (xValue == null || yValue == null) return false;
        x = Number(xValue); y = Number(yValue);
        return true;
    }

    private static object Get(object value, string name)
    {
        if (value == null) return null;
        System.Reflection.PropertyInfo property = value.GetType().GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        return property == null ? null : property.GetValue(value, null);
    }

    private static object Invoke(object value, string name, object[] arguments)
    {
        try { return value.GetType().InvokeMember(name, System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, value, arguments); }
        catch { return null; }
    }

    private static double Number(object value) { return value == null ? 0 : System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
    private static bool Boolean(object value) { return value != null && System.Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture); }
    private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
}
""";
}
