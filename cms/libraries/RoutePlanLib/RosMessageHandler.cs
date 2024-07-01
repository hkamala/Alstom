using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Apache.NMS;
using Serilog;

namespace RoutePlanLib
{
	public class RosMessageHandler
	{
		public string RoutePlanSchema { get; set; }
		public string CancelRoutePlanSchema { get; set; }
		public string MovementSchema { get; set; }
		public string PretestRequestSchema { get; set; }
		public string PretestResponseSchema { get; set; }
        public string ServiceRoutePlanSchema { get; set; }
        public string ServiceRoutePlanRequestSchema { get; set; }

        private string m_appName;
		private IDictionary<string, object> m_props = new Dictionary<string, object>();

		public RosMessageHandler(string appName, string rcsNode)
		{
			m_appName = appName;
			m_props.Add("rcsNode", rcsNode);
		}

		public Tuple<string, string, IDictionary<string, object>> OnMessage(IMessage msg)
		{
			if (msg is ITextMessage txtMsg)
			{
				if (txtMsg.Text.Contains($"<schema>{RoutePlanSchema}</schema>"))
					return ProcessRoutePlan(txtMsg, RoutePlanSchema);
				else if (txtMsg.Text.Contains($"<schema>{ServiceRoutePlanSchema}</schema>"))
                    return ProcessRoutePlan(txtMsg, ServiceRoutePlanSchema);
                else if (txtMsg.Text.Contains($"<schema>{CancelRoutePlanSchema}</schema>"))
					return ProcessCancelRoutePlan(txtMsg);
                else if (txtMsg.Text.Contains($"<schema>{ServiceRoutePlanRequestSchema}</schema>"))
                    return ProcessServiceRoutePlanRequest(txtMsg);
            }

            return new Tuple<string, string, IDictionary<string, object>>("", "", new Dictionary<string, object>());
		}

		public object? DeserializeMessage(IMessage msg)
		{
			object? retVal = null;

			if (msg is ITextMessage txtMsg)
			{
				if (txtMsg.Text.Contains($"<schema>{RoutePlanSchema}</schema>") || txtMsg.Text.Contains($"<schema>{ServiceRoutePlanSchema}</schema>"))
					retVal = XmlSerialization.DeserializeObjectFromString<XSD.RoutePlan.rcsMsg>(txtMsg.Text, out string errorText);
				else if (txtMsg.Text.Contains($"<schema>{CancelRoutePlanSchema}</schema>"))
					retVal = XmlSerialization.DeserializeObjectFromString<XSD.CancelRoutePlan.rcsMsg>(txtMsg.Text, out string errorText);
				else if (txtMsg.Text.Contains($"<schema>{PretestResponseSchema}</schema>"))
					retVal = XmlSerialization.DeserializeObjectFromString<XSD.PretestResponse.rcsMsg>(txtMsg.Text, out string errorText);
                else if (txtMsg.Text.Contains($"<schema>{ServiceRoutePlanRequestSchema}</schema>"))
                    retVal = XmlSerialization.DeserializeObjectFromString<XSD.ServiceRoutePlanRequest.rcsMsg>(txtMsg.Text, out string errorText);
            }

            return retVal;
		}

		public Tuple<string, string, IDictionary<string, object>> SerializeData(object rosData, string schema = "", string tripId = "")
		{
			if (rosData is XSD.RoutePlan.rcsMsg data)
				return SerializeRoutePlan(data, schema, tripId);
                //return SerializeRoutePlan(data, "");
            else if (rosData is XSD.CancelRoutePlan.rcsMsg cancelData)
				return SerializeCancelRoutePlan(cancelData);
			else if (rosData is XSD.PretestRequest.rcsMsg pretestData)
				return SerializePretest(pretestData);
			else if (rosData is XSD.ServiceRoutePlanRequest.rcsMsg serviceRoutePlanData)
				return SerializeServiceRoutePlanRequest(serviceRoutePlanData);

            return new Tuple<string, string, IDictionary<string, object>>("", "", new Dictionary<string, object>());
		}

