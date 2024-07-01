using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public abstract class CoreObj
	{
		public CoreObj(UInt32 objId, UInt32 objType, Enums.CLASS_TYPE classType, string name)
		{
			m_id = objId;
			m_objType = objType;
			m_classType = classType;
			m_name = name;
		}

		public virtual UInt32 getId() => m_id;
		public virtual string getName() => m_name;
        public virtual Enums.CLASS_TYPE getClassType() => m_classType;
		public virtual Enums.EDirection getUsageDir() => m_eUsageDir;
		public virtual int getLength() => m_length;
		public virtual int getDistanceFromInitPoint() => m_distanceFromInitPoint;
		public virtual void setUsageDir(Enums.EDirection eDir) => m_eUsageDir = eDir;
		public virtual void setLength(int len) => m_length = len;
		public virtual void setDistanceFromInitPoint(int distanceFromInitPoint) => m_distanceFromInitPoint = distanceFromInitPoint;
		public abstract List<UInt32> getAssociatedObjects();
		public abstract void setAssociatedObjects(List<UInt32> objects);
		
		protected UInt32 m_id;
		protected UInt32 m_objType;
		protected Enums.CLASS_TYPE m_classType;
		protected string m_name;
		protected int m_length = 0;
		protected int m_distanceFromInitPoint = 0;
		protected Enums.EDirection m_eUsageDir = Enums.EDirection.dUnknown;
	}
}
