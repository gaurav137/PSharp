//-----------------------------------------------------------------------
// <copyright file="ReliableRuntimeFactory.cs">
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
using System.Threading;
using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric
{
    /// <summary>
    /// Reliable state-machine runtime factory.
    /// </summary>
    public static class ReliableRuntimeFactory
    {
        internal static ReliableStateMachineRuntime Current { get; private set; }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes on top of Service Fabric.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime Create(IReliableStateManager stateManager,
            IRemoteMachineManager remoteMachineManager)
        {
            return Create(stateManager, remoteMachineManager, Configuration.Create(), CancellationToken.None);
        }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes on top of Service Fabric.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime Create(IReliableStateManager stateManager,
            IRemoteMachineManager remoteMachineManager, Configuration configuration)
        {
            return Create(stateManager, remoteMachineManager, configuration, CancellationToken.None);
        }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes on top of Service Fabric.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime Create(IReliableStateManager stateManager,
            IRemoteMachineManager remoteMachineManager, Configuration configuration,
            CancellationToken cancellationToken)
        {
            Current = new ReliableStateMachineRuntime(stateManager, remoteMachineManager, configuration, cancellationToken);
            Current.SetRsmNetworkProvider(new Net.DefaultRsmNetworkProvider(Current));
            return Current;
        }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes on top of Service Fabric.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="remoteMachineManager">Remote machine manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="networkProviderFunc">Network provider</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime Create(
            IReliableStateManager stateManager,
            IRemoteMachineManager remoteMachineManager,
            Configuration configuration,
            CancellationToken cancellationToken,
            Func<IReliableStateMachineRuntime, Net.IRsmNetworkProvider> networkProviderFunc)
        {
            Current = new ReliableStateMachineRuntime(stateManager, remoteMachineManager, configuration, cancellationToken);
            Current.SetRsmNetworkProvider(networkProviderFunc(Current));
            return Current;
        }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes locally.
        /// Used for testing purposes.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime CreateLocal(IReliableStateManager stateManager)
        {
            return Create(stateManager, new SingleProcessMachineManager(), Configuration.Create(), CancellationToken.None);
        }

        /// <summary>
        /// Creates a P# reliable state-machine runtime that executes locally.
        /// Used for testing purposes.
        /// </summary>
        /// <param name="stateManager">State manager</param>
        /// <param name="configuration">P# Configuration</param>
        /// <returns>IReliableStateMachineRuntime</returns>
        public static IReliableStateMachineRuntime CreateLocal(IReliableStateManager stateManager, Configuration configuration)
        {
            return Create(stateManager, new SingleProcessMachineManager(), configuration, CancellationToken.None);
        }
    }
}
