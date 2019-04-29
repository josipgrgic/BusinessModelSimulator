using System;
using System.Threading.Tasks;

namespace BusinessModelSimulator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var processDefinitionId = "process-model:3:1d7e2bf4-690d-11e9-a1c8-1e93a2b20fb4";
            var outFolderPath = "C:\\Users\\josip\\Desktop\\";
            var simualtor = new BusinessModelSimulator(processDefinitionId);

            simualtor.CreateProcessInstances(200);
            var startTime = DateTime.Now;
            simualtor.SimulateProcesses();
            var endTime = DateTime.Now;
            Console.WriteLine("Parallel execution: " + (endTime - startTime).TotalSeconds + " seconds:");

            //simualtor.ExtractEventLog(outFolderPath);
        }
    }
}
