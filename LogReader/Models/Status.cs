using System.Collections.Generic;

namespace LogReader.Models
{
    public partial class Status
    {
        public bool IsError { get; set; }
        public bool IsProgress { get; set; }
        public string StatusText { get; set; }
        public string PopupText { get; set; }

    }

    public partial class StateCollection
    {
        public Dictionary<string, Status> StateDictionary { get; set; }
    }
}
