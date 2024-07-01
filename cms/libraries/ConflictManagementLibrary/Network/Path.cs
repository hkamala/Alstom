using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Network
{
    public class Path
    {
        public int MyReferenceNumber { get; set; }
        public string MyStationName { get; set; }
        public int MyNodeId { get; set; }
        public Link MyLinkLeft { get; set; }
        public Link MyLinkRight { get; set; }
        public string MyRouteName { get; set; }
        public string MySignalEnter { get; set; }
        public string MySignalExit { get; set; }
        public string MyDirection { get; set; }
        public bool IsDiverging { get; set; }
        public int MyLength { get; set; }
        public List<Track> MyTracks = new List<Track>();
        public List<Platform> MyPlatforms = new List<Platform>();
        public int MyConnectionLeft { get; set; }
        public int MyConnectionRight { get; set; }
        public bool IsStationNeck { get; set; }
        public string MyConflictingStationNeckPath { get; set; }
        public string GetPathDescription()
        {
            var theDescription = new StringBuilder();

                theDescription.Append("@Station <" + MyStationName + "> Node <" + MyNodeId + ">" +
                                      "Enter Signal <" + MySignalEnter + ">" + "Exit Signal <" + MySignalExit + ">" +
                                      "Route Name <" + MyRouteName + ">");

            return theDescription.ToString();
        }
    }
}
