using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Models
{
    public class SprinklerProgramTypical
    {
        public TimeSpan Duration { get; set; }
        public int SprinklerNumber { get; set; }
        public TimeSpan StartTime { get; set; }
    }
}
