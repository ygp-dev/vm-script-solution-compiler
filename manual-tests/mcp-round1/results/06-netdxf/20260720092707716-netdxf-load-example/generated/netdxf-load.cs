using System;
using netDxf;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    public void Init() { }

    public bool Process()
    {
        try
        {
            DxfDocument document = DxfDocument.Load(DxfPath);
            Loaded = document != null ? 1 : 0;
            return document != null;
        }
        catch (Exception error)
        {
            Loaded = 0;
            ConsoleWrite(error.ToString());
            return false;
        }
    }
}
