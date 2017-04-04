using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace SprinklerRPI.Models
{
    class SoilHumidity
    {
        private const int GPIO_PIN = 21;
        private GpioPin soilhumio;

        public SoilHumidity()
        {
            var gpio = GpioController.GetDefault();
            soilhumio = gpio.OpenPin(GPIO_PIN);
            soilhumio.SetDriveMode(GpioPinDriveMode.Input);
        }

        public bool IsHumid
        {
            get
            {
                return soilhumio.Read() == GpioPinValue.High;
            }
        }
    }
}
