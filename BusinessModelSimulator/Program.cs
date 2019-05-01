using System;
using System.Diagnostics;

namespace BusinessModelSimulator
{
    class Program
    {
        static void Main()
        {
            var processDefinitionId = "nonlocal:2:1e6bdebc-6c50-11e9-82db-1e93a2b20fb4";
            var outFolderPath = "C:\\Users\\josip\\Desktop\\";
            var simualtor = new BusinessModelSimulator(processDefinitionId);

            simualtor.CreateProcessInstances(200);

            var stopWatch = Stopwatch.StartNew();
            simualtor.SimulateProcesses();
            stopWatch.Stop();
            Console.WriteLine("Simulation Execution time: " + stopWatch.Elapsed.TotalSeconds + " seconds.");

            simualtor.ExtractEventLog(outFolderPath);
        }
    }
}
