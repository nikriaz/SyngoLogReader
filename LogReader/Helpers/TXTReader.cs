using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogReader.Models;

namespace LogReader.Helpers
{
    class TXTReader
    {
        private int dtFormatSwitcher = 0;
        private char[] severities = Variables.severities.Select(s => char.Parse(s.Severity)).ToArray();
        public async Task<ViewResult> TXTattemptAsync(MemoryStream inputStream, CancellationToken token)
        {
            inputStream.Seek(0L, SeekOrigin.Begin);
            ViewResult viewResult = new ViewResult();
            int attemptedLines = 0;

            using (StreamReader reader = new StreamReader(inputStream, Encoding.UTF8))
            {
                string s = String.Empty;
                while ((s = await reader.ReadLineAsync()) != null)
                {
                    if (token.IsCancellationRequested)
                    {
                        viewResult.Error = "file-cancel";
                        return viewResult;
                    }
                    
                    var message = await Task.Run(() => LineFinder(s));
                    attemptedLines++;
                    if (message != null)
                    {
                        viewResult.LogList.Add(message);
                    }

                    if (attemptedLines == 100 && !viewResult.LogList.Any())
                    {
                        viewResult.Error = "parse-error";
                        return viewResult;
                    }
                }
            }

            viewResult.Error = viewResult.LogList.Any() ? "parse-ok" : "parse-error";
            return viewResult;
        }

        internal Message LineFinder(string s)
        {
            var message = new Message();
            var rowDt = default(DateTime);
            bool result = false;
            int eventId = 0;
            int seq = 0;

            string[] splitLine = s.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            if (splitLine.Length > 6 && splitLine[0].Length == 1)
            {
                var c = splitLine[0][0];
                result = severities.Contains(c);
            }

            if (result)
            {
                string[] dt = new string[2];
                Array.Copy(splitLine, 1, dt, 0, 2);
                //var dt = split.Skip(1).Take(2).ToArray(); //alternative to Array.Copy. Performance TBD.
                var ds = string.Join(" ", dt);

                switch (dtFormatSwitcher) //we use switcher to detect data format in the first occurance only to avoid checking of every row
                {
                    case 1:
                        result = DateTime.TryParseExact(ds, "dd-MMM-yyyy HH:mm:ss",
                                          CultureInfo.InvariantCulture, DateTimeStyles.None, out rowDt);
                        break;

                    case 2:
                        result = DateTime.TryParseExact(ds, "dd-MMM-yy HH:mm:ss",
                                          CultureInfo.InvariantCulture, DateTimeStyles.None, out rowDt);
                        break;

                    default:
                        result = DateTime.TryParseExact(ds, "dd-MMM-yyyy HH:mm:ss",
                                          CultureInfo.InvariantCulture, DateTimeStyles.None, out rowDt);
                        if (result)
                        {
                            dtFormatSwitcher = 1;
                        } else
                        {
                            result = DateTime.TryParseExact(ds, "dd-MMM-yy HH:mm:ss",
                                          CultureInfo.InvariantCulture, DateTimeStyles.None, out rowDt);
                            if (result) dtFormatSwitcher = 2;
                        }
                        break;
                }
            }

            if (result)
            {
                message.Severity = splitLine[0];
                message.EventDateTime = rowDt;
                result = Int32.TryParse(splitLine[3], out eventId);
            }

            if (result)
            {
                message.EventId = eventId;
                message.SourceName = splitLine[4];
                result = Int32.TryParse(splitLine[5], out seq);
            }

            if (result)
            {
                message.Sequence = seq;
                var mtLenght = splitLine.Length - 6;
                string[] mt = new string[mtLenght];
                Array.Copy(splitLine, 6, mt, 0, mtLenght);
                message.FTSMessage.MessageText = string.Join(" ", mt);
            }

            if (!result) message = null;

            return message;
        }
    }
}
