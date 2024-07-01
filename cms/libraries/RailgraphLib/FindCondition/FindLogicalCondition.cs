using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.FindCondition
{
	public class FindLogicalCondition : FindCondition
	{
		public enum EConditionalProceed
		{
			cpContinue, ///< target was not found and searching shall continue */
			cpFound,        ///< target was found and searching shall stop */
			cpFail
		};      ///< target was not found and searching shall stop */

		public FindLogicalCondition(UInt32 from, Enums.EDirection eDir) : base(from, eDir) { }

		public FindLogicalCondition(UInt32 from, UInt32 target, Enums.EDirection eSearchDir) : base(from, target, eSearchDir) { }

		public virtual EConditionalProceed isConditionFound(IReadOnlyList<UInt32> rPath)
		{
			if (rPath.Count > 0 && rPath.Last() == m_target)
				return EConditionalProceed.cpFound;

			return EConditionalProceed.cpContinue;
		}
	}
}
