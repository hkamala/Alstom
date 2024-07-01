using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.FindCondition
{
	public class FindAllCondition : FindCondition
	{
		public static readonly int maxHitCountDefault = 5;

		public enum EConditionalProceed
		{
			cpContinue,                 ///< target was not found and searching shall continue */
			cpFound,                        ///< target was found and searching shall stop */
			cpFoundAndContinue, ///< target was found but more hits are wanted */
			cpBreak,                        ///< stop searching but keep existing results */
			cpFail                          ///< target was not found and searching shall stop */
		};

		public FindAllCondition(UInt32 from, Enums.EDirection eDir) : base(from, eDir) { }

		public FindAllCondition(UInt32 from, UInt32 target, Enums.EDirection eSearchDir) : base(from, target, eSearchDir) { }

		public void addViaElementOrdered(UInt32 viaElementId)
		{
			m_bOrderedVias = true;
			m_viaElements.Add(viaElementId);
		}

		public void addViaElementsOrdered(List<UInt32> rViaElements)
		{
			m_bOrderedVias = true;
			m_viaElements.AddRange(rViaElements);
		}

		public virtual EConditionalProceed isConditionFound(UInt32 current, UInt32 previous)
		{
			if (current == m_target)
			{
				if (++m_hitCount < (maxHitCountDefault + 1))
					return EConditionalProceed.cpFoundAndContinue;

				return EConditionalProceed.cpFound;
			}

			return EConditionalProceed.cpContinue;
		}
		
		private int m_hitCount;

	}
}
