﻿//-----------------------------------------------------------------------
// <copyright file="TimerModel.cs">
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
	/// <summary>
	/// A timer model, used for testing purposes.
	/// </summary>
    public class TimerModel : Machine
    {
		#region fields

		/// <summary>
		/// Machine to which eTimeout events are dispatched.
		/// </summary>
		private MachineId client;

		/// <summary>
		/// True if periodic eTimeout events are desired.
		/// </summary>
		private bool IsPeriodic;

        /// <summary>
        /// Payload
        /// </summary>
        private object Payload;

        #endregion

        #region states
        [Start]
		[OnEntry(nameof(InitializeTimer))]
		[OnEventDoAction(typeof(HaltTimerEvent), nameof(DisposeTimer))]
        [OnEventDoAction(typeof(RepeatTimeout), nameof(SendTimeout))]
        private class Init : MachineState { }

		#endregion

		#region event handlers
		private void InitializeTimer()
		{
			InitTimer e = (this.ReceivedEvent as InitTimer);
			this.client = e.client;
			this.IsPeriodic = e.IsPeriodic;
            this.Payload = e.Payload;
            this.Send(this.Id, new RepeatTimeout());
		}

		private void SendTimeout()
		{
			// If not periodic, send a single timeout event
			if (!this.IsPeriodic)
			{
				this.Send(this.client, new TimerElapsedEvent(new TimerId(this.Id, this.Payload)));
			}
			else
			{
				if (this.Random())
				{
					this.Send(this.client, new TimerElapsedEvent(new TimerId(this.Id, this.Payload)));
				}
				this.Send(this.Id, new RepeatTimeout());
			}
			
		}

		private void DisposeTimer()
		{
			HaltTimerEvent e = (this.ReceivedEvent as HaltTimerEvent);

			// The client attempting to stop this timer must be the one who created it.
			this.Assert(e.client == this.client);

			// If the client wants to flush the inbox, send a markup event.
			// This marks the endpoint of all timeout events sent by this machine.
			if (e.flush)
			{
				this.Send(this.client, new Markup());
			}

			// Stop this machine
			this.Raise(new Halt());
		}
		#endregion
	}
}
