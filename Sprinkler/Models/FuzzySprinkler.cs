using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Models
{
    public class FuzzySprinkler
    {
        public float TempMin { get; set; }
        public float TempMax { get; set; }
        public float RainMax { get; set; }
        public float SprinklingMax { get; set; }
    }
}
