using SprinklerRPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SprinklerRPI.Controllers
{
    partial class SprinklerManagement
    {
        static private Timer myTimer;
        static public TimeSpan TimeCheck = new TimeSpan(0, 0, 0);
        static private DateTime LastTimeCheck = new DateTime(DateTime.Now.Year,DateTime.Now.Month, DateTime.Now.Day);

        static private async Task InitTypicalProgam()
        {
            FileStream fileToRead = null;
            try
            {
                fileToRead = new FileStream(await GetFilePathAsync(strFileTypicalProgram), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long fileLength = fileToRead.Length;
                byte[] buf = new byte[fileLength];
                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                // convert the read into a string
                var strdata = new string(Encoding.UTF8.GetChars(buf));
                TypicalProg = JsonConvert.DeserializeObject<SprinklerProgramTypical[]>(strdata);
            }
            catch (Exception e)
            {

            }
        }

        static private async Task InitPrograms()
        {
            //read all saved programs
            FileStream mystream = null;
            try
            {
                mystream = new FileStream(await GetFilePathAsync(strFileListProgram), FileMode.OpenOrCreate, FileAccess.Read);
                byte[] buff = new byte[mystream.Length];
                await mystream.ReadAsync(buff, 0, (int)mystream.Length);
                string strprogs = Encoding.UTF8.GetString(buff);
                var progs = Newtonsoft.Json.JsonConvert.DeserializeObject<SprinklerProgram[]>(strprogs);
                if (progs == null)
                    return;
                foreach (var prg in progs)
                    SprinklerPrograms.Add(prg);
                //string strDeSer = "";
                //byte mByte;
                ////read up to find a '?'
                //for (long i = 0; i < mystream.Length; i++)
                //{
                //    mByte = (byte)mystream.ReadByte();
                //    if (((mByte == (byte)Param.ParamStart) && (i > 0)) || (i == (mystream.Length - 1)))
                //    {
                //        if (i == (mystream.Length - 1))
                //            strDeSer += new String((char)mByte, 1);
                //        ProcessProgram(null, strDeSer);
                //        strDeSer = "";
                //    }
                //    strDeSer += new String((char)mByte, 1);
                //}
                mystream.Dispose();

            }
            catch (Exception e)
            {
                if (mystream != null)
                    mystream.Dispose();
            }


        }

        static private void InitTimer()
        {
            myTimer = new Timer(ClockTimer_Tick, null, new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0));
        }
        static void ClockTimer_Tick(object sender)
        {
            DateTime now = DateTime.Now;
            //Debug.Print(now.ToString("MM/dd/yyyy HH:mm:ss"));
            // check the midnight prediction if automated mode
            if (WunderSettings.AutomateAll == true)
                if (now >= LastTimeCheck.Add(TimeCheck))
                {
                    GetForecast("");
                    if (bNeedToSprinkle)
                    {
                        try
                        {
                            if (TypicalProg != null)
                            {
                                for (int i = 0; i < TypicalProg.Length; i++)
                                {
                                    DateTimeOffset dtoff = DateTimeOffset.Now;
                                    if (TypicalProg[i].StartTime.Hours < dtoff.Hour)
                                        if (TypicalProg[i].StartTime.Minutes < dtoff.Minute)
                                        {
                                            dtoff = dtoff.AddDays(1);
                                        }
                                    dtoff = new DateTimeOffset(dtoff.Year, dtoff.Month, dtoff.Day, TypicalProg[i].StartTime.Hours, TypicalProg[i].StartTime.Minutes, TypicalProg[i].StartTime.Seconds, dtoff.Offset);
                                    SprinklerPrograms.Add(new SprinklerProgram(dtoff, TypicalProg[i].Duration, TypicalProg[i].SprinklerNumber));
                                    LogToAzure("Adding Program", SprinklerPrograms[SprinklerPrograms.Count - 1]);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                        bNeedToSprinkle = false;
                    }
                    LastTimeCheck = now.AddDays(1);
                }
            long initialtick = now.Ticks;
            long actualtick;
            for (int i = 0; i < SprinklerPrograms.Count; i++)
            {
                SprinklerProgram MySpr = (SprinklerProgram)SprinklerPrograms[i];
                actualtick = MySpr.DateTimeStart.Ticks;
                if (initialtick >= actualtick)
                { // this is the time to open a sprinkler
                    //Debug.Print("Sprinkling " + i + " date time " + now.ToString("MM/dd/yyyy HH:mm:ss"));
                    Sprinklers[MySpr.SprinklerNumber].Open = true;
                    // it will close all sprinkler in the desired time of sprinkling. Timer will be called only once.
                    //10000 ticks in 1 milisecond
                    Sprinklers[MySpr.SprinklerNumber].TimerInterval = (int)(MySpr.Duration.Ticks / 10000); //= new Timer(new TimerCallback(ClockStopSprinkler), null, (int)(MySpr.Duration.Ticks / 10000), 0);
                    //Sprinklers[MySpr.SprinklerNumber].TimerCallBack.Start();
                    // Save the data
                    SaveProgamActual(MySpr);
                    LogToAzure("sprinkling", MySpr);
                    SprinklerPrograms.RemoveAt(i);
                    return;
                }
            }
        }

        static private async Task SaveProgamActual(SprinklerProgram spr)
        {
            FileStream fileToWrite = null;
            try
            {
                fileToWrite = new FileStream(await GetFilePathAsync(strFileActualPrograms), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                var strSer = JsonConvert.SerializeObject(spr);
                byte[] buff = Encoding.UTF8.GetBytes(strSer);
                if (fileToWrite.Length == 0)
                { //create the first [] 
                    fileToWrite.WriteByte(91);
                    fileToWrite.WriteByte(93);
                }
                //fileToWrite.Seek(0, SeekOrigin.End);
                fileToWrite.Position = fileToWrite.Length-1;
                if (fileToWrite.Length >= 2) // need to write ,
                    fileToWrite.WriteByte(44);
                fileToWrite.Write(buff, 0, buff.Length);
                // closing the ]
                fileToWrite.WriteByte(93);
                fileToWrite.Dispose();
            }
            catch (Exception e)
            {

            }
        }

        static private async Task<SprinklerProgram[]> ReadProgamActual()
        {
            FileStream fileToRead = null;
            try
            {
                fileToRead = new FileStream(await GetFilePathAsync(strFileActualPrograms), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                long fileLength = fileToRead.Length;
                byte[] buf = new byte[fileLength];
                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                // convert the read into a string
                var strdata = new string(Encoding.UTF8.GetChars(buf));
                var ret = JsonConvert.DeserializeObject<SprinklerProgram[]>(strdata);
                return ret;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
