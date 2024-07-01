using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;

namespace commUtils.MessageServer
{
	public interface IMessageServer
	{
		public enum ConnectionState
		{
			Disconnected,
			Closed,
			Connected,
			Started
		}
		public enum AddressType
		{
			Topic,
			Queue
		}
		public void Send(string address, IMessage message, bool sendAsync = true);
		public void Send(string address, string message, IDictionary<string, object> props, bool sendAsync = true);
		public void Send(string address, byte[] message, IDictionary<string, object> props, bool sendAsync = true);
		public void Send(string address, IDictionary<string, object> values, IDictionary<string, object> props, bool sendAsync = true);
		public void AddClient(IMessageProcessor? processor, string address, AddressType addressType);
		public void Start();
		public ConnectionState GetConnectionState();
	}
}
