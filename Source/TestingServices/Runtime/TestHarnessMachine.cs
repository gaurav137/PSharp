//-----------------------------------------------------------------------
// <copyright file="TestHarnessMachine.cs">
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
using System.Reflection;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# test harness machine. This is the root machine
    /// that executes a test method during bug-finding.
    /// </summary>
    internal sealed class TestHarnessMachine : AbstractMachine
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal BaseRuntime Runtime { get; private set; }

        /// <summary>
        /// The test method.
        /// </summary>
        private MethodInfo TestMethod;

        /// <summary>
        /// The test action.
        /// </summary>
        private Action<IPSharpRuntime> TestAction;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="testMethod">MethodInfo</param>
        /// <param name="testAction">Action</param>
        internal TestHarnessMachine(MethodInfo testMethod, Action<IPSharpRuntime> testAction)
        {
            this.TestMethod = testMethod;
            this.TestAction = testAction;
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="runtime">BaseRuntime</param>
        /// <param name="mid">MachineId</param>
        /// <param name="info">MachineInfo</param>
        internal void Initialize(BaseRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.Runtime = runtime;
            base.Initialize(mid, info);
        }

        /// <summary>
        /// Runs the test harness.
        /// </summary>
        internal void Run()
        {
            try
            {
                // Starts the test.
                if (this.TestAction != null)
                {
                    this.Runtime.Log("<TestHarnessLog> Running anonymous test method.");
                    this.TestAction(this.Id.Runtime);
                }
                else
                {
                    this.Runtime.Log("<TestHarnessLog> Running test method " +
                        $"'{this.TestMethod.DeclaringType}.{this.TestMethod.Name}'.");
                    this.TestMethod.Invoke(null, new object[] { base.Id.Runtime });
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        internal void ReportUnhandledException(Exception ex)
        {
            if (this.TestAction != null)
            {
                this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in anonymous test method, " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
            else
            {
                this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                    $"in test method '{this.TestMethod.DeclaringType}.{this.TestMethod.Name}', " +
                    $"'{ex.Source}':\n" +
                    $"   {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}");
            }
        }
    }
}
