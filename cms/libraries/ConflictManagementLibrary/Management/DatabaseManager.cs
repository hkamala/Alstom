using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;

namespace ConflictManagementLibrary.Management
{
    public class DatabaseManager
    {
        public CassandraDataHandler? MyCassandraDataHandler;

        private readonly List<string> cassandraContactPoints = new(); // Configuration file keys: Cassandra:CassandraNodeIPAddress1, Cassandra:CassandraNodeIPAddress2, ...

        public DatabaseManager(IMyLogger theLogger)
        {

        }

        private void InitializeCassandraDataHandler()
        {
            
        }
    }


    public class CassandraDataHandler
    {
        readonly ISession? cassandraSession = null;
        readonly ConsistencyLevel? cassandraConsistencyLevel = null;

        public static CassandraDataHandler CreateInstance(List<string> cassandraContactPoints, int cassandraPort, uint cassandraConsistencyLevel)
        {
            return new CassandraDataHandler(cassandraContactPoints, cassandraPort, cassandraConsistencyLevel);
        }
        private CassandraDataHandler(List<string> cassandraContactPoints, int cassandraPort, uint cassandraConsistencyLevel)
        {
            try
            {
                var keyspaceCMS = "cms";
                var cluster = Cluster.Builder()
                    .AddContactPoints(cassandraContactPoints)
                    .WithPort(cassandraPort)
                    .WithSocketOptions(new SocketOptions().SetReadTimeoutMillis(60000)) // Does nothing!
                    .WithQueryOptions(new QueryOptions().SetPageSize(100))  // Solution to read timeout problem!
                    .Build();

                this.cassandraConsistencyLevel = (ConsistencyLevel)Math.Max(1, Math.Min(cassandraConsistencyLevel, 3)); // ConsistencyLevel.One is minimum, ConsistencyLevel.Three is maximum

                // Connect and select 'cms' keyspace
                cassandraSession = cluster.Connect(keyspaceCMS);

            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger.LogException(e.ToString());
            }
        }

    }
}

