﻿namespace E2KService.ActiveMQ.AMQP;
using System.Xml.Linq;
using Serilog;

internal class Rcs5kXmlMessageProcessor : RcsXmlMessageProcessor
{
	public Rcs5kXmlMessageProcessor(string name) : base(name)
	{
		Log.Information("Rcs5k Message processor '{0}' created", name);
	}

	override protected XElement? ParseRootNode(string xml)
	{
		// Parse XML message
		XDocument doc = XDocument.Parse(xml);
		var rootNode = doc.Element(XMLNamespaces.GetXNamespace("rcs") + "Message");

		return rootNode;
	}

	override protected void ParseHeaders(XElement rootNode, Dictionary<string, string> hdr)
	{
		var hdrs = rootNode?.Element("hdr");
		if (hdrs != null)
		{
			foreach (var hdrElem in hdrs.Elements())
			{
				hdr[hdrElem.Name.ToString()] = hdrElem.Value;
			}
		}
	}

	override protected XElement? ParseDataMsgElement(XElement rootNode)
	{
		XElement? dataMsgElement = null;
		var dataNode = rootNode.Element("data");
		if (dataNode != null && dataNode.FirstNode != null)
			dataMsgElement = (XElement)dataNode.FirstNode;

		return dataMsgElement;
	}

	override protected string ParseMsgType(XElement dataMsgElement, Dictionary<string, string> msgProperties)
	{
		string? msgType = null;

		// Try to find out from properties first
		try
		{
			msgType = msgProperties["rcsContent"];
		}
		catch
		{}

		if (msgType == null || msgType == "")
		{
			// Find from real message's first node. Create proper message type with optional namespace
			string localName = dataMsgElement.Name.LocalName;
			string nameSpaceName = dataMsgElement.Name.NamespaceName;

			if (nameSpaceName != null && nameSpaceName != "")
				msgType = XMLNamespaces.GetPrefix(nameSpaceName) + ":" + localName;
			else
				msgType = localName;
		}

		return msgType;
	}

	override protected XElement CreateRootNode()
	{
		return new XElement(XMLNamespaces.GetXNamespace("rcs") + "Message");
	}

    override protected void CreateNamespaces(XElement rootNode, List<string>? namespaces = null)
    {
        // Add needed namespaces from caller ("rcs" and "xs" are always added)
        foreach (var ns in XMLNamespaces.NS.Keys)
        {
            if (ns is "rcs" || ns is "xs" || (namespaces != null && namespaces.Contains(ns)))
                rootNode.Add(new XAttribute(XNamespace.Xmlns + ns, XMLNamespaces.GetXNamespace(ns).NamespaceName));
        }
    }
}

