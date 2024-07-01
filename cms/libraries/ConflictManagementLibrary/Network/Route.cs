using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConflictManagementLibrary.Network
{
    public class Route
    {
        public List<Path> MyPaths = new List<Path>();

        public static Route CreateInstance()
        {
            return new Route();
        }
        private Route()
        {
            
        }

    }
}
