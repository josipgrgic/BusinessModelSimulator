using BusinessModelSimulator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BusinessModelSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var processDefinitionId = "process-model:3:1d7e2bf4-690d-11e9-a1c8-1e93a2b20fb4";
            var outFolderPath = "C:\\Users\\josip\\Desktop\\";
            var simualtor = new BusinessModelSimulator(processDefinitionId);
            simualtor.CreateProcessInstances(200);
            simualtor.SimulateProcesses();
            simualtor.ExtractEventLog(outFolderPath);
        }
    }
}
