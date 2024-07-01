using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.RailExtension
{
	using OBJID = UInt32;
	public class ElementExtension
	{
		public ElementExtension(int distanceFromStart, int distanceFromEnd, List<OBJID> elements, Enums.EDirection eStartDir = Enums.EDirection.dNominal, Enums.EDirection eEndDir = Enums.EDirection.dNominal)
		{
			m_iStartDistance = distanceFromStart;
			m_iEndDistance = distanceFromEnd;
			m_elementVector.AddRange(elements);
			m_eEndDir = eEndDir;
			m_eStartDir = eStartDir;
		}

		public ElementExtension() {}

		public IReadOnlyList<Enums.SYSOBJ_TYPE> getValidObjTypes() => m_validObjTypes;

		public void validObjTypes(IReadOnlyList<Enums.SYSOBJ_TYPE> validTypes) => m_validObjTypes = new List<Enums.SYSOBJ_TYPE>(validTypes);

		public Enums.EDirection getStartDirection() => m_eStartDir;

		public Enums.EDirection getStartDirection(Enums.EDirection eOrientedDir)
		{
			// Direction only changes, if extension does not "lie" on direction change point (start end end directions differ)
			if (isTrueReverseDirection(eOrientedDir) && m_eStartDir == m_eEndDir)
				return m_eStartDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;

			return m_eStartDir;
		}

		public Enums.EDirection getEndDirection() => m_eEndDir;

		public Enums.EDirection getEndDirection(Enums.EDirection eOrientedDir)
		{
			// Direction only changes, if extension does not "lie" on direction change point (start end end directions differ)
			if (isTrueReverseDirection(eOrientedDir) && m_eStartDir == m_eEndDir)
				return m_eEndDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;

			return m_eEndDir;
		}

		public Enums.EDirection getOrientedDirection() => m_eEndDir;

		public Enums.EDirection setOrientedDirection(Enums.EDirection eOrientedDir)
		{
			Enums.EDirection previousOrientedDir = getOrientedDirection();

			// If known direction is changed, we must do some value exchanges
			if (isTrueReverseDirection(eOrientedDir))
			{
				int tmpDistance = m_iStartDistance;
				m_iStartDistance = m_iEndDistance;
				m_iEndDistance = tmpDistance;
				m_elementVector.Reverse();

				// Directions only change, if extension does not "lie" on direction change point (start end end directions differ)
				if (m_eStartDir == m_eEndDir)
				{
					m_eStartDir = m_eStartDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
					m_eEndDir = m_eEndDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
				}
			}
			else if ((eOrientedDir == Enums.EDirection.dNominal || eOrientedDir == Enums.EDirection.dOpposite) && m_eEndDir != eOrientedDir)
				m_eEndDir = eOrientedDir;

			return previousOrientedDir;
		}

		public Enums.EDirection getUsageDirection() => getOrientedDirection();
		public Enums.EDirection setUsageDirection(Enums.EDirection eOrientedDir) => setOrientedDirection(eOrientedDir);
		public int getStartDistance() => m_iStartDistance;
		public int getStartDistance(Enums.EDirection eOrientedDir) => isTrueReverseDirection(eOrientedDir) ? m_iEndDistance : m_iStartDistance;

		public int getEndDistance() => m_iEndDistance;
		public int getEndDistance(Enums.EDirection eOrientedDir) => isTrueReverseDirection(eOrientedDir) ? m_iStartDistance : m_iEndDistance;
		public IReadOnlyList<OBJID> getExtensionElements() => m_elementVector;

		public IReadOnlyList<OBJID> getExtensionElements(Enums.EDirection eOrientedDir) 
		{
			if (isTrueReverseDirection(eOrientedDir))
			{
				List<OBJID> tmp = new List<OBJID>();
				tmp.AddRange(m_elementVector);
				tmp.Reverse();
				return tmp;
			}

			return m_elementVector;
		}

		public List<OBJID> getExtensionElementsRaw() => m_elementVector;

		public bool isOnDirectionChangePoint() => isTrueReverseDirection(m_eStartDir);

		public bool Equal(object? obj)
		{
			if (obj is ElementExtension te)
			{
				return m_iStartDistance == te.m_iStartDistance
					&& m_iEndDistance == te.m_iEndDistance
					&& m_eStartDir == te.m_eStartDir
					&& m_eEndDir == te.m_eEndDir
					&& EqualList<OBJID>(m_elementVector, te.m_elementVector)
					&& EqualList<Enums.SYSOBJ_TYPE>(m_validObjTypes, te.m_validObjTypes);
			}

			return false;
		}

		//	public bool operator !=(const ElementExtension& te) const
		// 	{
		//		return !(*this == te);
		//	}

		//	public ElementExtension & operator=(const ElementExtension& te)
		//{
		//	if (this != &te)
		//	{
		//		this->m_iStartDistance = te.m_iStartDistance;
		//		this->m_iEndDistance = te.m_iEndDistance;
		//		this->m_elementVector = te.m_elementVector;
		//		this->m_eStartDir = te.m_eStartDir;
		//		this->m_eEndDir = te.m_eEndDir;
		//		this->m_validObjTypes = te.m_validObjTypes;
		//	}
		//	return *this;
		//}

		public ElementExtension(ElementExtension te)
		{
			this.m_iStartDistance = te.m_iStartDistance;
			this.m_iEndDistance = te.m_iEndDistance;
			this.m_elementVector = te.m_elementVector;
			this.m_eStartDir = te.m_eStartDir;
			this.m_eEndDir = te.m_eEndDir;
			this.m_validObjTypes = te.m_validObjTypes;
		}

		private bool isTrueReverseDirection(Enums.EDirection eOrientedDir)
		{
			return (eOrientedDir == Enums.EDirection.dNominal || eOrientedDir == Enums.EDirection.dOpposite)
				&& (m_eEndDir == Enums.EDirection.dNominal || m_eEndDir == Enums.EDirection.dOpposite)
				&& eOrientedDir != m_eEndDir;
		}

		private static bool EqualList<T>(List<T> mine, List<T> other)
		{
			if (mine.Count != other.Count)
				return false;

			for (int i = 0; i < mine.Count; i++)
				if (!mine[i].Equals(other[i]))
					return false;

			return true;
		}

		private int m_iStartDistance = 0; // Start distance to oriented direction
		private int m_iEndDistance = 0; // End distance to oriented direction
		private Enums.EDirection m_eStartDir = Enums.EDirection.dUnknown; // Start direction (tail direction in train extensions)
		private Enums.EDirection m_eEndDir = Enums.EDirection.dUnknown; // End direction (head direction in train extensions)
		private List<OBJID> m_elementVector = new List<OBJID>(); // All elements including start and end elements in oriented direction
		private List<Enums.SYSOBJ_TYPE> m_validObjTypes = new List<Enums.SYSOBJ_TYPE>();
	}
}
