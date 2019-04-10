using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace BPMSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var processDefinitionId = "Log2:12:80b539d9-3deb-11e9-9a59-1e93a2b20fb4";
            var simualtor = new BPMSimulator(processDefinitionId);
            simualtor.CreateProcessInstances(200);
            simualtor.SimulateProcesses();
            simualtor.ExtractEventLogToFile();
        }
    }

    public class BPMSimulator
    {
        private readonly string _restApiEndpoint = "http://localhost:8080/engine-rest/";
        private string _processInstancePath = "history/process-instance";
        private string _activityInstancePath = "history/activity-instance";
        private string _startProcessPath = "process-definition/{0}/start";
        private string _fetchAndLockExternalTaskPath = "external-task/fetchAndLock";
        private string _completeExternalTaskPath = "external-task/{0}/complete";

        private string _processInstanceEndpoint;
        private string _activityInstanceEndpoint;
        private string _startProcessEndpoint;
        private string _fetchAndLockExternalTaskEndpoint;
        private string _completeExternalTaskEndpoint;

        public string ProcessDefinitionId { get; set; }

        public BPMSimulator(string processDefinitionId)
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
                InvokeWebRequest<StartedProcessInstance>(_startProcessEndpoint, "POST");
            }
        }

        public void SimulateProcesses()
        {
            var workerId = "1";

            var topics = new [] { new TopicSpecification("wait", 5 * 1000, ProcessDefinitionId) };
            var configuration = new FetchExternalTaskConfiguration(workerId, 10, 5 * 1000, topics);

            while (true)
            {
                var data = InvokeWebRequest<List<FetchedExternalTask>>(_fetchAndLockExternalTaskEndpoint, "POST", configuration);
                if (data == null || data.Count == 0)
                {
                    return;
                }

                data.ForEach(externalTask =>
                {
                    var sleepDuration = new Random().Next(100) + 1;
                    Thread.Sleep(sleepDuration);
                    var completedTaskConfiguration = new CompleteExternalTaskConifguration(workerId);
                    var completeTaskUrl = string.Format(_completeExternalTaskEndpoint, externalTask.Id);
                    InvokeWebRequest(completeTaskUrl, "POST", completedTaskConfiguration);
                });
            }
        }

        public void ExtractEventLogToFile()
        {
            var processInstances = GetProcessInstances();

            var lines = new List<string>();
            var lines2 = new List<string>();
            lines.Add("case,activity,completeTime");

            for (int i = 0; i < processInstances.Count; i++)
            {
                var caseNumber = i < 10 ? $"0{i}" : i.ToString();
                var instance = processInstances[i];
                var activities = GetActivitiesForInstance(instance.Id);

                var activityNames = activities.Select(x => x.ActivityName);

                var joinedActivityNames = string.Join(" ", activityNames);
                lines2.Add(joinedActivityNames);

                for (int j = 0; j < activities.Count(); j++)
                {
                    var activity = activities[j];
                    var date = activity.EndTime.AddMinutes(j);

                    lines.Add($"case{caseNumber},{activity.ActivityName},{date.ToUniversalTime().ToString("s", System.Globalization.CultureInfo.InvariantCulture)}");
                    
                }
            }

            File.WriteAllLines("C:\\Users\\josip\\Desktop\\out.csv", lines);
            File.WriteAllLines("C:\\Users\\josip\\Desktop\\out.txt", lines2);
        }

        private List<ProcessInstance> GetProcessInstances()
        {
            var url = $"{_processInstanceEndpoint}?processDefinitionId={ProcessDefinitionId}";
            var response = InvokeWebRequest<List<ProcessInstance>>(url);
            response.Sort((x, y) => x.EndTime.CompareTo(y.EndTime));
            return response;
        }

        private List<ActivityInstance> GetActivitiesForInstance(string processInstanceId)
        {
            var url = $"{_activityInstanceEndpoint}?processInstanceId={processInstanceId}"; //*&activityType=task";
            var response = InvokeWebRequest<List<ActivityInstance>>(url);

            var tasks = response.Where(a => string.Equals(a.ActivityType, "serviceTask", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(a.ActivityType, "task", StringComparison.InvariantCultureIgnoreCase)).ToList();

            tasks.Sort((x, y) => x.EndTime.CompareTo(y.EndTime));
            return tasks;
        }

        private HttpWebRequest PrepareWebRequest(string url, string method, object body)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.ContentType = "application/json";
            if (body != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    var serializedData = JsonConvert.SerializeObject(body, serializerSettings);
                    var data = Encoding.ASCII.GetBytes(serializedData);
                    request.ContentLength = data.Length;
                    stream.Write(data, 0, data.Length);
                }
            }
            return request;
        }

        private T InvokeWebRequest<T>(string url, string method = "GET", object body = null)
        {
            var request = PrepareWebRequest(url, method, body);
            var content = GetRequestResponseContent(request);
            var responseSerialized = JsonConvert.DeserializeObject<T>(content);
            return responseSerialized;
        }

        private WebResponse InvokeWebRequest(string url, string method = "GET", object body = null)
        {
            return PrepareWebRequest(url, method, body).GetResponse();
        }

        private string GetRequestResponseContent(HttpWebRequest request)
        {
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class ProcessInstance
    {
        public string Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ProcessDefinitionName { get; set; }
    }

    public class ActivityInstance
    {
        public string Id { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public string ActivityType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class StartedProcessInstance
    {
        public string Id { get; set; }
        public string DefinitionId { get; set; }
    }

    public class FetchExternalTaskConfiguration
    {
        public string WorkerId { get; set; }
        public int MaxTasks { get; set; }
        public int AsyncResponseTimeout {
            get => _asyncResponseTimeout;
            set
            {
                if (value > 1800000)
                {
                    _asyncResponseTimeout = 1800000;
                    return;
                }
                _asyncResponseTimeout = value;
            }
        }
        public TopicSpecification[] Topics { get; set; }

        public FetchExternalTaskConfiguration(string workerId, int maxTasks, int asyncResponseTimeout, TopicSpecification[] topics)
        {
            WorkerId = workerId;
            MaxTasks = maxTasks;
            AsyncResponseTimeout = asyncResponseTimeout;
            Topics = topics;
        }

        private int _asyncResponseTimeout;
    }

    public class TopicSpecification
    {
        public string TopicName { get; set; }
        public int LockDuration { get; set; }
        public string ProcessDefinitionId { get; set; }

        public TopicSpecification(string topicName, int lockDuration, string processDefinitionId)
        {
            TopicName = topicName;
            LockDuration = lockDuration;
            ProcessDefinitionId = processDefinitionId;
        }
    }

    public class FetchedExternalTask
    {
        public string ActivityId { get; set; }
        public string ActivityInstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string Id { get; set; }
        public string ProcessDefinitionId { get; set; }
    }

    public class CompleteExternalTaskConifguration
    {
        public string WorkerId { get; set; }

        public CompleteExternalTaskConifguration(string workerId)
        {
            WorkerId = workerId;
        }
    }
}
