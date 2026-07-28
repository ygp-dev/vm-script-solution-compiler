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
      /// 图像坐标系下计算角度：
      /// 上=0°，右=90°，下=180°，左=270°，顺时针为正
      /// </summary>
      private float CalcWaferAngle(float cx, float cy, float px, float py)
      {
          float dx = px - cx;
          float dy = py - cy;

          if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9)
              return 0.0F;

          float angle = (float)(Math.Atan2(dx, -dy) * 180.0F / Math.PI);

          angle %= 360.0F;
          if (angle < 0.0)
              angle += 360.0F;

          return angle;
      }



    
    /// <summary>
    /// Enter the process function when running code once
    /// </summary>
    /// <returns></returns>
    public bool Process()
    {
        //You can add your codes here, for realizing your desired function
        float cx = EndPoint[0].PointX;
        float cy = EndPoint[0].PointY;

        float px = StartPoint[0].PointX;
        float py = StartPoint[0].PointY;

        float angle = CalcWaferAngle(cx, cy, px, py);

        outLineAngle = angle;

        return true;
    }
}
                            