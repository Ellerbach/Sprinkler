using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Controllers
{
    partial class SprinklerManagement
    {
        #region All const string
        // security key
        static string MySecurityKey = "Key1234";
        const string paramSecurityKey = "sec";
        static string securityKey = "";
        // parameters
        const string paramYear = "y";
        const string paramMonth = "mo";
        const string paramDay = "da";
        const string paramHour = "h";
        const string paramMinute = "mi";
        const string paramDuration = "du";
        const string paramSpr = "spr";
        const string paramInv = "inv";
        const string paramClk = "clk";
        const string paramReboot = "rbt";
        const string paramSave = "save";
        const string paramNoUI = "noui";
        const string paramSprName = "spn";
        const string paramOK = "OK";
        const string paramProblem = "Problem";
        const string paramPageProgram = "prgm.aspx";
        const string pageCSS = "spr.css";
        const string paramPageListPrgm = "lstprg.aspx";
        const string paramPageCalendar = "cal.aspx";
        const string paramPageSprinkler = "spr.aspx";
        const string paramPageUtil = "util.aspx";
        const string paramPageSprinklersInfo = "sprdt.aspx";
        const string paramPagePredictions = "predict.aspx";
        const string paramPageHistoric = "hist.aspx";
        const string paramPageTypic = "typic.aspx";
        const string paramAutomateAll = "auto";
        //const string paramPageHumidity = "hum.aspx";

        const string strFileProgram = "Prog.config";
        const string strFileTypicalProgram = "typic.txt";
        const string strFileListProgram = "lstprg.txt";
        const string strFilePrediction = "wunder.config";
        const string strFileActualPrograms = "actual.txt";
        #endregion All const string
    }
}
