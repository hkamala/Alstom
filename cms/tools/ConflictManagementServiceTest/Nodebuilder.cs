using System;
using System.Collections.Generic;
using System.ComponentModel;
using ConflictManagementLibrary.Network;
using Newtonsoft.Json;
using Link = ConflictManagementLibrary.Network.Link;
using Path = ConflictManagementLibrary.Network.Path;
using Track = ConflictManagementLibrary.Network.Track;

namespace ConflictManagementServiceTest
{
    public class NodeBuilder
    {
       // private List<Link> LinkList { get; set; } = new List<Link>();

        private HashSet<Station> StationList { get; set; } = new HashSet<Station>();

        private int Counter = 0;

        public NodeBuilder()
        {
            Zemitani();
            Sarkanaugava();
            Mangali();
            Ziemelblazma();
            Vecaki();
            Carnikava();
            Lilaste();
            Saulkrasti();
            Skulte();
        }

        private Platform BuildPlatform(string name, string abbreviation, int stationId)
        {
            Counter += 1;
            return new Platform
            {
                MyReferenceNumber = Counter,
                MyName = $"{name}_{abbreviation.Trim().ToUpper()}",
                StationAbbreviation = abbreviation,
                StationId = stationId,
                //MyTrack = myTracks
            };
        }
        private Link BuildLink(int refNo, int leftLink, int rightLink, bool leftOnly, bool rightOnly, bool biDir = true, List<Platform> platforms = null,
            bool stationLeft = false, bool stationRight = false, string theDescription = "Link", string edgeName = "", string edgeId = "")
        {
            platforms = platforms ?? new List<Platform>();
            var theLink = new Link
            {
                MyReferenceNumber = refNo,
                MyDescription = theDescription,
                MyConnectionLeft = leftLink,
                MyConnectionRight = rightLink,
                IsRightDirectionOnly = rightOnly,
                IsLeftDirectionOnly = leftOnly,
                IsBiDirectional = biDir,
                MyDistance = 0,
                MyTracks = new List<Track>(),
                MyPlatforms = platforms,
                IsLinkedToStationLeft = stationLeft,
                IsLinkedToStationRight = stationRight,
                EdgeName = edgeName,
                EdgeId = edgeId
            };
            return theLink;
        }
        private Path BuildPath(string RefNum, string StnName, string nodeId, string linkL, string linkR, string routeName, string sigE, string sigX, string direction, bool isDiverging = true)
        {
            return new Path
            {
                MyReferenceNumber = Convert.ToInt32(RefNum),
                IsDiverging = isDiverging,
                MyStationName = StnName,
                MyNodeId = Convert.ToInt32(nodeId),
                MyConnectionRight = Convert.ToInt32(linkR),
                MyConnectionLeft = Convert.ToInt32(linkL),
                MyLinkLeft = GetLink(StnName, Convert.ToInt32(nodeId),Convert.ToInt32(linkL)),
                MyLinkRight = GetLink(StnName, Convert.ToInt32(nodeId), Convert.ToInt32(linkR)),
                MyRouteName = routeName,
                MySignalEnter = sigE,
                MySignalExit = sigX,
                MyDirection = direction
            };
        }
        private Link GetLink(string StnName, int NodeId, int LinkRef)
        {
            foreach (var stn in StationList)
            {
                if (stn.MyReferenceNumber == Convert.ToInt32(StnName))
                {
                    foreach (var nd in stn.MyNodes)
                    {
                        if (nd.MyReferenceNumber == NodeId)
                        {
                            foreach (var ll in nd.MyLeftLinks)
                            {
                                if (ll.MyReferenceNumber == LinkRef)
                                {
                                    return ll;
                                }
                            }
                            foreach (var rr in nd.MyRightLinks)
                            {
                                if (rr.MyReferenceNumber == LinkRef)
                                {
                                    return rr;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public string BuildStation()
        {
            var str = JsonConvert.SerializeObject(StationList);
            return str;
        }

        [Description("120")]
        private void Zemitani()
        {
            var station = new Station
            {
                MyReferenceNumber = 5,
                StationName = "Zemitani",
                Abbreviation = "ZEM"
            };
            StationList.Add(station);
            var platform = new Platform();
            var paths = new List<Path>();
            //var pLink110300 = new List<Platform>
            //{
            //    BuildPlatform("P3BGP", station.Abbreviation, station.MyReferenceNumber),
            //};
            //var pLink110310 = new List<Platform>
            //{
            //    BuildPlatform("P2BGP", station.Abbreviation, station.MyReferenceNumber),
            //};

            //#region In LeftLinks - 100 series
            //var leftLinks110 = new List<Link>();
            //#endregion End In LeftLinks

            //#region RightLinks - 120 series

            //var link110_120_1 = BuildLink(110300, 110, 120, false, false, true, pLink110300, true, true, "Link 1 between 110 and 120", edgeName: "E_PT2_DAR_PT205_ZEM", edgeId: "258776");
            //var link110_120_2 = BuildLink(110310, 110, 120, false, false, true, pLink110310, true, true, "Link 2 between 110 and 120", edgeName: "E_PT1_DAR_PT211_ZEM", edgeId: "258774");

            //var rightLinks110 = new List<Link>
            //{
            //    link110_120_1,
            //    link110_120_2,
            //};
            //#endregion
            //var node110 = new Node
            //{
            //    MyStation = station,
            //    MyPaths = paths,
            //    MyReferenceNumber = 110,
            //    MyLeftLinks = leftLinks110,
            //    MyRightLinks = rightLinks110
            //};
            //station.MyNodes.Add(node110);



            #region In LeftLinks - 100 series
            var leftLinks120 = new List<Link>();
            #endregion End In LeftLinks

            #region RightLinks - 300 series

            var link120_200_1 = BuildLink(120300, 120, 200, false, false, true,null, true, true, "Link 1 between 120 and 200", edgeName: "E_PT209_ZEM_PT2_SAR", edgeId: "2108");
            var link120_200_2 = BuildLink(120310, 120, 200, false, false, true, null, true, true, "Link 2 between 120 and 200", edgeName: "E_PT207_ZEM_PT4_SAR", edgeId: "2100");

            var rightLinks120 = new List<Link>
            {
                link120_200_1,
                link120_200_2,
            };
            #endregion
            var node120 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 120,
                MyLeftLinks = leftLinks120,
                MyRightLinks = rightLinks120
            };
            station.MyNodes.Add(node120);

        }
        [Description("200")]
        private void Sarkanaugava()
        {
            var station = new Station
            {
                MyReferenceNumber = 10,
                StationName = "Sarkanaugava",
                Abbreviation = "SAR"
            };
            StationList.Add(station);

            var platform = new Platform();
            var paths = new List<Path>();

            #region Platform Between 200-220

            var pLink200220_1 = new List<Platform>
            {
                BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber),
            };
            var pLink200220_2 = new List<Platform>
            {
                BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber),
            };
            var pLink200220_3 = new List<Platform>
            {
                BuildPlatform("P4C", station.Abbreviation, station.MyReferenceNumber),
            };

            #endregion

            #region Node 200

            #region In LeftLinks - 100 series
            var leftLinks200 = new List<Link>();
            #endregion End In LeftLinks

            #region RightLinks - 300 series

            var link200_220_1 = BuildLink(200100, 200, 220, false, false, true, pLink200220_1, true, true, "Link 1 between 200 and 220", edgeName: "E_PT6_PT5_SAR", edgeId:"2111");
            var link200_220_2 = BuildLink(200110, 200, 220, false, false, true, pLink200220_2, true, true, "Link 2 between 200 and 220", edgeName: "E1_PT10_PT9_SAR", edgeId:"2102");
            var link200_220_3 = BuildLink(200120, 200, 220, false, false, true, pLink200220_3, true, true, "Link 3 between 200 and 220", edgeName: "E2_PT10_PT9_SAR", edgeId:"2103");

            var rightLinks200 = new List<Link>
            {
                link200_220_1,
                link200_220_2,
                link200_220_3
            };

            #endregion End Out RightLinks


            var node200 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 200,
                MyLeftLinks = leftLinks200,
                MyRightLinks = rightLinks200
            };
            station.MyNodes.Add(node200);
            #region 200 Paths
            var paths200 = new List<Path>();
            #region Paths Node 200

            var pPath200_1 = BuildPath("200100", "20", "200", "120310", "200110", "SIP_SAR-SIP2_SAR", "P", "P2", "R", false);
            var pPath200_2 = BuildPath("200110", "20", "200", "120310", "200120", "SIP_SAR-SIP4_SAR", "P", "P4", "R");
             paths200.Add(pPath200_1);
            paths200.Add(pPath200_2);
            node200.MyPaths = paths200;

            #endregion


            #endregion

            #endregion End Node 200

            #region Node 220

            #region In LeftLinks - 100 series


            var leftLinks220 = new List<Link>();

            #endregion End In LeftLinks

            #region RightLinks - 300 series

            var link220_300_1 = BuildLink(220300, 220, 300, false, false, stationRight: true, theDescription: "Link 1 between 220 and 300", edgeName: "E_PT3_SAR_PT68_MAN", edgeId:"258802");
            var link220_300_2 = BuildLink(220310, 220, 300, false, false, stationRight: true, theDescription: "Link 2 between 220 and 300", edgeName: "E_PT1_SAR_PT2_MAN", edgeId:"258783");

            var rightLinks220 = new List<Link>
            {
                link220_300_1,
                link220_300_2
            };

            #endregion End Out RightLinks


            var node220 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 220,
                MyLeftLinks = leftLinks220,
                MyRightLinks = rightLinks220
            };

            #endregion End Node 220
            station.MyNodes.Add(node220);
            //var nList = new List<Node> { node200, node220 };
            //station.MyNodes = new List<Node>(nList);

            #region 220 Paths
            var paths220 = new List<Path>();
            #region Paths Node 220

            var pPath220_1 = BuildPath("220900", "20", "220", "200100", "220300", "SIP1_SAR-SIN_SAR", "P1", "N", "R", false);
            var pPath220_12 = BuildPath("220901", "20", "220", "200110", "220310", "SIP1_SAR-SINP_SAR", "P1", "NP", "R");
            var pPath220_2 = BuildPath("220902", "20", "220", "200110", "220300", "SIP2_SAR-SIN_SAR", "P2", "N", "R");
            var pPath220_3 = BuildPath("220903", "20", "220", "200110", "220310", "SIP2_SAR-SINP_SAR", "P2", "NP", "R", false);
            var pPath220_4 = BuildPath("220904", "20", "220", "200120", "220300", "SIP4_SAR-SIN_SAR", "P4", "N", "R");
            var pPath220_5 = BuildPath("220905", "20", "220", "200120", "220310", "SIP4_SAR-SINP_SAR", "P4", "NP", "R");
            var pPath220_6 = BuildPath("220906", "20", "220", "200100", "220300", "SIN_SAR-SIN1_SAR", "N", "P1", "L", false);
            var pPath220_7 = BuildPath("220907", "20", "220", "200110", "220300", "SIN_SAR-SIN2_SAR", "N", "P2", "L");
            var pPath220_8 = BuildPath("220908", "20", "220", "200120", "220300", "SIN_SAR-SIN4_SAR", "N", "P4", "L");
            var pPath220_9 = BuildPath("220909", "20", "220", "200110", "220310", "SINP_SAR-SIN1_SAR", "NP", "P1", "L");
            var pPath220_10 = BuildPath("220910", "20", "220", "200110", "220310", "SINP_SAR-SIN2_SAR", "NP", "P2", "L", false);
            var pPath220_11 = BuildPath("220911", "20", "220", "200120", "220310", "SINP_SAR-SIN4_SAR", "NP", "P4", "L");
            paths220.Add(pPath220_1);
            paths220.Add(pPath220_12);
            paths220.Add(pPath220_2);
            paths220.Add(pPath220_3);
            paths220.Add(pPath220_4);
            paths220.Add(pPath220_5);
            paths220.Add(pPath220_6);
            paths220.Add(pPath220_7);
            paths220.Add(pPath220_8);
            paths220.Add(pPath220_9);
            paths220.Add(pPath220_10);
            paths220.Add(pPath220_11);

            node220.MyPaths = paths220;

            #endregion


            #endregion

        }

        [Description("300")]
        private void Mangali()
        {
            var station = new Station
            {
                MyReferenceNumber = 30,
                StationName = "Mangali",
                Abbreviation = "MAN"
            };
            StationList.Add(station);

            var paths = new List<Path>();

            #region Platforms

            #region Platform Between 300
            var p300320_1 = new List<Platform> { BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber), };
            var p300320_2 = new List<Platform> { BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber),};
            var p300320_3 = new List<Platform> { BuildPlatform("P3C", station.Abbreviation, station.MyReferenceNumber ), };
            var p300320_4 = new List<Platform> { BuildPlatform("P6C", station.Abbreviation, station.MyReferenceNumber ), };
            var p300320_5 = new List<Platform> { BuildPlatform("P7C", station.Abbreviation, station.MyReferenceNumber ), };
            var p300320_6 = new List<Platform> { BuildPlatform("P8C", station.Abbreviation, station.MyReferenceNumber ), };
            var p300320_7 = new List<Platform> { BuildPlatform("P9C", station.Abbreviation, station.MyReferenceNumber ), };
            var p300320_8 = new List<Platform> { BuildPlatform("P10AC", station.Abbreviation, station.MyReferenceNumber ), };

            #endregion End  300-320

            #endregion Platforms

            #region Node 300

            #region In LeftLinks - 100 series
            var leftLinks300 = new List<Link>();
            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks300 = new List<Link>
            {
                {
                    BuildLink(300330, 300, 0, false, false, platforms: p300320_4, theDescription: "Link 4 from 300 to P6C", edgeId:"258794", edgeName:"E_PT22_PT70_MAN")
                },
                {
                    BuildLink(300340, 300, 0, false, false, platforms: p300320_5, theDescription: "Link 5 from 300 to P7C", edgeId:"258796", edgeName:"E_PT24_PT54_MAN") 
                },
                {
                    BuildLink(300350, 300, 0, false, false, platforms: p300320_6, theDescription: "Link 6 from 300 to P8C", edgeId:"258798", edgeName:"E_PT26_PT50_MAN") 
                },
                {
                    BuildLink(300360, 300, 0, false, false, platforms: p300320_7, theDescription: "Link 7 from 300 to P9C", edgeId:"258799", edgeName:"E_PT28_PT50_MAN") 
                },
                {
                    BuildLink(300370, 300, 0, false, false, platforms: p300320_8, theDescription: "Link 8 from 300 to P10AC",edgeId:"258800", edgeName:"E_PT28_PT30_MAN") 
                },

            };
            var link300_320_1 = BuildLink(300300, 300, 320, false, false, platforms: p300320_1, stationRight: true, theDescription: "Link 1 between 300 and 320", edgeName: "E_PT12_PT1_MAN", edgeId:"258784");
            var link300_320_2 = BuildLink(300310, 300, 320, false, false, platforms: p300320_2, stationRight: true, theDescription: "Link 2 between 300 and 320", edgeName: "E1_PT10_PT5_MAN", edgeId:"258833");
            var link300_320_3 = BuildLink(300320, 300, 320, false, false, platforms: p300320_3, stationRight: true, theDescription: "Link 3 between 300 and 320", edgeName: "E2_PT10_PT5_MAN", edgeId:"258836");
            rightLinks300.Add(link300_320_1);
            rightLinks300.Add(link300_320_2);
            rightLinks300.Add(link300_320_3);

            #endregion End Out RightLinks

            var node300 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 300,
                MyLeftLinks = leftLinks300,
                MyRightLinks = rightLinks300
            };
            station.MyNodes.Add(node300);

