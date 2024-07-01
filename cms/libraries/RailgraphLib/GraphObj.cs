using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public class GraphObj
	{
		private UInt32 m_coreId; // vertex or edge
		private int m_length;
		private int m_speedRestriction = 0;
		private int m_distance2NomVertex;
		private int m_distance2OppVertex;
		private int m_distanceFromInitPoint;
		private UInt32 m_externalIdentity;
		private string m_strOperName = "";
		private readonly UInt32 m_objId;
		private readonly UInt32 m_objType;
		private readonly Enums.CLASS_TYPE m_classType;
		private readonly string m_strName;
		private UInt64 m_dwlAuthorityMask;
		private Enums.EDirection m_eUsageDir;

		public GraphObj(UInt32 objId, UInt32 objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir = Enums.EDirection.dUnknown)
		{
			m_objId = objId;
			m_objType = objType;
			m_classType = classType;
			m_strName = objName;
		}

		public UInt32 getId() => m_objId;                                     ///< unique graph object identifier */
		public UInt32 getType() => m_objType;                             ///< object type */
		public Enums.CLASS_TYPE getClassType() => m_classType;                    ///< class type */
		public string getName() => m_strName ;                     ///< unique object name */
		public string getOperName() => m_strOperName;             ///< name to be "shown" for user */
		public UInt64 getAuthorityMask() => m_dwlAuthorityMask;      ///< authority mask */
		public int getDistanceToNomVertex() => m_distance2NomVertex;      ///< distance (millimeters) to nominal side vertex type object */
		public int getDistanceToOppVertex() => m_distance2OppVertex;      ///< distance (millimeters) to opposite side vertex type object */
		public int getDistanceFromInitPoint() => m_distanceFromInitPoint;    ///< distance (millimeters) from logical init point. Project specific */
		public int getSpeedRestriction() => m_speedRestriction;             ///< default speed restriction in km/h format */
		public UInt32 getCoreId() => m_coreId;                             ///< association to core object (edge- or vertex) */
		public UInt32 getExternalIdentity() => m_externalIdentity;           ///< possible object identifier used in external interface */
		public virtual int getLength() => m_length;                              ///< object length in millimeters */
		public virtual Enums.EDirection getUsageDir() => m_eUsageDir;    ///< typical usage direction conducted from associated core object  */
		public virtual UInt32 getLogicalAdj(Enums.EDirection eSearchDir, UInt32 previousId = UInt32.MaxValue) => 0;
		public virtual bool hasLogicalAdj(Enums.EDirection eSearchDir, UInt32 target) => false;
		public void setOperName(string strOpername) => m_strOperName = strOpername;
		public void setAuthorityMask(UInt64 AuthorityMask) => m_dwlAuthorityMask = AuthorityMask;
		public void setLength(int length) => m_length = length;
		public void setCoreId(UInt32 coreObjId) => m_coreId = coreObjId;
		public void setExternalIdentity(UInt32 externalIdentity) => m_externalIdentity = externalIdentity;
		public void setUsageDir(Enums.EDirection eUsageDir) => m_eUsageDir = eUsageDir;
		public void setDistanceToNomVertex(int distance2NomVertex) => m_distance2NomVertex = distance2NomVertex;
		public void setDistanceToOppVertex(int distance2OppVertex) => m_distance2OppVertex = distance2OppVertex;
		public void setDistanceFromInitPoint(int distanceFromInitPoint) => m_distanceFromInitPoint = distanceFromInitPoint;
	}
}
