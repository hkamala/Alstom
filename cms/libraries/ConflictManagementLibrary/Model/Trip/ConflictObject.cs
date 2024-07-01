using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Model.Conflict;
using RailgraphLib;

namespace ConflictManagementLibrary.Model.Trip
{
    public class ConflictObject
    {
        public GraphObj MyObject { get; }
        public Conflict.Conflict MyConflict { get; }
        public string MyGuid = Guid.NewGuid().ToString();
        private ConflictObject(GraphObj theObject, Conflict.Conflict theConflict)
        {
            MyObject = theObject;
            MyConflict = theConflict;
        }
        public static ConflictObject CreateInstance(GraphObj theObject, Conflict.Conflict theConflict)
        {
            return new ConflictObject(theObject, theConflict);
        }
    }
}
