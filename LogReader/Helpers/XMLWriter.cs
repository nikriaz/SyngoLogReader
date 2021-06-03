using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using LogReader.Models;

namespace LogReader.Helpers
{
    public interface IXMLWriter
    {
        string SaveFile(ViewResult viewResult, FilterSet filterSet);
    }
    public class XMLWriter : IXMLWriter
    {
        private ViewResult _viewResult;
        private FilterSet _filterSet;
        private int duration;
        private string dayhour;
        private string fileNameToSave;

        public  string SaveFile (ViewResult viewResult, FilterSet filterSet)
        {
            _viewResult = viewResult;
            _filterSet = filterSet;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                var span = _viewResult.ViewMaxDt - _viewResult.ViewMinDt;
                var daysDiff = span.Days;
                if (daysDiff > 0)
                {
                    duration = daysDiff + 1;
                    dayhour = "day";
                }
                else
                {
                    duration = span.Hours + 1;
                    dayhour = "hour";
                }

                if (duration > 1) dayhour += "s";

                var fileName = _viewResult.ViewMaxDt.ToString("dd-MM-yyyy") + "_" + duration.ToString() + dayhour + ".xml";

                saveFileDialog.InitialDirectory = Variables.selectedPath;
                saveFileDialog.Filter = "XML files(*.xml) | *.xml";
                saveFileDialog.FileName = fileName;
                saveFileDialog.Title = "Export " + _viewResult.Counter.ToString() + " messages for " + duration.ToString() + " " + dayhour + " to XML file";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileNameToSave = saveFileDialog.FileName;
                    try
                    {
                        var export = ConvertToXML();
                        WriteXML(export);
                    }
                    catch (Exception e)
                    {

                        return "save-error";
                    }

                    return "save-ok";
                }
            }

            return "file-cancel";
        }
        private XMLModel ConvertToXML()
        {
            var allMessages = new List<XMLMessage>();
            var severities = Variables.severities.ToArray();

            foreach (var message in _viewResult.LogList)
            {
                var rawMessage = new XMLMessage();
                
                rawMessage.Severity = message.Severity;
                switch (message.Severity)
                {
                    case "E":
                        severities[0].IsChecked = true;
                        break;
                    case "W":
                        severities[1].IsChecked = true;
                        break;
                    case "I":
                        severities[2].IsChecked = true;
                        break;
                    case "S":
                        severities[3].IsChecked = true;
                        break;
                }

                rawMessage.EventDateTime = message.EventDateTime.ToString("dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                rawMessage.EventId = message.EventId;
                rawMessage.SourceName = message.SourceName;
                rawMessage.Sequence = message.Sequence == -1 ? "n/a" : message.Sequence.ToString();
                rawMessage.MessageText = message.FTSMessage.MessageText;

                allMessages.Add(rawMessage);
            } 
            
            var export = new XMLModel(){ messages = allMessages};

            var secondary = 1;
            for (int i = 0; i < severities.Length; i++)
            {
                var mark = Convert.ToInt32(severities[i].IsChecked) * secondary;
                if (mark == 1) secondary = -1; //It looks like the first checked severity Siemens considers as a "main" and marks it as 1, and the following checked severities as "secondary" and marks them as "-1"
                switch (severities[i].Severity)
                {
                    case "E":
                        export.query_summary.filter.error = mark;
                        break;
                    case "W":
                        export.query_summary.filter.warning = mark;
                        break;
                    case "I":
                        export.query_summary.filter.information = mark;
                        break;
                    case "S":
                        export.query_summary.filter.success = mark;
                        break;
                }
            }

            export.query_summary.filter.messagetext = _filterSet.MessageText;
            export.query_summary.filter.messageid = _filterSet.MessageId;
            export.query_summary.filter.srcname = string.IsNullOrWhiteSpace(_filterSet.SourceName) ? "*" : _filterSet.SourceName;

            export.query_summary.filter.fromdate = _viewResult.ViewMinDt.Day;
            export.query_summary.filter.frommonth = _viewResult.ViewMinDt.Month;
            export.query_summary.filter.fromyear = _viewResult.ViewMinDt.Year;
            export.query_summary.filter.fromhour = _viewResult.ViewMinDt.Hour;
            export.query_summary.filter.frommin = _viewResult.ViewMinDt.Minute;
            export.query_summary.filter.fromsec = _viewResult.ViewMinDt.Second;
            export.query_summary.filter.todate = _viewResult.ViewMaxDt.Day;
            export.query_summary.filter.tomonth = _viewResult.ViewMaxDt.Month;
            export.query_summary.filter.toyear = _viewResult.ViewMaxDt.Year;
            export.query_summary.filter.tohour = _viewResult.ViewMaxDt.Hour;
            export.query_summary.filter.tomin = _viewResult.ViewMaxDt.Minute;
            export.query_summary.filter.tosec = _viewResult.ViewMaxDt.Second;

            export.query_summary.filter.duration = duration;
            export.query_summary.filter.dayhour = dayhour;

            export.query_summary.filter.messagecount = _viewResult.Counter;

            return export;
        }
        private void WriteXML(XMLModel export)
        {
            XmlSerializer xml = new XmlSerializer(typeof(XMLModel));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            Encoding utf8WithoutBom = new UTF8Encoding(false);

            using (StreamWriter writer = new StreamWriter(fileNameToSave, false, utf8WithoutBom))
            {
                xml.Serialize(writer, export, ns);
            }
        }
    }
}
