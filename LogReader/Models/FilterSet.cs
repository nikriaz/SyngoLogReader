using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogReader.Models
{
    public class FilterSet
    {
        public string[] SeveretiesChecked { get; set; }
        public string MessageText { get; set; }
        public string MessageId { get; set; }
        public string SourceName { get; set; }
        public DateTime FromDt { get; set; }
        public DateTime ToDt { get; set; }

    }
}
