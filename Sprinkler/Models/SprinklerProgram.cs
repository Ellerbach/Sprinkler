using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Models
{
    public sealed class SprinklerProgram
    {
        private DateTimeOffset myDateTimeStart;
        private TimeSpan myDuration;
        private int mySprinklerNumber;

        public SprinklerProgram(DateTimeOffset mDT, TimeSpan mTS, int mSN)
        {
            myDateTimeStart = mDT;
            myDuration = mTS;
            mySprinklerNumber = mSN;
        }

        public DateTimeOffset DateTimeStart
        {
            get { return myDateTimeStart; }
            set { myDateTimeStart = value; }
        }
        public TimeSpan Duration
        {
            get { return myDuration; }
            set { myDuration = value; }
        }

        public int SprinklerNumber
        {
            get { return mySprinklerNumber; }
            set { mySprinklerNumber = value; }
        }

    }
}
