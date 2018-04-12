using CreativeGurus.Weather.Wunderground;
using CreativeGurus.Weather.Wunderground.Models;
using DarkSkyApi;
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
        static private ForecastIOSettings ForecastSettings = new ForecastIOSettings();
        static private FuzzySprinkler[] Fuzzy = null;

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
                fileToRead.Close();
                fileToRead.Dispose();

                // convert the read into a string
                var strdata = new string(Encoding.UTF8.GetChars(buf));
                WunderSettings = JsonConvert.DeserializeObject<WundergroundSettings>(strdata);
                // check settings and make sure time formating is correct
                if ((WunderSettings.TimeToCheck == "") || !(WunderSettings.TimeToCheck.Contains(':')))
                    WunderSettings.TimeToCheck = "00:00";
                TimeCheck = new TimeSpan(Convert.ToInt32(WunderSettings.TimeToCheck.Substring(0, WunderSettings.TimeToCheck.IndexOf(':'))),
                    Convert.ToInt32(WunderSettings.TimeToCheck.Substring(WunderSettings.TimeToCheck.IndexOf(':') + 1)),
                    0
                    );
                WunderSettings.NeedToSprinkle = false;
                if (WunderSettings.MinTemp <= 0)
                    WunderSettings.MinTemp = 15;
                if (WunderSettings.MaxTemp <= 0)
                    WunderSettings.MaxTemp = 30;
                WunderSettings.PercentageCorrection = 1;

                //setup the forecast.IO source as well
                fileToRead = new FileStream(await GetFilePathAsync(strFileForecast), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileLength = fileToRead.Length;
                buf = new byte[fileLength];
                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                fileToRead.Close();
                fileToRead.Dispose();
                // convert the read into a string
                strdata = new string(Encoding.UTF8.GetChars(buf));
                ForecastSettings = JsonConvert.DeserializeObject<ForecastIOSettings>(strdata);

            }
            catch (Exception e)
            {

            }

        }

        static private async Task InitFuzzyLogic()
        {
            FileStream fileToRead = null;
            try
            {
                fileToRead = new FileStream(await GetFilePathAsync(strFileFuzzy), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long fileLenght = fileToRead.Length;
                byte[] buf = new byte[fileLenght];
                fileToRead.Read(buf, 0, (int)fileLenght);
                var strdata = new string(Encoding.UTF8.GetChars(buf));
                Fuzzy = JsonConvert.DeserializeObject<FuzzySprinkler[]>(strdata);
            }
            catch (Exception e)
            {

                throw;
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
                // Get the forecast
                var client = new DarkSkyService(ForecastSettings.ApiKey);
                var tsk = client.GetWeatherDataAsync(ForecastSettings.Latitude, ForecastSettings.Longitude, ForecastSettings.Unit, ForecastSettings.Language);
                tsk.Wait();
                var forecast = tsk.Result;
                // Get Forcast max temperature for the next 24h and same for precipitations
                float ForecastMaxTemp = 0;
                float ForecastTotalPrecipitation = 0;
                float ForecastProbabilityPrecipitation = 0;
                if (forecast.Daily.Days != null)
                    if (forecast.Daily.Days.Count > 0)
                    {
                        if (forecast.Daily.Days[0].HighTemperature >= ForecastMaxTemp)
                            ForecastMaxTemp = forecast.Daily.Days[0].HighTemperature;
                        if ((forecast.Daily.Days[0].PrecipitationIntensity * 24) >= ForecastTotalPrecipitation)
                            ForecastTotalPrecipitation = forecast.Daily.Days[0].PrecipitationIntensity * 24;
                        if (forecast.Daily.Days[0].PrecipitationProbability >= ForecastProbabilityPrecipitation)
                            ForecastProbabilityPrecipitation = forecast.Daily.Days[0].PrecipitationProbability * 100;

                    }
                // Get historical temperature of the day before
                tsk = client.GetTimeMachineWeatherAsync(ForecastSettings.Latitude, ForecastSettings.Longitude, DateTime.Now.AddDays(-1), ForecastSettings.Unit, ForecastSettings.Language);
                tsk.Wait();
                var history = tsk.Result;
                // find the al up precipitation and max temperature
                float HistMaxTemp = 0;
                float HistTotalPrecipitation = 0;
                if (history.Daily.Days != null)
                    if (history.Daily.Days.Count > 0)
                    {
                        if (history.Daily.Days[0].HighTemperature >= HistMaxTemp)
                            HistMaxTemp = history.Daily.Days[0].HighTemperature;
                        if (history.Daily.Days[0].PrecipitationAccumulation >= HistTotalPrecipitation)
                            HistTotalPrecipitation = history.Daily.Days[0].PrecipitationAccumulation;
                    }

                // Creating the header 
                sb.Append(BuildHeader());
                sb.Append("<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=iso-8859-1\"><meta name=\"Generator\" content=\"Meteo forecast\"></head><body>");
                sb.Append("Meteo forecast for <b>" + ForecastSettings.City + "</b></br>" + forecast.Daily.Summary + "<img src=\"https://www.ellerbach.net/public/meteo/" + forecast.Daily.Icon + ".png\"><br>");

                WunderSettings.NeedToSprinkle = false;
                // Do all the math without fuzzy logic
                if (Fuzzy == null)
                {
                    // Did it rained anough?
                    if (HistTotalPrecipitation > WunderSettings.PrecipitationThresholdActuals)
                    {
                        sb.Append($"<b>No need to sprinkle</b>: yesterday rain was {HistTotalPrecipitation} mm more than the threshold {WunderSettings.PrecipitationThresholdActuals} mm.<br>");
                        WunderSettings.NeedToSprinkle = false;
                    }
                    //Is it warm enough?
                    else if (ForecastMaxTemp > WunderSettings.MinTemp)
                    {
                        WunderSettings.PercentageCorrection = (float)(((WunderSettings.PrecipitationThresholdActuals - ForecastTotalPrecipitation) / WunderSettings.PrecipitationThresholdActuals));
                        //Will it rain?
                        if (ForecastTotalPrecipitation > WunderSettings.PrecipitationThresholdForecast)
                        {
                            var introstr = $"Forecast is for rain with {ForecastTotalPrecipitation} mm, more than {WunderSettings.PrecipitationThresholdForecast} mm.<br>";
                            //Enough rain?
                            if (ForecastProbabilityPrecipitation > WunderSettings.PrecipitationPercentForecast)
                            {
                                sb.Append("<b>No need to sprinkle</b><ol>");
                                sb.Append($"<li>{introstr}</li>");
                                sb.Append($"<li>Considence index is {ForecastTotalPrecipitation} % more than the threshold {WunderSettings.PrecipitationPercentForecast} %.</ol><br>");
                                WunderSettings.NeedToSprinkle = false;
                            }
                            //Not enough rain, so need to sprinkler
                            else
                            {
                                sb.Append($"<b>I plan to sprinkle</b>:<ol>");
                                sb.Append($"<li>Confidence index is {ForecastTotalPrecipitation} % less than threshold {WunderSettings.PrecipitationPercentForecast} %</li>");
                                sb.Append($"<li>sprinkling will be adjusted by {(WunderSettings.PercentageCorrection * 100).ToString("0")} %</li></ol><br>");
                                WunderSettings.NeedToSprinkle = true;
                            }
                        }
                    }
                    //Not warm enough to srpinkler
                    else
                    {
                        sb.Append($"<b>No need to sprinkle</b>: Temperature will be {ForecastMaxTemp}°C lower than threshold {WunderSettings.MinTemp}. Please use manual program if you still want to sprinkler.");
                        WunderSettings.NeedToSprinkle = false;
                    }
                }
                //Do all math with fuzzy logic
                else
                {
                    foreach (var objective in Fuzzy)
                    {
                        //Found the righ range
                        if ((ForecastMaxTemp >= objective.TempMin) && (ForecastMaxTemp < objective.TempMax))
                        {
                            // How much it rained ?
                            if (HistTotalPrecipitation >= objective.RainMax)
                            {
                                sb.Append($"<b>No need to sprinkle</b>: It rained {HistTotalPrecipitation} mm more than the threshold {objective.RainMax} mm.<br>");
                                WunderSettings.NeedToSprinkle = false;
                            }
                            // Will it rain for sure? and will it rain enough?
                            else if ((ForecastProbabilityPrecipitation >= WunderSettings.PrecipitationPercentForecast) && (ForecastTotalPrecipitation >= objective.RainMax))
                            {
                                sb.Append($"<b>No need to sprinkle</b>:<ol><li>Confidence index is {ForecastProbabilityPrecipitation} % more than the threshold {WunderSettings.PrecipitationPercentForecast} %</li>");
                                sb.Append($"<li>With {ForecastTotalPrecipitation} mm more than the threshold {objective.RainMax} mm.</li></ol><br>");
                                WunderSettings.NeedToSprinkle = false;
                            }
                            else
                            {   // so we need to sprinkler. Make the math how long with the correction factor
                                // first calculate proportion of time vs the theoritical maximum
                                WunderSettings.PercentageCorrection = (float)(((objective.RainMax - HistTotalPrecipitation) / objective.RainMax) * objective.SprinklingMax / 100.0);
                                sb.Append($"<b>I plan to sprinkle</b>: <ol><li>Yesterday rain was {HistTotalPrecipitation} mm and {HistMaxTemp}°C</li>");
                                sb.Append($"<li>Today forecast is {ForecastTotalPrecipitation} mm at {ForecastProbabilityPrecipitation}% and max temperature {ForecastMaxTemp}°C</li>");
                                sb.Append($"<li>Percentage adjustment is {(WunderSettings.PercentageCorrection * 100).ToString("0")}%</li></ol><br>");
                                WunderSettings.NeedToSprinkle = true;
                            }
                        }

                    }
                }

                // display the forecast per day
                sb.Append("<table><tr><th>Date</th><th>Summary</th><th></th><th>Temp min</th><th>Temp max</th><th>Wind speed</th><th>Precipitation</th><tr>");
                for (int i = 0; i < forecast.Daily.Days.Count; i++) 
                {
                    sb.Append("<tr><td>" + forecast.Daily.Days[i].Time.AddDays(1).ToString("yyyy-MM-dd ddd") + "</td><td>");
                    sb.Append(forecast.Daily.Days[i].Summary + "</td><td>");
                    sb.Append("<img src=\"https://www.ellerbach.net/public/meteo/" + forecast.Daily.Days[i].Icon + ".png\"></td><td>");
                    sb.Append(forecast.Daily.Days[i].LowTemperature.ToString("0.0") + "°C</td><td>");
                    sb.Append(forecast.Daily.Days[i].HighTemperature.ToString("0.0") + "°C</td><td>");
                    sb.Append((forecast.Daily.Days[i].WindSpeed * 3.6).ToString("0.0") + "km/h</td><td>");
                    sb.Append(forecast.Daily.Days[i].PrecipitationType + " " + forecast.Daily.Days[i].PrecipitationProbability * 100 + "% chance with " + (forecast.Daily.Days[i].PrecipitationIntensity * 24).ToString("0.0") + " mm</td>");
                }
                //hourly forecast
                sb.Append("</table><br />Prévisions par heure:<br />");
                sb.Append("<table><tr><th>Date</th><th>Summary</th><th></th><th>Temp</th><th>Temp app</th><th>Wind speed</th><th>Precipitation</th><tr>");
                for (int i = 0; i < forecast.Hourly.Hours.Count; i++) 
                {
                    sb.Append("<tr><td>" + forecast.Hourly.Hours[i].Time.ToString("yyyy-MM-dd ddd hh:mm") + "</td><td>");
                    sb.Append(forecast.Hourly.Hours[i].Summary + "</td><td>");
                    sb.Append("<img src=\"https://www.ellerbach.net/public/meteo/" + forecast.Hourly.Hours[i].Icon + ".png\"></td><td>");
                    sb.Append(forecast.Hourly.Hours[i].Temperature.ToString("0.0") + "°C</td><td>");
                    sb.Append(forecast.Hourly.Hours[i].ApparentTemperature.ToString("0.0") + "°C</td><td>");
                    sb.Append((forecast.Hourly.Hours[i].WindSpeed * 3.6).ToString("0.0") + "km/h</td><td>");
                    sb.Append(forecast.Hourly.Hours[i].PrecipitationType + " " + forecast.Hourly.Hours[i].PrecipitationProbability * 100 + "% chance with " + (forecast.Hourly.Hours[i].PrecipitationIntensity).ToString("0.0") + " mm</td>");
                }

                //End of the page
                sb.Append("</table></body></html>");

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

        static private string GetForecastWunder(string param)
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
                WunderSettings.NeedToSprinkle = false;
                for (int i = 0; i < WunderSettings.Stations.Length; i++)
                {
                    var forecast = client.GetForecast(QueryType.PWSId, new QueryOptions() { PWSId = WunderSettings.Stations[i] });
                    var history = client.GetHistory(QueryType.PWSId, new QueryOptions() { PWSId = WunderSettings.Stations[i], Date = DateTime.Now.AddDays(-1) });

                    sb.Append($"<p>Full forecast for this station: <a href=\"https://www.wunderground.com/cgi-bin/findweather/getForecast?query=pws:{WunderSettings.Stations[i]}\">{WunderSettings.Stations[i]}</a><br>");
                    //Console.WriteLine("Magic recommendation:");
                    sb.Append("Magic recommendation:<br>");

                    //check temperature 
                    // so far, threshold are manual 
                    // Min temp is 15, don't sprinkler below 15 and 10C is 1 minute
                    // More thabn 30, then sprinkler the maximum time
                    var maxtmp = history.History.Dailysummary[0].MaxTempMetric;
                    if (maxtmp == null)
                        maxtmp = 0;

                    // Adjust the simple proportion with min/max thresholds
                    if ((maxtmp > 0))
                    {
                        if (maxtmp >= WunderSettings.MaxTemp)
                            WunderSettings.PercentageCorrection = 1;
                        else if (maxtmp <= WunderSettings.MinTemp)
                            WunderSettings.PercentageCorrection = 0;
                        else
                        {
                            WunderSettings.PercentageCorrection = ((float)maxtmp - WunderSettings.MinTemp) / (WunderSettings.MaxTemp - WunderSettings.MinTemp);
                        }

                    }
                    else
                        WunderSettings.PercentageCorrection = 1;

                    // Case we just want to use the simple proportion
                    if (Fuzzy == null)
                    {
                        if (history.History.Dailysummary[0].PrecipitationMetric > WunderSettings.PrecipitationThresholdActuals)
                        {
                            //Console.Write($"It rained and more than 5mm ({history.History.DailySummaries[0].PrecipitationMetric}mm), no need to sprinkle.");
                            sb.Append($"It rained and more than {WunderSettings.PrecipitationThresholdActuals} mm ({history.History.Dailysummary[0].PrecipitationMetric}mm), <b>no need to sprinkle</b>.<br>");
                            WunderSettings.NeedToSprinkle = false;
                        }
                        else if (forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm > WunderSettings.PrecipitationThresholdForecast)
                        {
                            //Console.WriteLine($"Forecast is for rain, more than 3mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm}mm), checking the confidence index.");
                            sb.Append($"Forecast is for rain, more than {WunderSettings.PrecipitationThresholdForecast} mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm} mm), checking the confidence index.<br>");
                            if (forecast.Forecast.SimpleForecast.ForecastDay[0].Pop > WunderSettings.PrecipitationPercentForecast)
                            {
                                //Console.WriteLine($"Considence index if more than 60% ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop}%), so no need to sprinkle.");
                                sb.Append($"Considence index is more than {WunderSettings.PrecipitationPercentForecast} % ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop} %), so <b>no need to sprinkle</b>.<br>");
                                WunderSettings.NeedToSprinkle = false;
                            }
                            else
                            {
                                if (WunderSettings.PercentageCorrection > 0)
                                {
                                    WunderSettings.NeedToSprinkle = true;
                                    sb.Append($"Confidence index is less than {WunderSettings.PrecipitationPercentForecast} % ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop} %), ");
                                    sb.Append($"sprinkling will be adjusted by {(WunderSettings.PercentageCorrection * 100).ToString("0")} %, so yes, <b>plan to sprinkle!</b><br>");
                                }
                                else
                                {
                                    sb.Append($"Temperature is too low {maxtmp}C, so we will not sprinkler at all. Please use manual program if you still want to sprinkler.");
                                    WunderSettings.NeedToSprinkle = false;
                                }
                            }
                        }
                        else
                        {
                            if (WunderSettings.PercentageCorrection > 0)
                            {
                                WunderSettings.NeedToSprinkle = true;
                                sb.Append($"Forecast is for rain, but less than {WunderSettings.PrecipitationThresholdActuals} mm ({forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm} mm), ");
                                sb.Append($"sprinkling will be adjusted by {(WunderSettings.PercentageCorrection * 100).ToString("0")} %, so yes, <b>plan to sprinkle!</b><br>");
                            }
                            else
                            {
                                sb.Append($"Temperature is too low {maxtmp}C, so we will not sprinkler at all. Please use manual program if you still want to sprinkler.");
                                WunderSettings.NeedToSprinkle = false;
                            }
                        }
                    }
                    else
                    {
                        // Use of Fuzzy logic
                        foreach (var objective in Fuzzy)
                        {
                            //Found the righ range
                            if ((objective.TempMin >= maxtmp) && (objective.TempMax < maxtmp))
                            {
                                // How much rain?
                                if (history.History.Dailysummary[0].PrecipitationMetric > objective.RainMax)
                                {
                                    sb.Append($"It rained and more than {objective.RainMax} mm ({history.History.Dailysummary[0].PrecipitationMetric}mm), <b>no need to sprinkle</b>.<br>");
                                    WunderSettings.NeedToSprinkle = false;
                                }
                                else if (forecast.Forecast.SimpleForecast.ForecastDay[0].Qpf_AllDay.Mm > WunderSettings.PrecipitationThresholdForecast)
                                {
                                    sb.Append($"Considence index is more than {WunderSettings.PrecipitationPercentForecast} % ({forecast.Forecast.SimpleForecast.ForecastDay[0].Pop} %), so <b>no need to sprinkle</b>.<br>");
                                    WunderSettings.NeedToSprinkle = false;
                                }
                                else
                                {   // so we need to sprinkler. Make the math how long with the correction factor
                                    // first calculate proportion of time vs the theoritical maximum
                                    if (history.History.Dailysummary[0].PrecipitationMetric != null)
                                    {
                                        WunderSettings.PercentageCorrection = (float)(((objective.RainMax - history.History.Dailysummary[0].PrecipitationMetric.Value) / objective.RainMax) * objective.SprinklingMax / 100.0);
                                    }
                                    else
                                        WunderSettings.PercentageCorrection = (float)(objective.SprinklingMax / 100.0);
                                }
                            }
                        }

                    }

                    if (WunderSettings.NeedToSprinkle)
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
