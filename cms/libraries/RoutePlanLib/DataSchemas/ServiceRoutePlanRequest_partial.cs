using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XSD.ServiceRoutePlanRequest
{
    public partial class rcsMsg
    {
        public rcsMsg()
        {
        }

        public rcsMsg(string sender, string schema)
        {
            this.hdr = new hdr(sender, schema);
            this.data = new data();
        }

        public rcsMsg(string sender, string schema, int serviceId)
        {
            this.hdr = new hdr(sender, schema);
            this.data = new data(serviceId);
        }
    }

    public partial class hdr
    {
        public hdr()
        {
        }

        public hdr(string sender, string schema)
        {
            this.sender = sender;
            this.schema = schema;
            this.utc = DateTime.Now.ToString("yyyyMMddThhmmss");
        }
    }

    public partial class data
    {
        public data()
        {
        }

        public data(int serviceId)
        {
            this.ServiceRoutePlanRequest = new ServiceRoutePlanRequest(serviceId);
        }
    }

    public partial class ServiceRoutePlanRequest
    {
        public ServiceRoutePlanRequest()
        {
        }

        public ServiceRoutePlanRequest(int serviceId)
        {
            this.serid = serviceId;
        }
    }
}
