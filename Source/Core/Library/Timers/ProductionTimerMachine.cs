﻿//-----------------------------------------------------------------------
// <copyright file="ProductionTimerMachine.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Timers;

namespace Microsoft.PSharp.Timers
{
	/// <summary>
	/// Wrapper class for a system timer.
	/// </summary>
	public class ProductionTimerMachine : Machine
	{
		/// <summary>
		/// Specified if periodic timeout events are desired.
		/// </summary>
		private bool IsPeriodic;

		/// <summary>
		/// Specify the periodicity of timeout events.
		/// </summary>
		private int Period;

		/// <summary>
		/// Machine to which eTimeout events are dispatched.
		/// </summary>
		private MachineId Client;

        /// <summary>
        /// TimerId.
        /// </summary>
        private TimerId tid;

		/// <summary>
		/// System timer to generate Elapsed timeout events in production mode.
		/// </summary>
		private Timer timer;

		/// <summary>
		/// Flag to prevent timeout events being sent after stopping the timer.
		/// </summary>
		private bool IsTimerEnabled = false;

		/// <summary>
		/// Used to synchronize the Elapsed event handler with timer stoppage.
		/// </summary>
		private readonly Object tlock = new object();

		[Start]
		[OnEntry(nameof(InitializeTimer))]
		[OnEventDoAction(typeof(HaltTimerEvent), nameof(DisposeTimer))]
		private class Init : MachineState { }

		private void InitializeTimer()
		{
			InitTimer e = (this.ReceivedEvent as InitTimer);
			this.Client = e.client;
			this.IsPeriodic = e.IsPeriodic;
			this.Period = e.Period;
            this.tid = e.tid;

			this.IsTimerEnabled = true;
			this.timer = new Timer(Period);

			if (!IsPeriodic)
			{
				this.timer.AutoReset = false;
			}

			this.timer.Elapsed += ElapsedEventHandler;
			this.timer.Start();
		}

		/// <summary>
		/// Handler for the Elapsed event generated by the system timer.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void ElapsedEventHandler(Object source, ElapsedEventArgs e)
		{
            lock (this.tlock)
			{
				if (this.IsTimerEnabled)
				{
					this.Runtime.SendEvent(this.Client, new TimerElapsedEvent(tid));
				}
			}
		}

		private void DisposeTimer()
		{
			HaltTimerEvent e = (this.ReceivedEvent as HaltTimerEvent);

			// The client attempting to stop this timer must be the one who created it.
			this.Assert(e.client == this.Client);

			lock (this.tlock)
			{
				this.IsTimerEnabled = false;
				this.timer.Stop();
				this.timer.Dispose();
			}

			// If the client wants to flush the inbox, send a markup event.
			// This marks the endpoint of all timeout events sent by this machine.
			if (e.flush)
			{
				this.Send(this.Client, new Markup());
			}

			// Stop this machine
			this.Raise(new Halt());
		}
	}
}
