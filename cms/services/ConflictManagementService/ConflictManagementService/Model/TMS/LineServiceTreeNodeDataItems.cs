using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    public enum UpdateReason
    {
        [XmlEnum("0")]
        Reply,
        [XmlEnum("1")]
        Created,
        [XmlEnum("2")]
        Modified,
        [XmlEnum("3")]
        Deleted,
        [XmlEnum("4")]
        DeActivated,
        [XmlEnum("5")]
        Reload
    }

    [XmlType("Line")]
    public class LineNodeDataItem
    {
        [XmlElement("Id")]
        public int id;
        [XmlElement("Name")]
        public string? name;
        [XmlElement("dc")]
        public int dayCode;

        //bad!!
        //public ServiceNodeDataList serviceNodeDataList;
        //public DutyNodeDataList dutyNodeDataList;
    }

    [XmlRoot("LineList")]
    public class LineList
    {
        [XmlElement("ReqID")]
        public uint ReqID;
        [XmlElement("dc")]
        public int dayCode;
        [XmlArray("Lines")]
        public List<LineNodeDataItem>? lines;
    }

    [XmlType("Service")]
    public class ServiceNodeDataItem
    {
        [XmlAttribute("serid")]
        public int serviceID;
        [XmlAttribute("Name")]
        public string? name;
        [XmlAttribute("StartSite")]
        public string? startSite;
        [XmlAttribute("EndSite")]
        public string? endSite;
        [XmlAttribute("AreaID")]
        public int areaID;

        [XmlAttribute("SSt")]
        public SchedulingState serviceStatus;
        [XmlAttribute("HasTr")]
        public bool hasConnectedTrain;
        //[XmlAttribute("CTId")]
        //public ulong currentTripID;
        //[XmlAttribute("CTNa")]
        //public string currentTripName;
        //[XmlAttribute("CTDe")]
        //public string currentTripDestination;
        [XmlAttribute("CTTy")]
        public uint tripTypeID;
        //[XmlAttribute("PCId")]
        //public uint plannedCrewID;
        //[XmlAttribute("CCId")]
        //public uint currentCrewID;
    }

    [XmlRoot("ServiceList")]
    public class ServiceNodeDataList
    {
        [XmlElement("ReqID")]
        public uint ReqID;
        [XmlElement("LiID")]
        public int lineId;
        [XmlElement("dc")]
        public int dayCode;
        [XmlElement("Deg")]
        public bool IsDegradedList;
        [XmlArray("Services")]
        public List<ServiceNodeDataItem>? services;
    }

    [XmlType("ScheduledDayList")]
    public class ScheduledDayList
    {
        [XmlElement("ReqID")]
        public uint ReqID;
        [XmlElement("UpdR")]
        public UpdateReason updateReason;
        [XmlArray("ScheduledDays")]
        public List<ScheduledDayItem>? scheduledDays;
    }
    [XmlType("SD")]
    public class ScheduledDayItem
    {
        public enum eDaySchedulingState
        {
            UNKNOWN,
            PLANNED_NORMAL,
            PLANNED_DEGRADED
        }

        public enum eArchivedState
        {
            UNKNOWN
        }

        public ScheduledDayItem()
        {
        }
        public static int CalculateDayCode(DateTime dt)
        {
            return dt.Year * 1000 + dt.DayOfYear + 1;
        }
        public ScheduledDayItem(DateTime dt)
        {
            scheduledDayCode = CalculateDayCode(dt);
            startYear = dt.Year;
            startMonth = dt.Month;
            startDay = dt.Day;
            isArchived = false;
            schedulingState = (int)eDaySchedulingState.PLANNED_NORMAL;
        }
        public eDaySchedulingState SchedulingState
        {
            get { return (eDaySchedulingState)schedulingState; }
            set { schedulingState = (int)value; }
        }

        public eArchivedState ArchivedState
        {
            get { return (eArchivedState)archivedState; }
            set { archivedState = (int)value; }
        }

        [XmlAttribute("SDC")]
        public int scheduledDayCode;
        [XmlAttribute("StY")]
        public int startYear;
        [XmlAttribute("StM")]
        public int startMonth;
        [XmlAttribute("StD")]
        public int startDay;
        [XmlAttribute("isAr")]
        public bool isArchived;
        [XmlAttribute("ScSt")]
        public int schedulingState;
        [XmlAttribute("ArSt")]
        public int archivedState;
        [XmlAttribute("isAc")]
        public bool isActive;
        [XmlAttribute("HAS")]
        public bool hasActiveService;
    }
}
