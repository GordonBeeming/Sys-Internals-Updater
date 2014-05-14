using System.Collections.ObjectModel;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Windows.Input;
using System.IO;

namespace SysInternalsUpdater
{
    internal class LiveSysInternals : ViewModelBase
    {
        public static readonly string LiveSysInternalsUrl = "http://live.sysinternals.com/";

        public LiveSysInternals()
        {
            RefreshContentCommand = new RelayCommand(o => RefreshContent());
            DownloadFileCommand = new RelayCommand(o => DownloadFile(o));
            DownloadAllFilesCommand = new RelayCommand(o => DownloadAllFiles());

            RefreshContent();
        }

        public delegate void SaveFilesToChangedDelegate(string location);
        public event SaveFilesToChangedDelegate SaveFilesToChanged;

        private ObservableCollection<SysInternalFile> files = new ObservableCollection<SysInternalFile>();
        public ObservableCollection<SysInternalFile> Files
        {
            get
            {
                return files;
            }
            set
            {
                base.UpdateProperty(ref files, value);
            }
        }

        private ObservableCollection<string> errors = new ObservableCollection<string>();
        public ObservableCollection<string> Errors
        {
            get
            {
                return errors;
            }
            set
            {
                base.UpdateProperty(ref errors, value);
            }
        }

        private string saveFilesTo = Properties.Settings.Default.LastSaveToLocation;
        public string SaveFilesTo
        {
            get
            {
                return saveFilesTo;
            }
            set
            {
                SaveFilesToGood = Directory.Exists(value);
                if (SaveFilesToGood)
                {
                    base.UpdateProperty(ref saveFilesTo, value);
                    Properties.Settings.Default.LastSaveToLocation = value;
                    Properties.Settings.Default.Save();
                    SaveFilesToChanged(value);
                }
            }
        }

        private bool saveFilesToGood = false;
        public bool SaveFilesToGood
        {
            get
            {
                return saveFilesToGood;
            }
            set
            {

                base.UpdateProperty(ref saveFilesToGood, value);
            }
        }

        public async Task RefreshContent()
        {
            string liveSysInternalsHtmlContent = await new WebClient().DownloadStringTaskAsync(LiveSysInternalsUrl);
            liveSysInternalsHtmlContent = liveSysInternalsHtmlContent.Remove(0, liveSysInternalsHtmlContent.IndexOf(" < pre>") + 5);
            liveSysInternalsHtmlContent = liveSysInternalsHtmlContent.Remove(liveSysInternalsHtmlContent.LastIndexOf("<br>"));
            while (liveSysInternalsHtmlContent.Contains("<br>"))
            {
                SysInternalFile file = null;
                try
                {
                    string thisLine = liveSysInternalsHtmlContent.Remove(liveSysInternalsHtmlContent.IndexOf("<br>")).Trim();
                    liveSysInternalsHtmlContent = liveSysInternalsHtmlContent.Remove(0, liveSysInternalsHtmlContent.IndexOf("<br>") + 4);
                    Regex reg = new Regex(@"(?i)<a href=""([^>]+)"">(.+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match match = reg.Match(thisLine);
                    if (!match.Success)
                    {
                        continue;
                    }
                    string relativePath = match.Groups[1].Value;
                    file = Files.FirstOrDefault(o => o.RelativePath == relativePath);
                    if (file == null)
                    {
                        file = new SysInternalFile(this, match, thisLine);
                        if (file.FileSize != 0)
                        {
                            Files.Add(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (file != null && Files.Contains(file))
                    {
                        Files.Remove(file);
                    }
                    errors.Add(ex.Message + Environment.NewLine);
                }
            }
        }

        public async Task DownloadFile(object cmdRelPath)
        {
            SysInternalFile file = Files.FirstOrDefault(o => o.RelativePath == cmdRelPath.ToString());
            if (file != null)
            {
                file.DownloadFile();
            }
        }

        private async Task DownloadAllFiles()
        {
            foreach (SysInternalFile file in Files)
            {
                await file.DownloadFile();
            }
        }

        public static ICommand RefreshContentCommand { get; private set; }
        public static ICommand DownloadFileCommand { get; internal set; }
        public static ICommand DownloadAllFilesCommand { get; internal set; }

    }
}