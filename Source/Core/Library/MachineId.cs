//-----------------------------------------------------------------------
// <copyright file="MachineId.cs">
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
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    public sealed class MachineId : IEquatable<MachineId>, IComparable<MachineId>
    {
        /// <summary>
        /// The P# runtime that executes the machine with this id.
        /// </summary>
        internal BaseRuntime Runtime { get; private set; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        [DataMember]
        public readonly string FriendlyName;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        public readonly ulong Value;

        /// <summary>
        /// Generation of the runtime that created this machine id.
        /// </summary>
        [DataMember]
        public readonly ulong Generation;

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        public readonly string Endpoint;

        /// <summary>
        /// Creates a new machine id.
        /// </summary>
        /// <param name="runtime">BaseRuntime</param>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        internal MachineId(BaseRuntime runtime, Type type, string friendlyName)
            : this(runtime, type.FullName, friendlyName, runtime.Configuration.RuntimeGeneration, runtime.NetworkProvider.GetLocalEndpoint())
        { }

        /// <summary>
        /// Create a fresh MachineId borrowing information from a given id.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineId(MachineId mid)
            : this(mid.Runtime, mid.Type, mid.FriendlyName, mid.Generation, mid.Endpoint)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">BaseRuntime</param>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="generation">Runtime generation</param>
        /// <param name="endpoint">Endpoint</param>
        private MachineId(BaseRuntime runtime, string type, string friendlyName, ulong generation, string endpoint)
        {
            // Atomically increments and safely wraps into an unsigned long.
            this.Value = (ulong)Interlocked.Increment(ref runtime.MachineIdCounter) - 1;
            // Checks for overflow.
            this.Runtime.Assert(this.Value != ulong.MaxValue, "Detected MachineId overflow.");

            this.FriendlyName = friendlyName;
            this.Runtime = runtime;
            this.Endpoint = endpoint;
            this.Generation = generation;
            this.Type = type;

            if (endpoint != null && endpoint.Length > 0 && friendlyName != null && friendlyName.Length > 0)
            {
                this.Name = string.Format("{0}.{1}({2})", endpoint, friendlyName, Value);
            }
            else if (endpoint != null && endpoint.Length > 0)
            {
                this.Name = string.Format("{0}({1})", type, Value);
            }
            else if (friendlyName != null && friendlyName.Length > 0)
            {
                this.Name = string.Format("{0}({1})", friendlyName, Value);
            }
            else
            {
                this.Name = string.Format("{0}({1})", type, Value);
            }
        }

        /// <summary>
        /// Bind the machine id.
        /// </summary>
        /// <param name="runtime">BaseRuntime</param>
        internal void Bind(BaseRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal
        /// to the current <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            MachineId mid = obj as MachineId;
            if (mid == null)
            {
                return false;
            }

            return this.Value == mid.Value && this.Generation == mid.Generation;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + this.Value.GetHashCode();
            hash = hash * 23 + this.Generation.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Indicates whether the specified <see cref="MachineId"/> is equal
        /// to the current <see cref="MachineId"/>.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(MachineId other)
        {
            return this.Equals((object)other);
        }

        /// <summary>
        /// Compares the specified <see cref="MachineId"/> with the current
        /// <see cref="MachineId"/> for ordering or sorting purposes.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MachineId other)
        {
            return string.Compare(this.Name, other == null ? null : other.Name);
        }
    }
}
