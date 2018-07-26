using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PSharp.ServiceFabric;

namespace WordCountApp
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class WordCountApp : Microsoft.PSharp.ServiceFabric.PSharpService
    {
        public static List<Type> KnownTypes = new List<Type>()
        {
            // Events
            typeof(WordCount.WordFreqEvent),
            typeof(WordCount.WordEvent),
            typeof(WordCount.WordCountInitEvent)
        };

        public static Dictionary<Type, string> TypeToPartitionMap = new Dictionary<Type, string>
        {
                      {  typeof(WordCount.ClientMachine), "DriverPartition"  },
                      {  typeof(WordCount.SimpleGatherResultsMachine), "DriverPartition"  },
                      {  typeof(WordCount.WordCountMachine), "WorkerPartition" }
        };

        public WordCountApp(StatefulServiceContext context)
             : base(context, KnownTypes)
        { }

        protected override IRemoteMachineManager GetMachineManager()
        {
            if (!(this.Partition.PartitionInfo is NamedPartitionInformation))
            {
                throw new InvalidOperationException("Service must have named paritions");
            }

            var partitionName = (this.Partition.PartitionInfo as NamedPartitionInformation).Name;

            return new SingleServiceMachineManager(this.Context.ServiceName.ToString(), partitionName, TypeToPartitionMap);
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            if ((this.Partition.PartitionInfo as NamedPartitionInformation).Name == "DriverPartition")
            {
                FakeClient();
            }

            await base.RunAsync(cancellationToken);
            await Task.Delay(-1, cancellationToken);
        }

        private void FakeClient()
        {
            Task.Run(async () =>
            {
                // start a ping machine
                var machineType = typeof(WordCount.ClientMachine).AssemblyQualifiedName;
                var mid = await this.CreateMachineId(machineType, "ClientMachine");
                await this.CreateMachine(mid, machineType, null);
            });
        }

        protected override Microsoft.PSharp.ServiceFabric.IPSharpEventSourceLogger GetPSharpRuntimeLogger()
        {
            return new MyLogger();
        }

        private class MyLogger : Microsoft.PSharp.ServiceFabric.IPSharpEventSourceLogger
        {
            public void Message(string message)
            {
                ServiceEventSource.Current.Message(message);
            }

            public void Message(string message, params object[] args)
            {
                ServiceEventSource.Current.Message(message, args);
            }
        }
    }
}
