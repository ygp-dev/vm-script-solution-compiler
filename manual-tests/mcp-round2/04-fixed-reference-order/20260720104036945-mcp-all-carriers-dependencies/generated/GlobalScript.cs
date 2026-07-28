using System;
using System.Globalization;
using VM.GlobalScript.Methods;
using VM.Core;
using VM.PlatformSDKCS;

public class UserGlobalScript : UserGlobalMethods, IScriptMethods
{
    public int Init()
    {
        try
        {
            int initResult = InitSDK();
            if (initResult != 0) return initResult;
            return 0;
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
            return -1;
        }
    }

    public int Process()
    {
        try
        {
            ConsoleWrite(Convert.ToString("MCP GlobalScript OK", CultureInfo.InvariantCulture));
            return 0;
        }
        catch (Exception error)
        {
            ConsoleWrite(error.ToString());
            return -1;
        }
    }
}
