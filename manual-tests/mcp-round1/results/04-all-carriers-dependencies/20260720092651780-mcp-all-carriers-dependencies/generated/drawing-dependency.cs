using System;
using System.Drawing;
using Script.Methods;
public partial class UserScript : ScriptMethods, IProcessMethods
{
    private Color color;
    public void Init() { color = Color.Lime; }
    public bool Process() { ColorName = color.Name; return true; }
}
