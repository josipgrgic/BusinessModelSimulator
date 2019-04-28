using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModelSimulator.Models
{
    class ActivityInstance
    {
        public string Id { get; set; }
        public string ProcessInstanceId { get; set; }
        public string ActivityId { get; set; }
        public string ActivityName { get; set; }
        public string ActivityType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
