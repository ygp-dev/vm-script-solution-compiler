using System.Text;

namespace VmScriptCompiler.Core;

internal sealed record VmIoFilter(string Name, string? ValueType = null, string? Style = null, IReadOnlyList<VmIoFilter>? Children = null)
{
    public bool IsCombination => Style is not null;
}

internal sealed record VmPortBinding(IoRequirement Port, string LogicalName, string ValueType, IReadOnlyList<string> StructNames, VmIoFilter Dynamic);

internal sealed class Vm44CSharpIoLayout
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.Ordinal);

    private Vm44CSharpIoLayout(IReadOnlyList<IoRequirement> inputs, IReadOnlyList<IoRequirement> outputs)
    {
        Inputs = inputs.Select(CreateBinding).ToArray();
        Outputs = outputs.Select(CreateBinding).ToArray();
    }

    public IReadOnlyList<VmPortBinding> Inputs { get; }
    public IReadOnlyList<VmPortBinding> Outputs { get; }

    public static Vm44CSharpIoLayout Create(IReadOnlyList<IoRequirement> inputs, IReadOnlyList<IoRequirement> outputs) => new(inputs, outputs);

    private VmPortBinding CreateBinding(IoRequirement port)
    {
        var logical = "%" + port.Name + "%";
        var vmType = VmType(port.Type);
        if (!IsComplex(port.Type))
            return new(port, logical, vmType, [logical], new(logical, DynamicScalarType(port.Type)));

        var counterKey = port.Type == "roibox[]" ? "roibox" : port.Type;
        var index = _counters.TryGetValue(counterKey, out var current) ? current : 0;
        _counters[counterKey] = index + 1;
        var dynamic = CreateComplex(logical, port.Type, index);
        return new(port, logical, vmType, Flatten(dynamic).Select(x => x.Name).ToArray(), dynamic);
    }

    private static VmIoFilter CreateComplex(string logical, string type, int index)
    {
        VmIoFilter F(string name, string valueType) => new("%" + name + index + "%", valueType);
        VmIoFilter C(string name, string style, params VmIoFilter[] children) => new("%" + name + index + "%", Style: style, Children: children);
        return type switch
        {
            "image" => new(logical, Style: "IMAGE", Children: [F("Image", "image"), F("ImageWidth", "int"), F("ImageHeight", "int"), F("ImagePixelFormat", "int")]),
            "roibox" or "roibox[]" => new(logical, Style: "ROIBOX", Children: [
                C("ROI CenterPoint", "POINT", F("roicenterx", "float"), F("roicentery", "float")),
                F("roiwidth", "float"), F("roiheight", "float"), F("roiangle", "float")]),
            "roiannulus" => new(logical, Style: "ROIANNULUS", Children: [
                C("DetectAnnulusCenterPoint", "POINT", F("DetectAnnulusCenterX", "float"), F("DetectAnnulusCenterY", "float")),
                F("DetectAnnulusInnerRadius", "float"), F("DetectAnnulusOuterRadius", "float"), F("DetectAnnulusStartAngle", "float"), F("DetectAnnulusAngleExtend", "float")]),
            "roipolygon" => new(logical, Style: "ROIPOLYGON", Children: [
                F("BlindPolygonPointNum", "int"),
                C("BlindPolygonPoints", "POINT", F("BlindPolygonPointsX", "float"), F("BlindPolygonPointsY", "float"))]),
            "point" => new(logical, Style: "POINT", Children: [F("pointx", "float"), F("pointy", "float")]),
            "line" => new(logical, Style: "LINE", Children: [
                C("LineStartPoint", "POINT", F("LineStartPointX", "float"), F("LineStartPointY", "float")),
                C("LineEndPoint", "POINT", F("LineEndPointX", "float"), F("LineEndPointY", "float"))]),
            "fixture" => new(logical, Style: "FIXTURE", Children: [
                C("Fixtured Point", "POINT", F("FixtureInitPointX", "float"), F("FixtureInitPointY", "float")),
                F("FixtureInitAngle", "float"), F("FixtureInitScaleX", "float"), F("FixtureInitScaleY", "float"),
                C("Unfixtured Point", "POINT", F("FixtureRunPointX", "float"), F("FixtureRunPointY", "float")),
                F("FixtureRunAngle", "float"), F("FixtureRunScaleX", "float"), F("FixtureRunScaleY", "float")]),
            "rect" => new(logical, Style: "Rect", Children: [
                C("RectPoint", "POINT", F("BlobRectX", "float"), F("BlobRectY", "float")), F("BlobRectWidth", "float"), F("BlobRectHeight", "float")]),
            "ellipse" => new(logical, Style: "ELLIPSE", Children: [
                C("Center Point", "POINT", F("CenterX", "float"), F("CenterY", "float")),
                F("MajorRadius", "float"), F("MinorRadius", "float"), F("Angle", "float")]),
            _ => throw new CompilerException("IO_TYPE_MISMATCH", "Unsupported complex C# IO type: " + type)
        };
    }

    internal static IEnumerable<VmIoFilter> Flatten(VmIoFilter node)
    {
        if (!node.IsCombination) { yield return node; yield break; }
        foreach (var child in node.Children ?? [])
            foreach (var filter in Flatten(child)) yield return filter;
    }

    internal static string VmType(string type) => type switch
    {
        "bool" => "int", "int" => "int", "int[]" => "int[]", "float" => "float", "float[]" => "float[]",
        "string" => "string", "string[]" => "string[]", "byte" => "byte", "image" => "IMAGE",
        "roibox" => "ROIBOX", "roibox[]" => "ROIBOX[]", "roiannulus" => "ROIANNULUS",
        "roipolygon" => "ROIPOLYGON", "point" => "POINT", "line" => "LINE", "fixture" => "FIXTURE",
        "rect" => "Rect", "ellipse" => "ELLIPSE", "pointset" => "pointset",
        _ => throw new CompilerException("IO_TYPE_MISMATCH", "Unsupported IO type: " + type)
    };

    internal static string DynamicScalarType(string type) => type switch
    {
        "bool" => "int", "int" or "int[]" => "int", "float" or "float[]" => "float",
        "string" or "string[]" => "string", "byte" => "byte", "pointset" => "pointset",
        _ => throw new CompilerException("IO_TYPE_MISMATCH", "Unsupported scalar DynamicIO type: " + type)
    };

    private static bool IsComplex(string type) => type is "image" or "roibox" or "roibox[]" or "roiannulus" or "roipolygon" or "point" or "line" or "fixture" or "rect" or "ellipse";
}
