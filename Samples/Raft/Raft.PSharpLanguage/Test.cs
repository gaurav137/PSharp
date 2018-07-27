﻿using System;
using Microsoft.PSharp;

namespace Raft.PSharpLanguage
{
    /// <summary>
    /// A single-process implementation of the Raft consensus protocol written using the
    /// P# high-level syntax.
    ///
    /// The description of Raft can be found here: https://raft.github.io/raft.pdf
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of P#.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = RuntimeFactory.Create(configuration);

            // Executes the P# program.
            Program.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute(IStateMachineRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(SafetyMonitor));
            runtime.CreateMachine(typeof(ClusterManager));
        }
    }
}
