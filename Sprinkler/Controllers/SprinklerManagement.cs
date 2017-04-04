using Devkoes.Restup.WebServer.Attributes;
using Devkoes.Restup.WebServer.Models.Schemas;
using IoTCoreHelpers;
using SprinklerRPI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Controllers
{
    [RestController(InstanceCreationType.Singleton)]
    partial class SprinklerManagement
    {
        public static int NUMBER_SPRINKLERS { get; internal set; }
        public static Sprinkler[] Sprinklers { get; internal set; }
        public static int SprDuration { get; internal set; } //= 20;
        public static ArrayList SprinklerPrograms { get; internal set; } //= new ArrayList();
        public static SprinklerProgramTypical[] TypicalProg { get; set; }
        public static SoilHumidity soilHumidity { get; set;}

        static public async Task InitParam()
        {
            NUMBER_SPRINKLERS = 3;
            SprDuration = 20;
            SprinklerPrograms = new ArrayList();

            FileStream fileToRead = null;
            try
            {

                //fileToRead = new FileStream(strDefaultDir + "\\" + strFileProgram, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileToRead = new FileStream(await GetFilePathAsync(strFileProgram), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long fileLength = fileToRead.Length;

                byte[] buf = new byte[fileLength];
                //string mySetupString = "";

                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                //await str.ReadAsync(buf,  )
                // convert the read into a string

                List<Param> Params = Param.decryptParam(new String(Encoding.UTF8.GetChars(buf)));
                int mSpr = -1;
                int mDur = -1;
                bool mReboot = false;
                if (Params != null)
                {
                    MySecurityKey = Param.CheckConvertString(Params, paramSecurityKey);
                    mSpr = Param.CheckConvertInt32(Params, paramSpr);
                    mDur = Param.CheckConvertInt32(Params, paramDuration);
                    mReboot = Param.CheckConvertBool(Params, paramReboot);
                }
                //write the last time a boot has happened and initialize the next reboot
                if (mReboot)
                {
                    //MyRebootManager = new Timer(new TimerCallback(RebootManager), null, (mReboot* 3600000), (mReboot* 3660000));
                }
                if (mDur > 0)
                    SprDuration = mDur;
                securityKey = paramSecurityKey + Param.ParamEqual + MySecurityKey;
                if (mSpr != -1)
                {
                    NUMBER_SPRINKLERS = mSpr;
                    // Initiate Sprinklers (3 by default)
                    Sprinklers = new Sprinkler[NUMBER_SPRINKLERS];
                    for (int i = 0; i < Sprinklers.Length; i++)
                    {
                        bool isinvert = Param.CheckConvertBool(Params, paramInv + i);
                        Sprinklers[i] = new Sprinkler(i, isinvert);
                    }
                    for (int i = 0; i < NUMBER_SPRINKLERS; i++)
                    {
                        Sprinklers[i].Name = Param.CheckConvertString(Params, paramSprName + i);
                    }
                }
            }
            catch (Exception e)
            {
                if (fileToRead != null)
                {
                    fileToRead.Dispose();
                }
            }
            soilHumidity = new SoilHumidity();
            await InitPrograms();
            await InitTypicalProgam();
            //init the timer that will ruin every minute to check when to stop/start 
            InitTimer();
            await InitIoTHub();
            SendDataToAzure("{\"info\":\"Sprinkler system started\"}");
            ReceiveDataFromAzure();

        }

        private bool SecCheck(string strFilePath)
        {
            if (strFilePath.IndexOf(securityKey) == -1)
                return false;
            return true;

        }
        private string ErrorAuth()
        {
            string strResp = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
            strResp += "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Gestion train</title>";
            strResp += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/></head>";
            strResp += "<meta http-equiv=\"Cache-control\" content=\"no-cache\"/>";
            strResp += "<meta http-equiv=\"EXPIRES\" content=\"0\" />";
            strResp += "<BODY><h1>RaspberryPi2 Lego Train running Windows 10</h1><p>";
            strResp += "Invalid security key</body></html>";
            return strResp;
        }

        [UriFormat("/prgm.aspx{param}")]
        public GetResponse Programm(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessProgram(param));
        }

        [UriFormat("/lstprg.aspx{param}")]
        public GetResponse ListProgramm(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessListProgram(param));
        }

        [UriFormat("/cal.aspx{param}")]
        public GetResponse Calendar(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessCalendar(param));
        }

        [UriFormat("/spr.aspx{param}")]
        public GetResponse Sprinkler(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessSprinkler(param));
        }

        [UriFormat("/util.aspx{param}")]
        public GetResponse Util(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessUtil(param));
        }

        [UriFormat("/sprdt.aspx{param}")]
        public GetResponse SprinklerDetails(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessSprinklerDetails(param));
        }

        [UriFormat("/typic.aspx{param}")]
        public GetResponse Typical(string param)
        {
            if (!SecCheck(param))
                return new GetResponse(GetResponse.ResponseStatus.OK, ErrorAuth());
            return new GetResponse(GetResponse.ResponseStatus.OK, ProcessTypical(param));
        }

    }
}
