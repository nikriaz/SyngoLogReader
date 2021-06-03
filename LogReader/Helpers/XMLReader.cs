using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using LogReader.Models;

namespace LogReader.Helpers
{
    public class XMLReader
    {
        private XMLModel XMLData;
        public ViewResult XMLattempt(MemoryStream inputStream, bool firstAttempt)
        {
            inputStream.Seek(0L, SeekOrigin.Begin);
            ViewResult viewResult = new ViewResult();
            XmlSerializer deserializer = new XmlSerializer(typeof(XMLModel));
            dynamic streamReader = null;

            if (firstAttempt)
            {
                streamReader = new StreamReader(inputStream, Encoding.UTF8);
            } else
            {
                streamReader = new DirtyStreamReader(inputStream);
            }

            try
            {
                using (XmlReader reader = XmlReader.Create(streamReader)) //new StreamReader(inputStream, Encoding.UTF8), settings) //ew DirtyStreamReader(inputStream, (char)32), settings)
                {
                    reader.MoveToContent();
                    XMLData = (XMLModel)deserializer.Deserialize(reader);
                    viewResult.LogList = ConvertFromXML();
                    viewResult.Error = viewResult.LogList.Any() ? "parse-ok" : "empty-xml";

                }
            }
            catch (Exception e)
            {
                if (e.InnerException?.Message?.Contains("invalid character") ?? false)
                {
                    viewResult.Error = "dirty-xml";
                } else
                {
                    viewResult.Error = "parse-txt";
                }
            }

            return viewResult;
        }

        internal List<Message> ConvertFromXML()
        {
            List<Message> logList = new List<Message>(XMLData.messages.Count);

            foreach (var rawMessage in XMLData.messages)
            {
                var message = new Message();
                message.Severity = rawMessage.Severity;
                message.EventDateTime = DateTime.ParseExact(rawMessage.EventDateTime, "dd-MMM-yyyy HH:mm:ss",
                                                        System.Globalization.CultureInfo.InvariantCulture);
                message.EventId = rawMessage.EventId;
                message.SourceName = rawMessage.SourceName;
                int s = 0;
                message.Sequence = int.TryParse(rawMessage.Sequence, out s) ? s : -1;
                message.FTSMessage.MessageText = rawMessage.MessageText;

                logList.Add(message);
            }

            return logList;
        }

        public class DirtyStreamReader : StreamReader
        {
            private readonly char _replacementCharacter;
            public DirtyStreamReader(Stream stream) : base(stream, Encoding.UTF8)
            {
                _replacementCharacter = (char)32;
            }

            public override int Peek()
            {
                var ch = base.Peek();
                if (ch != -1 && IsInvalidChar(ch))
                {
                    return _replacementCharacter;
                }
                return ch;
            }

            public override int Read()
            {
                var ch = base.Read();
                if (ch != -1 && IsInvalidChar(ch))
                {
                    return _replacementCharacter;
                }
                return ch;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var readCount = base.Read(buffer, index, count);
                ReplaceInBuffer(buffer, index, readCount);
                return readCount;
            }

            public override async Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                var readCount = await base.ReadAsync(buffer, index, count).ConfigureAwait(false);
                ReplaceInBuffer(buffer, index, readCount);
                return readCount;
            }

            private void ReplaceInBuffer(char[] buffer, int index, int readCount)
            {
                for (var i = index; i < readCount + index; i++)
                {
                    var ch = buffer[i];
                    if (IsInvalidChar(ch))
                    {
                        buffer[i] = _replacementCharacter;
                    }
                }
            }

            private static bool IsInvalidChar(int ch)
            {
                return IsInvalidChar((char)ch);
            }

            private static bool IsInvalidChar(char ch)
            {
                return !XmlConvert.IsXmlChar(ch);
            }
        }

    }
}