        private Tuple<string, string, IDictionary<string, object>> SerializeRoutePlan(XSD.RoutePlan.rcsMsg msg, string schema, string tripId = "")
        {
			if (schema == "")
			{
				schema = RoutePlanSchema;

				// TODO: This is not well thought over, because data is used for decision of schema, but schemas in this library are any way used in wrong way, so...
				if (msg.data.RoutePlan.Trains.Count() > 0)
				{
					if (msg.data.RoutePlan.Trains.First().CTCID == "" && msg.data.RoutePlan.Trains.First().TrackedGUID == "")
						schema = ServiceRoutePlanSchema;
				}
			}
			//XSD.RoutePlanResponce.rcsMsg outMsg = new XSD.RoutePlanResponce.rcsMsg("testApp", "testSchema");
			//outMsg.data.ActionPlan = new XSD.RoutePlanResponce.ActionPlan { Trains = new XSD.RoutePlanResponce.ActionPlanTrains(msg.data.RoutePlan.Trains)};
			//string stringMsg = RoutePlanLib.XmlSerialization.SerializeObject<XSD.RoutePlanResponce.rcsMsg>(outMsg, out string errorText);
            var tripUid = msg.data.RoutePlan.Trains[0].Items[0].TrID;
            Log.Information("RosMessageHandler:Trip Uid Set as <" + tripUid + "> <" + msg.data.RoutePlan.Trains[0].Items.Length + ">");

			XSD.RoutePlanResponce.rcsMsg outMsg = new XSD.RoutePlanResponce.rcsMsg(m_appName, schema);
            outMsg.data.ActionPlan = new XSD.RoutePlanResponce.ActionPlan { Trains = new XSD.RoutePlanResponce.ActionPlanTrains(msg.data.RoutePlan.Trains) };

			//debug
			string actionPlan = XmlSerialization.SerializeObject<XSD.RoutePlanResponce.rcsMsg>(outMsg, out string errorTextPlan);
            Log.Information("RosMessageHandler:Action Plan Message Sent <" + actionPlan + ">");
            //outMsg.data.ActionPlan.Trains.Train[0].TripID = Convert.ToInt32(tripId);
			string stringMsg = XmlSerialization.SerializeObject<XSD.RoutePlanResponce.rcsMsg>(outMsg, out string errorText);
            Log.Information("RosMessageHandler:Route Plan Message Sent <" + stringMsg + ">");
            return new Tuple<string, string, IDictionary<string, object>>(schema, stringMsg, m_props);
		}

		private Tuple<string, string, IDictionary<string, object>> SerializeCancelRoutePlan(XSD.CancelRoutePlan.rcsMsg msg)
		{
			XSD.CancelRoutePlanResponce.rcsMsg outMsg = new XSD.CancelRoutePlanResponce.rcsMsg(m_appName, CancelRoutePlanSchema);
			outMsg.data.CancelTrainPlan = new XSD.CancelRoutePlanResponce.CancelTrainPlan() { Trains = new XSD.CancelRoutePlanResponce.CancelTrainPlanTrains(msg.data.CancelRoutePlan.Trains) };

			string stringMsg = XmlSerialization.SerializeObject<XSD.CancelRoutePlanResponce.rcsMsg>(outMsg, out string errorText);
			return new Tuple<string, string, IDictionary<string, object>>(CancelRoutePlanSchema, stringMsg, m_props);
		}

        private Tuple<string, string, IDictionary<string, object>> SerializeServiceRoutePlanRequest(XSD.ServiceRoutePlanRequest.rcsMsg msg)
        {
            // This is the same message in and out
            XSD.ServiceRoutePlanRequest.rcsMsg outMsg = new XSD.ServiceRoutePlanRequest.rcsMsg(m_appName, ServiceRoutePlanRequestSchema);
			outMsg.data.ServiceRoutePlanRequest = new XSD.ServiceRoutePlanRequest.ServiceRoutePlanRequest(msg.data.ServiceRoutePlanRequest.serid);

            string stringMsg = XmlSerialization.SerializeObject<XSD.ServiceRoutePlanRequest.rcsMsg>(msg, out string errorText);
            return new Tuple<string, string, IDictionary<string, object>>(ServiceRoutePlanRequestSchema, stringMsg, m_props);
        }

        private Tuple<string, string, IDictionary<string, object>> SerializePretest(XSD.PretestRequest.rcsMsg msg)
		{
			string stringMsg = XmlSerialization.SerializeObject<XSD.PretestRequest.rcsMsg>(msg, out string errorText);
			return new Tuple<string, string, IDictionary<string, object>>(PretestRequestSchema, stringMsg, m_props);
		}

		private Tuple<string, string, IDictionary<string, object>> ProcessRoutePlan(ITextMessage msg, string schema)
		{
			XSD.RoutePlan.rcsMsg? message = XmlSerialization.DeserializeObjectFromString<XSD.RoutePlan.rcsMsg>(msg.Text, out string errorText);
			if (message?.data?.RoutePlan == null)
				return new Tuple<string, string, IDictionary<string, object>>("", "", new Dictionary<string, object>());

			return SerializeRoutePlan(message, schema);
		}

		private Tuple<string, string, IDictionary<string, object>> ProcessCancelRoutePlan(ITextMessage msg)
		{
			XSD.CancelRoutePlan.rcsMsg? message = XmlSerialization.DeserializeObjectFromString<XSD.CancelRoutePlan.rcsMsg>(msg.Text, out string errorText);
			if (message?.data?.CancelRoutePlan == null)
				return new Tuple<string, string, IDictionary<string, object>>("", "", new Dictionary<string, object>());

			return SerializeCancelRoutePlan(message);
		}

        private Tuple<string, string, IDictionary<string, object>> ProcessServiceRoutePlanRequest(ITextMessage msg)
        {
            XSD.ServiceRoutePlanRequest.rcsMsg? message = XmlSerialization.DeserializeObjectFromString<XSD.ServiceRoutePlanRequest.rcsMsg>(msg.Text, out string errorText);
            if (message?.data?.ServiceRoutePlanRequest == null)
                return new Tuple<string, string, IDictionary<string, object>>("", "", new Dictionary<string, object>());

            return SerializeServiceRoutePlanRequest(message);
        }

    }
}
