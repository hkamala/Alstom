using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public interface NetworkCreatorIf
	{
		void add(TopoGraph topoGraph);
		void create();
		void destroy();
		TopoConverterIf getConverter();
	}
}
