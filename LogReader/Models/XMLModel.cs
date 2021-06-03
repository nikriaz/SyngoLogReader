using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LogReader.Models
{
    /// <summary>
    /// This class mimics original Siemens XML structure
    /// </summary>

    [XmlRoot("extracted_messages")]
    [Serializable]
    public class XMLModel
    {
        public QuerySummary query_summary = new QuerySummary();
        [XmlElement("message")]
        public List<XMLMessage> messages = new List<XMLMessage>();
    }
    public class XMLMessage
    {
        [XmlElement("severity")]
        public string Severity { get; set; }
        [XmlElement("date_time")]
        public string EventDateTime { get; set; }
        [XmlElement("id")]
        public int EventId { get; set; }
        [XmlElement("source_comp")]
        public string SourceName { get; set; }
        [XmlElement("sequence")]
        public string Sequence { get; set; }
        [XmlIgnore]
        public string MessageText { get; set; }
        [XmlElement("message_text")]
        public System.Xml.XmlCDataSection MyStringCDATA
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(MessageText);
            }
            set
            {
                MessageText = value?.Value;
            }
        }
    }

    public class QuerySummary
    {
        public Filter filter = new Filter();
    }

    public class Filter
    {
        public int application = 1;
        public int customerlog = 0;
        public int security = 0;
        public int system = 0;
        public int error;
        public int warning;
        public int information;
        public int success;
        public string facility = "Service";
        public string messagetext;
        public string messageid;
        public string srcname;
        public int usetime = 1;
        public int absolutetime = 1;
        public int frommonth;
        public int fromdate;
        public int fromyear;
        public int fromhour;
        public int frommin;
        public int fromsec;
        public int todate;
        public int tomonth;
        public int toyear;
        public int tohour;
        public int tomin;
        public int tosec;
        public int relativetime = 0;
        public int duration;
        public string dayhour;
        public int messagecount;
        public string searchorder = "Newest first";

    }
}
