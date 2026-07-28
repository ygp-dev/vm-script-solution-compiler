using Script.Methods;
using System;
using System.Collections.Generic;
using System.IO;
using TranslationCalibModuCs;
using VM.Core;

/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript : ScriptMethods, IProcessMethods
{
    // 当前已经采集的点位数量
    int processCount;

    // 图像坐标点集合
    List<PointData> ImgPoint = new List<PointData>();

    // 机械坐标点集合
    List<PointData> MachinePoint = new List<PointData>();

    TranslationCalibModuTool calibTool;
    /// <summary>
    /// 脚本初始化时执行一次
    /// </summary>
    public void Init()
    {
        // 初始化采集计数
        processCount = 0;

    }

    /// <summary>
    /// 脚本每运行一次都会进入该函数
    /// </summary>
    public bool Process()
    {
        string CalibName = "";
        VmProcedure prc = (VmProcedure)VmSolution.Instance[strName];
        ModuleInfoList allModule;
        allModule = prc.GetAllModuleList();
        for (int i = 0; i < allModule.nNum; i++)
        {
            if (allModule.astModuleInfo[i].strModuleName == "TranslationCalibModu")
            {
                CalibName = allModule.astModuleInfo[i].strDisplayName;
                calibTool = (TranslationCalibModuTool)VmSolution.Instance[strName + "." + CalibName];
            }
        }
        /********************************************************
         * 1. 复位逻辑
         * 
         * count <= 0：
         *      外部未设置有效采集数量，清空已有数据
         * 
         * resetvar == 1：
         *      外部触发复位，清空图像点和机械点
         * 
         * 注意：
         *      这里保持你原来的逻辑，不 return。
         *      也就是说复位后，如果 count > 0，本次仍然会继续采集一个点。
         ********************************************************/
        if (count <= 0 || resetvar == 1)
        {
            ImgPoint.Clear();
            MachinePoint.Clear();

            processCount = 0;
            state = 0;

            calibTool.ModuParams....