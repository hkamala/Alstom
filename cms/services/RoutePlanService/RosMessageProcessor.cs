using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using commUtils;
using RoutePlanLib;

namespace RoutePlanService
{
	internal class RosMessageProcessor : IMessageProcessor
	{
		private readonly RosMessageHandler m_handler;

		public RosMessageProcessor(string appName, string rcsNode, string routePlanScheme, string cancelRoutePlanScheme, string routePlanReplyTo)
		{
			m_handler = new RosMessageHandler(appName, rcsNode)
			{
				RoutePlanSchema = routePlanScheme,
				CancelRoutePlanSchema = cancelRoutePlanScheme,
				MovementSchema = routePlanReplyTo,
				PretestRequestSchema = "", //not used here
				PretestResponseSchema = "" //not used here
			};
		}

		public Tuple<string, string, IDictionary<string, object>> OnMessage(IMessage msg) => m_handler.OnMessage(msg);
	}
}
