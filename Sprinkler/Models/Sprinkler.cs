using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace SprinklerRPI.Models
{
    public sealed class Sprinkler
    {
        private bool MySprinklerisOpen = false;
        private int MySprinklerNumber;
        private GpioPin MySprOpen;
        private GpioPinValue pinValue;
        private Timer MyTimerCallBack;
        private long MyTicksWait;

        public bool IsInverted { get; set; }

        private const int GPIO_PIN_D0 = 5;
        private const int GPIO_PIN_D1 = 6;
        private const int GPIO_PIN_D2 = 13;
        private const int GPIO_PIN_D3 = 19;
        private const int GPIO_PIN_D4 = 26;

        public Sprinkler(int SprNum, bool isinverted = false)
        {
            IsInverted = isinverted;
            MyTimerCallBack = new Timer(MyTimerCallBack_Tick, this, Timeout.Infinite, Timeout.Infinite);
            //MyTimerCallBack.Tick += MyTimerCallBack_Tick;

            MySprinklerNumber = SprNum;
            MyTicksWait = DateTime.Now.Ticks;
            var gpio = GpioController.GetDefault();
            switch (SprNum)
            {
                case 0:
                    MySprOpen = gpio.OpenPin(GPIO_PIN_D0);
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low;
                    MySprOpen.Write(pinValue);
                    MySprOpen.SetDriveMode(GpioPinDriveMode.Output);
                    break;
                case 1:
                    MySprOpen = gpio.OpenPin(GPIO_PIN_D1);
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low;
                    MySprOpen.Write(pinValue);
                    MySprOpen.SetDriveMode(GpioPinDriveMode.Output);
                    break;
                case 2:
                    MySprOpen = gpio.OpenPin(GPIO_PIN_D2);
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low;
                    MySprOpen.Write(pinValue);
                    MySprOpen.SetDriveMode(GpioPinDriveMode.Output);
                    break;
                case 3:
                    MySprOpen = gpio.OpenPin(GPIO_PIN_D3);
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low;
                    MySprOpen.Write(pinValue);
                    MySprOpen.SetDriveMode(GpioPinDriveMode.Output);
                    break;
                case 4:
                    MySprOpen = gpio.OpenPin(GPIO_PIN_D4);
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low;
                    MySprOpen.Write(pinValue);
                    MySprOpen.SetDriveMode(GpioPinDriveMode.Output);
                    break;
            }
        }

        // open or close a sprinkler
        public bool Open
        {
            get { return MySprinklerisOpen; }
            set
            {
                MySprinklerisOpen = value;
                //do harware here
                if (MySprinklerisOpen)
                    pinValue = IsInverted ? GpioPinValue.Low : GpioPinValue.High; 
                else
                    pinValue = IsInverted ? GpioPinValue.High : GpioPinValue.Low; 
                MySprOpen.Write(pinValue);
            }
        }

        //read only property
        public int Number
        {
            get { return MySprinklerNumber; }
        }

        public int TimerInterval
        {
            get { return 0; }
            set { MyTimerCallBack.Change(value, 0); }
            //set { MyTimerCallBack = value; }
        }

        public string Name { get; set; }

        //public HumiditySensor HumiditySensor;

        private void MyTimerCallBack_Tick(object sender)
        {
            Sprinkler Sprinklers = (Sprinkler)sender;
            Sprinklers.Open = false;
            //Sprinklers.TimerCallBack.Stop();
            Sprinklers.TimerInterval = Timeout.Infinite;
        }
    }
}
