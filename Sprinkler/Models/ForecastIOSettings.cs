using DarkSkyApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Models
{
    public class ForecastIOSettings
    {
        public string ApiKey { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public Unit Unit { get; set; }
        public Language Language { get; set; }

        public string City { get; set; }
    }
}