            #endregion End Node 300

            #region Node 320

            #region In LeftLinks - 100 series

            var leftLinks320 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks320 = new List<Link>();
            var link320_40_1 = BuildLink(320400, 320, 400, false, false, stationRight: true, theDescription: "Link 1 from 320 to 400", edgeName: "E_PT1_MAN_PT4_ZIE", edgeId:"258839");
            var link320_40_2 = BuildLink(320410, 320, 400, false, false, stationRight: true, theDescription: "Link 2 from 320 to 400", edgeName: "E_PT3_MAN_PT2_ZIE", edgeId:"258840");
            rightLinks320.Add(link320_40_1);
            rightLinks320.Add(link320_40_2);

            #endregion End Out RightLinks



            var node320 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 320,
                MyLeftLinks = leftLinks320,
                MyRightLinks = rightLinks320
            };
            station.MyNodes.Add(node320);

            #region Paths 300

            var paths300 = new List<Path>();

            var pPath300_1 = BuildPath("300900", "30", "300", "220300", "300360", "SIPP_MAN-SHM22_MAN", "PP", "N10", "R");
            var pPath300_2 = BuildPath("300901", "30", "300", "220300", "300370", "SIPP_MAN-SHM28_MAN", "PP", "N6", "R");
            var pPath300_3 = BuildPath("300902", "30", "300", "220300", "300300", "SIPP_MAN-SHM5_MAN", "PP", "N1", "R", false);
            var pPath300_4 = BuildPath("300903", "30", "300", "220300", "300350", "SIPP_MAN-SHM50_MAN", "PP", "N9", "R");
            var pPath300_5 = BuildPath("300904", "30", "300", "220300", "300340", "SIPP_MAN-SHM54_MAN", "PP", "N8", "R");
            var pPath300_6 = BuildPath("300905", "30", "300", "220300", "300330", "SIPP_MAN-SHM66_MAN", "PP", "N7", "R");
            var pPath300_7 = BuildPath("300906", "30", "300", "220300", "300310", "SIPP_MAN-SIP2_MAN", "PP", "N2", "R");
            var pPath300_8 = BuildPath("300907", "30", "300", "220300", "300320", "SIPP_MAN-SIP3_MAN", "PP", "N3", "R");
            var pPath300_9 = BuildPath("300908", "30", "300", "220310", "300360", "SIP_MAN-SHM22_MAN", "P", "N10", "R");
            var pPath300_10 = BuildPath("300909", "30", "300", "220310", "300370", "SIP_MAN-SHM28_MAN", "P", "N6", "R");
            var pPath300_11 = BuildPath("300910", "30", "300", "220310", "300300", "SIP_MAN-SHM5_MAN", "P", "N1", "R");
            var pPath300_12 = BuildPath("300911", "30", "300", "220310", "300350", "SIP_MAN-SHM50_MAN", "P", "N9", "R");
            var pPath300_13 = BuildPath("300912", "30", "300", "220310", "300340", "SIP_MAN-SHM54_MAN", "P", "N8", "R");
            var pPath300_14 = BuildPath("300913", "30", "300", "220310", "300330", "SIP_MAN-SHM66_MAN", "P", "N7", "R");
            var pPath300_15 = BuildPath("300914", "30", "300", "220310", "300310", "SIP_MAN-SIP2_MAN", "P", "N2", "R", false);
            var pPath300_16 = BuildPath("300915", "30", "300", "220310", "300320", "SIP_MAN-SIP3_MAN", "P", "N3", "R");
            var pPath300_17 = BuildPath("300916", "30", "300", "220310", "300370", "SIN6_MAN-SIP_MAN", "N6", "P", "L");
            var pPath300_18 = BuildPath("300917", "30", "300", "220310", "300300", "SIN1_MAN-SIP_MAN", "N1", "P", "L");
            var pPath300_19 = BuildPath("300918", "30", "300", "220310", "300350", "SIN9_MAN-SIP_MAN", "N9", "P", "L");
            var pPath300_20 = BuildPath("300919", "30", "300", "220310", "300340", "SIN8_MAN-SIP_MAN", "N8", "P", "L");
            var pPath300_21 = BuildPath("300920", "30", "300", "220310", "300330", "SIN7_MAN-SIP_MAN", "N7", "P", "L");
            var pPath300_22 = BuildPath("300921", "30", "300", "220310", "300310", "SIN2_MAN-SIP_MAN", "N2", "P", "L", false);
            var pPath300_23 = BuildPath("300922", "30", "300", "220310", "300320", "SIN3_MAN-SIP_MAN", "N3", "P", "L");
            var pPath300_24 = BuildPath("300923", "30", "300", "220310", "300360", "SIN10_MAN-SIP_MAN", "N10", "P", "L");
            var pPath300_25 = BuildPath("300924", "30", "300", "220300", "300370", "SIN6_MAN-SIPP_MAN", "N6", "PP", "L");
            var pPath300_26 = BuildPath("300925", "30", "300", "220300", "300300", "SIN1_MAN-SIPP_MAN", "N1", "PP", "L", false);
            var pPath300_27 = BuildPath("300926", "30", "300", "220300", "300350", "SIN9_MAN-SIPP_MAN", "N9", "PP", "L");
            var pPath300_28 = BuildPath("300927", "30", "300", "220300", "300340", "SIN8_MAN-SIPP_MAN", "N8", "PP", "L");
            var pPath300_29 = BuildPath("300928", "30", "300", "220300", "300330", "SIN7_MAN-SIPP_MAN", "N7", "PP", "L");
            var pPath300_30 = BuildPath("300929", "30", "300", "220300", "300310", "SIN2_MAN-SIPP_MAN", "N2", "PP", "L");
            var pPath300_31 = BuildPath("300930", "30", "300", "220300", "300320", "SIN3_MAN-SIPP_MAN", "N3", "PP", "L");
            var pPath300_32 = BuildPath("300931", "30", "300", "220300", "300360", "SIN10_MAN-SIPP_MAN", "N10", "PP", "L");

