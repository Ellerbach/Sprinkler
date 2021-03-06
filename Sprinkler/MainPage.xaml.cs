﻿using Devkoes.Restup.WebServer.File;
using Devkoes.Restup.WebServer.Http;
using Devkoes.Restup.WebServer.Rest;
using SprinklerRPI.Controllers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SprinklerRPI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //private DispatcherTimer RebootTimer;
        private static HttpServer webserver;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await IoTCoreHelpers.Networking.Wifi.UpdateConnectivity("wifi.config");

            await SprinklerManagement.InitParam();

            webserver = new HttpServer(80);

            var restRouteHandler = new RestRouteHandler();
            try
            {
                webserver.RegisterRoute("file", new StaticFileRouteHandler(ApplicationData.Current.LocalFolder.Path + "\\file"));
                webserver.RegisterRoute("", restRouteHandler);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            restRouteHandler.RegisterController<SprinklerManagement>();

            await webserver.StartServerAsync();

            //RebootTimer = new DispatcherTimer();
            //RebootTimer.Interval = new TimeSpan(1, 0, 0, 0);
            //RebootTimer.Tick += RebootTimer_Tick;
            //RebootTimer.Start();
        }

        private void RebootTimer_Tick(object sender, object e)
        {
            // Save the programs
            Task<bool> t = SprinklerManagement.SavePrograms();
            while (!t.IsCompleted)
                ;
            //reboot the device
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, new TimeSpan(0, 0, 5));
        }
    }
}
