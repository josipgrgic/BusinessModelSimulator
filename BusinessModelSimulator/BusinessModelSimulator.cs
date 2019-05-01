using BusinessModelSimulator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelSimulator
{
    public class BusinessModelSimulator
    {
        private const int _maxDegreeOfParallelism = 10;
        private readonly string _restApiEndpoint = "http://localhost:8080/engine-rest/";
        private readonly string _processInstancePath = "history/process-instance";
        private readonly string _activityInstancePath = "history/activity-instance";
        private readonly string _startProcessPath = "process-definition/{0}/start";
        private readonly string _fetchAndLockExternalTaskPath = "external-task/fetchAndLock";
        private readonly string _completeExternalTaskPath = "external-task/{0}/complete";

        private string _processInstanceEndpoint;
        private string _activityInstanceEndpoint;
        private string _startProcessEndpoint;
        private string _fetchAndLockExternalTaskEndpoint;
        private string _completeExternalTaskEndpoint;

        public string ProcessDefinitionId { get; set; }

        public BusinessModelSimulator(string processDefinitionId)
        {
            ProcessDefinitionId = processDefinitionId;
            _processInstanceEndpoint = _restApiEndpoint + _processInstancePath;
            _activityInstanceEndpoint = _restApiEndpoint + _activityInstancePath;
            _startProcessEndpoint = _restApiEndpoint + string.Format(_startProcessPath, processDefinitionId);
            _fetchAndLockExternalTaskEndpoint = _restApiEndpoint + _fetchAndLockExternalTaskPath;
            _completeExternalTaskEndpoint = _restApiEndpoint + _completeExternalTaskPath;
        }

        public void CreateProcessInstances(int count)
        {
            for (var i = 0; i < count; i++)
            {
                WebRequestHandler.InvokeWebRequest<StartedProcessInstance>(_startProcessEndpoint, "POST");
            }
        }

        public void SimulateProcesses()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < _maxDegreeOfParallelism; i++)
            {
                var workerId = i.ToString();
                tasks.Add(Task.Run(() => SimulateProcessesInner(workerId)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void SimulateProcessesInner(string workerId)
        {
            var topics = new[] { new TopicSpecification("wait", 10 * 1000, ProcessDefinitionId) };
            var configuration = new FetchExternalTaskConfiguration(workerId, 10, 5 * 1000, topics);

            while (true)
            {
                var data = WebRequestHandler.InvokeWebRequest<List<FetchedExternalTask>>(_fetchAndLockExternalTaskEndpoint, "POST", configuration);
                if (data == null || data.Count == 0)
                {
                    return;
                }

                data.ForEach(externalTask =>
                {
                    DoWork();
                    CompleteTask(workerId, externalTask);
                });
            }
        }

        private void CompleteTask(string workerId, FetchedExternalTask externalTask)
        {
            var completedTaskConfiguration = new CompleteExternalTaskConifguration(workerId);
            var completeTaskUrl = string.Format(_completeExternalTaskEndpoint, externalTask.Id);
            WebRequestHandler.InvokeWebRequest(completeTaskUrl, "POST", completedTaskConfiguration);
        }

        private void DoWork()
        {
            var sleepDuration = new Random().Next(100) + 1;
            Thread.Sleep(sleepDuration);
        }

        public void ExtractEventLog(string folderPath)
        {
            var processInstances = GetProcessInstances();

            if (processInstances == null || processInstances.Count == 0)
            {
                return;
            }

            var csvFileLines = new List<string>();
            var txtFileLines = new List<string>();
            csvFileLines.Add("case,event,completeTime");

            var maxDigitCount = processInstances.Count.ToString().Length;

            for (int i = 0; i < processInstances.Count; i++)
            {
                var caseNumber = GetCaseNumber(maxDigitCount, i);

                var instance = processInstances[i];
                var activities = GetActivitiesForInstance(instance.Id);

                var joinedActivityNames = string.Join(" ", activities.Select(x => x.ActivityName));

                txtFileLines.Add(joinedActivityNames);

                activities.ForEach(activity => 
                {
                    var endTimeString = activity.EndTime
                        .ToUniversalTime()
                        .ToString("yyyy-MM-dd HH:mm:ss.fff",
                            System.Globalization.CultureInfo.InvariantCulture);

                    csvFileLines.Add($"case{caseNumber},{activity.ActivityName},{endTimeString}");
                });
            }

            File.WriteAllLines($"{folderPath}out.csv", csvFileLines);
            File.WriteAllLines($"{folderPath}out.txt", txtFileLines);
        }

        private string GetCaseNumber(int maxDigitCount, int caseNumber)
        {
            var caseNumberLength = caseNumber.ToString().Length;

            return $"{new string('0', maxDigitCount - caseNumberLength)}{caseNumber}";
        }

        private List<ProcessInstance> GetProcessInstances()
        {
            var url = $"{_processInstanceEndpoint}?processDefinitionId={ProcessDefinitionId}";
            var response = WebRequestHandler.InvokeWebRequest<List<ProcessInstance>>(url);
            var completedInstances = response.Where(instance => instance.EndTime != null).ToList();
            completedInstances.Sort((x, y) => ((DateTime) x.EndTime).CompareTo(y.EndTime));
            return completedInstances;
        }

        private List<ActivityInstance> GetActivitiesForInstance(string processInstanceId)
        {
            var url = $"{_activityInstanceEndpoint}?processInstanceId={processInstanceId}";
            var response = WebRequestHandler.InvokeWebRequest<List<ActivityInstance>>(url);

            var tasks = response.Where(activity => 
                ActivityIsOfType(activity, "serviceTask") || ActivityIsOfType(activity, "task"))
                .ToList();

            tasks.Sort((x, y) => x.EndTime.CompareTo(y.EndTime));
            return tasks;
        }

        private bool ActivityIsOfType(ActivityInstance activity, string type)
        {
            return string.Equals(activity.ActivityType, type, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
