using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Markup;

namespace LogReader.Models
{
    [ContentProperty("Items")]
    public class Message
    {
        public Message()
        {
            this.FTSMessage = new FTSMessage();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RowId { get; set; }
        public string Severity { get; set; }
        public DateTime EventDateTime { get; set; }
        public int EventId { get; set; }
        public string SourceName { get; set; }
        public int Sequence { get; set; }
        public virtual FTSMessage FTSMessage { get; set; }

    }

    public class FTSMessage
    {
        public int RowId { get; set; }
        public string MessageText { get; set; }
        public string Match { get; set; }
        public double? Rank { get; set; }
        public virtual Message Message { get; set; }

    }

}
