using System.Collections.Generic;
using System.Text.Json.Serialization;
using ConflictManagementLibrary.Management;


namespace ConflictManagementLibrary.Network
{
    public class Platform
    {
        public int MyReferenceNumber { get; set; }
        public string MyName { get; set; }
        public int StationId { get; set; }
        public string StationAbbreviation { get; set; }
        public PlatformAlternate MyPlatformAlternate { get; set; }
        [JsonIgnore] public uint MyTrackUId { get; set; }
        [JsonIgnore] public RailgraphLib.HierarchyObjects.Track? MyHierarchyTrack;

        [JsonIgnore] public RailgraphLib.HierarchyObjects.Platform? MyHierarchyPlatform;
        [JsonIgnore] public ElementPosition? MyElementPosition;
        [JsonIgnore] public long MyDistanceInMeters = 0;
        [JsonIgnore] public Track? MyTrackNetwork { get; set; }
        [JsonIgnore] public RailgraphLib.HierarchyObjects.Track? MyTrackHierarchy { get; set; }

    }
}
