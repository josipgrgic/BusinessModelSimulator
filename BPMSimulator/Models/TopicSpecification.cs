using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModelSimulator.Models
{
    class TopicSpecification
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
}
