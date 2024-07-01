namespace E2KService.ActiveMQ;

using System.Xml.Linq;

internal class XMLNamespaces
{
	public static Dictionary<string, XNamespace> NS = new()
	{
		{ "rcs", "http://www.transport.bombardier.com/2014/RcsCmm" },
		{ "rcswds", "http://www.transport.bombardier.com/2014/RcsCmm/wds" },
		{ "xs", "http://www.w3.org/2001/XMLSchema" },
		{ "vc", "http://www.w3.org/2007/XMLSchema-versioning" }
	};

	public static XNamespace GetXNamespace(string prefix)
	{
		if (NS.ContainsKey(prefix))
			return NS[prefix];
		return NS["xs"];
	}

	public static string GetNamespaceName(string prefix)
	{
		if (NS.ContainsKey(prefix))
			return NS[prefix].NamespaceName;
		return "";
	}

	public static string GetPrefix(string namespaceName)
    {
		foreach (var prefix in NS.Keys)
        {
			if (NS[prefix].NamespaceName == namespaceName)
				return prefix;
        }
		return "";
    }
}
