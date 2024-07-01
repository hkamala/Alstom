using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSD.CancelRoutePlanResponce
{
	public partial class rcsMsg
	{
		public rcsMsg(string sender, string schema)
		{
			this.hdrField = new hdr(sender, schema);
			this.dataField = new data();
		}

		public rcsMsg()
		{

		}
	}

	public partial class hdr
	{ 
		public hdr(string sender, string schema)
		{
			this.senderField = sender;
			this.utcField = DateTime.Now.ToString("yyyyMMddThhmmss");
			this.schemaField = schema;
		}

		public hdr()
		{

		}
	}

	public partial class CancelTrainPlanTrains
	{
		public CancelTrainPlanTrains(XSD.CancelRoutePlan.Train[] trains)
		{
			if ((trains?.Length ?? 0) == 0)
				return;

			this.trainField = new Train[trains.Length];

			int index = 0;
			foreach (var train in trains)
				trainField[index++] = new Train(train);
		}

		public CancelTrainPlanTrains()
		{

		}
	}

	public partial class Train
	{
		public Train(XSD.CancelRoutePlan.Train train)
		{
			this.ctcTrainIdField = train.CTCID;
			this.serviceNameField = train.SerN;
		}

		public Train()
		{

		}
	}
	
}
