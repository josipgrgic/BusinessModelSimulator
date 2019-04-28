using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModelSimulator.Models
{
    class FetchedExternalTask
    {
        public string ActivityId { get; set; }
        public string ActivityInstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string Id { get; set; }
        public string ProcessDefinitionId { get; set; }
    }
}
