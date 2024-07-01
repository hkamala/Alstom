using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Model.Trip
{
    public class RunningTimes
    {
        public string FromStation { get; }
        public string ToStation { get; }
        public string Priority { get; }
        public string TimeOdd { get; }
        public string TimeEven { get; }

        private RunningTimes(string fromStation, string toStation, string priority, string timeOdd, string timeEven)
        {
            FromStation = fromStation;
            ToStation = toStation;
            Priority = priority;
            TimeOdd = timeOdd;
            TimeEven = timeEven;
        }

        public static RunningTimes CreateInstance(string fromStation, string toStation, string priority, string timeOdd, string timeEven)
        {
            return new RunningTimes(fromStation, toStation, priority, timeOdd, timeEven);
        }
    }

    public class IncreaseDecreaseTimes
    {
        public string FromStation { get; }
        public string ToStation { get; }
        public string TimeIncreaseOdd { get; }
        public string TimeDecreaseOdd { get; }
        public string TimeIncreaseEven { get; }
        public string TimeDecreaseEven { get; }
        public string ScenarioOne { get; }
        public string ScenarioTwo { get; }
        public string Priority { get; }
 
        private IncreaseDecreaseTimes(string priority, string fromStation, string toStation, string timeIncreaseOdd, string timeDecreaseOdd, string timeIncreaseEven, string timeDecreaseEven, string scenarioOne, string scenarioTwo)
        {
            FromStation = fromStation;
            ToStation = toStation;
            TimeIncreaseOdd = timeIncreaseOdd;
            TimeDecreaseOdd = timeDecreaseOdd;
            TimeIncreaseEven = timeIncreaseEven;
            TimeDecreaseEven = timeDecreaseEven;
            ScenarioOne = scenarioOne;
            ScenarioTwo = scenarioTwo;
            Priority = priority;
        }

        internal static IncreaseDecreaseTimes CreateInstance(string priority, string fromStation, string toStation, string timeIncreaseOdd, string timeDecreaseOdd, string timeIncreaseEven, string timeDecreaseEven, string scenarioOne, string scenarioTwo)
        {
            return new IncreaseDecreaseTimes(priority, fromStation, toStation, timeIncreaseOdd, timeDecreaseOdd, timeIncreaseEven, timeDecreaseEven, scenarioOne, scenarioTwo);
        }
    }

}
