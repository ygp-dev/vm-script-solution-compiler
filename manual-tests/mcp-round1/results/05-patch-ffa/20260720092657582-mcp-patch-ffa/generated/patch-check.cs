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
            Doubled = (Value * 2);
            return true;
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
            return false;
        }
    }
}
