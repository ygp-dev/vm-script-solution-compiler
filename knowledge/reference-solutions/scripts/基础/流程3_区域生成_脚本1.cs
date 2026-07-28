using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
using VisionDesigner;
using VisionDesigner.PreproMask;
using System.Linq;
using System.Collections.Generic;
/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript : ScriptMethods, IProcessMethods
{
    //the count of process
    //执行次数计数
    int processCount;

    private CPreproMaskTool m_cMaskToolObj = null;
    private CMvdImage m_cInputImage = null;
    private CMvdImage m_cMaskImage = null;
    /// <summary>
    /// Initialize the field's value when compiling
	/// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        //You can add other global fields here
        //变量初始化，其余变量可在该函数中添加
        processCount = 0;
        m_cMaskToolObj = new CPreproMaskTool();
    }

    /// <summary>
    /// Enter the process function when running code once
	/// 流程执行一次进入Process函数
    /// </summary>
    /// <returns></returns>
    public bool Process()
    {
        //You can add your codes here, for realizing your desired function
        //每次执行将进入该函数，此处添加所需的逻辑流程处理

        CMvdImage Base = new CMvdImage();
        Base.InitImage((uint)img1.Width, (uint)img1.Heigth, MVD_PIXEL_FORMAT.MVD_PIXEL_MONO_08);
        m_cMaskToolObj.InputImage = Base;

        m_cMaskToolObj.RegionList.Clear();
        VisionDesigner.CMvdRectangleF _RectangleF = new VisionDesigner.CMvdRectangleF((Base.Width / 2)+2000, Base.Height / 2, 2000, 2000);
        m_cMaskToolObj.RegionList.Add(new Tuple<CMvdShape, bool>(_RectangleF, true));
        VisionDesigner.CMvdCircleF _CircleF = new CMvdCircleF(new MVD_POINT_F((Base.Width / 2)+2000, Base.Height / 2), 800);
        m_cMaskToolObj.RegionList.Add(new Tuple<CMvdShape, bool>(_CircleF, false));
        m_cMaskToolObj.Run();

        outIMG = CMvdImageToImageData( m_cMaskToo...