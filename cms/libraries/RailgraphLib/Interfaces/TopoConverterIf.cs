using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.RailExtension;

namespace RailgraphLib
{
	public interface TopoConverterIf
	{
		Enums.ECreateExtensionResult createExtension(UInt32 startId, UInt32 endId, int distFromStart, int distFromEnd, Enums.EDirection edir, List<UInt32> viaElements, TopoGraph topoGraph, ElementExtension resultExtension);
		Enums.EConversionResult convertToCoreExtension(ElementExtension sourceExtension, ref CoreExtension coreExtension, TopoGraph sourceTopoGraph);
		Enums.EConversionResult convertToExtension(CoreExtension coreExtension, ref ElementExtension targetExtension, TopoGraph targetTopoGraph);
		Enums.EConversionResult convertExtension(ElementExtension sourceExtension, ElementExtension targetExtension, TopoGraph sourceTopoGraph, TopoGraph targetTopoGraph);
	}
}
