namespace Script.Methods
{
    #region Enums

    /// <summary>
    /// 数据类型枚举，用于通信发送数据时指定类型
    /// </summary>
    public enum DataType
    {
        Int,
        Float,
        String,
        ByteType
    }

    /// <summary>
    /// 图像像素格式
    /// </summary>
    public enum ImagePixelFormate
    {
        MONO8 = 17301505,
        RGB24 = 35127316
    }

    #endregion

    #region 数据结构

    public class AnnulusData
    {
        public float CenterX { get; set; }

        public float CenterY { get; set; }

        public float InnerRadius { get; set; }

        public float OuterRadius { get; set; }

        public float StartAngle { get; set; }

        public float AngleExtend { get; set; }
    }

    public class EllipseData
    {
        public float CenterX { get; set; }

        public float CenterY { get; set; }

        public float MajorRadius { get; set; }

        public float MinorRadius { get; set; }

        public float Angle { get; set; }
    }

    public class LineData
    {
        public float StartPointX { get; set; }

        public float StartPointY { get; set; }

        public float EndPointX { get; set; }

        public float EndPointY { get; set; }
    }

    /// <summary>
    /// 图像数据
    /// 注意：高度字段拼写随 VM 版本变化：
    ///   ≤ VM 4.3 → public int Heigth { get; set; }   （历史拼写错误）
    ///   ≥ VM 4.4 → public int Height { get; set; }   （已修正）
    /// 生成代码时必须先与用户确认 VM 版本，再决定使用哪个名称。
    /// </summary>
    public class ImageData
    {
        public byte[] Buffer { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }  // VM 4.4+；VM 4.3 及之前为 Heigth

        public ImagePixelFormate PixelFormat { get; set; }
    }

    public class CircleData
    {
        public float Radius { get; set; }

        public float CenterX { get; set; }

        public float CenterY { get; set; }
    }

    /// <summary>
    /// 轮廓点数据结构
    /// </summary>
    public class ContourPointData
    {
        public float PointX { get; set; }

        public float PointY { get; set; }

        public float PointScore { get; set; }

        public int PointIndex { get; set; }
    }

