using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace RoutePlanLib
{
	public class XmlSerialization
	{
		public XmlSerialization()
		{

		}

		public static T DeserializeObjectFromFile<T>(string filename, out string errorText)
		{
			errorText = "";
			if (string.IsNullOrEmpty(filename))
				return default!;

			try
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(filename);
				return DeserializeObject<T>(xmlDoc, out errorText);
			}
			catch (Exception ex)
			{
				errorText = ex.Message;
			}
			return default!;
		}

		public static T DeserializeObjectFromString<T>(string content, out string errorText)
		{
			try
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(content);
				return DeserializeObject<T>(xmlDoc, out errorText);
			}
			catch (Exception ex)
			{
				errorText = ex.Message;
			}
			return default!;
		}

		public static T DeserializeObject<T>(XmlDocument xmlDoc, out string errorText)
		{
			errorText = "";
			T objectOut = default;

			try
			{
				string xmlString = xmlDoc.OuterXml;

				StringReader read = new StringReader(xmlString);
				Type outType = typeof(T);

				XmlSerializer serializer = new XmlSerializer(outType);
				using (XmlReader reader = new XmlTextReader(read))
				{
					objectOut = (T)serializer.Deserialize(reader);
					reader.Close();
				}

				read.Close();
			}
			catch (Exception ex)
			{
				//Log exception here
				errorText = ex.ToString();
			}

			return objectOut!;
		}

		public static string SerializeObject<T>(T obj, out string errorText)
		{
			errorText = "";
			string retVal = "";
			try
			{
				MemoryStream write = new MemoryStream();
				Type inType = typeof(T);

				XmlSerializer serializer = new XmlSerializer(inType);
				using (XmlWriter writer = new XmlTextWriter(write, System.Text.Encoding.UTF8))
				{
                    XmlSerializerNamespaces test = new XmlSerializerNamespaces();
                    test.Add("","");

					serializer.Serialize(writer, obj, test);
					retVal = Encoding.UTF8.GetString(write.ToArray());
                }
			}
			catch (Exception ex)
			{
				errorText = ex.ToString();
			}

			return retVal;
		}
	}
}
