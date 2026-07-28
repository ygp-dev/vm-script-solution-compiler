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
            Sum = (A + B);
            Passed = ((Enabled != 0)) ? 1 : 0;
            Message = "MCP CSharp OK";
            return true;
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
            return false;
        }
    }
}