    /// <summary>
    /// 3D点数据
    /// </summary>
    public class Point3DData
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }
    }

    /// <summary>
    /// 点云数据
    /// </summary>
    public class PointCloudData
    {
        public int Capacity { get; set; }

        public int PntNum { get; set; }

        public int NormalsNum { get; set; }

        public int ColorsNum { get; set; }

        public ImageData PcdImage { get; set; }

        public ImageData NormalsImage { get; set; }

        public ImageData ColorsImage { get; set; }

        public int CoorType { get; set; }

        public int FlagSize { get; set; }

        public float Length { get; set; }

        public float Width { get; set; }

        public int FlagHeight { get; set; }

        public float Height { get; set; }

        public int FlagArea { get; set; }

        public float Area { get; set; }

        public int FlagDensity { get; set; }

        public float Density { get; set; }

        public int FlagVolume { get; set; }

        public float Volume { get; set; }

        public int FlagCenter { get; set; }

        public float Center_X { get; set; }

        public float Center_Y { get; set; }

        public float Center_Z { get; set; }

        public int FlagCvx { get; set; }

        public float[] CvxHull_X { get; set; }

        public float[] CvxHull_Y { get; set; }

        public float[] CvxHull_Z { get; set; }

        public int CvxPntNum { get; set; }

        public int FlagRect { get; set; }

        public float[] RectPt_X { get; set; }

        public float[] RectPt_Y { get; set; }

        public float[] RectPt_Z { get; set; }

        public int FlagRectRat { get; set; }

        public float RectRat { get; set; }

        public int FlagPlane { get; set; }

        public float PlaneCoe_a { get; set; }

        public float PlaneCoe_b { get; set; }

        public float PlaneCoe_c { get; set; }

        public float PlaneCoe_d { get; set; }

        public int FlagNormal { get; set; }

        public float PcdNormal_X { get; set; }

        public float PcdNormal_Y { get; set; }

        public float PcdNormal_Z { get; set; }

        public int FlagRange { get; set; }

        public float RangeMin_X { get; set; }

        public float RangeMin_Y { get; set; }

        public float RangeMin_Z { get; set; }

        public float RangeMax_X { get; set; }

        public float RangeMax_Y { get; set; }

        public float RangeMax_Z { get; set; }

        public int FlagBbox { get; set; }

        public float[] Bbox_X { get; set; }

        public float[] Bbox_Y { get; set; }

        public float[] Bbox_Z { get; set; }

        public int FlagIncircle { get; set; }

        public float IncircleCenter_X { get; set; }

        public float IncircleCenter_Y { get; set; }

        public float IncircleCenter_Z { get; set; }

        public float LongAxis { get; set; }

        public float ShortAxis { get; set; }

        public int FlagOutcircle { get; set; }

        public float OutcircleCenter_X { get; set; }

        public float OutcircleCenter_Y { get; set; }

        public float OutcircleCenter_Z { get; set; }

        public float OutcircleRadius { get; set; }

        public int FlagPose { get; set; }

        public float Pose0 { get; set; }

        public float Pose1 { get; set; }

        public float Pose2 { get; set; }

        public float Pose3 { get; set; }

        public float Pose4 { get; set; }

        public float Pose5 { get; set; }

        public float Pose6 { get; set; }

        public float Pose7 { get; set; }

        public float Pose8 { get; set; }

        public float Pose9 { get; set; }

        public float Pose10 { get; set; }

        public float Pose11 { get; set; }

        public float Pose12 { get; set; }

        public float Pose13 { get; set; }

        public float Pose14 { get; set; }

        public float Pose15 { get; set; }

        public int FlagClass { get; set; }

        public int ClassType { get; set; }

        public int FlagDimension { get; set; }

        public float DimensionScale { get; set; }
    }

    public class PointData
    {
        public float PointX { get; set; }

        public float PointY { get; set; }
    }

    public class PolygonData
    {
        public int PointNum { get; set; }

        public float[] PointXArray { get; set; }

        public float[] PointYArray { get; set; }
    }

    /// <summary>
    /// 位姿数据
    /// </summary>
    public class PoseInfoData
    {
        /// <summary>位姿类型：1-欧拉/旋转向量位姿，2-四元数，3-3D变换矩阵</summary>
        public int PoseType;

        /// <summary>位姿坐标类型：1-rgb相机坐标系，2-深度相机坐标系，3-机器人坐标系，4-工作台坐标系，5-物体自身坐标系</summary>
        public int PoseCoorType;

        /// <summary>X方向平移</summary>
        public double Trans_Tx;

        /// <summary>Y方向平移</summary>
        public double Trans_Ty;

        /// <summary>Z方向平移</summary>
        public double Trans_Tz;

        /// <summary>绕X轴旋转的角度</summary>
        public double Trans_Rx;

        /// <summary>绕Y轴旋转的角度</summary>
        public double Trans_Ry;

        /// <summary>绕Z轴旋转的角度</summary>
        public double Trans_Rz;

        /// <summary>旋转类型，默认1表示ZYX类型</summary>
        public int RotType;

        /// <summary>旋转四元数1</summary>
        public double Quat_Rotate1;

        /// <summary>旋转四元数2</summary>
        public double Quat_Rotate2;

        /// <summary>旋转四元数3</summary>
        public double Quat_Rotate3;

        /// <summary>旋转四元数4</summary>
        public double Quat_Rotate4;

        /// <summary>（四元数）X方向平移</summary>
        public double Quat_Tx;

        /// <summary>（四元数）Y方向平移</summary>
        public double Quat_Ty;

        /// <summary>（四元数）Z方向平移</summary>
        public double Quat_Tz;

        /// <summary>3D变换矩阵[0]</summary>
        public double PoseMat0;

        /// <summary>3D变换矩阵[1]</summary>
        public double PoseMat1;

        /// <summary>3D变换矩阵[2]</summary>
        public double PoseMat2;

        /// <summary>3D变换矩阵[3]</summary>
        public double PoseMat3;

        /// <summary>3D变换矩阵[4]</summary>
        public double PoseMat4;

        /// <summary>3D变换矩阵[5]</summary>
        public double PoseMat5;

        /// <summary>3D变换矩阵[6]</summary>
        public double PoseMat6;

        /// <summary>3D变换矩阵[7]</summary>
        public double PoseMat7;

        /// <summary>3D变换矩阵[8]</summary>
        public double PoseMat8;

        /// <summary>3D变换矩阵[9]</summary>
        public double PoseMat9;

        /// <summary>3D变换矩阵[10]</summary>
        public double PoseMat10;

        /// <summary>3D变换矩阵[11]</summary>
        public double PoseMat11;

        /// <summary>3D变换矩阵[12]</summary>
        public double PoseMat12;

        /// <summary>3D变换矩阵[13]</summary>
        public double PoseMat13;

        /// <summary>3D变换矩阵[14]</summary>
        public double PoseMat14;

        /// <summary>3D变换矩阵[15]</summary>
        public double PoseMat15;
    }

    public class RectData
    {
        public float CenterX { get; set; }

        public float CenterY { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }
    }

    /// <summary>
    /// ROI的BOX数据（识别框等）
    /// 注意：高度字段拼写随 VM 版本变化：
    ///   ≤ VM 4.3 → public float Heigth { get; set; }   （历史拼写错误）
    ///   ≥ VM 4.4 → public float Height { get; set; }   （已修正）
    /// 生成代码时必须先与用户确认 VM 版本，再决定使用哪个名称。
    /// </summary>
    public class RoiboxData
    {
        public float CenterX { get; set; }

        public float CenterY { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }  // VM 4.4+；VM 4.3 及之前为 Heigth

        public float Angle { get; set; }
    }

    /// <summary>
    /// 立体图像数据
    /// </summary>
    public class StereoImageData
    {
        /// <summary>立体图像的图像源</summary>
        public int StereoImageSource { get; set; }

        /// <summary>轮廓仪深度图数据</summary>
        public ImageData ProfileRangeImage { get; set; }

        /// <summary>轮廓仪亮度图数据</summary>
        public ImageData ProfileIntensityImage { get; set; }

        /// <summary>RGB-D相机深度图数据</summary>
        public ImageData RGBDRangeImage { get; set; }

        /// <summary>RGB-D相机RGB图数据</summary>
        public ImageData RGBDRgbImage { get; set; }

        /// <summary>图像原点在X轴方向的偏移</summary>
        public int Xoffset { get; set; }

        /// <summary>图像原点在Y轴方向的偏移</summary>
        public int Yoffset { get; set; }

        /// <summary>图像原点在Z轴方向的偏移</summary>
        public int Zoffset { get; set; }

        /// <summary>图像在X轴方向的缩放尺度</summary>
        public float Xscale { get; set; }

        /// <summary>图像在Y轴方向的缩放尺度</summary>
        public float Yscale { get; set; }

        /// <summary>图像在Z轴方向的缩放尺度</summary>
        public float Zscale { get; set; }

        /// <summary>深度到RGB变换矩阵[0]</summary>
        public double Depth2Rgb0 { get; set; }

        /// <summary>深度到RGB变换矩阵[1]</summary>
        public double Depth2Rgb1 { get; set; }

        /// <summary>深度到RGB变换矩阵[2]</summary>
        public double Depth2Rgb2 { get; set; }

        /// <summary>深度到RGB变换矩阵[3]</summary>
        public double Depth2Rgb3 { get; set; }

        /// <summary>深度到RGB变换矩阵[4]</summary>
        public double Depth2Rgb4 { get; set; }

        /// <summary>深度到RGB变换矩阵[5]</summary>
        public double Depth2Rgb5 { get; set; }

        /// <summary>深度到RGB变换矩阵[6]</summary>
        public double Depth2Rgb6 { get; set; }

        /// <summary>深度到RGB变换矩阵[7]</summary>
        public double Depth2Rgb7 { get; set; }

        /// <summary>深度到RGB变换矩阵[8]</summary>
        public double Depth2Rgb8 { get; set; }

        /// <summary>深度到RGB变换矩阵[9]</summary>
        public double Depth2Rgb9 { get; set; }

        /// <summary>深度到RGB变换矩阵[10]</summary>
        public double Depth2Rgb10 { get; set; }

        /// <summary>深度到RGB变换矩阵[11]</summary>
        public double Depth2Rgb11 { get; set; }

        /// <summary>深度到RGB变换矩阵[12]</summary>
        public double Depth2Rgb12 { get; set; }

        /// <summary>深度到RGB变换矩阵[13]</summary>
        public double Depth2Rgb13 { get; set; }

        /// <summary>深度到RGB变换矩阵[14]</summary>
        public double Depth2Rgb14 { get; set; }

        /// <summary>深度到RGB变换矩阵[15]</summary>
        public double Depth2Rgb15 { get; set; }

        /// <summary>RGB相机内参矩阵[0]</summary>
        public double RgbCamMatrix0 { get; set; }

        /// <summary>RGB相机内参矩阵[1]</summary>
        public double RgbCamMatrix1 { get; set; }

        /// <summary>RGB相机内参矩阵[2]</summary>
        public double RgbCamMatrix2 { get; set; }

        /// <summary>RGB相机内参矩阵[3]</summary>
        public double RgbCamMatrix3 { get; set; }

        /// <summary>RGB相机内参矩阵[4]</summary>
        public double RgbCamMatrix4 { get; set; }

        /// <summary>RGB相机内参矩阵[5]</summary>
        public double RgbCamMatrix5 { get; set; }

        /// <summary>RGB相机内参矩阵[6]</summary>
        public double RgbCamMatrix6 { get; set; }

        /// <summary>RGB相机内参矩阵[7]</summary>
        public double RgbCamMatrix7 { get; set; }

        /// <summary>RGB相机内参矩阵[8]</summary>
        public double RgbCamMatrix8 { get; set; }

        /// <summary>RGB相机畸变系数[0]</summary>
        public double RgbDisCoeffs0 { get; set; }

        /// <summary>RGB相机畸变系数[1]</summary>
        public double RgbDisCoeffs1 { get; set; }

        /// <summary>RGB相机畸变系数[2]</summary>
        public double RgbDisCoeffs2 { get; set; }

        /// <summary>RGB相机畸变系数[3]</summary>
        public double RgbDisCoeffs3 { get; set; }

        /// <summary>RGB相机畸变系数[4]</summary>
        public double RgbDisCoeffs4 { get; set; }

        /// <summary>深度相机畸变系数[0]</summary>
        public double DepthDisCoeffs0 { get; set; }

        /// <summary>深度相机畸变系数[1]</summary>
        public double DepthDisCoeffs1 { get; set; }

        /// <summary>深度相机畸变系数[2]</summary>
        public double DepthDisCoeffs2 { get; set; }

        /// <summary>深度相机畸变系数[3]</summary>
        public double DepthDisCoeffs3 { get; set; }

        /// <summary>深度相机畸变系数[4]</summary>
        public double DepthDisCoeffs4 { get; set; }
    }



    #endregion
}
