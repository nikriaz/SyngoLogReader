using System;
using System.Collections.Generic;

namespace LogReader.Models
{
    public class ViewResult
    {
        public List<Message> LogList { get; set; } = new List<Message>();
        public DateTime ViewMinDt { get; set; }
        public DateTime ViewMaxDt { get; set; }
        public int Counter { get; set; }
        public string Error { get; set; }
    }
}
