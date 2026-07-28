using Script.Methods;
public partial class UserScript
{
    public ImageData SourceImage { get { return default(ImageData); } }
    public PointData[] SourcePoint { get { return default(PointData[]); } }
    public RoiboxData SourceBox { get { return default(RoiboxData); } }
    public LineData[] SourceLine { get { return default(LineData[]); } }
    public RectData[] SourceRect { get { return default(RectData[]); } }
    public EllipseData[] SourceEllipse { get { return default(EllipseData[]); } }
    public ImageData ImageEcho { set { } }
    public PointData[] PointEcho { set { } }
    public RoiboxData BoxEcho { set { } }
    public LineData[] LineEcho { set { } }
    public RectData[] RectEcho { set { } }
    public EllipseData[] EllipseEcho { set { } }
}
