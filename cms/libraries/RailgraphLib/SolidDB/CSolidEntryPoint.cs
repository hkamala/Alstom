using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;
using System.Data;
using System.Security.Cryptography;

namespace RailgraphLib.SolidDB
{
	public class CSolidEntryPoint : IDisposable
	{
		private OdbcConnection? m_solidConn = null;
		private int m_dbVersion;

		public static string ADJLEGNO = "ADJLEGNO";
		public static string ASSOCIATIONID = "ASSOCIATIONID";
		public static string ASSOCIATIONTYPE = "ASSOCIATIONTYPE";
		public static string AUTHMASK1 = "AUTHMASK1";
		public static string AUTHMASK2 = "AUTHMASK2";
		public static string BPID = "BPID";
		public static string CLASSTYPE = "CLASSTYPE";
		public static string COREOBJID = "COREOBJID";
		public static string COST = "COST";
		public static string DESCR = "DESCR";
		public static string DIRECTION = "DIRECTION";
		public static string DIRCHANGE = "DIRCHANGE";
		public static string DISTANCE1 = "DISTANCE1";
		public static string DISTANCE2 = "DISTANCE2";
		public static string DISTFROMINITPOINT = "DISTFROMINITPOINT";
		public static string ENDOFFSET = "ENDOFFSET";
		public static string ENDOFFSETSECTION = "ENDOFFSETSECTION";
		public static string EPID = "EPID";
		public static string ISEQUAL = "ISEQUAL";
		public static string JOINTID = "JOINTID";
		public static string LEGNO = "LEGNO";
		public static string LEN = "LEN";
		public static string MASTERID = "MASTERID";
		public static string MAXSPEED = "MAXSPEED";
		public static string NAMEPART = "NAMEPART";
		public static string OBJID = "OBJID";
		public static string OBJNO = "OBJNO";
		public static string OBJTYPE = "OBJTYPE";
		public static string OBJTYPENO = "OBJTYPENO";
		public static string OPERNAME = "OPERNAME";
		public static string OPERNAMEW = "OPERNAMEW";
		public static string PRETMASK = "PRETMASK";
		public static string RDIRE = "RDIRE";
		public static string SELECTOR = "SELECETOR";
		public static string SEQNO = "SEQNO";
		public static string STARTOFFSET = "STARTOFFSET";
		public static string STARTOFFSETSECTION = "STARTOFFSETSECTION";
		public static string STATICBITS = "STATICBITS";
		public static string SYSNAME = "SYSNAME";
		public static string USAGEDIR = "USAGEDIR";
		public static string STATIONID = "STATIONID";

