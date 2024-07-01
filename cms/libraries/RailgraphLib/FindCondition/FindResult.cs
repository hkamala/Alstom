using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.FindCondition
{
	public class FindResult
	{
		public FindResult(DateTime startTime)
		{
			m_searchingTime = startTime;
		}

		public FindResult()
		{
			m_searchingTime = DateTime.Now;
		}

		public FindResult(FindResult path)
		{
			m_dirChangesInPath = path.m_dirChangesInPath;
			m_eCurrentSearchDir = path.m_eCurrentSearchDir;
			m_pathFound = path.m_pathFound;
			m_searchingTime = path.m_searchingTime;
			m_elements.AddRange(path.m_elements);
		}

		//public FindResult& operator=(const FindResult& other);

		public bool isPathFound() => m_pathFound;

		public List<UInt32> getResult() => m_elements;

		public DateTime getSearchingTime() => m_searchingTime;

		public bool isDirectionChangeInPath() => m_dirChangesInPath != 0;

		public int getDirectionChangeCountInPath() => m_dirChangesInPath;

		public Enums.EDirection getSearchingDir() => m_eCurrentSearchDir;

		public void reverseResults() => m_elements.Reverse();

		public void pathFound(bool bPathFound) => m_pathFound = bPathFound;

		public void setSearchingDir(Enums.EDirection eDir) => m_eCurrentSearchDir = eDir;

		public void addBack(UInt32 id) => m_elements.Add(id);

		public void popBack() => m_elements.RemoveAt(m_elements.Count - 1);

		public void dirChangeInPath() => m_dirChangesInPath++;

		public void decChangesInPath() => --m_dirChangesInPath;

		public void setDirChangesInPath(int dirChangesInPath) => m_dirChangesInPath = dirChangesInPath;
		
		private int m_dirChangesInPath = 0;
		private Enums.EDirection m_eCurrentSearchDir = Enums.EDirection.dUnknown;
		private bool m_pathFound = false;
		private DateTime m_searchingTime;      // todo: not actually used. This and constructor shall rewrite.
		private List<UInt32> m_elements = new List<uint>();

	}
}
