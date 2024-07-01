﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Interlocking
{ 
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	public class ShuntingSignal : SignalOptical
	{
		public ShuntingSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection aspectDir) : base(objId, objType, classType, objName, aspectDir) { }
	}
}
