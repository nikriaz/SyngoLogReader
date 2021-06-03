using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogReader.Models;

namespace LogReader.Helpers
{
    public interface IFileNavigator
    {
        event EventHandler<string> StatusChanged;
        Task OpenFileAsync(CancellationToken token);
    }

    public class FileNavigator : IFileNavigator
    {
        private MemoryStream _memoryStream; 
        private readonly ISQLActions _sqlActions;

        public event EventHandler<string> StatusChanged;
        protected virtual void OnStatusChanged(string statusText)
        {
            StatusChanged?.Invoke(this, statusText);
        }

        public FileNavigator(ISQLActions sqlActions)
        {
            _sqlActions = sqlActions;
        }

        public async Task OpenFileAsync(CancellationToken token)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "All files (*.*)|*.*|XML files (*.xml)|*.xml|TXT files (*.txt)|*.txt|Log files (*.log)|*.log|GZ arhives (*.gz, *.z)|*.gz;*.z|Zip archives (*.zip)|*.zip";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            bool fileSuccess = true;

            if (openFileDialog.ShowDialog() == true)
            {
                _memoryStream = new MemoryStream();
                Variables.selectedPath = Path.GetDirectoryName(openFileDialog.FileName) + "//";

                try
                {
                    using (Stream inputStream = openFileDialog.OpenFile())
                    {
                        await inputStream.CopyToAsync(_memoryStream).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    OnStatusChanged("file-error");
                    fileSuccess = false;
                }

                if (_memoryStream.Length < 50)
                {
                    OnStatusChanged("file-small");
                    fileSuccess = false;
                }

                if (fileSuccess) fileSuccess = await DetectArchivesAsync();
                if (fileSuccess) await ParseAttemptAsync(token);
            }
            else
            {
                OnStatusChanged("file-cancel");
            }
        }
        private async Task<bool> DetectArchivesAsync()
        {
            byte[] headerBytes = new byte[3];
            _memoryStream.Seek(0L, SeekOrigin.Begin);
            var read = _memoryStream.Read(headerBytes, 0, headerBytes.Length); //read returns either the readed lenght or 0 if the end of the file was reached

            string fileType = null;

            foreach (var type in Variables.knownFilesList)
            {
                bool equal = true;
                for (int i = 0; i < type.Signature.Length; i++) //supports variable lenght of file types signatures for the future use
                {
                    equal = type.Signature[i] == headerBytes[i];
                }

                if (equal)
                {
                    fileType = type.Extension;
                    break;
                }
            }

            switch (fileType)
            {
                case "gz":
                    OnStatusChanged("archive-gz");
                    _memoryStream.Seek(0L, SeekOrigin.Begin);
                    _memoryStream = new Decompressor().GZipDecompress(_memoryStream);
                    break;

                case "zip":
                    OnStatusChanged("archive-zip");
                    _memoryStream.Seek(0L, SeekOrigin.Begin);
                    _memoryStream = new Decompressor().ZipDecompress(_memoryStream);
                    break;
            }

            if (_memoryStream == null)
            {
                OnStatusChanged("archive-error");
                return false;
            }

            if (fileType != null) await Task.Delay(500); //since decompressing is very fast, add delay to display type of archive

            return true;
        }

        private async Task ParseAttemptAsync(CancellationToken token)
        {
            var viewResult = new ViewResult();

            OnStatusChanged("parse-xml");
            viewResult = await Task.Run(() => new XMLReader().XMLattempt(_memoryStream, true));
            OnStatusChanged(viewResult.Error);

            if (viewResult.Error == "dirty-xml")
            {
                _memoryStream.Seek(0L, SeekOrigin.Begin);
                viewResult = await Task.Run(() => new XMLReader().XMLattempt(_memoryStream, false));
                OnStatusChanged(viewResult.Error);
            }

            if (viewResult.Error == "parse-txt")
            {
                viewResult = await new TXTReader().TXTattemptAsync(_memoryStream, token);
                OnStatusChanged(viewResult.Error);
            }

            if (viewResult.Error == "parse-ok")
            {
                OnStatusChanged("save-sql");
                await _sqlActions.SQLSaveAsync(viewResult.LogList);
            }
            else
            {
                OnStatusChanged(viewResult.Error);
            }

        }

    }
}