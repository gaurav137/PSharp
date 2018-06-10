using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Timers;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SimpleTimers
{
    /// <summary>
    /// Simple machine to demonstrate the use of reliable timers
    /// </summary>
    class SimpleTimerMachine : ReliableStateMachine
    {
        /// <summary>
        /// Keeps counters
        /// </summary>
        IReliableDictionary<string, int> Counters;


        [Start]
        [OnEntry(nameof(OnEnterA))]
        [OnExit(nameof(OnExitA))]
        [OnEventDoAction(typeof(TimeoutEvent), nameof(OnTimeoutA))]
        class A : MachineState { }

        [OnEntry(nameof(OnEnterB))]
        [OnExit(nameof(OnExitB))]
        [OnEventDoAction(typeof(TimeoutEvent), nameof(OnTimeoutB))]
        class B : MachineState { }

        private async Task OnEnterA()
        {
            await StartTimer("A", 10);
        }

        private async Task OnExitA()
        {
            await StopTimer("A");
        }

        private async Task OnEnterB()
        {
            await StartTimer("B", 10);
        }

        private async Task OnExitB()
        {
            await StopTimer("B");
        }

        private async Task OnTimeoutA()
        {
            this.Assert(CurrentTransaction != null);

            var name = (this.ReceivedEvent as TimeoutEvent).Name;
            var cnt = await Counters.AddOrUpdateAsync(CurrentTransaction, name, 1, (k, v) => v + 1);

            this.Logger.WriteLine("SimpleTimer: Obtained timeout {0} in state A, count = {1}", name, cnt);
            this.Assert(name == this.CurrentState.Name);

            if(cnt == 23)
            {
                this.Monitor<LivenessMonitor>(new Done());
                await StopTimer("A");
            }

            if(cnt % 2 == 0)
            {
                this.Goto<B>();
            }
        }

        private async Task OnTimeoutB()
        {
            var name = (this.ReceivedEvent as Microsoft.PSharp.ReliableServices.Timers.TimeoutEvent).Name;
            var cnt = await Counters.AddOrUpdateAsync(CurrentTransaction, name, 1, (k, v) => v + 1);

            this.Logger.WriteLine("SimpleTimer: Obtained timeout {0} in state B, count = {1}", name, cnt);
            this.Assert(name == this.CurrentState.Name);

            if (cnt == 23)
            {
                this.Monitor<LivenessMonitor>(new Done());
                await StopTimer("B");
            }

            if (cnt % 2 == 0)
            {
                this.Goto<A>();
            }
        }

        protected override async Task OnActivate()
        {
            Counters = await this.Host.GetOrAddAsync<IReliableDictionary<string, int>>("Counters");
        }
    }

    class Done : Event { }

    class LivenessMonitor : Monitor
    {
        [Start]
        [Hot]
        [OnEventGotoState(typeof(Done), typeof(S2))]
        class S1 : MonitorState { }

        [Cold]
        [IgnoreEvents(typeof(Done))]
        class S2 : MonitorState { }
    }
}
