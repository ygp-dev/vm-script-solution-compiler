using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript:ScriptMethods,IProcessMethods
{
    //the count of process
    int processCount ;  

    /// <summary>
    /// Initialize the field's value when compiling
    /// </summary>
    public void Init()
    {
        //You can add other global fields here
        processCount = 0;
       
    }

    /// <summary>
    /// Enter the process function when running code once
    /// </summary>
    /// <returns></returns>
    public bool Process()
    {
        //You can add your codes here, for realizing your desired function
        if (modelType == 0)
        {
        	if(PosStatus == 1)
        	{
        		outStatus = "OK";
        		outAngle = 0;
        	}
        	else
        	{
        		outStatus = "NG";
        		outAngle = 0;
        	}
        }
        else if(modelType == 1)
        {
        	if(PosStatus == 1 && AngleStatus ==1)
    		{
        		outStatus = "OK";
        		outAngle = angle;
        	}
        	else
        	{
        		outStatus = "NG";
        		outAngle = angle;
        	}
        }
        	
        return true;
    }
}
                            