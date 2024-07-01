using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.armd
{
	public struct SArmdObj
	{
		public bool m_bUseStaticBits = true;
		public UInt64 m_lMask = 0; // Pretmask
		public UInt64 m_lBits = 0; // IsEqual

		public SArmdObj(UInt64 lMask, UInt64 lBits, bool bUseStaticBits)
		{
			m_bUseStaticBits = bUseStaticBits;
			m_lMask = lMask;
			m_lBits = lBits;
		}

		public SArmdObj() { }
		
	};
}
