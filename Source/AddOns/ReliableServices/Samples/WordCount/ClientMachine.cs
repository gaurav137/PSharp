﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace WordCount
{
    /// <summary>
    /// ClientMachine
    /// </summary>
    class ClientMachine : ReliableStateMachine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        async Task InitOnEntry()
        {
            var targetMachine = await this.ReliableCreateMachine<SimpleGatherResultsMachine>(new RsmInitEvent());
            var wordCountMachines = new IRsmId[Config.NumMachines];

            for (int i = 0; i < Config.NumMachines; i++)
            {
                wordCountMachines[i] = await this.ReliableCreateMachine<WordCountMachine>(new WordCountInitEvent(targetMachine));
            }

            for (int i = 0; i < Config.NumWords; i++)
            {
                var word = RandomString();
                await this.ReliableSend(wordCountMachines[Math.Abs(word.GetHashCode() % Config.NumMachines)], new WordEvent(word, i));
            }

        }


        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }

        private string RandomString()
        {
            var ret = "";
            var len = Config.StringLen;
            while (len > 0)
            {
                ret += this.Random() ? "0" : "1"; 
                len--;
            }
            return ret;
        }

    }

}