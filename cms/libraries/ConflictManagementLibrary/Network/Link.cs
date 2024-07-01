using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib;
using Console = System.Console;

namespace ConflictManagementLibrary.Network
{
    public class Link
    {
        public string MySystemGuid { get; set; } = Guid.NewGuid().ToString();
        public int MyReferenceNumber { get; set; }
        public string? MyDescription { get; set; }
        public int MyConnectionLeft { get; set; }
        public int MyConnectionRight { get; set; }
        public bool IsRightDirectionOnly { get; set; }
        public bool IsLeftDirectionOnly { get; set; }
        public bool IsBiDirectional { get; set; }
        public int MyDistance { get; set; }
        public List<Track> MyTracks = new List<Track>();
        public List<Platform> MyPlatforms = new List<Platform>();
        public bool IsLinkedToStationLeft { get; set; }
        public bool IsLinkedToStationRight { get; set; }
        public string? EdgeName { get; set; }
        public string? EdgeId { get; set; }
        public string? EdgeKiloMeterValue { get; set; }
        public LinkEdgeAssociation? MyEdgeAssociation { get; set; }
        public bool HasFollowUpSignalProtection { get; set; }
        public bool IsStationTrack { get; set; }

    }

    public class LinkEdgeAssociation
    {
        public string? EdgeName { get; }
        public string? EdgeUid { get; }
        public string? MyKiloMeterValue { get; }

        public int MyDistanceInMeters;
        public Edge? MyEdge;
        public List<TrackAssociation> MyTrackAssociations = new();

        private LinkEdgeAssociation(string edgeName, string edgeUid, string kmValue)
        {
            EdgeName = edgeName;
            EdgeUid = edgeUid;
            MyKiloMeterValue = kmValue;
        }

        public LinkEdgeAssociation()
        {
            
        }
        public static LinkEdgeAssociation CreateInstance(string edgeName, string edgeUid, string kmValue)
        {
            return new LinkEdgeAssociation(edgeName, edgeUid, kmValue);
        }

        public static implicit operator LinkEdgeAssociation(string v)
        {
            throw new NotImplementedException();
        }
    }

}
