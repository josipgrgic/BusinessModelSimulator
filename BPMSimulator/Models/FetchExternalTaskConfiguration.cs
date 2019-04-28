using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModelSimulator.Models
{
    class FetchExternalTaskConfiguration
    {
        public string WorkerId { get; set; }
        public int MaxTasks { get; set; }
        public int AsyncResponseTimeout
        {
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
}
