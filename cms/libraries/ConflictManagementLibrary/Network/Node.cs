using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Network
{
    public class Node
    {
        public int MyReferenceNumber { get; set; }
        [JsonIgnore]   public Station MyStation { get; set; }
        public List<Link> MyLeftLinks = new List<Link>();
        public List<Link> MyRightLinks = new List<Link>();
        public List<Path> MyPaths = new List<Path>();


    }
}
