using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;

using System.IO;
using System.Net;
/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript:ScriptMethods,IProcessMethods
{
    //the count of process
	//执行次数计数
    int processCount ;  

    /// <summary>
    /// Initialize the field's value when compiling
	/// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        //You can add other global fields here
		//变量初始化，其余变量可在该函数中添加
        processCount = 0;
       
    }
    
	static void UploadFileToFtp(string ftpServer, string ftpUsername, string ftpPassword, string localFilePath, string remoteFileName)
    {
        try
        {
            // 为FTP服务器创建URI
            Uri ftpUri = new Uri(ftpServer + remoteFileName);

            // 创建FTP请求对象
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpUri);
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

            // 读取本地文件
            using (FileStream localFileStream = new FileStream(localFilePath, FileMode.Open))
            using (Stream ftpStream = ftpRequest.GetRequestStream())
            {
                // 将文件上载到FTP服务器
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = localFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ftpStream.Write(buffer, 0, bytesRead);
                }
            }
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Error: {ex.Message}");
        }
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
        
		string ftpServer = "ftp://yourftpserver.com/";  //ftp IP
        string ftpUsername = "yourUsername";  //用户名
        string ftpPassword = "yourPassword";  //密码
        string localFilePath = @"C:\path\to\your\local\file.txt";  //文件本地路径
        string remoteFileName = "remote_file.txt";  //传输文件名

        UploadFileToFtp(ftpServer, ftpUsername, ftpPassword, localFilePath, remoteFileName);

        //Console.WriteLine("File uploaded successfully");
		
        return true;
    }
}
                            