		public bool Connect(string dns = "my-server", string user = "dba", string pass = "dba", int dbVersion = 1)
		{
			m_dbVersion = dbVersion;
			try
			{
				string conn_str = $"DSN={dns};UID={user};PWD={pass}";
				m_solidConn = new OdbcConnection(conn_str);
				m_solidConn.Open();
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

        public OdbcConnection? OdbcConnection { get => m_solidConn; }

        public bool IsConnected { get { return m_solidConn.State == ConnectionState.Open; } }

        public int DbVersion { get => m_dbVersion; }

        public void CloseConnection() => Dispose();

		public void Dispose()
		{
			try
			{
				m_solidConn?.Close();
			}
			finally
			{
				m_solidConn?.Dispose();
				m_solidConn = null;
			}
		}

		private string GetSafeString(OdbcDataReader reader, int colIndex, string defaultValue = "")
		{
			if (reader.IsDBNull(colIndex))
				return defaultValue;

			return reader.GetString(colIndex);
		}

		private string GetSafeString(OdbcDataReader reader, string colName, string defaultValue = "")
		{
			if (reader.IsDBNull(colName))
				return defaultValue;

			return reader.GetString(colName);
		}

		private int GetSafeInt(OdbcDataReader reader, int colIndex, int defaultValue = 0)
		{
			if (reader.IsDBNull(colIndex))
				return defaultValue;

			return reader.GetInt32(colIndex);
		}

		private int GetSafeInt(OdbcDataReader reader, string colName, int defaultValue = 0)
		{
			if (reader.IsDBNull(colName))
				return defaultValue;

			return reader.GetInt32(colName);
		}

		private long GetSafeInt64(OdbcDataReader reader, string colName, int defaultValue = 0)
		{
			if (reader.IsDBNull(colName))
				return defaultValue;

			return reader.GetInt64(colName);
		}

		private Int16 GetSafeInt16(OdbcDataReader reader, string colName, Int16 defaultValue = 0)
		{
			if (reader.IsDBNull(colName))
				return defaultValue;

			return reader.GetInt16(colName);

		}

		public SortedDictionary<uint, int> ReadETCLevels(int objTypeETCSLevel)
		{
			SortedDictionary<uint, int> retVal = new SortedDictionary<uint, int>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysid OBJID, objno ETCSLEVEL from sysobj where OBJTYPENO = {objTypeETCSLevel} and VERSIONNO = {m_dbVersion}";
					OdbcDataReader reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						int objID = reader.GetInt32(0);
						int etcsLevel = GetSafeInt(reader, 1, -1);
						retVal.Add((UInt32)objID, etcsLevel);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{
				
			}
			catch (Exception)
			{

			}

			return retVal;
		}

		public SortedDictionary<UInt32, UInt32> ReadCbrs(int objTypeInterlocking, int interlockingInCbr)
		{
			SortedDictionary<UInt32, UInt32> retVal = new SortedDictionary<UInt32, UInt32>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysobj.sysid INTERLOCKINGID, hierarchy.masterid CBRID from sysobj,hierarchy where OBJTYPENO = {objTypeInterlocking} and sysobj.sysid = hierarchy.sysid" +
						$" and sysobj.VERSIONNO = {m_dbVersion} and hierarchy.VERSIONNO = {m_dbVersion} and hierarchytype = {interlockingInCbr}";

					OdbcDataReader reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						int cbrId = reader.GetInt32(1);
						int interlockingId = reader.GetInt32(0);
						retVal.Add((UInt32)interlockingId, (UInt32)cbrId);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{

			}
			catch (Exception)
			{

			}

			return retVal;
		}

		public SortedDictionary<UInt32, UInt32> ReadExternalOwners(int objTypeExternalOwner)
		{
			SortedDictionary<UInt32, UInt32> retVal = new SortedDictionary<UInt32, UInt32>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysobj.sysid EXTERNALOWNERSYSID, sysobj.objno EXTERNALOWNERID from sysobj where OBJTYPENO = {objTypeExternalOwner} and VERSIONNO = {m_dbVersion}";

					OdbcDataReader reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						int externalOwnerSysId = reader.GetInt32(0);
						int externalOwnerId = reader.GetInt32(1);
						retVal.Add((UInt32)externalOwnerSysId, (UInt32)externalOwnerId);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{

			}
			catch (Exception)
			{

			}

			return retVal;
		}

		public List<SortedDictionary<string, object>> ReadCoreObjects(int graphScope, int objTypeVertex, int objTypeEdge, int typBoundaryEdge)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysobj.sysid OBJID, sysname, cast(opernamew as varchar(100)) opername, sysobj.objtypeno OBJTYPE, objno, objtype.classtype, " +
						$"hostsysid coreObjId, distance1, distance2, length LEN, distfrominitpoint, authmask1, authmask2, maxspeed, rdire, staticbits, usagedir" +
						$" from sysobj inner join objtype on sysobj.objtypeno = objtype.objtypeno and scope = {graphScope} and (classtype = {objTypeVertex}" +
						$" or objtype.classtype = {objTypeEdge} or objtype.classtype = {typBoundaryEdge}) and sysobj.versionno = {m_dbVersion}" +
						$" left outer join track on sysobj.sysid = track.sysid and scope = {graphScope} and sysobj.versionno = {m_dbVersion}";

					OdbcDataReader reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(OBJID, reader.GetInt32(OBJID));
						objs.Add(SYSNAME, reader.GetString(SYSNAME));
						objs.Add(OPERNAME, GetSafeString(reader, OPERNAME, "no opername"));
						objs.Add(OBJTYPE, GetSafeInt16(reader, OBJTYPE));
						objs.Add(OBJNO, GetSafeInt16(reader, OBJNO));
						objs.Add(CLASSTYPE, GetSafeInt(reader, CLASSTYPE));
						objs.Add(COREOBJID, GetSafeInt(reader, COREOBJID));
						objs.Add(DISTANCE1, GetSafeInt(reader, DISTANCE1));
						objs.Add(DISTANCE2, GetSafeInt(reader, DISTANCE2));
						objs.Add(LEN, GetSafeInt(reader, LEN));
						objs.Add(DISTFROMINITPOINT, GetSafeInt(reader, DISTFROMINITPOINT));
						objs.Add(AUTHMASK1, GetSafeInt(reader, AUTHMASK1));
						objs.Add(AUTHMASK2, GetSafeInt(reader, AUTHMASK2));
						objs.Add(MAXSPEED, GetSafeInt16(reader, MAXSPEED));
						objs.Add(RDIRE, GetSafeInt16(reader, RDIRE, -1));

						long staticBits = 0;
						staticBits = reader.GetInt64(STATICBITS);
						objs.Add(STATICBITS, staticBits);

						objs.Add(USAGEDIR, GetSafeInt(reader, USAGEDIR, -1));
						retVal.Add(objs);
					}

					reader.Close();
					return retVal;
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}
		}

