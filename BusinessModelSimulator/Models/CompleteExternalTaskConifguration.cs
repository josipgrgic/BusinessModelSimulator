using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModelSimulator.Models
{
    class CompleteExternalTaskConifguration
    {
        public string WorkerId { get; set; }

        public CompleteExternalTaskConifguration(string workerId)
        {
            WorkerId = workerId;
        }
    }
}
