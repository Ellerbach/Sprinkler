using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using SprinklerRPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Controllers
{
    partial class SprinklerManagement
    {
        private const string strFileIoT = "iot.config";
        static private string strconn = "";
        static private async Task InitIoTHub()
        {
            try
            {
                var fileToRead = new FileStream(await GetFilePathAsync(strFileIoT), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long fileLength = fileToRead.Length;

                byte[] buf = new byte[fileLength];
                //string mySetupString = "";

                // Reads the data.
                fileToRead.Read(buf, 0, (int)fileLength);
                //await str.ReadAsync(buf,  )
                // convert the read into a string

                strconn = new string(Encoding.UTF8.GetChars(buf));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Azure Iot Hub connection string: {ex.Message}");
            }

        }

        static private async Task ReceiveDataFromAzure()
        {
            if (strconn == "")
                return;
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(strconn, TransportType.Http1);

            Message receivedMessage = null;
            string messageData;

            try
            {


                while (true)
                {
                    try
                    {
                        receivedMessage = await deviceClient.ReceiveAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error receiving from Azure Iot Hub: {ex.Message}");
                    }


                    if (receivedMessage != null)
                    {
                        bool ballOK = true;
                        // {"command":"addprogram","message":"{\"DateTimeStart\":\"2016-06-02T03:04:05+00:00\",\"Duration\":\"00:02:05\",\"SprinklerNumber\":3}"}
                        //MessageIoT temp = new MessageIoT();
                        //temp.command = "test";
                        //temp.message = JsonConvert.SerializeObject(new SprinklerProgram(new DateTimeOffset(2016, 6, 2, 3, 4, 5, new TimeSpan(0, 0, 0)), new TimeSpan(0, 2, 5), 3));
                        //var ret = JsonConvert.SerializeObject(temp);
                        //SendDataToAzure(ret);
                        messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                        MessageIoT cmdmsg = null;
                        try
                        {
                            cmdmsg = JsonConvert.DeserializeObject<MessageIoT>(messageData);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                await deviceClient.RejectAsync(receivedMessage);
                                ballOK = false;
                            }
                            catch (Exception)
                            {
                                ballOK = false;
                            }

                        }
                        if (!ballOK)
                        { }
                        else if (cmdmsg.command.ToLower() == "sprinklername")
                        {
                            cmdmsg.message = JsonConvert.SerializeObject(Sprinklers);
                            Task.Delay(500);
                            SendDataToAzure(JsonConvert.SerializeObject(cmdmsg));
                        }
                        else if (cmdmsg.command.ToLower() == "programs")
                        {
                            cmdmsg.message = JsonConvert.SerializeObject(SprinklerPrograms);
                            Task.Delay(500);
                            SendDataToAzure(JsonConvert.SerializeObject(cmdmsg));
                        }
                        else if (cmdmsg.command.ToLower() == "addprogram")
                        {
                            if (cmdmsg.message != null)
                            {
                                try
                                {
                                    SprinklerPrograms.Add(JsonConvert.DeserializeObject<SprinklerProgram>(cmdmsg.message));
                                }
                                catch (Exception)
                                {
                                    ballOK = false;
                                }
                            }
                        }
                        else if (cmdmsg.command.ToLower() == "removeprogram")
                        {
                            if (cmdmsg.message != null)
                            {
                                try
                                {
                                    //need to be smart how to remove a program
                                    //so loop and check the elements
                                    for (int i = 0; i < SprinklerPrograms.Count; i++)
                                    {
                                        SprinklerProgram MySpr = (SprinklerProgram)SprinklerPrograms[i];
                                        SprinklerProgram spr = JsonConvert.DeserializeObject<SprinklerProgram>(cmdmsg.message);
                                        if ((MySpr.SprinklerNumber == spr.SprinklerNumber) &&
                                            (MySpr.Duration.CompareTo(spr.Duration) == 0) &&
                                            (MySpr.DateTimeStart.CompareTo(spr.DateTimeStart) == 0))
                                            SprinklerPrograms.RemoveAt(i);
                                    }
                                }
                                catch (Exception)
                                {
                                    ballOK = false;
                                }

                            }
                        }
                        else if ((cmdmsg.command.ToLower() == "pumpstart") || (cmdmsg.command.ToLower() == "pumpstop"))
                        {
                            int sprNum = -1;
                            try
                            {
                                sprNum = Convert.ToInt32(cmdmsg.message);
                            }
                            catch { }
                            if ((sprNum >= 0) && (sprNum < NUMBER_SPRINKLERS))
                            {
                                if (cmdmsg.command.ToLower() == "pumpstart")
                                    Sprinklers[sprNum].Open = true;
                                else
                                    Sprinklers[sprNum].Open = false;
                            }
                        }

                        try
                        {
                            if (ballOK)
                                await deviceClient.CompleteAsync(receivedMessage);
                            else
                                await deviceClient.RejectAsync(receivedMessage);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                await deviceClient.RejectAsync(receivedMessage);
                            }
                            catch (Exception)
                            {

                            }
                            //throw;
                        }

                    }
                }
            }
            catch (Exception)
            {
                ReceiveDataFromAzure();
            }
        }

        static private async Task SendDataToAzure(string text)
        {
            if (strconn == "")
                return;
            try
            {
                DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(strconn, TransportType.Http1);

                //var text = "{\"info\":\"RPI SerreManagment Working\"}";
                var msg = new Message(Encoding.UTF8.GetBytes(text));

                await deviceClient.SendEventAsync(msg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error posting on Azure Iot Hub: {ex.Message}");
            }

        }

    }
}
