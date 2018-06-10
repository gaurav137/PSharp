using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;

namespace ReliableSpec
{
    class Program
    {
        static void Main(string[] args)
        {
            var stateManager = new StateManagerMock(null);
            var config = Configuration.Create().WithVerbosityEnabled(2);
            var clientRuntime = PSharpRuntime.Create(config);
            var origHost = RsmHost.Create(stateManager, "ThisPartition", config);
            origHost.ReliableCreateMachine<M>(new RsmInitEvent()).Wait();

            Console.ReadLine();
        }

        [Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(N));
            var stateManager = new StateManagerMock(runtime);

            var origHost = RsmHost.CreateForTesting(stateManager, "ThisPartition", runtime);
            origHost.ReliableCreateMachine<M>(new RsmInitEvent()).Wait();
        }

    }

    class E : Event { }

    class M : ReliableStateMachine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.ReliableMonitor<N>(new E());
        }

        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }
    }

    class N : Monitor
    {
        [Start]
        [Hot]
        [OnEventGotoState(typeof(E), typeof(S2))]
        class S1 : MonitorState { }

        [Cold]
        class S2 : MonitorState { }
    }

}
