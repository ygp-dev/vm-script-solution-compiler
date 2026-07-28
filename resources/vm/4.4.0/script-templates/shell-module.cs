using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    public void Init()
    {
        // Initialize persistent fields here.
    }

    public bool Process()
    {
        // Read configured inputs, execute logic, and assign configured outputs here.
        return true;
    }
}
