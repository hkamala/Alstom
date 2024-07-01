using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.RailExtension
{
	using OBJID = UInt32;

	public class TrackExtension : ElementExtension
	{
		public TrackExtension(int distanceFromStart, int distanceFromEnd, List<OBJID> elements, Enums.EDirection eStartDir = Enums.EDirection.dNominal, Enums.EDirection eEndDir = Enums.EDirection.dNominal) :
					base(distanceFromStart, distanceFromEnd, elements, eStartDir, eEndDir)
		{
			addValidClassTypes();
		}

		public TrackExtension() : base()
		{
			addValidClassTypes();
		}

		private void addValidClassTypes()
		{
			List<Enums.SYSOBJ_TYPE> validTypes = new List<Enums.SYSOBJ_TYPE>();
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_TRACK);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_VIRTUAL_TRACK);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_POINT);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_POINT_LEG);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_DERAILER);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_DARK_TRACK);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_VIRTUAL_TRACK);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_CROSSING);
			validTypes.Add(Enums.SYSOBJ_TYPE.TYP_CROSSING_TS);
			validObjTypes(validTypes);
		}
	}
}
