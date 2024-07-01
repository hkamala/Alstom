using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Core
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using COREOBJID = UInt32;

	public class CoreGraphObj : GraphObj
	{
		public CoreGraphObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName)
		{
		}

		public virtual COREOBJID getExternalCoreObjId() => getExternalIdentity();
	}
}
