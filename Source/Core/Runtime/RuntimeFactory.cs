//-----------------------------------------------------------------------
// <copyright file="RuntimeFactory.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// State-machine runtime factory.
    /// </summary>
    public static class RuntimeFactory
    {
        /// <summary>
        /// Creates a new state-machine runtime.
        /// </summary>
        /// <returns>IStateMachineRuntime</returns>
        public static IStateMachineRuntime Create()
        {
            return new StateMachineRuntime();
        }

        /// <summary>
        /// Creates a new state-machine runtime with the specified
        /// <see cref="Configuration"/>.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>IStateMachineRuntime</returns>
        public static IStateMachineRuntime Create(Configuration configuration)
        {
            return new StateMachineRuntime(configuration);
        }
    }
}
