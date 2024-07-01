using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Enums;

namespace RailgraphLib.RailExtension
{
	using OBJID = UInt32;

	class SegmentExtension : ElementExtension
	{
		public SegmentExtension(int distanceFromStart, int distanceFromEnd, List<OBJID> elements,
							 EDirection eStartDir = EDirection.dNominal, EDirection eEndDir = EDirection.dNominal) :
				base(distanceFromStart, distanceFromEnd, elements, eStartDir, eEndDir)
		{
			addValidClassTypes();
		}

		public SegmentExtension() : base()
		{
			addValidClassTypes();
		}

		private void addValidClassTypes()
		{
			List<Enums.SYSOBJ_TYPE> validTypes = new List<Enums.SYSOBJ_TYPE>();
			validTypes.Add(SYSOBJ_TYPE.TYP_RATO_SEGMENT);
			validObjTypes(validTypes);
		}
	};

	//typedef ElementExtensionVector SegmentExtensionVector;
}