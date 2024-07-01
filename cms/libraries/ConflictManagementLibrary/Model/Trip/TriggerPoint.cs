using RailgraphLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Interlocking;

namespace ConflictManagementLibrary.Model.Trip
{
    public class TriggerPoint
    {
        public string TheTriggerName { get; set; }
        public GraphObj TheGraphObj { get; set; }
        public DateTime LastOccupancyOnTime;
        public EOccupation MyOccupationStatus;
        public string MyRouteName;
        private TriggerPoint(string theTriggerName, GraphObj theGraphObj, string theRouteName)
        {
            TheTriggerName = theTriggerName;
            TheGraphObj = theGraphObj;
            MyRouteName = theRouteName;
        }
        public static TriggerPoint CreateInstance(string theTriggerName, GraphObj theGraphObj,string theRouteName)
        {
            return new TriggerPoint(theTriggerName, theGraphObj, theRouteName);
        }

        public void UpdateOccupancyStatus()
        {
                switch (TheGraphObj)
                {
                    case PointLeg leg:
                    {
                        MyOccupationStatus = leg.getOccupationState();
                        if (MyOccupationStatus == EOccupation.occupationOn) LastOccupancyOnTime = DateTime.Now;
                        break;
                    }
                    case TrackSection trackSection:
                    {
                        MyOccupationStatus = trackSection.getOccupationState();
                        if (MyOccupationStatus == EOccupation.occupationOn) LastOccupancyOnTime = DateTime.Now;
                        break;
                    }
                    case Track track:
                    {
                        MyOccupationStatus = track.getOccupationState();
                        if (MyOccupationStatus == EOccupation.occupationOn) LastOccupancyOnTime = DateTime.Now;
                        break;
                    }
                    case RailgraphLib.Interlocking.Point:
                    {
                        break;
                    }
                }
        }

        public bool IsOccupied()
        {
            if (MyOccupationStatus == EOccupation.occupationOn) return true;
            if (LastOccupancyOnTime > DateTime.Now.AddSeconds(-20)) return true;
            return false;

        }
    }
}
