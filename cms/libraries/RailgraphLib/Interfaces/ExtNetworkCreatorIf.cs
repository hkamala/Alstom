using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public interface ExtNetworkCreatorIf
	{
		enum EClassType {
			ctEdge,
			ctVertex
		};
		void add(TopoGraph topoGraph);
		void createCoreObj(UInt32 objId, UInt16 objType, EClassType eClassType, string objName);
		void addAdjacency(Enums.EDirection eDir, UInt32 from, UInt32 to, int cost = 0);
		void createExt();
		void destroyExt();
	}
}