            paths300.Add(pPath300_1);
            paths300.Add(pPath300_2);
            paths300.Add(pPath300_3);
            paths300.Add(pPath300_4);
            paths300.Add(pPath300_5);
            paths300.Add(pPath300_6);
            paths300.Add(pPath300_7);
            paths300.Add(pPath300_8);
            paths300.Add(pPath300_9);
            paths300.Add(pPath300_10);
            paths300.Add(pPath300_11);
            paths300.Add(pPath300_12);
            paths300.Add(pPath300_13);
            paths300.Add(pPath300_14);
            paths300.Add(pPath300_15);
            paths300.Add(pPath300_16);
            paths300.Add(pPath300_17);
            paths300.Add(pPath300_18);
            paths300.Add(pPath300_19);
            paths300.Add(pPath300_20);
            paths300.Add(pPath300_21);
            paths300.Add(pPath300_22);
            paths300.Add(pPath300_23);
            paths300.Add(pPath300_24);
            paths300.Add(pPath300_25);
            paths300.Add(pPath300_26);
            paths300.Add(pPath300_27);
            paths300.Add(pPath300_28);
            paths300.Add(pPath300_29);
            paths300.Add(pPath300_30);
            paths300.Add(pPath300_31);
            paths300.Add(pPath300_32);
            node300.MyPaths = paths300;

