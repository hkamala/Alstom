using RailgraphLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Interlocking;

namespace ConflictManagementLibrary.Model.Trip
{
    public class SignalPoint
    {
        public string TheSignalName { get; }
        public GraphObj TheGraphObj { get; }
        public EAspect MySignalAspect;
        public DateTime LastClearTime;

        private SignalPoint(string theSignalName, GraphObj theGraphObj)
        {
            TheSignalName = theSignalName;
            TheGraphObj = theGraphObj;
        }
        public static SignalPoint CreateInstance(string theSignalName, GraphObj theGraphObj)
        {
            return new SignalPoint(theSignalName, theGraphObj);
        } 
        public void UpdateClearedStatus()
        {
            if (MyRailGraphManager?.ILGraph?.getGraphObj(TheSignalName) is not SignalOptical signal) return;
            var aspectState = signal.getAspectState();
            MySignalAspect = aspectState;
            if (aspectState == EAspect.aspectClear) LastClearTime = DateTime.Now;
        }
        public bool IsClear()
        {
            if (MyRailGraphManager?.ILGraph?.getGraphObj(TheSignalName) is SignalOptical signal)
            {
                if (MySignalAspect == EAspect.aspectClear) return true;
                if (LastClearTime > DateTime.Now.AddSeconds(-20)) return true;
            }
            return false;
        }

    }
}
