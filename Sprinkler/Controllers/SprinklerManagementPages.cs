using IoTCoreHelpers;
using Newtonsoft.Json;
using SprinklerRPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace SprinklerRPI.Controllers
{
    partial class SprinklerManagement
    {
        private string BuildHeader(bool withsec = true)
        {
            string strResp = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
            strResp += "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Gestion arrosage</title>";
            if (withsec)
                strResp += "<link href=\"/file/spr.css?" + securityKey + "\" rel=\"stylesheet\" type=\"text/css\" />";
            strResp += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/></head>";
            strResp += "<BODY><h1>RaspberryPi2 sprinkler running Windows 10</h1><p>";
            return strResp;
        }

        private string ProcessProgram(string param)
        {
            string strResp = "";
            try
            {
                // decode params
                // params must be sprX=0; or 1 where X is a number from 0 to 2
                int intYear = -1;
                int intMonth = -1;
                int intDay = -1;
                int intHour = -1;
                int intMinute = -1;
                int intDuration = -1;
                int intSprinklerNumber = -1;
                bool bnoUI = false;
                DateTime MyDate;
                TimeSpan MySpanDuration;
                List<Param> Params = Param.decryptParam(param);

                // decrypt all params in a readeable sentense
                if (Params != null)
                {
                    bnoUI = Param.CheckConvertBool(Params, paramNoUI);
                    intYear = Param.CheckConvertInt32(Params, paramYear);
                    intMonth = Param.CheckConvertInt32(Params, paramMonth);
                    intDay = Param.CheckConvertInt32(Params, paramDay);
                    intHour = Param.CheckConvertInt32(Params, paramHour);
                    intMinute = Param.CheckConvertInt32(Params, paramMinute);
                    intDuration = Param.CheckConvertInt32(Params, paramDuration);
                    intSprinklerNumber = Param.CheckConvertInt32(Params, paramSpr);
                }
                // valid all params are in normal limits and in the future
                if (!bnoUI)
                    strResp = BuildHeader(true);

                DateTime tmpNow = DateTime.Now;
                //case delete a program
                if (intDuration == 0)
                {
                    for (int i = 0; i < SprinklerPrograms.Count; i++)
                    {
                        // case the date already exist and durqtion is 0 => delete 
                        if ((intYear == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Year)
                            && (intMonth == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Month)
                            && (intDay == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Day)
                            && (intSprinklerNumber == ((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber))
                        {

                            SprinklerPrograms.RemoveAt(i);
                            if (!bnoUI)
                            {

                                strResp += "Deleting Sprinkler " + Sprinklers[intSprinklerNumber].Name + " for " + intYear + " " + intMonth + " " + intDay + ". <br>";
                                strResp += "<a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Back to main page</a>";
                                //strResp = await OutPutStream(response, strResp);
                            }
                            else
                            {
                                strResp += paramOK;
                                // end if it is deleted and no ui 
                                return strResp;
                            }
                        }
                    }
                    // if we are there there was anything to update so return a problem
                    if (bnoUI)
                    {
                        strResp = paramProblem;
                        // end if it is deleted and no ui 
                        return strResp;
                    }

                }
                else if ((intYear > 1900) && (intMonth > 0) && (intMonth < 13) && (intHour >= 0) && (intHour < 24) && (intMinute >= 0) && (intMinute < 60))
                {
                    MyDate = new DateTime(intYear, intMonth, intDay, intHour, intMinute, 0);
                    bool TodayIsToday = false;
                    if ((intYear == tmpNow.Year) && (intMonth == tmpNow.Month) && (intDay == tmpNow.Day))
                        TodayIsToday = true;
                    // Is the program in the future or today!
                    if ((MyDate >= tmpNow) || (TodayIsToday))
                    {
                        //display the possibility to setup another Sprinkler
                        for (int i = 0; i < NUMBER_SPRINKLERS; i++)
                        {
                            //TO DO: display something here
                        }
                        bool updated = false;
                        // is the duration the right one? with an existing sprinkler?
                        if ((intDuration > 0) && (intDuration < 1440) && (intSprinklerNumber >= 0) && (intSprinklerNumber < NUMBER_SPRINKLERS))
                        {
                            MySpanDuration = new TimeSpan(0, intDuration, 0);

                            // is it a new program for a day a just an update (only 1 program per day available)

                            for (int i = 0; i < SprinklerPrograms.Count; i++)
                            {
                                // case the date already exist => update the hour, minute and duration for the given Sprinkler
                                if ((MyDate.Year == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Year)
                                    && (MyDate.Month == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Month)
                                    && (MyDate.Day == ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Day)
                                    && (intSprinklerNumber == ((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber)
                                    && (updated == false))
                                {
                                    ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart = MyDate;
                                    ((SprinklerProgram)SprinklerPrograms[i]).Duration = MySpanDuration;
                                    updated = true;
                                    if (!bnoUI)
                                    {
                                        strResp += "Updating Sprinkler " + Sprinklers[intSprinklerNumber].Name + " for " + MyDate.ToString("yyyy MMM d") + " to start at " + MyDate.ToString("HH:mm") + " and duration of " + MySpanDuration.Minutes + " minutes. <br>";
                                        //strResp = await OutPutStream(response, strResp);
                                    }
                                    else
                                    {   //if it was an update, then everything is fine
                                        strResp += paramOK;
                                        // end if it is deleted and no ui 
                                        return strResp;
                                    }
                                }
                            }
                            // does not exist, then will need to create it
                            if (updated == false)
                            {
                                SprinklerPrograms.Add(new SprinklerProgram(MyDate, MySpanDuration, intSprinklerNumber));
                                if (!bnoUI)
                                {
                                    strResp += "Adding Sprinkler " + Sprinklers[intSprinklerNumber].Name + " for " + MyDate.ToString("yyyy MMM d") + " to start at " + MyDate.ToString("HH:mm") + " and duration of " + MySpanDuration.Minutes + " minutes. <br>";
                                    updated = true;
                                    //strResp = await OutPutStream(response, strResp);
                                }
                                else
                                {
                                    strResp += paramOK;
                                    // end if it is deleted and no ui 
                                    return strResp;
                                }
                            }


                        }


                        if (updated == false)
                        {
                            if (bnoUI)
                            {
                                // we have a problem if we are here if we don't display UI
                                strResp += paramProblem;
                                // end if it is deleted and no ui 
                                return strResp;
                            }
                            //create a timeline to select hour and minutes
                            strResp += "<br>Select your starting time.<br>";
                            strResp += "<table BORDER=\"0\">";
                            //in case it's Today, allow programation for the next hour
                            int StartTime = 0;
                            if (TodayIsToday)
                                StartTime = intHour + 1;
                            //strResp = await OutPutStream(response, strResp);
                            for (int i = StartTime; i < 24; i++)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    strResp += "<tr><td>";
                                    DateTime tmpDateTime = new DateTime(intYear, intMonth, intDay, i, j * 30, 0);
                                    strResp += tmpDateTime.ToString("HH:mm");
                                    strResp += "</td><td>";
                                    strResp += "<a href='" + paramPageProgram + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear
                                        + Param.ParamEqual + tmpDateTime.Year + Param.ParamSeparator + paramMonth + Param.ParamEqual + tmpDateTime.Month
                                        + Param.ParamSeparator + paramDay + Param.ParamEqual + tmpDateTime.Day + Param.ParamSeparator + paramHour
                                        + Param.ParamEqual + i + Param.ParamSeparator + paramMinute + Param.ParamEqual + j * 30 + Param.ParamSeparator
                                        + paramDuration + Param.ParamEqual + SprDuration + Param.ParamSeparator + paramSpr + Param.ParamEqual
                                        + intSprinklerNumber + "'>" + SprDuration + " minutes</a>";
                                    strResp += "</td>";
                                    //if (strResp.Length > 800)
                                    //{
                                    //    strResp = await OutPutStream(response, strResp);
                                    //}
                                    strResp += "</tr>";
                                }
                            }
                            strResp += "</table>";
                        }
                        else
                        {
                            // something has been updated so redirect to the main page
                            strResp += "<a href='/" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + intMonth + Param.ParamSeparator + paramSpr
                                + Param.ParamEqual + intSprinklerNumber + "'>Program this month</a>";
                            //strResp = await OutPutStream(response, strResp);
                        }
                    }
                    else
                    {
                        if (bnoUI)
                        {
                            strResp += paramProblem;
                            // end if it is deleted and no ui 
                            return strResp;
                        }
                        strResp += "Date must be in the future";
                    }
                }
                else
                {
                    if (bnoUI)
                    {
                        strResp += paramProblem;
                        // end if it is deleted and no ui 
                        return strResp;
                    }
                    strResp += "Date must be in the future";
                }

                // if no UI we should not be there!
                strResp += "<br><a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Back to main page<a><br>";
                strResp += "</BODY></HTML>";
            }
            catch (Exception e)
            {

                // do nothing
            }
            return strResp;
        }

        private string ProcessListProgram(string param)
        {
            string strResp = "";
            try
            {
                // decode params
                // params must be sprX=0; or 1 where X is a number from 0 to 2
                int intYear = -1;
                int intMonth = -1;
                int intSprinklerNumber = -1;
                bool bnoUI = false;
                List<Param> Params = Param.decryptParam(param);

                //response.RedirectLocation = "/Calendar.aspx?Year=" + intYear + ";Month=" + intMonth;

                // decrypt all params in a readeable sentense
                if (Params != null)
                {
                    intYear = Param.CheckConvertInt32(Params, paramYear);
                    intMonth = Param.CheckConvertInt32(Params, paramMonth);
                    intSprinklerNumber = Param.CheckConvertInt32(Params, paramSpr);
                    bnoUI = Param.CheckConvertBool(Params, paramNoUI);
                }

                if (!bnoUI)
                {
                    strResp = BuildHeader();
                    //strResp = await OutPutStream(response, strResp);

                    // is SPR a real number?
                    if ((intSprinklerNumber >= 0) && (intSprinklerNumber < NUMBER_SPRINKLERS))
                    {
                        strResp += "List of programs for Spinkler " + Sprinklers[intSprinklerNumber].Name + ".<br>";
                        for (int i = 0; i < SprinklerPrograms.Count; i++)
                        {
                            if (((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber == intSprinklerNumber)
                            {
                                strResp += "Next program date " + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.ToString("yyyy MM d") + " to start at "
                                    + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.ToString("HH:mm") + " and duration of "
                                    + ((SprinklerProgram)SprinklerPrograms[i]).Duration.Minutes + " minutes. <a href='" + paramPageProgram + Param.ParamStart
                                    + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                    + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Year + Param.ParamSeparator + paramMonth + Param.ParamEqual
                                    + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Month + Param.ParamSeparator + paramDay + Param.ParamEqual
                                    + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Day + Param.ParamSeparator + paramDuration + Param.ParamEqual
                                    + "0" + Param.ParamSeparator + paramSpr + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber
                                    + "'>Delete</a><br>";
                                //strResp = await OutPutStream(response, strResp);
                            }
                        }

                    } // do we need to display all sprinklers?
                    else if (intSprinklerNumber == int.MaxValue)
                    {
                        strResp += "List of programs for all Sprinklers.<br>";
                        for (int i = 0; i < SprinklerPrograms.Count; i++)
                        {
                            strResp += "Next program date " + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.ToString("yyyy MM d") + " to start at "
                                + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.ToString("HH:mm") + " and duration of "
                                + ((SprinklerProgram)SprinklerPrograms[i]).Duration.Minutes + " minutes for Sprinkler "
                                + Sprinklers[((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber].Name + ". <a href='" + paramPageProgram + Param.ParamStart
                                + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Year
                                + Param.ParamSeparator + paramMonth + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Month
                                + Param.ParamSeparator + paramDay + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Day
                                + Param.ParamSeparator + paramDuration + Param.ParamEqual + "0" + Param.ParamSeparator + paramSpr + Param.ParamEqual
                                + ((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber + "'>Delete</a><br>";
                            //strResp = await OutPutStream(response, strResp);
                        }
                    }
                    strResp += "<br><a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Back to main page</a>";
                    strResp += "</BODY></HTML>";
                }
                else
                {
                    //strResp = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n";
                    strResp += JsonConvert.SerializeObject(SprinklerPrograms);

                }
            }
            catch (Exception e)
            {


            }
            return strResp;
        }

        private string ProcessCalendar(string param)
        {
            string strResp = BuildHeader();
            try
            {
                // decode params
                int intMonth = -1;
                int intYear = -1;
                int intSprinkler = -1;
                List<Param> Params = Param.decryptParam(param);
                if (Params != null)
                {
                    intMonth = Param.CheckConvertInt32(Params, paramMonth);
                    intYear = Param.CheckConvertInt32(Params, paramYear);
                    intSprinkler = Param.CheckConvertInt32(Params, paramSpr);
                }
                if ((intMonth > 0) && (intMonth < 13) && (intYear > 2009) && (intYear < 2200))
                {
                    //are we in the future?
                    DateTime tmpDT = new DateTime(intYear, intMonth, 1);
                    DateTime tmpNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    if (tmpDT >= tmpNow)
                    {

                        for (int i = 0; i < NUMBER_SPRINKLERS; i++)
                            if (i != intSprinkler)
                                strResp += "Calendar for <a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator
                                    + paramYear + Param.ParamEqual + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + intMonth
                                    + Param.ParamSeparator + paramSpr + Param.ParamEqual + i + "'>sprinkler " + Sprinklers[i].Name + "</a><br>";
                        //strResp = await OutPutStream(response, strResp);
                        strResp += "Month: " + intMonth + "<br>";
                        strResp += "Year: " + intYear + "<br>";
                        // Display some previous and next.
                        // is it the first month? (case 1rst of January of the year to program but in the future year so month =12 and 1 less year)
                        if ((intMonth == 1) && (intYear > DateTime.Now.Year))
                            strResp += "<a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + (intYear - 1) + Param.ParamSeparator + paramMonth + Param.ParamEqual + "12" + Param.ParamSeparator + paramSpr
                                + Param.ParamEqual + intSprinkler + "'>Previous month</a>&nbsp&nbsp&nbsp";
                        else if ((intMonth > DateTime.Now.Month) && (intYear == DateTime.Now.Year)) // (other cases
                            strResp += "<a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + (intMonth - 1) + Param.ParamSeparator
                                + paramSpr + Param.ParamEqual + intSprinkler + "'>Previous month</a>&nbsp&nbsp&nbsp";
                        else if (intYear > DateTime.Now.Year)
                            strResp += "<a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + (intMonth - 1) + Param.ParamSeparator
                                + paramSpr + Param.ParamEqual + intSprinkler + "'>Previous month</a>&nbsp&nbsp&nbsp";
                        // next month //case december
                        //strResp = await OutPutStream(response, strResp);
                        if (intMonth == 12)
                            strResp += "<a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + (intYear + 1) + Param.ParamSeparator + paramMonth + Param.ParamEqual + "1" + Param.ParamSeparator + paramSpr +
                                Param.ParamEqual + intSprinkler + "'>Next month</a>";
                        else // (other cases
                            strResp += "<a href='" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear + Param.ParamEqual
                                + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + (intMonth + 1) + Param.ParamSeparator + paramSpr +
                                Param.ParamEqual + intSprinkler + "'>Next month</a>";
                        // display and build a calendar :)
                        strResp += "<p>";
                        strResp += "<table BORDER=\"0\"><tr>";
                        for (int i = 0; i < Helpers.DayOfWeek.Days.Length; i++)
                            strResp += "<td>" + Helpers.DayOfWeek.Days[i] + "</td>";
                        strResp += "</tr><tr>";
                        int NbDays = Helpers.DayOfWeek.NumberDaysPerMonth(intMonth, intYear);
                        DateTime dt = new DateTime(intYear, intMonth, 1);
                        for (int i = 0; i < (int)dt.DayOfWeek; i++)
                            strResp += "<td></td>";
                        //strResp = await OutPutStream(response, strResp);
                        for (int i = 1; i <= NbDays; i++)
                        {
                            if ((intMonth == DateTime.Now.Month) && (intYear == DateTime.Now.Year) && (i < DateTime.Now.Day))
                            { // don't add a link to program a past day
                                strResp += "<td>" + i + "</td>";
                            }
                            else
                            {
                                strResp += "<td><a href='" + paramPageProgram + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear
                                    + Param.ParamEqual + intYear + Param.ParamSeparator + paramMonth + Param.ParamEqual + intMonth + Param.ParamSeparator
                                    + paramDay + Param.ParamEqual + i + Param.ParamSeparator + paramHour + Param.ParamEqual + DateTime.Now.Hour
                                    + Param.ParamSeparator + paramMinute + Param.ParamEqual + DateTime.Now.Minute + Param.ParamSeparator + paramSpr
                                    + Param.ParamEqual + intSprinkler + "'>" + i + "</a></td>";
                            }
                            if ((i + (int)dt.DayOfWeek) % 7 == 0)
                                strResp += "</tr><tr>";
                            //if (strResp.Length > 800)
                            //{
                            //    strResp = await OutPutStream(response, strResp);
                            //}
                        }
                        strResp += "</tr></table>";
                    }
                    else
                    {
                        //TODO: looks like a section to fix!
                        strResp += "Not in the future, please select a valid month and year, <a href='" + paramPageCalendar + Param.ParamStart
                            + securityKey + Param.ParamSeparator + "Year=" + DateTime.Now.Year + ";Month=" + DateTime.Now.Month + ";Spr="
                            + intSprinkler + "'>click here</a> to go to the actual month";
                    }
                }
                strResp += "<br><a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Back to main page<a><br>";
                strResp += "</BODY></HTML>";
            }
            catch (Exception e)
            {

            }
            return strResp;
        }

        private string ProcessSprinkler(string param)
        {
            string strResp = "";
            try
            {
                // decode params
                // params must be sprX=0; or 1 where X is a number from 0 to 2
                bool bnoUI = false;
                List<Param> Params = Param.decryptParam(param);
                if (Params != null)
                {
                    //on cherche le paramètre spr
                    bnoUI = Param.CheckConvertBool(Params, paramNoUI);



                    int j = param.ToLower().IndexOf(paramSpr);
                    if (j >= 0)
                    {
                        //vérifie si l'index est 0, 1 ou 2
                        //assumes number of sprinkler is less than 10
                        int k = Convert.ToInt32(param.Substring(j + paramSpr.Length, 1));
                        bool bresult = false;
                        bresult = Param.CheckConvertBool(Params, (paramSpr + k));
                        // if open has been forced, then put the manual value at open. it will prevent to close early
                        Sprinklers[k].Open = bresult;
                    }
                }
                if (!bnoUI)
                    strResp = BuildHeader(true);
                if (!bnoUI)
                {
                    for (int i = 0; i < NUMBER_SPRINKLERS; i++)
                    {
                        int toopen = 0;
                        if (!Sprinklers[i].Open)
                            toopen = 1;
                        strResp += "Springler " + Sprinklers[i].Name + ": <a href='/" + paramPageSprinkler + Param.ParamStart + securityKey
                            + Param.ParamSeparator + paramSpr + i + Param.ParamEqual + toopen + "'>" + Sprinklers[i].Open + "</a><br>";
                        strResp += "<a href='/" + paramPageCalendar + Param.ParamStart + securityKey + Param.ParamSeparator + paramYear
                            + Param.ParamEqual + DateTime.Now.Year + Param.ParamSeparator + paramMonth + Param.ParamEqual + DateTime.Now.Month
                            + Param.ParamSeparator + paramSpr + Param.ParamEqual + i + "'>Program Sprinkler " + Sprinklers[i].Name + "</a><br>";
                        strResp += "<a href='/" + paramPageListPrgm + Param.ParamStart + securityKey + Param.ParamSeparator + paramSpr
                            + Param.ParamEqual + i + "'>List all programs for Sprinkler " + Sprinklers[i].Name + "</a><br>";
                        //if (Sprinklers[i].HumiditySensor != null)
                        //{
                        //    strResp += "Humidity: " + Sprinklers[i].HumiditySensor.Humidity;
                        //    if (Sprinklers[i].HumiditySensor.IsHumid)
                        //        strResp += " and it is humid<br>";
                        //    else
                        //        strResp += " and it is time to sprinkle!<br>";
                        //}
                        //strResp = await OutPutStream(response, strResp);
                    }
                    strResp += "<p><a href='/typic.aspx" + Param.ParamStart + securityKey + Param.ParamSeparator + paramClk
                        + Param.ParamEqual + "1'>Create typical program</a><br>";
                    strResp += "<p><a href='/" + paramPageUtil + Param.ParamStart + securityKey + Param.ParamSeparator + paramClk
                        + Param.ParamEqual + "1'>Update date and time</a><br>";
                    strResp += "<a href='/" + paramPageUtil + Param.ParamStart + securityKey + Param.ParamSeparator + paramSave
                        + Param.ParamEqual + "1'>Save all programs</a><br></p>";
                    strResp += "<p><a href='/" + paramPageUtil + Param.ParamStart + securityKey + Param.ParamSeparator + paramReboot
                        + Param.ParamEqual + "1'>Reboot</a><br></p>";
                    //strResp += "<p><a href='/" +  pageUtil + ParamStart +  save+ ParamEqual + "1'>Save</a><br>";
                    strResp += DateTime.Now.ToString();
                    strResp += "</BODY></HTML>";
                    //strResp = await OutPutStream(response, strResp);
                }
                else
                {
                    strResp += paramOK;
                    //strResp = await OutPutStream(response, strResp);
                }
            }
            catch (Exception e)
            {

            }
            return strResp;

        }

        private string ProcessTypical(string param)
        {
            string strResp = "";

            strResp = BuildHeader();

            strResp += "Programming typical sprinkling<br>";
            try
            {
                if (TypicalProg != null)
                {
                    for (int i = 0; i < TypicalProg.Length; i++)
                    {
                        DateTimeOffset dtoff = DateTimeOffset.Now;

                        if (TimeSpan.Compare(TypicalProg[i].StartTime, dtoff.TimeOfDay) < 0)
                        {
                            dtoff = dtoff.AddDays(1);
                        }
                        dtoff = new DateTimeOffset(dtoff.Year, dtoff.Month, dtoff.Day, TypicalProg[i].StartTime.Hours, TypicalProg[i].StartTime.Minutes, TypicalProg[i].StartTime.Seconds, dtoff.Offset);
                        
                        SprinklerPrograms.Add(new SprinklerProgram(dtoff, TypicalProg[i].Duration, TypicalProg[i].SprinklerNumber));
                        strResp += $"Adding program on Sprinkler {TypicalProg[i].SprinklerNumber}, at {dtoff} for {TypicalProg[i].Duration}<br>";
                    }
                }
                else
                    strResp += "Sorry, there is no typical program setup";
            }
            catch (Exception e)
            {
                strResp +=$"Ups, something went wrong! {e.Message}";
            }
            
            strResp += "<a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Return to main page</a>";
            strResp += "</BODY></HTML>";

            return strResp;

        }

        private string ProcessUtil(string param)
        {
            string strResp = "";
            try
            {
                // decode params
                bool bClock = false;
                bool bReboot = false;
                bool bSave = false;
                List<Param> Params = Param.decryptParam(param);
                if (Params != null)
                {
                    bClock = Param.CheckConvertBool(Params, paramClk);
                    bReboot = Param.CheckConvertBool(Params, paramReboot);
                    bSave = Param.CheckConvertBool(Params, paramSave);
                }
                strResp = BuildHeader();
                //strResp = await OutPutStream(response, strResp);

                //if we need to reboot, then reboot :-)
                if (bReboot)
                {
                    strResp += "Please allow time for reboot...<br>";
                    strResp += "<a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Return to main page</a>";
                    strResp += "</BODY></HTML>";
                    ShutdownManager.BeginShutdown(ShutdownKind.Restart, new TimeSpan(0, 0, 10));
                    //strResp = await OutPutStream(response, strResp);
                    //Thread.Sleep(1000);
                    //PowerState.RebootDevice(false);
                    //nothing happen here :-)
                }
                if (bClock)
                {
                    //SetInternalTime();
                    strResp += "New date and time: " + DateTime.Now.ToString("yyyy MM d HH:mm");
                }
                if (bSave)
                {
                    Task<bool> t = SavePrograms();
                    while (!t.IsCompleted)
                        ;
                    if (t.Result)
                    {
                        strResp += "Number of programs saved: " + SprinklerPrograms.Count + "<br>";
                        strResp += "<a href='" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Back to main page</a>";
                    }
                    else
                    {
                        strResp += "Error saving file :-(";
                    }

                }
                strResp += "</BODY></HTML>";
            }
            catch (Exception e)
            {

            }
            return strResp;
        }
        private string ProcessSprinklerDetails(string param)
        {
            string strResp = "";
            strResp += JsonConvert.SerializeObject(Sprinklers);
            return strResp;
        }

        public static async Task<bool> SavePrograms()
        {
            try
            {
                //clear the previous list of program
                //create the serialized version of the params
                string strSer = "";
                //for (int i = 0; i < SprinklerPrograms.Count; i++)
                //{
                //    strSer += Param.ParamStart + paramYear + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Year + Param.ParamSeparator;
                //    strSer += paramMonth + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Month + Param.ParamSeparator;
                //    strSer += paramDay + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Day + Param.ParamSeparator;
                //    strSer += paramHour + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Hour + Param.ParamSeparator;
                //    strSer += paramMinute + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).DateTimeStart.Minute + Param.ParamSeparator;
                //    strSer += paramDuration + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).Duration.Minutes + Param.ParamSeparator;
                //    strSer += paramSpr + Param.ParamEqual + ((SprinklerProgram)SprinklerPrograms[i]).SprinklerNumber;
                //    //saved the serialized info into the file
                //    // LogToFile.Print(strDefaultDir + "\\" + strFileListProgram, strSer);
                //}               
                strSer = JsonConvert.SerializeObject(SprinklerPrograms);
                FileStream fileToWrite = new FileStream(await GetFilePathAsync(strFileListProgram), FileMode.OpenOrCreate, FileAccess.Write);
                byte[] buff = Encoding.UTF8.GetBytes(strSer);
                //fileToWrite.Write(fileToWrite.Length, 0);
                fileToWrite.Write(buff, 0, buff.Length);
                fileToWrite.Dispose(); //.Close();
                                       //Debug.GC(true);

            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        async static Task<string> GetFilePathAsync(string filename)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                //var files = await localFolder.GetFilesAsync();
                //StorageFile file = files.FirstOrDefault(x => x.Name == filename);
                var file = await localFolder.GetFileAsync(filename);
                if (file != null)
                    return file.Path;

            }
            catch (Exception)
            {
            }
            return "";
        }
    }
}
