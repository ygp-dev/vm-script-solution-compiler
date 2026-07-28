using System;
using System.Globalization;
using System.Threading;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    public void Init()
    {
        try
        {
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
        }
    }

    public bool Process()
    {
        try
        {
            ImageEcho = SourceImage;
            PointEcho = SourcePoint;
            BoxEcho = SourceBox;
            LineEcho = SourceLine;
            RectEcho = SourceRect;
            EllipseEcho = SourceEllipse;
            return true;
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
            return false;
        }
    }
}
