using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace commUtils
{
	public interface IMessageProcessor
	{
		public Tuple<string, string, IDictionary<string, object>> OnMessage(IMessage msg);
	}
}
