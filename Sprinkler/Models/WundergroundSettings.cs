﻿using System;
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
    }
}
