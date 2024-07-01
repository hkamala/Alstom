using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	using EBISYSID = UInt32;

	public class KmOffsetSection : ElementOffsetSection
	{
		public KmOffsetSection() { }

		public static bool convertOffsetToKm(EBISYSID elementId, int offset, ref double km)
		{
			int value = 0;
			if (convertElementOffsetToSectionValue(elementId, offset, ref value))
			{
				// Round the value so that it survives km <-> mm conversions correctly
				km = roundDouble(value / 1000000.0);
				return true;
			}
			return false;
		}

		public static bool convertKmToOffset(EBISYSID elementId, double km, ref int offset)
		{
			// Round km so that it survives km <-> mm conversions correctly
			km = ((int)(km * 1000000.0)) / 1000000.0; // Remove possible previous rounding
			int value = asRoundedInt(km * 1000000.0);  // and round again
			return convertSectionValueToElementOffset(elementId, value, ref offset);
		}

		private static double roundDouble(double value)
		{
			double rounder = 0.000000299999999999999999;
			value += value < 0.0 ? -rounder : rounder;
			return value;
		}
	}
}
