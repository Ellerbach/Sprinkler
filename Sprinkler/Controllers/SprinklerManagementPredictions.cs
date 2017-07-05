using CreativeGurus.Weather.Wunderground;
using CreativeGurus.Weather.Wunderground.Models;
using IoTCoreHelpers;
using Newtonsoft.Json;
using SprinklerRPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Controllers
{
    partial class SprinklerManagement
    {
        static private WundergroundSettings WunderSettings = new WundergroundSettings();
        static private bool bNeedToSprinkle;

        static private async Task InitPredictions()
        {
            FileStream fileToRead = null;
            try
            {
                fileToRead = new FileStream(await GetFilePathAsync(strFilePrediction), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long fileLength = fileToRead.Length;
                byte[] buf = new byte[fileLength];
                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                // convert the read into a string
                var strdata = new string(Encoding.UTF8.GetChars(buf));
                WunderSettings = JsonConvert.DeserializeObject<WundergroundSettings>(strdata);
                // check settings and make sure time formating is correct
                if ((WunderSettings.TimeToCheck == "") || !(WunderSettings.TimeToCheck.Contains(':')))
                    WunderSettings.TimeToCheck = "00:00";
                TimeCheck = new TimeSpan(Convert.ToInt32(WunderSettings.TimeToCheck.Substring(0, WunderSettings.TimeToCheck.IndexOf(':'))),
                    Convert.ToInt32(WunderSettings.TimeToCheck.Substring(WunderSettings.TimeToCheck.IndexOf(':')+1)),
                    0
                    );
            }
            catch (Exception e)
            {

            }
        }

        static private string GetForecast(string param)
        {

            List<Param> Params = Param.decryptParam(param);
            if (Params != null)
            {
                //Check if there is an automation setting
                if (Params.Where(m => m.Name.ToLower() == paramAutomateAll).Any())
                    WunderSettings.AutomateAll = Param.CheckConvertBool(Params, paramAutomateAll);
            }
            
            StringBuilder sb = new StringBuilder();
            try
            {
                WeatherClient client = new WeatherClient(WunderSettings.Key);
                sb.Append(BuildHeader());
                bNeedToSprinkle = false;
                for (int i = 0; i < WunderSettings.Stations.Length; i++)
                {
                    var forecast = client.GetForecast(QueryType.PWSId, new QueryOptions() { PWSId = WunderSettings.Stations[i] });
                    var history = client.GetHistory(QueryType.PWSId, new QueryOptions() { PWSId = WunderSettings.Stations[i], Date = DateTime.Now.AddDays(-1) });

                    sb.Append($"<p>Full forecast for this station: <a href=\"https://www.wunderground.com/cgi-bin/findweather/getForecast?query=pws:{WunderSettings.Stations[i]}\">{WunderSettings.Stations[i]}</a><br>");
                    //Console.WriteLine("Magic recommendation:");
                    sb.Append("Magic recommendation:<br>");

                    if (history.History.Dailysummary[0].PrecipitationMetric > WunderSettings.PrecipitationThresholdActuals)
                    {
                        //Console.Write($"It rained and more than 5mm ({history.History.DailySummaries[0].PrecipitationMetric}mm), no need to sprinkle.");
                        sb.Append($"It rained and more than {WunderSettings.PrecipitationThresholdActuals} mm ({history.History.Dailysummary[0].PrecipitationMetric}mm), <b>no need to sprinkle</b>.<br>");
                    }
                    else if (forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm > WunderSettings.PrecipitationThresholdForecast)
                    {
                        //Console.WriteLine($"Forecast is for rain, more than 3mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm}mm), checking the confidence index.");
                        sb.Append($"Forecast is for rain, more than {WunderSettings.PrecipitationThresholdForecast} mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm} mm), checking the confidence index.<br>");
                        if (forecast.Forecast.SimpleForecast.ForecastDay[0].Pop > WunderSettings.PrecipitationPercentForecast)
                        {
                            //Console.WriteLine($"Considence index if more than 60% ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop}%), so no need to sprinkle.");
                            sb.Append($"Considence index is more than {WunderSettings.PrecipitationPercentForecast} % ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop} %), so <b>no need to sprinkle</b>.<br>");
                        }
                        else
                        {
                            bNeedToSprinkle = true;
                            sb.Append($"Considence index is less than {WunderSettings.PrecipitationPercentForecast} % ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop} %), so yes, <b>plan to sprinkle!</b><br>");
                        }
                    }
                    else
                    {
                        bNeedToSprinkle = true;
                        sb.Append($"Forecast is for rain, but less than {WunderSettings.PrecipitationThresholdActuals} mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm} mm), yes, <b>plan to sprinkle!</b><br>");
                    }
                    if (bNeedToSprinkle)
                    {
                        //Console.WriteLine("I will use the typical programs. Please adjust manually if needed.<br>");
                        sb.Append($"I will use the typical programs. Please adjust manually if needed.<br>");
                    }
                    sb.Append("<br>Forecast:<br><table><tr><th>Date</th><th>Rain</th><th>Chances</th><th></th><th>Conditions</th><tr>");
                    //Console.WriteLine("Forecast");
                    foreach (var itm in forecast.Forecast.SimpleForecast.ForecastDay)
                    {
                        //Console.WriteLine($"{itm.Date.Pretty} rain: {itm.Qpf_AllDay.Mm}, chances: {itm.Pop}%");
                        sb.Append($"<tr><td>{itm.Date.Pretty}</td><td>{itm.Qpf_AllDay.Mm} mm</td><td>{itm.Pop}%</td><td><img src=\"{itm.Icon_Url}\"></td><td>{itm.Conditions}</td></tr>");
                    }
                    sb.Append("</table><br>");
                    //Console.WriteLine("Observation");
                    sb.Append("Observations:");
                    sb.Append("<br><table><tr><th>Date</th><th>Rain</th><th>Humidity</th><th>Pressure</th><th>Temperature</th><th>Wind Direction</th><tr>");
                    sb.Append($"<tr><td>{history.History.Dailysummary[0].Date.Pretty}</td><td>{history.History.Dailysummary[0].PrecipitationMetric} mm</td><td>{history.History.Dailysummary[0].Humidity} %</td><td>{history.History.Dailysummary[0].MeanPressureMetric}</td><td>{history.History.Dailysummary[0].MeanTempMetric} °C</td><td>{history.History.Dailysummary[0].MeanWindSpeedMetric}</td></tr>");
                    foreach (var itm in history.History.Observations)
                    {
                        //Console.WriteLine($"{itm.Date.Hour}:{itm.Date.Min} rain: {itm.PrecipitationTotalMetric}");
                        sb.Append($"<tr><td>{itm.Date.Pretty}</td><td>{itm.PrecipitationTotalMetric} mm</td><td>{itm.Humidity} %</td><td>{itm.PressureMetric}</td><td>{itm.TempCelcius} °C</td><td>{itm.WindDirection}</td></tr>");
                    }
                    sb.Append("</table><br></p>");
                    //Console.WriteLine($"Total day rain: {history.History.DailySummaries[0].PrecipitationMetric}");
                    //call the sendmail
                }

            }
            catch (Exception ex)
            {
                sb.Append($"<p>ERROR:<br>{ex.Message}");
            }
            sb.Append("<p><a href='/typic.aspx" + Param.ParamStart + securityKey + Param.ParamSeparator + paramClk
                        + Param.ParamEqual + "1'>Create typical program</a><br>");
            sb.Append("<a href='/" + paramPageSprinkler + Param.ParamStart + securityKey + "'>Return to main page</a>");
            sb.Append("</p></BODY></HTML>");
            return sb.ToString();
        }


    }
}
