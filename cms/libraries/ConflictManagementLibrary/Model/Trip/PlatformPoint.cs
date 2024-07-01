using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Network;

namespace ConflictManagementLibrary.Model.Trip
{
    public class PlatformPoint
    {
        public Platform MyPlatform { get; }

        private PlatformPoint(Platform thePlatform)
        {
            MyPlatform = thePlatform;
        }
        public static PlatformPoint CreateInstance(Platform thePlatform)
        {
            return new PlatformPoint(thePlatform);
        }
    }
}
