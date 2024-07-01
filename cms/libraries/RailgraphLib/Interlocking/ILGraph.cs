using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib;
using RailgraphLib.armd;
using RailgraphLib.Enums;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using BINTYPE = UInt64;
	using OBJTYPE = UInt16;

	public class ILGraph : Graph
	{
		private const Enums.HT_TYPE ObjInInterlocking = Enums.HT_TYPE.HT_IL;
		private const Enums.HT_TYPE ObjInControlArea = Enums.HT_TYPE.HT_CONTROL_AREA;
		private const Enums.HT_TYPE ObjInLocicalStation = Enums.HT_TYPE.HT_SITE;
		private const Enums.HT_TYPE ObjInPlatform = Enums.HT_TYPE.HT_PLATFORM_TRACK;
		private const Enums.HT_TYPE InterlockingInCbr = Enums.HT_TYPE.HT_CBR;
		private const Enums.HT_TYPE TrackInZone = Enums.HT_TYPE.HT_ZONE_TRACKSECTION;
		private const Enums.HT_TYPE ETCSLevelOfObject = Enums.HT_TYPE.HT_ETCS_LEVEL;
		private const Enums.HT_TYPE ExternalOwnerOfObject = Enums.HT_TYPE.HT_EXTERNAL_OWNER;

		private SolidDB.CSolidEntryPoint m_solidEntryPoint;
		private static bool m_initialized;
		private SortedDictionary<OBJID, int> m_mapETCSLevels = new SortedDictionary<OBJID, int>();
		private SortedDictionary<OBJID, OBJID> m_mapCbrInInterlocking = new SortedDictionary<OBJID, OBJID>();
		private SortedDictionary<OBJID, OBJID> m_mapIdOfExternalOwner = new SortedDictionary<OBJID, OBJID>();

		public ILGraph(SolidDB.CSolidEntryPoint solidEntryPoint)
		{
			m_solidEntryPoint = solidEntryPoint;
		}

		public ILGraphObj getILGraphObj(OBJID ILGraphObjId) => getGraphObj(ILGraphObjId) as ILGraphObj;

		public OBJID getExternalOwnerSysIdByExternalOwnerId(int iExternalOwnerId) { return 0; }

		public static BINTYPE getDynamicBits(OBJID objId)
		{
			BINTYPE dynBits = ArmdPredefinedIf.getErrorValue();
			if (m_initialized)
			{
				// mfo todo: critical section ?
				bool bObjExist = SharedMemory.SharedMemory.Inst().GetDynBitsAsNumber(objId, ref dynBits);
				if (!bObjExist)
				{
					// already initialised to correct error value
				}
			}

			return dynBits;
		}

		public static BINTYPE getStaticBits(OBJID objId)
		{
			BINTYPE staBits = ArmdPredefinedIf.getErrorValue();
			if (m_initialized)
			{
				// mfo todo: critical section ?
				bool bObjExist = SharedMemory.SharedMemory.Inst().GetStaBits(objId, ref staBits);
				if (!bObjExist)
				{
					// already initialised to correct error value
				}
			}

			return staBits;
		}

		public static bool getMeasurement(OBJID objId, ref float real, ref BINTYPE value)
		{
			if (m_initialized)
			{
				// mfo todo: critical section ?
				return SharedMemory.SharedMemory.Inst().GetMeasurement(objId, ref real, ref value);
			}
			return false;
		}

		protected override void initialize(bool initSharedMemory)
		{
			if (!Armd.alreadyInitiated())
				Armd.init(m_solidEntryPoint);

			if (!m_initialized && initSharedMemory)
			{
				try
				{
					m_initialized = SharedMemory.SharedMemory.Inst().InitTable();
				}
				catch (Exception)
				{
					throw new Exception("ILGraph::initialize / Connection to cdh shared mem failed 1");
				}

				if (!m_initialized)
					throw new Exception("ILGraph::initialize / Connection to cdh shared mem failed 2");
			}

			if (!initSharedMemory)
				m_initialized = true;

			m_mapETCSLevels = m_solidEntryPoint.ReadETCLevels((int)ILGraphObjType.ETCS_LEVEL);
			m_mapCbrInInterlocking = m_solidEntryPoint.ReadCbrs((int)ILGraphObjType.INTERLOCKING, (int)InterlockingInCbr);
			m_mapIdOfExternalOwner = m_solidEntryPoint.ReadExternalOwners((int)ExternalOwnerOfObject);
		}

		protected override void shutdown() { }

		protected override GraphObj createGraphObj(OBJID objId, OBJTYPE objType, CLASS_TYPE classType, string name, EDirection eDir)
		{ 
			switch (classType)
			{
				case Enums.CLASS_TYPE.CLASS_TRACK: return createTrack(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_DARK_TRACK: return createDarkTrack(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_POINT_LEG: return createPointLeg(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_CROSSING_TS: return createCrossingTS(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_CROSSING: return createCrossing(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_POINT: return createPoint(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_DERAILER: return createDerailer(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_MAIN_SIGNAL: return createMainSignal(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_SHUNTING_SIGNAL: return createShuntingSignal(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_DEPARTING_SIGNAL: return createDepartSignal(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_FICTIVE_SIGNAL: return createFictiveSignal(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_COMBINED_SIGNAL: return createCombinedSignal(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_TRACK_CIRCUIT_BOUNDARY: return createTrackCircuitBoundary(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_BUFFER_STOP: return createBufferStop(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_LINE_BLOCK: return createLineBlock(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_CONTROL_OBJECT: return createControl(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_BALISE: return createBalise(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_ZONE: return createZone(objId, objType, classType, name, eDir);
				default: return null;
			}
		}

		protected virtual GraphObj createTrack(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new TrackSection(objId, objType, classType, name);
		protected virtual GraphObj createDarkTrack(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new DarkTrack(objId, objType, classType, name);
		protected virtual GraphObj createTrackSection(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new Track(objId, objType, classType, name);
		protected virtual GraphObj createMainSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new MainSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createShuntingSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new ShuntingSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createDepartSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new DepartSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createFictiveSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new FictiveSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createTrackCircuitBoundary(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => createFictiveSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createPoint(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new Point(objId, objType, classType, name);
		protected virtual GraphObj createPointLeg(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new PointLeg(objId, objType, classType, name);
		protected virtual GraphObj createDerailer(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new PointMachine(objId, objType, classType, name);
		protected virtual GraphObj createBufferStop(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new BufferStop(objId, objType, classType, name);
		protected virtual GraphObj createLineBlock(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new LineBlock(objId, objType, classType, name, eDir);
		protected virtual GraphObj createCrossingTS(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new CrossingTS(objId, objType, classType, name);
		protected virtual GraphObj createCrossing(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new Crossing(objId, objType, classType, name);
		protected virtual GraphObj createControl(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new FictiveSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createLevelCross(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new LevelCrossing(objId, objType, classType, name);
		protected virtual GraphObj createBalise(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new Balise(objId, objType, classType, name, eDir);
		protected virtual GraphObj createCombinedSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new MainSignal(objId, objType, classType, name, eDir);
		protected virtual GraphObj createZone(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir) => new Zone(objId, objType, classType, name);

		public override void commonGraphCreated() { }

		public override void interestedAssociation(ref List<Enums.HT_TYPE> associations)
		{
			associations.Add(ObjInInterlocking);
			associations.Add(ObjInControlArea);
			associations.Add(ObjInLocicalStation);
			associations.Add(ObjInPlatform);
			associations.Add(TrackInZone);
			associations.Add(ETCSLevelOfObject);
			associations.Add(ExternalOwnerOfObject);
		}

		public override void associationCreated(OBJID masterId, OBJID associationId, Enums.HT_TYPE associationType)
		{
			ILGraphObj iLGraphObj = getILGraphObj(associationId);
			if (iLGraphObj == null)
				return;

			switch (associationType)
			{
				case ObjInInterlocking:
					iLGraphObj.setInterlocking(masterId);
					if (m_mapCbrInInterlocking.ContainsKey(masterId))
						iLGraphObj.setCbr(m_mapCbrInInterlocking[masterId]);
					break;
				case ObjInLocicalStation:
					iLGraphObj.setLogicalStation(masterId);
					break;
				case ObjInPlatform:
					iLGraphObj.setPlatform(masterId);
					break;
				case ObjInControlArea:
					iLGraphObj.setControlArea(masterId);
					break;
				case TrackInZone:
					if (getILGraphObj(masterId) is Zone zone)
					{
						iLGraphObj.setZone(masterId);
						zone.addTrackId(associationId);
						break;
					}
					else
						return;
				case ETCSLevelOfObject:
					if (m_mapETCSLevels.ContainsKey(masterId))
						iLGraphObj.setETCSLevel(m_mapETCSLevels[masterId]);
					break;
				case ExternalOwnerOfObject:
					if (m_mapIdOfExternalOwner.ContainsKey(masterId))
						iLGraphObj.setExternalOwner(m_mapIdOfExternalOwner[masterId]);
					break;
				default: break;

			}
		}
	}
}
