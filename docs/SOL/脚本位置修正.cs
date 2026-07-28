using Script.Methods;
using System;
using VisionDesigner;
using VisionDesigner.PositionFix;

/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript : ScriptMethods, IProcessMethods
{
    //执行次数计数
    int processCount;
    CPositionFixTool PosFixToolObj = null;

    /// <summary>
    /// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        processCount = 0;

        // 工具只初始化一次
        PosFixToolObj = new CPositionFixTool();
    }

    /// <summary>
    /// 流程执行一次进入 Process 函数
    /// </summary>
    public bool Process()
    {
        outROI = RunFix(BaseROI);
        return true;
    }

    /// <summary>
    /// 执行位置修正
    /// </summary>
    RoiboxData RunFix(RoiboxData roibox)
    {
        try
        {
            // 重新取 BasicParam，不重新 new 工具
            CPositionFixBasicParam basicParam = PosFixToolObj.BasicParam;

            // Base
            VisionDesigner.PositionFix.MVD_FIDUCIAL_POINT_F stBasinInit =
                new VisionDesigner.PositionFix.MVD_FIDUCIAL_POINT_F();
            stBasinInit.stPosition.fX = BaseX;
            stBasinInit.stPosition.fY = BaseY;
            stBasinInit.fAngle = BaseAngle;
            stBasinInit.fScaleX = 1;
            stBasinInit.fScaleY = 1;
            basicParam.BasePoint = stBasinInit;

            // Running
            VisionDesigner.PositionFix.MVD_FIDUCIAL_POINT_F stBasicRun =
                new VisionDesigner.PositionFix.MVD_FIDUCIAL_POINT_F();
            stBasicRun.stPosition.fX = RunX;
            stBasicRun.stPosition.fY = RunY;
            stBasicRun.fAngle = RunAngle;
            stBasicRun.fScaleX = 1;
            stBasicRun.fScaleY = 1;
            basicParam.RunningPoint = stBasicRun;

            // Image尺寸
            MVD_SIZE_I stImageSize = new MVD_SIZE_I();
            stImageSize.nWidth = ImgWidth;
            stImageSize.nHeight = ImgHeight;
            basicParam.RunImageSize = stImageSize;

            // 修正模式
            basicParam.FixMode = MVD_POSFIX_MODE.MVD_POSFIX_MODE_HVA;

            // 执行
            return FixPointgon(roibox);
        }
        catch (MvdException ex)
        {
            ShowMessageBox("定位修正失败！错误码 = 0x" +
                           ex.ErrorCode.ToString("X") +
                           "\n消息: " + ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            ShowMessageBox("标准异常: " + ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 修正 ROI
    /// </summary>
    RoiboxData FixPointgon(RoiboxData roibox)
    {
        CMvdRectangleF PointROI = new CMvdRectangleF(
            roibox.CenterX,
            roibox.CenterY,
            roibox.Width,
            roibox.Height
        );
        PointROI.Angle = roibox.Angle;

        PosFixToolObj.BasicParam.InitialShape = PointROI;

        PosFixToolObj.Run();

        CMvdRectangleF FixPointRoi = PosFixToolObj.Result.CorrectedShape as CMvdRectangleF;

        RoiboxData resultRoi = new RoiboxData();
        resultRoi.CenterX = FixPointRoi.CenterX;
        resultRoi.CenterY = FixPointRoi.CenterY;
        resultRoi.Width = FixPointRoi.Width;
        resultRoi.Height = FixPointRoi.Height;
        resultRoi.Angle = FixPointRoi.Angle;

        return resultRoi;
    }
}
