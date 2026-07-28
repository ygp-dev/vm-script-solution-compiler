using System; 
using VM.GlobalScript.Methods;
using System.Windows.Forms; 
using iMVS_6000PlatformSDKCS;
using System.Runtime.InteropServices;

/*****************************************
 * Example explanation:Example of multi process control operation
 * Logic Control:Single run, each flow execute once
 * Continuous run:continuous run, each flow execute continuous
 * ***************************************/
public class UserGlobalScript : UserGlobalMethods,IScriptMethods
{
        /// <summary>
        /// Init
        /// </summary>
		/// <returns>Success:return 0</returns>
        public int Init()
        {
        	//SDK init
            return InitSDK();
        }

        /// <summary>
        /// execute function
        /// Single run:the function execute once
        /// Continuous run:Repeat the function at regular intervals
        /// </summary>
		/// <returns>Success:return 0</returns>
        public int Process()
        {
        	//m_operateHandle SDK handle
            if (m_operateHandle == IntPtr.Zero)
            {return ImvsSdkPFDefine.IMVS_EC_NULL_PTR;}
			
            //All processes are executed by default
			//If execute in your own define logic,please remove the function :DefaultExecuteProcess, Create your own logic function.
			int nRet = DefaultExecuteProcess();
			
			return nRet;
        }
        
        /// <summary>
        /// SDK callback function 
        /// </summary>
        public override void ResultDataCallBack(IntPtr outputPlatformInfo, IntPtr puser)
        {
            base.ResultDataCallBack(outputPlatformInfo, puser);
            ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO struInfo = (ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO)Marshal.PtrToStructure(outputPlatformInfo, typeof(ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO));
            switch (struInfo.nInfoType)
            {
                //Get module result
                case (uint)ImvsSdkPFDefine.IMVS_CTRLC_OUTPUT_PlATFORM_INFO_TYPE.IMVS_ENUM_CTRLC_OUTPUT_PLATFORM_INFO_MODULE_RESULT:
                    {
                    	ImvsSdkPFDefine.IMVS_PF_MODULE_RESULT_INFO_LIST_P resultInfo = (ImvsSdkPFDefine.IMVS_PF_MODULE_RESULT_INFO_LIST_P)Marshal.PtrToStructure(struInfo.pData, typeof(ImvsSdkPFDefine.IMVS_PF_MODULE_RESULT_INFO_LIST_P));
                    	break;
                    }
                //Get process execute state
                case (uint)ImvsSdkPFDefine.IMVS_CTRLC_OUTPUT_PlATFORM_INFO_TYPE.IMVS_ENUM_CTRLC_OUTPUT_PLATFORM_INFO_WORK_STATE:
                    {
                        ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS stWorkStatus = (ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS)Marshal.PtrToStructure(struInfo.pData, typeof(ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS));
                        break;
                    }
                default:
                    break;
            }
        }
}