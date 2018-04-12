using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Models
{
    public class WundergroundSettings
    {
        public string Key { get; set; }
        public string[] Stations { get; set; }
        public bool AutomateAll { get; set; }
        public float PrecipitationThresholdActuals { get; set; }
        public float PrecipitationThresholdForecast { get; set; }
        public int PrecipitationPercentForecast { get; set; }
        public string TimeToCheck { get; set; }
        public bool NeedToSprinkle { get; set; }
        public float PercentageCorrection { get; set; }
        public float MinTemp { get; set; }
        public float MaxTemp { get; set; }
    }
}