            #endregion End Paths 300

            #endregion End Node 320

            #region 320 paths
            var paths320 = new List<Path>();
            var pPath320_1 = BuildPath("320900", "30", "320", "300300", "320400", "SIP1_MAN-SIN_MAN", "P1", "N", "R", false);
            var pPath320_2 = BuildPath("320902", "30", "320", "300310", "320400", "SIP2_MAN-SINP_MAN", "P2", "NP", "R", false);
            var pPath320_3 = BuildPath("320903", "30", "320", "300310", "320410", "SIP2_MAN-IN_MAN", "P2", "N", "R");
            var pPath320_4 = BuildPath("320904", "30", "320", "300320", "320400", "SIP3_MAN-SIN_MAN", "P3", "N", "R");
            var pPath320_5 = BuildPath("320905", "30", "320", "300320", "320410", "SIP3_MAN-SINP_MAN", "P3", "NP", "R");
            var pPath320_6 = BuildPath("320906", "30", "320", "300300", "320400", "SIN_MAN-SIN1_MAN", "N", "N1", "L", false);
            var pPath320_7 = BuildPath("320907", "30", "320", "300310", "320400", "SIN_MAN-SIN2_MAN", "N", "N2", "L");
            var pPath320_8 = BuildPath("320908", "30", "320", "300320", "320400", "SIN_MAN-SIN3_MAN", "N", "N3", "L");
            var pPath320_9 = BuildPath("320909", "30", "320", "300310", "320410", "SINP_MAN-SIN2_MAN", "NP", "N2", "L", false);
            var pPath320_10 = BuildPath("320910", "30", "320", "300320", "320410", "SINP_MAN-SIN3_MAN", "NP", "N3", "L");
            paths320.Add(pPath320_1);
            paths320.Add(pPath320_2);
            paths320.Add(pPath320_3);
            paths320.Add(pPath320_4);
            paths320.Add(pPath320_5);
            paths320.Add(pPath320_6);
            paths320.Add(pPath320_7);
            paths320.Add(pPath320_8);
            paths320.Add(pPath320_9);
            paths320.Add(pPath320_10);
            node320.MyPaths = paths320;

