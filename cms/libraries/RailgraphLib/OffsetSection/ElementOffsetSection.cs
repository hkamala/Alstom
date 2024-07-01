using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	using EBISYSID = UInt32;
	using OFFSETSECTIONS = SortedDictionary<UInt32, List<OffsetSection>>;

	public class ElementOffsetSection
	{
		public ElementOffsetSection() { }
		public static void addOffsetSection(EBISYSID elementId, OffsetSection offset_section)
		{
			if (!sOffsetSections.ContainsKey(elementId))
				sOffsetSections.Add(elementId, new List<OffsetSection>());
			
			sOffsetSections[elementId].Add(offset_section);
		}

		public static bool hasOffsetSections(EBISYSID elementId) => sOffsetSections.ContainsKey(elementId);

		public static bool convertElementOffsetToSectionValue(EBISYSID elementId, int offset, ref int value)
		{
			if (!sOffsetSections.ContainsKey(elementId))
				return false;

			foreach (var section in sOffsetSections[elementId])
			{
				if (section.IsOffsetInOffset(offset))
				{
					int x = offset - section.StartOffset;

					// Try to avoid unnecessary scaling and rounding errors
					if (section.OffsetLength() != section.OffsetSectionLength())
					{
						double scaling = (double)section.OffsetSectionLength() / (double)section.OffsetLength();
						x = asRoundedInt(x * scaling);
					}

					value = section.StartOffsetSection + section.OffsetSectionFactor() * x;
					return true;
				}
			}

			return false;
		}

		public static bool convertSectionValueToElementOffset(EBISYSID elementId, int value, ref int offset)
		{
			if (!sOffsetSections.ContainsKey(elementId))
				return false;

			
			OffsetSection prevOffsetSection = new OffsetSection();
			foreach (var section in sOffsetSections[elementId])
			{
				// In hole?
				if (prevOffsetSection.IsValid() && value > prevOffsetSection.EndOffsetSection&& value < section.StartOffsetSection)
				{
					offset = section.StartOffset;
					return true;
				}

				// In section? 
				if (section.IsOffsetInOffsetSection(value))
				{
					int x = value - section.StartOffsetSection;

					// Try to avoid unnecessary scaling and rounding errors
					if (section.OffsetLength() != section.OffsetSectionLength())
					{
						double scaling = (double)section.OffsetLength() / (double)section.OffsetSectionLength();
						x = asRoundedInt(x * scaling);
					}

					offset = section.StartOffset + section.OffsetSectionFactor() * x;
					return true;
				}

				prevOffsetSection = section;
			}

			return false;
		}

		protected static List<OffsetSection> getOffsetSections(EBISYSID elementId)
		{
			if (sOffsetSections.ContainsKey(elementId))
				return sOffsetSections[elementId];
			return new List<RailgraphLib.OffsetSection>();
		}

		protected static int asRoundedInt(double value) => (int)(value + (value < 0.0 ? -0.5 : 0.5));

		private static OFFSETSECTIONS sOffsetSections = new OFFSETSECTIONS();
	}
}
