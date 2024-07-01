using System;
using System.Collections.Generic;
using System.Text;

namespace XSD.PretestRequest
{
	public partial class rcsMsg
	{
		public rcsMsg()
		{
		}

		public rcsMsg(string sender, string schema, string trainID, string cmd, string target, string type)
		{
			this.hdr = new hdr(sender, schema);
			this.data = new data(trainID, cmd, target, type);
		}
	}

	public partial class hdr
	{
		public hdr(string sender, string schema)
		{
            this.sender = sender;
            this.schema = schema;
			this.utc = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
		}

        public hdr()
        {

        }
	}

	public partial class data
	{
		public data()
		{
		}

		public data(string trainID, string cmd, string target, string type)
		{
			this.PretestRequest = new PretestRequest(trainID, cmd, target, type);
		}
	}

	public partial class PretestRequest
	{
		public PretestRequest()
		{
		}

		public PretestRequest(string trainID, string cmd, string target, string type)
		{
			this.CtcTrainId = trainID;
			this.Cmd = cmd;
			this.Target = target;
			this.type = type;
		}
	}
}