		public SortedDictionary<int, SortedDictionary<string, int>> ReadOffsetSections()
		{
			SortedDictionary<int, SortedDictionary<string, int>> retVal = new SortedDictionary<int, SortedDictionary<string, int>>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysid OBJID, STARTOFFSETSECTION, ENDOFFSETSECTION, STARTOFFSET, ENDOFFSET from OFFSETSECTION where versionno = {m_dbVersion}";
					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, int> objs = new SortedDictionary<string, int>();

						objs.Add(OBJID, reader.GetInt32(OBJID));
						objs.Add(STARTOFFSETSECTION, reader.GetInt32(STARTOFFSETSECTION));
						objs.Add(ENDOFFSETSECTION, reader.GetInt32(ENDOFFSETSECTION));
						objs.Add(STARTOFFSET, reader.GetInt32(STARTOFFSET));
						objs.Add(ENDOFFSET, reader.GetInt32(ENDOFFSET));

						retVal.Add(reader.GetInt32(OBJID), objs);
					}

					reader.Close();
					return retVal;
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}
		}

		public List<SortedDictionary<string, object>> GetAdjacencies(List<Enums.HT_TYPE> adjacents)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = "select sysid MASTERID, adjsysid ASSOCIATIONID, jointid JOINTID, adjtype ASSOCIATIONTYPE, tcjoint DIRCHANGE, legno LEGNO, adjlegno ADJLEGNO, cost COST from adjacency where ";

					if (adjacents.Count > 0)
					{
						cmd.CommandText += "(";
						foreach (var adjacent in adjacents)
							cmd.CommandText += $" adjtype = {(int)adjacent} or ";

						cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 4) + ") and ";
					}

					cmd.CommandText += $"versionno = {m_dbVersion} order by jointid desc";

					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(MASTERID, reader.GetInt32(MASTERID));
						objs.Add(ASSOCIATIONID, reader.GetInt32(ASSOCIATIONID));
						objs.Add(JOINTID, GetSafeInt(reader, JOINTID));
						objs.Add(ASSOCIATIONTYPE, reader.GetInt32(ASSOCIATIONTYPE));
						objs.Add(LEGNO, reader.GetInt16(LEGNO));
						objs.Add(ADJLEGNO, GetSafeInt16(reader, ADJLEGNO));
						objs.Add(COST, GetSafeInt(reader, COST));
						objs.Add(DIRCHANGE, reader.GetInt16(DIRCHANGE));

						retVal.Add(objs);
					}

					reader.Close();
					return retVal;
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}
		}

		public List<SortedDictionary<string, object>> GetGraphObjects(Enums.SCOPE_TYPE graphScope)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select sysobj.sysid OBJID, sysname, cast(opernamew as varchar(100)) opername, sysobj.objtypeno, objno, objtype.classtype, hostsysid coreObjId, " +
						$"seqno, distance1, distance2, length LEN, distfrominitpoint, authmask1, authmask2, maxspeed, rdire, (length / 2 + distance1) as d1InMiddle " +
						$"from sysobj, track, objtype where sysobj.objtypeno = objtype.objtypeno and scope = {(int)graphScope} and sysobj.sysid = track.sysid " +
						$"and track.hostsysid > 0 and track.hostsysid is not null and sysobj.versionno = {m_dbVersion} order by hostsysid, d1InMiddle, distance2 DESC, length, seqno";

					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(OBJID, reader.GetInt32(OBJID));
						objs.Add(SYSNAME, reader.GetString(SYSNAME));
						objs.Add(OPERNAME, GetSafeString(reader, OPERNAME, "no opername"));
						objs.Add(OBJTYPENO, reader.GetInt32(OBJTYPENO));
						objs.Add(OBJNO, GetSafeInt16(reader, OBJNO));
						objs.Add(CLASSTYPE, GetSafeInt(reader, CLASSTYPE));
						objs.Add(COREOBJID, GetSafeInt(reader, COREOBJID));
						objs.Add(SEQNO, GetSafeInt(reader, SEQNO));
						objs.Add(DISTANCE1, GetSafeInt(reader, DISTANCE1));
						objs.Add(DISTANCE2, GetSafeInt(reader, DISTANCE2));
						objs.Add(LEN, GetSafeInt(reader, LEN));
						objs.Add(DISTFROMINITPOINT, GetSafeInt(reader, DISTFROMINITPOINT));
						objs.Add(AUTHMASK1, GetSafeInt(reader, AUTHMASK1));
						objs.Add(AUTHMASK2, GetSafeInt(reader, AUTHMASK2));
						objs.Add(MAXSPEED, GetSafeInt16(reader, MAXSPEED));
						objs.Add(RDIRE, GetSafeInt16(reader, RDIRE));

						retVal.Add(objs);
					}

					reader.Close();
					return retVal;
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}
		}

		public List<SortedDictionary<string, object>> GetHierarchies(List<Enums.HT_TYPE> hierarchyAssocs)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();

			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select h.masterId MASTERID, h.sysid ASSOCIATIONID, h.hierarchyType ASSOCIATIONTYPE, h.SEQNO SEQNO, s.SYSNAME SYSNAME from hierarchy h "
					+ "left join sysobj s on s.sysid = h.sysid "
					+ "where ";

					if (hierarchyAssocs.Count > 0)
					{
						cmd.CommandText += "(";
						foreach (var hierarchy in hierarchyAssocs)
							cmd.CommandText += $" adjtype = {hierarchy} or ";

						cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 4) + ") and ";
					}

					cmd.CommandText += $" h.versionno = {m_dbVersion}";

					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(MASTERID, reader.GetInt32(MASTERID));
						objs.Add(ASSOCIATIONID, reader.GetInt32(ASSOCIATIONID));
						objs.Add(ASSOCIATIONTYPE, reader.GetInt32(ASSOCIATIONTYPE));
						objs.Add(SYSNAME, reader.GetString(SYSNAME));
						objs.Add(SEQNO, GetSafeInt(reader, SEQNO));

						retVal.Add(objs);
					}

					reader.Close();
					return retVal;
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}
		}

		public List<SortedDictionary<string, object>> GetObjectsByType(int objType, int nametype)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();
			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select s.SYSID OBJID, s.OBJTYPENO OBJTYPENO, s.SYSNAME SYSNAME, n.NAMEPART NAMEPART from sysobj s ";
					cmd.CommandText += $"left join nameparts n on n.sysname = s.sysname and n.nametype = {nametype} ";
					cmd.CommandText += $" where s.objtypeno = {objType} and s.versionno = {m_dbVersion}";

					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(OBJID, reader.GetInt32(OBJID));
						objs.Add(OBJTYPENO, reader.GetInt32(OBJTYPENO));
						objs.Add(SYSNAME, reader.GetString(SYSNAME));
						objs.Add(NAMEPART,  GetSafeString(reader, NAMEPART));

						retVal.Add(objs);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}

			return retVal;

		}

		public List<SortedDictionary<string, object>> GetRoutes(int hierarchyTypeSite)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string, object>>();
			if (m_solidConn == null)
				return retVal;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					// route2.type = 0 -> single route
					// route2.type = 1 -> combined route (SUBROUTE2 table)
					cmd.CommandText = "select route2.sysid OBJID, route2.bpid BPID, route2.epid EPID, route2.direction DIRECTION, sysobj.sysname SYSNAME, hierarchy.masterid STATIONID from sysobj";
					cmd.CommandText += $" join route2 on route2.sysid = sysobj.sysid left join hierarchy on sysobj.sysid = hierarchy.sysid";
					cmd.CommandText += $" where route2.type = 0 and route2.versionno = {m_dbVersion} and hierarchy.hierarchytype = {hierarchyTypeSite} and hierarchy.versionno = {m_dbVersion}";
					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(OBJID, reader.GetInt32(OBJID));
						objs.Add(BPID, reader.GetInt32(BPID));
						objs.Add(EPID, reader.GetInt32(EPID));
						objs.Add(DIRECTION, reader.GetInt32(DIRECTION));
						objs.Add(SYSNAME, GetSafeString(reader, SYSNAME));
						objs.Add(STATIONID, reader.GetInt32(STATIONID));
						retVal.Add(objs);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}

			return retVal;
		}

		public List<SortedDictionary<string, object>> InitArmd(string armdCommand)
		{
			List<SortedDictionary<string, object>> retVal = new List<SortedDictionary<string,object>>();
			if (m_solidConn == null)
				return retVal;

			int armdSysid = 0;

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand()) 
				{
					cmd.CommandText = $"select sysid from sysobj where sysname = '{armdCommand}' and versionno = {m_dbVersion}";
					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						armdSysid = GetSafeInt(reader, "SYSID");
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}

			try
			{
				using (OdbcCommand cmd = m_solidConn.CreateCommand())
				{
					cmd.CommandText = $"select pretmask, isequal, selecetor, descr from pretests where sysid = {armdSysid} and versionno = {m_dbVersion}";
					OdbcDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						SortedDictionary<string, object> objs = new SortedDictionary<string, object>();

						objs.Add(PRETMASK, GetSafeInt64(reader, PRETMASK));
						objs.Add(ISEQUAL, GetSafeInt64(reader, ISEQUAL));
						objs.Add(SELECTOR, GetSafeInt(reader, SELECTOR, -1)); //is it ok?

						if (reader.IsDBNull(DESCR))
							throw new Exception("Armd.Init DESCR not defined in DB");

						objs.Add(DESCR, GetSafeString(reader, DESCR));

						retVal.Add(objs);
					}

					reader.Close();
				}
			}
			catch (OdbcException)
			{
				return retVal;
			}
			catch (Exception)
			{
				return retVal;
			}

			return retVal;
		}
	}
}
