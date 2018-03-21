//-----------------------------------------------------------------------
// <copyright file="Timers.cs">
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
	/// <summary>
	/// Extends the P# Machine with a simple timer.
	/// </summary>
	public abstract class TMachine : Machine
	{
		#region private fields

		/// <summary>
		/// Set of currently active timers.
		/// </summary>
		HashSet<TimerId> timers = new HashSet<TimerId>();

        #endregion

        #region Timer API

        /// <summary>
        /// Start a timer. 
        /// </summary>
        /// <param name="payload">Payload of the timeout event.</param>
        /// <param name="IsPeriodic">Specifies whether a periodic timer is desired.</param>
        /// <param name="period">Periodicity of the timeout events in ms.</param>
        /// <returns>The id of the created timer.</returns>
        protected TimerId StartTimer(object payload, bool IsPeriodic, int period)
        {
            // The specified period must be valid
            this.Assert(period >= 0, "Timer period must be non-negative");

            var mid = this.Runtime.CreateMachineId(this.Runtime.GetTimerMachineType());
            var tid = new TimerId(mid, payload);

            this.Runtime.CreateMachine(mid, this.Runtime.GetTimerMachineType(), new InitTimer(this.Id, tid, IsPeriodic, period));

            timers.Add(tid);
            return tid;

        }

		/// <summary>
		/// Stop the timer.
		/// </summary>
		/// <param name="timer">Id of the timer machine which is being stopped.</param>
		/// <param name="flush">Clear the queue of all timeout events generated by "timer".</param>
		protected async Task StopTimer(TimerId timer, bool flush = true)
		{
			// Check if the user is indeed trying to halt a valid timer
			this.Assert(timers.Contains(timer), "Illegal timer-id given to StopTimer");
			timers.Remove(timer);

			this.Send(timer.mid, new HaltTimerEvent(this.Id, flush));

            // Flush the buffer: the timer being stopped sends a markup event to the inbox of this machine.
            // Keep dequeuing eTimeout events (with payload being the timer being stopped), until we see the markup event.
            if (flush)
            {
                while (true)
                {
                    var ev = await this.Receive(Tuple.Create(typeof(Markup), new Func<Event, bool>(e => true)),
                        Tuple.Create(typeof(TimerElapsedEvent), new Func<Event, bool>(e => (e as TimerElapsedEvent).Tid == timer)));

                    if (ev is Markup)
                    {
                        break;
                    }
                }
            }
		}
		#endregion
	}
}