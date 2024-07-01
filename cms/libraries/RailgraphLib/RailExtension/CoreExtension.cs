using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.RailExtension
{
	using OBJID = UInt32;

	public class CoreExtension : ElementExtension
	{
		public
			CoreExtension(int distanceFromStart, int distanceFromEnd, List<OBJID> elements, Enums.EDirection eStartDir = Enums.EDirection.dNominal, Enums.EDirection eEndDir = Enums.EDirection.dNominal) :
					base(distanceFromStart, distanceFromEnd, elements, eStartDir, eEndDir)
		{
			addValidClassTypes();
		}

		public CoreExtension() : base()
		{
			addValidClassTypes();
		}

		private void addValidClassTypes()
		{
			List<Enums.SYSOBJ_TYPE> validTypes = new List<Enums.SYSOBJ_TYPE>();
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_VERTEX);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_EDGE);
			validObjTypes(validTypes);
		}
	}
}