            #endregion
        }

        [Description("400")]
        private void Ziemelblazma()
        {
            var station = new Station
            {
                MyReferenceNumber = 40,
                StationName = "Ziemelblazma",
                Abbreviation = "ZIE"
            };
            StationList.Add(station);

            var paths = new List<Path>();

            #region Platforms

            #region Platform Between 400320 on a path
            var p400320_1 = new List<Platform> { BuildPlatform("P3AC", station.Abbreviation, station.MyReferenceNumber), BuildPlatform("P3BC", station.Abbreviation, station.MyReferenceNumber) };
            #endregion End  400320

            #region Platform Between 400420
            var p400420_1 = new List<Platform> { BuildPlatform("P1AC", station.Abbreviation, station.MyReferenceNumber), BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber) };
            var p400420_2 = new List<Platform> { BuildPlatform("P2AC", station.Abbreviation, station.MyReferenceNumber), BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber) };
            #endregion End  400420

            #region Platform right of 420000
            var p420320_1 = new List<Platform> { BuildPlatform("P9C", station.Abbreviation, station.MyReferenceNumber), };
            var p420310_1 = new List<Platform> { BuildPlatform("P7C", station.Abbreviation, station.MyReferenceNumber), };
            var p420300_1 = new List<Platform> { BuildPlatform("P5C", station.Abbreviation, station.MyReferenceNumber), };
            #endregion End  400422

            #region Platform Between 420000 on a PATH
            var p420000_1 = new List<Platform> { BuildPlatform("P3CC", station.Abbreviation, station.MyReferenceNumber), };
            #endregion End  420000

            #endregion End Platforms

            #region Node 400

            #region In LeftLinks - 100 series

            var leftLinks400 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks400 = new List<Link>();
            var link400300 = BuildLink(400300, 400, 420, false, false, platforms: p400420_1, theDescription: "Link 1 between 400 and 420"
                , edgeName: "E_PT6_PT10_ZIE", edgeId:"258847");
            var link400310 = BuildLink(400310, 400, 420, false, false, platforms: p400420_2, theDescription: "Link 2 between 400 and 420"
                , edgeName: "E_PT2_PT5_ZIE", edgeId:"258842");
            var link400320 = BuildLink(400320, 400, 422, false, false, platforms: p400320_1, theDescription: "Link 3 between 400 and 422"
                , edgeName: "E_PT6_PT7_ZIE", edgeId:"258848");
            rightLinks400.Add(link400300);
            rightLinks400.Add(link400310);
            rightLinks400.Add(link400320);

            #endregion End Out RightLinks

            var node400 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 400,
                MyLeftLinks = leftLinks400,
                MyRightLinks = rightLinks400
            };
            station.MyNodes.Add(node400);

            #region Paths 400
            var paths400 = new List<Path>();
            var pPath400_1 = BuildPath("400900", "40", "400", "320400", "400300", "SIPP_ZIE-SIP1_ZIE", "PP", "N1", "R", false);
            var pPath400_2 = BuildPath("400901", "40", "400", "320400", "400320", "SIPP_ZIE-SIPM3A_ZIE", "PP", "PM3A", "R");
            var pPath400_3 = BuildPath("400902", "40", "400", "320410", "400310", "SIP_ZIE-SIP2_ZIE", "P", "N2", "R", false);
            var pPath400_4 = BuildPath("400903", "40", "400", "320410", "400300", "SIP_ZIE-SIP2_ZIE", "P", "N1", "R");
            var pPath400_5 = BuildPath("400904", "40", "400", "320410", "400320", "SIP_ZIE-SIPM3A_ZIE", "P", "PM3A", "R");
            var pPath400_6 = BuildPath("400905", "40", "400", "320400", "400300", "SIN1_ZIE-SIPP_ZIE", "N1", "PP", "L", false);
            var pPath400_7 = BuildPath("400906", "40", "400", "320410", "400300", "SIN1_ZIE-SIP_ZIE", "N1", "P", "L");
            var pPath400_8 = BuildPath("400907", "40", "400", "320410", "400310", "SIN2_ZIE-SIP_ZIE", "N2", "P", "L", false);
            var pPath400_9 = BuildPath("400908", "40", "400", "320400", "400320", "SIN3_ZIE-SIPP_ZIE", "N3", "PP", "L");
            var pPath400_10 = BuildPath("400909", "40", "400", "320410", "400320", "SIN3_ZIE-SIP_ZIE", "N3", "P", "L");
            paths400.Add(pPath400_1);
            paths400.Add(pPath400_2);
            paths400.Add(pPath400_3);
            paths400.Add(pPath400_4);
            paths400.Add(pPath400_5);
            paths400.Add(pPath400_6);
            paths400.Add(pPath400_7);
            paths400.Add(pPath400_8);
            paths400.Add(pPath400_9);
            paths400.Add(pPath400_10);
            node400.MyPaths = paths400;
            #endregion

            #endregion End Node 400


            #region Node 420

            #region In LeftLinks - 100 series

            var leftLinks420 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series
            var rightLinks420 = new List<Link>
            {
                {BuildLink(420500, 420, 500, false, false,theDescription: "Link 1 between 420 and 500"  , edgeName: "E_PT1_MAN_PT4_ZIE", edgeId:"258839") },
                {BuildLink(420510, 420, 500, false, false,theDescription: "Link 2 between 420 and 500"  , edgeName: "E_PT3_MAN_PT2_ZIE", edgeId:"258840" ) },
                {BuildLink(420300, 0, 0, false, false, platforms: p420300_1,theDescription: "Link 3 between 420 and Platform 5C", edgeName:"E_PT3_PT17_ZIE", edgeId:"258858") }, 
                {BuildLink(420310, 0, 400, false, false, platforms: p420310_1, theDescription: "Link 4 between 420 and Platform 7C", edgeName:"E_PT19_PT17_ZIE", edgeId:"258857") }, 
                {BuildLink(420320, 0, 400, false, false, platforms: p420320_1, theDescription: "Link 5 between 420 and Platform 9C", edgeName:"E_PT21_PT15_ZIE", edgeId:"258861") }
            };

            #endregion End Out RightLinks

            var node420 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 420,
                MyLeftLinks = leftLinks420,
                MyRightLinks = rightLinks420
            };
            station.MyNodes.Add(node420);

            var paths420 = new List<Path>();
            //var pPath420_1 = BuildPath("400910", "40", "420", "400320", "400911", "SIPM3A_ZIE-SIP3_ZIE", "PM3A", "M20", "R");
            //var pPath420_2 = BuildPath("400911", "40", "420", "400910", "420300", "SIP3_ZIE-SHM13_ZIE", "P3", "NM5", "R");

            var pPath420_2 = BuildPath("400911", "40", "420", "400320", "420310", "SIPM3A_ZIE-SHM11_ZIE", "PM3A", "NM7", "L", true);

            var pPath420_3 = BuildPath("400912", "40", "420", "400320", "420310", "SIPM3A_ZIE-SHM11_ZIE", "PM3A", "NM7", "R");
            var pPath420_4 = BuildPath("400913", "40", "420", "400320", "420320", "SIPM3A_ZIE-SHM9_ZIE", "PM3A", "NM9", "R");
            var pPath420_5 = BuildPath("400914", "40", "420", "400320", "420310", "SINM7_ZIE-SIN3_ZIE", "NM7", "PM3A", "L");
            var pPath420_6 = BuildPath("400915", "40", "420", "400320", "420320", "SINM9_ZIE-SIN3_ZIE", "NM9", "PM3A", "L");
            var pPath420_7 = BuildPath("400916", "40", "420", "400300", "420510", "SIP1_ZIE-SINP_ZIE", "P1", "NP", "R");
            var pPath420_8 = BuildPath("400917", "40", "420", "400310", "420510", "SIP2_ZIE-SINP_ZIE", "P2", "NP", "R");
            var pPath420_9 = BuildPath("400918", "40", "420", "400300", "420500", "SIP1_ZIE-SIN_ZIE", "N", "P1", "L");
            //paths420.Add(pPath420_1);
            paths420.Add(pPath420_2);

            paths420.Add(pPath420_3);
            paths420.Add(pPath420_4);
            paths420.Add(pPath420_5);
            paths420.Add(pPath420_6);
            paths420.Add(pPath420_7);
            paths420.Add(pPath420_8);
            paths420.Add(pPath420_9);
            node420.MyPaths = paths420;

            #endregion End Node 420

        }

        [Description("500")]
        private void Vecaki()
        {
            var station = new Station
            {
                MyReferenceNumber = 50,
                StationName = "Vecaki",
                Abbreviation = "VEC"
            };
            var paths = new List<Path>();
            StationList.Add(station);

            #region Platforms

            #region Platform Between 500520
            var p500520_1 = new List<Platform> { BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber), };
            var p500520_2 = new List<Platform> { BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber), };
            var p500520_3 = new List<Platform> { BuildPlatform("P3C", station.Abbreviation, station.MyReferenceNumber), };
            #endregion End  300-320
            #endregion

            #region Node 500

            #region In LeftLinks - 100 series

            var leftLinks500 = new List<Link>();
            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks500 = new List<Link>
            {
                { BuildLink(500300, 500, 520, false, true, platforms:p500520_1,
                    theDescription:"Link 1 between 500 and 520"  , edgeName: "E_PT8_PT3_VEC", edgeId:"2123") },
                { BuildLink(500310, 500, 520, false, true, platforms:p500520_2,
                    theDescription:"Link 2 between 500 and 520"  , edgeName: "E_PT10_PT5_VEC", edgeId:"2127") },
                { BuildLink(500320, 500, 520, false, true, platforms:p500520_3,
                    theDescription : "Link 3 between 500 and 520"  , edgeName: "E_PT6_PT1_VEC", edgeId:"2134") },
            };
            #endregion End Out RightLinks

            var node500 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 500,
                MyLeftLinks = leftLinks500,
                MyRightLinks = rightLinks500
            };
            station.MyNodes.Add(node500);

            var paths500 = new List<Path>();
            var pPath500_1 = BuildPath("500900", "50", "500", "420500", "500300", "SIPP_VEC-SIP2_VEC", "PP", "N2", "R");
            var pPath500_2 = BuildPath("500901", "50", "500", "420510", "500320", "SIP_VEC-SIP3_VEC", "P", "P3", "R", false);
            var pPath500_3 = BuildPath("500902", "50", "500", "420510", "500310", "SIP_VEC-SIP2_VEC", "P", "N2", "R");
            var pPath500_4 = BuildPath("500903", "50", "500", "420500", "500300", "SIN1_VEC-SIPP_VEC", "N1", "PP", "L", false);
            var pPath500_5 = BuildPath("500904", "50", "500", "420500", "500310", "SIN2_VEC-SIPP_VEC", "N2", "PP", "L");
            var pPath500_6 = BuildPath("500905", "50", "500", "500310", "500310", "SIN2_VEC-SIP_VEC", "N2", "P", "L");
            paths500.Add(pPath500_1);
            paths500.Add(pPath500_2);
            paths500.Add(pPath500_3);
            paths500.Add(pPath500_4);
            paths500.Add(pPath500_5);
            paths500.Add(pPath500_6);
            node500.MyPaths = paths500;

            #endregion End Node 500

            #region Node 520

            #region In LeftLinks - 100 series

            //var leftLinks520 = new List<Link>
            //{
            //    { BuildLink(520100, 500, 0, false, true, platforms:p500520_1) },
            //    { BuildLink(520110, 500, 0, false, true, platforms:p500520_2) },
            //    { BuildLink(520120, 500, 0, false, true, platforms:p500520_3) },
            //};
            var leftLinks520 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks520 = new List<Link>
            {
                { BuildLink(520300, 520, 600, false, true,stationRight:true,
                    theDescription:"Link 1 between 520 and 600"  , edgeName: "E_PT3_VEC_PT4_CAR", edgeId:"2124" ) },
                { BuildLink(520310, 520, 600, false, true,stationRight:true,
                    theDescription:"Link 2 between 520 and 600"  , edgeName: "E_PT9_VEC_PT2_CAR", edgeId:"2130" ) },
            };
            #endregion End Out RightLinks

            var node520 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 520,
                MyLeftLinks = leftLinks520,
                MyRightLinks = rightLinks520
            };
            station.MyNodes.Add(node520);

            var paths520 = new List<Path>();
            var pPath520_7 = BuildPath("500906", "50", "520", "500310", "520310", "SIP2_VEC-SI10_VEC", "P2", "M1", "R", false);
            var pPath520_8 = BuildPath("500907", "50", "520", "500320", "520310", "SIP3_VEC-SI10_VEC", "P3", "M1", "R");
            var pPath520_9 = BuildPath("500908", "50", "520", "500300", "520300", "SIN_VEC-SIN1_VEC", "N", "N1", "L", false);
            var pPath520_10 = BuildPath("500909", "50", "520", "500310", "520300", "SIN_VEC-SIN2_VEC", "N", "P2", "L");
            paths520.Add(pPath520_7);
            paths520.Add(pPath520_8);
            paths520.Add(pPath520_9);
            paths520.Add(pPath520_10);
            node520.MyPaths = paths520;

            #endregion End Node 520

        }

        [Description("600")]
        private void Carnikava()
        {
            var station = new Station
            {
                MyReferenceNumber = 60,
                StationName = "Carnikava",
                Abbreviation = "CAR"
            };
            var paths = new List<Path>();
            StationList.Add(station);

            #region Platforms

            #region Platform Between 600620
            var p600620_1 = new List<Platform> { BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber) };
            var p600620_2 = new List<Platform> { BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber) };

            #endregion End  300-320
            #region Platform Between 600000
            var p600000_1 = new List<Platform> { BuildPlatform("P3C", station.Abbreviation, station.MyReferenceNumber) };
            #endregion End  300-320

            #endregion

            #region Node 600

            #region In LeftLinks - 100 series
            var leftLinks = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks = new List<Link>
            {
                { BuildLink(600300, 600, 620, false, false, platforms:p600620_1,
                    theDescription:"Link 1 between 600 and 620"  , edgeName: "E_PT6_PT1_CAR", edgeId:"2140" ) },
                { BuildLink(600310, 600, 620, false, false, platforms:p600620_2,
                    theDescription:"Link 2 between 600 and 620"  , edgeName: "E_PT2_PT1_CAR", edgeId:"2145" ) },
                { BuildLink(600320, 600, 0, false, false, platforms:p600000_1, theDescription : "Link 3 between 600 and Platform P3C", edgeId:"2137",edgeName:"E_PT6_RIGHT_CAR") },
            };
            #endregion End Out RightLinks

            var node600 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 600,
                MyLeftLinks = leftLinks,
                MyRightLinks = rightLinks
            };
            station.MyNodes.Add(node600);

            var paths600 = new List<Path>();
            var pPath600_1 = BuildPath("600900", "60", "600", "520310", "600310", "SIP_CAR-SIP2_CAR", "P", "P2", "R", false);
            var pPath600_2 = BuildPath("600901", "60", "600", "520310", "600300", "SIP_CAR-SIP1_CAR", "P", "N1", "R");
            var pPath600_3 = BuildPath("600902", "60", "600", "520310", "600320", "SIP_CAR-SIN3_CAR", "P", "N3", "R");
            var pPath600_4 = BuildPath("600903", "60", "600", "520300", "600300", "SIN1_CAR-SI9_CAR", "N1", "N", "L", false);
            var pPath600_5 = BuildPath("600904", "60", "600", "520300", "600320", "SIN3_CAR-SI9_CAR", "N3", "N", "L");
            paths600.Add(pPath600_1);
            paths600.Add(pPath600_2);
            paths600.Add(pPath600_3);
            paths600.Add(pPath600_4);
            paths600.Add(pPath600_5);
            node600.MyPaths = paths600;


            #endregion End Node 600

            #region Node 620
            #region Platform on 620300
            var p620300_1 = new List<Platform> { BuildPlatform("P1", "GAU", station.MyReferenceNumber), };
            #endregion
            #region Platform on 620310
            var p620310_1 = new List<Platform> { BuildPlatform("P2", "GAU", station.MyReferenceNumber), };
            #endregion


            #region In LeftLinks - 100 series
            var leftLinks620 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks620 = new List<Link>
            {
                { BuildLink(620300, 620, 800, false, false, platforms:p620300_1,stationRight:true,
                    theDescription:"Link 1 between 620 and 800"  , edgeName: "E_PT7_CAR_PT4_LIL", edgeId:"2143") },
                { BuildLink(620310, 620, 800, false, false, platforms:p620310_1,stationRight:true,
                    theDescription:"Link 2 between 620 and 800"  , edgeName: "E_PT5_5S_CAR_PT2_LIL", edgeId:"2146") },
            };
            #endregion End Out RightLinks

            var node620 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 620,
                MyLeftLinks = leftLinks620,
                MyRightLinks = rightLinks620
            };
            station.MyNodes.Add(node620);


            #endregion End Node 620
            var paths620 = new List<Path>();
            var pPath620_1 = BuildPath("620900", "60", "620", "600310", "620310", "SIP2_CAR-SI4_CAR", "P2", "P", "R", false);
            var pPath620_2 = BuildPath("620901", "60", "620", "600300", "620310", "SIP1_CAR-SI4_CAR", "P1", "P", "R");
            var pPath620_3 = BuildPath("620902", "60", "620", "600300", "620300", "SIN_CAR-SIN1_CAR", "N", "N1", "L", false);
            paths620.Add(pPath620_1);
            paths620.Add(pPath620_2);
            paths620.Add(pPath620_3);
            node620.MyPaths = paths620;
        }

        [Description("800")]
        private void Lilaste()
        {
            var station = new Station
            {
                MyReferenceNumber = 80,
                StationName = "Lilaste",
                Abbreviation = "LIL"
            };
            var paths = new List<Path>();
            StationList.Add(station);

            #region Platforms
            #region Platform Between 800820
            var p800820_1 = new List<Platform> { BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber), };
            var p800820_2 = new List<Platform> { BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber), };
            var p800820_3 = new List<Platform> { BuildPlatform("P3C", station.Abbreviation, station.MyReferenceNumber), };
            #endregion End 800820
            #endregion

            #region Node 800

            #region In LeftLinks - 100 series
            var leftLinks800 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks800 = new List<Link>
            {
                { BuildLink(800300, 800, 820, false, false, platforms:p800820_1, theDescription:"Link 1 between 800 and 820", edgeName: "E_PT6_PT1_LIL", edgeId:"2147") },
                { BuildLink(800310, 800, 820, false, false, platforms:p800820_2, theDescription : "Link 1 between 800 and 820",  edgeName: "E_PT8_PT5_LIL", edgeId:"2149") },
                { BuildLink(800320, 800, 820, false, false, platforms:p800820_3, theDescription : "Link 1 between 800 and 820",  edgeName: "E_PT10_PT5_LIL", edgeId:"2156") },
            };
            #endregion End Out RightLinks

            var node800 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 800,
                MyLeftLinks = leftLinks800,
                MyRightLinks = rightLinks800
            };
            station.MyNodes.Add(node800);

            var paths800 = new List<Path>();
            var pPath800_1 = BuildPath("800900", "80", "800", "520310", "800320", "SIP_LIL-SIP1_LIL", "P", "N1", "R", false);
            var pPath800_2 = BuildPath("800901", "80", "800", "520310", "800310", "SIP_LIL-SIP2_LIL", "P", "N2", "R");
            var pPath800_3 = BuildPath("800902", "80", "800", "520310", "800300", "SIP_LIL-SIP3_LIL", "P", "N3", "R");
            var pPath800_4 = BuildPath("800903", "80", "800", "520300", "800310", "SIN2_LIL-SI3_LIL", "N2", "3", "L", false);
            var pPath800_5 = BuildPath("800904", "80", "800", "520300", "800320", "SIN1_LIL-SI3_LIL", "N1", "3", "L");
            var pPath800_6 = BuildPath("800905", "80", "800", "520300", "800300", "SIN3_LIL-SI3_LIL", "N3", "3", "L");
            paths800.Add(pPath800_1);
            paths800.Add(pPath800_2);
            paths800.Add(pPath800_3);
            paths800.Add(pPath800_4);
            paths800.Add(pPath800_5);
            paths800.Add(pPath800_6);
            node800.MyPaths = paths800;



            #endregion End Node 800

            #region Node 820

            #region In LeftLinks - 100 series

            var leftLinks820 = new List<Link>();
            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks820 = new List<Link>
            {
                { BuildLink(820300, 820, 900, false, false,stationRight:true, theDescription:"Link 1 between 800 and 900", edgeName: "E_PT1_LIL_PT2A_INC",edgeId: "2152" ) },
            };
            #endregion End Out RightLinks

            var node820 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 820,
                MyLeftLinks = leftLinks820,
                MyRightLinks = rightLinks820
            };

            #endregion End Node 800
            station.MyNodes.Add(node820);

            var paths820 = new List<Path>();
            var pPath820_1 = BuildPath("800906", "80", "820", "800310", "820300", "SIP2_LIL-SIN_LIL", "P2", "N", "R", false);
            var pPath820_2 = BuildPath("800907", "80", "820", "800320", "820300", "SIP1_LIL-SIN_LIL", "P1", "N", "R");
            var pPath820_3 = BuildPath("800908", "80", "820", "800310", "820300", "SIP3_LIL-SIN_LIL", "P3", "N", "R");
            var pPath820_4 = BuildPath("800909", "80", "820", "800310", "820300", "SIN_LIL-SIN2_LIL", "N", "P2", "L", false);
            var pPath820_5 = BuildPath("800910", "80", "820", "800310", "820300", "SIN_LIL-SIN3_LIL", "N", "P3", "L");
            var pPath820_6 = BuildPath("800911", "80", "820", "800320", "820300", "SIN_LIL-SIN1_LIL", "N", "P1", "L");
            paths820.Add(pPath820_1);
            paths820.Add(pPath820_2);
            paths820.Add(pPath820_3);
            paths820.Add(pPath820_4);
            paths820.Add(pPath820_5);
            paths820.Add(pPath820_6);
            node820.MyPaths = paths820;

        }

        [Description("1000")]
        private void Saulkrasti()
        {
            var station = new Station
            {
                MyReferenceNumber = 100,
                StationName = "Saulkrasti",
                Abbreviation = "SAU"
            };
            var paths = new List<Path>();
            StationList.Add(station);


            #region Node 900

            #region In LeftLinks - 100 series

            var leftLinks900 = new List<Link>();
            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks900 = new List<Link>
            {
                { BuildLink(900300, 900, 1000, false, false,stationRight:true, theDescription:"Link 1 between 900 and 1000", edgeName:"E_PT2A_INC_PT2_SAU", edgeId:"2157") },
                { BuildLink(900310, 900, 1000, false, false,stationRight:true, theDescription : "Link 2 between 900 and 1000", edgeName:"E_PT2A_INC_PT6_SAU", edgeId:"2164") },
            };
            #endregion End Out RightLinks

            var node900 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 900,
                MyLeftLinks = leftLinks900,
                MyRightLinks = rightLinks900
            };
            station.MyNodes.Add(node900);

            var paths900 = new List<Path>();
            var pPath900_1 = BuildPath("900900", "100", "900", "820300", "900310", "SIPB_SAU-SI4_SAU", "PB", "4", "R", false);
            var pPath900_2 = BuildPath("900901", "100", "900", "820300", "900300", "SINB_SAU-SIPB_SAU", "NB", "PB", "L", false);
            paths900.Add(pPath900_1);
            paths900.Add(pPath900_2);
            node900.MyPaths = paths900;




            #endregion End Node 900

            #region Node 1000

            #region Platforms

            #region Platform Between 10001020
            var p10001020_1 = new List<Platform> { BuildPlatform("P1C", station.Abbreviation, station.MyReferenceNumber), };
            var p10001020_2 = new List<Platform> { BuildPlatform("P2C", station.Abbreviation, station.MyReferenceNumber), };
            var p10001020_3 = new List<Platform> { BuildPlatform("P3C", station.Abbreviation, station.MyReferenceNumber), };
            #endregion End 10001020
            #endregion End Platforms

            #region In LeftLinks - 100 series

            var leftLinks = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks = new List<Link>
            {
                { BuildLink(1000300, 1000, 1020, false, false, platforms:p10001020_1, theDescription:"Link 1 between 1000 and 1020", edgeName:"E_PT10_PT5_SAU", edgeId:"2160") },
                { BuildLink(1000310, 1000, 1020, false, false, platforms:p10001020_2, theDescription:"Link 1 between 1000 and 1020", edgeName:"E_PT12_PT5_SAU", edgeId:"2168") },
                { BuildLink(1000320, 1000, 1020, false, false, platforms:p10001020_3, theDescription : "Link 1 between 1000 and 1020", edgeName:"E_PT14_PT1_SAU", edgeId:"2171") },
            };
            #endregion End Out RightLinks

            var node1000 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 1000,
                MyLeftLinks = leftLinks,
                MyRightLinks = rightLinks
            };
            station.MyNodes.Add(node1000);

            var paths1000 = new List<Path>();
            var pPath1000_1 = BuildPath("1000900", "100", "1000", "900310", "1000310", "SIP_SAU-SIP2_SAU", "P", "N2", "R", false);
            var pPath1000_2 = BuildPath("1000901", "100", "1000", "900310", "1000320", "SIP_SAU-SIP4_SAU", "P", "N4", "R");
            var pPath1000_3 = BuildPath("1000902", "100", "1000", "900310", "1000300", "SIP_SAU-SIP1_SAU", "P", "N1", "R");
            var pPath1000_4 = BuildPath("1000903", "100", "1000", "900300", "1000300", "SIN1_SAU-SI3_SAU", "N1", "3", "L", false);
            var pPath1000_5 = BuildPath("1000904", "100", "1000", "900300", "1000310", "SIN2_SAU-SI3_SAU", "N2", "3", "L");
            var pPath1000_6 = BuildPath("1000905", "100", "1000", "900300", "1000310", "SIN2_SAU-SI3_SAU PT4", "N2", "3", "L");
            var pPath1000_7 = BuildPath("1000906", "100", "1000", "900300", "1000320", "SIN4_SAU-SI3_SAU", "N4", "3", "L");


            paths1000.Add(pPath1000_1);
            paths1000.Add(pPath1000_2);
            paths1000.Add(pPath1000_3);
            paths1000.Add(pPath1000_4);
            paths1000.Add(pPath1000_5);
            paths1000.Add(pPath1000_6);
            paths1000.Add(pPath1000_7);
            node1000.MyPaths = paths1000;

            #endregion End Node 1000

            #region Node 1020

            #region In LeftLinks - 100 series
            var leftLinks1020 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks1020 = new List<Link>
            {
                { BuildLink(1020300, 1020, 1100, false, false, stationRight:true, theDescription : "Link 1 between 1020 and 1100", edgeId:"2170", edgeName:"E_PT1_SAU_PT2_SKU") },
            };
            #endregion End Out RightLinks

            var node1020 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 1020,
                MyLeftLinks = leftLinks1020,
                MyRightLinks = rightLinks1020
            };
            station.MyNodes.Add(node1020);

            var paths1020 = new List<Path>();
            var pPath1020_1 = BuildPath("1020900", "100", "1020", "1000300", "1020300", "SIP1_SAU-SIN_SAU", "P1", "N", "R");
            var pPath1020_2 = BuildPath("1020901", "100", "1020", "1000310", "1020300", "SIP2_SAU-SIN_SAU", "P2", "N", "R", false);
            var pPath1020_3 = BuildPath("1020902", "100", "1020", "1000320", "1020300", "SIP4_SAU-SIN_SAU", "P4", "N", "R");
            var pPath1020_4 = BuildPath("1020903", "100", "1020", "1000300", "1020300", "SIN_SAU-SIN1_SAU", "N", "P1", "L");
            var pPath1020_5 = BuildPath("1020904", "100", "1020", "1000310", "1020300", "SIN_SAU-SIN2_SAU", "N", "P2", "L", false);
            var pPath1020_6 = BuildPath("1020905", "100", "1020", "1000320", "1020300", "SIN_SAU-SIN4_SAU", "N", "P4", "L");
            paths1020.Add(pPath1020_1);
            paths1020.Add(pPath1020_2);
            paths1020.Add(pPath1020_3);
            paths1020.Add(pPath1020_4);
            paths1020.Add(pPath1020_5);
            paths1020.Add(pPath1020_6);
            node1020.MyPaths = paths1020;
            //
            //
            //
            //
            //
            //

            #endregion End Node 1020
        }

        [Description("1100")]
        private void Skulte()
        {
            var station = new Station
            {
                MyReferenceNumber = 110,
                StationName = "Skulte",
                Abbreviation = "SKU"
            };
            var paths = new List<Path>();
            StationList.Add(station);

            #region Platforms

            #region Platform Between 11001120
            var p11001120_1 = new List<Platform> { BuildPlatform("P1C", "SKU", station.MyReferenceNumber), };
            var p11001120_2 = new List<Platform> { BuildPlatform("P2C", "SKU", station.MyReferenceNumber), };
            var p11001120_3 = new List<Platform> { BuildPlatform("P3C", "SKU", station.MyReferenceNumber), };

            #endregion End 11001120

            #endregion End Platforms

            #region Node 1100

            #region In LeftLinks - 100 series
            var leftLinks1100 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks1100 = new List<Link>
            {
                { BuildLink(1100300, 1100, 1120, false, false, platforms:p11001120_1, theDescription:"Link 1 between 1100 and 1120", edgeId:"2175", edgeName:"E_PT4_PT1_SKU") }, 
                { BuildLink(1100310, 1100, 1120, false, false, platforms:p11001120_2, theDescription:"Link 2 between 1100 and 1120", edgeId:"2177", edgeName:"E_PT2_PT3_SKU") },
                { BuildLink(1100320, 1100, 0, false, false, platforms:p11001120_3, theDescription:"Link 3 between 1100 and Platform P3C", edgeId:"2173", edgeName:"E_PT4_RIGHT_SKU") }, 

            };

            #endregion End Out RightLinks

            var node1100 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 1100,
                MyLeftLinks = leftLinks1100,
                MyRightLinks = rightLinks1100
            };
            station.MyNodes.Add(node1100);
            var paths1100 = new List<Path>();
            var pPath1100_1 = BuildPath("1100900", "110", "1100", "1020300", "1100300", "SIP_SKU-SIN1_SKU", "P", "N1", "R", false);
            var pPath1100_2 = BuildPath("1100901", "110", "1100", "1020300", "1100310", "SIP_SKU-SIN2_SKU", "P", "N2", "R");
            var pPath1100_3 = BuildPath("1100902", "110", "1100", "1020300", "1100320", "SIP_SKU-SIN3_SKU", "P", "N3", "R");
            var pPath1100_4 = BuildPath("1100903", "110", "1100", "1020300", "1100300", "SIN1_SKU-SIP_SKU", "N1", "P", "L", false);
            var pPath1100_5 = BuildPath("1100904", "110", "1100", "1020300", "1100310", "SIN2_SKU-SIP_SKU", "N2", "P", "L");
            var pPath1100_6 = BuildPath("1100905", "110", "1100", "1020300", "1100320", "SIN3_SKU-SIP_SKU", "N3", "P", "L");

            paths1100.Add(pPath1100_1);
            paths1100.Add(pPath1100_2);
            paths1100.Add(pPath1100_3);
            paths1100.Add(pPath1100_4);
            paths1100.Add(pPath1100_5);
            paths1100.Add(pPath1100_6);
            node1100.MyPaths = paths1100;

            #endregion End Node 1100

            #region Node 1120

            #region In LeftLinks - 100 series
            var leftLinks1120 = new List<Link>();

            #endregion End In LeftLinks

            #region Out RightLinks - 300 series

            var rightLinks1120 = new List<Link>
            {
                { BuildLink(1100300, 1100, 0, false, false, theDescription:"Link 1 between 1120 and END/START") },
            };

            #endregion End Out RightLinks

            var node1120 = new Node
            {
                MyStation = station,
                MyPaths = paths,
                MyReferenceNumber = 1120,
                MyLeftLinks = leftLinks1120,
                MyRightLinks = rightLinks1120
            };
            station.MyNodes.Add(node1120);

            #endregion End Node 1120

        }

        #region Non Existing Platforms

        //private void NonExistingPfs()
        //{

        //    var trks_P1BGP_ZEM = new List<Track>
        //    {
        //        new Track { TrackName = "TC1BGP_ZEM", MyReferenceNumber =259149 , MyLength =957500  }
        //    };
        //    var trks_P2BGP_ZEM = new List<Track>
        //    {
        //        new Track { TrackName = "TC2BGP_ZEM", MyReferenceNumber =259150 , MyLength =1045500  }
        //    };
        //    var trks_P3BGP_ZEM = new List<Track>
        //    {
        //        new Track { TrackName = "TC3BGP_ZEM", MyReferenceNumber =259151 , MyLength =1380000  }
        //    };
        //    var trks_PVO1_DAR = new List<Track>
        //    {
        //        new Track { TrackName = "VO1_DAR", MyReferenceNumber =1921 , MyLength =150000  }
        //    };
        //    var trks_PVO2_DAR = new List<Track>
        //    {
        //        new Track { TrackName = "VO2_DAR", MyReferenceNumber =1922 , MyLength =450000  }
        //    };
















        //}

        #endregion
    }
}