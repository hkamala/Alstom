using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Network
{
    public class Track
    {
        public uint MyUid { get; set; }
        public string? MyName { get; set; }
        public Link? MyLink { get; set; }
        public int MyLength { get; set; }
        public decimal MyMeasurePointLeft { get; set; }
        public decimal MyMeasurePointRight { get; set; }
        public TrackAssociation? MyTrackAssociation { get; set; }

        private Track(uint theUid, string theName, RailgraphLib.Interlocking.Track theTrack)
        {
            MyUid = theUid;
            MyName = theName;
            MyTrackAssociation = TrackAssociation.CreateInstance(theName,theUid, theTrack);
        }
        public static Track CreateInstance(uint theUid, string theName, RailgraphLib.Interlocking.Track theTrack)
        {
            return new Track(theUid, theName, theTrack);
        }

        public Track()
        {
            
        }
    }
    public class TrackAssociation
    {
        public uint TrackUid { get; }
        public string TrackName { get; }
        public int MyDistanceInMeters;
        public RailgraphLib.Interlocking.Track? MyInterlockingTrack;
        public RailgraphLib.HierarchyObjects.Track? MyHierarchyTrack;
        private TrackAssociation(string trackName, uint trackUid, RailgraphLib.Interlocking.Track theTrack)
        {
            TrackName = trackName;
            TrackUid = trackUid;
            MyInterlockingTrack = theTrack;
        }

        public static TrackAssociation CreateInstance(string trackName, uint trackUid, RailgraphLib.Interlocking.Track theTrack)
        {
            return new TrackAssociation(trackName, trackUid, theTrack);
        }
    }

}
