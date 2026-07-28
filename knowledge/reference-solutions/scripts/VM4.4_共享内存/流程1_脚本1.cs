using Script.Methods;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Windows.Forms;
/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript:ScriptMethods,IProcessMethods
{
    //the count of process
    int processCount ;
    private const string MapName = "FrameMap";
    private const string MutexName = @"Global\FrameMapMutex";
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
        ImageData img = ReadFrame();
        SetImageValue("out0", img);
        return true;
    }

    public static ImageData ReadFrame()
    {
        try
        {
            using (var mutex = Mutex.OpenExisting(MutexName))
            {
                mutex.WaitOne();
                try
                {
                    using (var mmf = MemoryMappedFile.OpenExisting(MapName))
                    using (var stream = mmf.CreateViewStream())
                    using (var reader = new BinaryReader(stream))
                    {
                        int width = reader.ReadInt32();
                        int height = reader.ReadInt32();
                        int pixelFormat = reader.ReadInt32();
                        int dataLen = reader.ReadInt32();
                        byte[] data = reader.ReadBytes(dataLen);

                        return new ImageData()
                        {
                            Width = width,
                            Height = height,
                            PixelFormat = (...