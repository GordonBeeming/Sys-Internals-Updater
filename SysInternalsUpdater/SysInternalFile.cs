using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SysInternalsUpdater
{
    [DebuggerDisplay("{FileName} ({FileSize}kb) - {LastChangedOn}")]
    internal class SysInternalFile : ViewModelBase
    {
        private LiveSysInternals sysInternals;
        public string SaveToDirectory { get; set; }

        public SysInternalFile(LiveSysInternals liveSysInternals, Match match, string line)
        {
            this.sysInternals = liveSysInternals;
            this.match = match;
            this.SaveToDirectory = liveSysInternals.SaveFilesTo;

            RelativePath = match.Groups[1].Value;
            FileName = match.Groups[2].Value;
            if (FileName.ToLower().EndsWith(".exe") || FileName.ToLower().EndsWith(".chm") || FileName.ToLower().EndsWith(".hlp"))
            {
                line = line.Remove(match.Index).Trim();
                FileSize = Convert.ToInt32(line.Remove(0, line.LastIndexOf(" ")).Trim());
                line = line.Remove(line.LastIndexOf(" "));
                LastChangedOn = Convert.ToDateTime(line.Trim());
                IsNew = DateTime.UtcNow.Subtract(LastChangedOn).TotalDays < 7;
                UpdateCommandBools();

                this.sysInternals.SaveFilesToChanged += LiveSysInternals_SaveFilesToChanged;
            }
        }

        public async Task DownloadFile()
        {
            if (!IsDownloading && (ShowDownloadNow || ShowUpdateNow))
            {
                IsDownloading = true;
                try
                {
                    WebClient client = new WebClient();
                    await client.DownloadFileTaskAsync(DownloadFrom, SaveFileTo + ".tmp");
                    File.Delete(SaveFileTo);
                    File.Move(SaveFileTo + ".tmp", SaveFileTo);
                    File.SetCreationTime(SaveFileTo, LastChangedOn);
                    IsDownloading = false;
                    UpdateCommandBools();
                }
                catch (Exception ex)
                {
                    IsDownloading = false;
                    MessageBox.Show(ex.ToString(), "error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    if (File.Exists(SaveFileTo + ".tmp"))
                    {
                        try
                        {
                            File.Delete(SaveFileTo + ".tmp");
                        }
                        catch
                        { }
                    }
                }
            }
        }

        private string fileName = string.Empty;
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                base.UpdateProperty(ref fileName, value);
            }
        }

        private string downloadFrom = string.Empty;
        public string DownloadFrom
        {
            get
            {
                return downloadFrom;
            }
            set
            {
                base.UpdateProperty(ref downloadFrom, value);
            }
        }

        private bool isDownloading = false;
        public bool IsDownloading
        {
            get
            {
                return isDownloading;
            }
            set
            {
                base.UpdateProperty(ref isDownloading, value);
                UpdateCommandBools();
            }
        }

        private int fileSize = 0;
        public int FileSize
        {
            get
            {
                return fileSize;
            }
            set
            {
                base.UpdateProperty(ref fileSize, value);
                base.RaisePropertyChanged("FileSizeString");
            }
        }

        public string FileSizeString
        {
            get
            {
                return (fileSize / 1000D).ToString("F2") + "kb";
            }
        }

        private bool isNew = false;
        public bool IsNew
        {
            get
            {
                return isNew;
            }
            set
            {
                base.UpdateProperty(ref isNew, value);
                base.RaisePropertyChanged("IsNewVisibility");
            }
        }

        public Visibility IsNewVisibility
        {
            get
            {
                return isNew ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private bool showDownloadNow = false;
        public bool ShowDownloadNow
        {
            get
            {
                return showDownloadNow;
            }
            set
            {
                base.UpdateProperty(ref showDownloadNow, value);
            }
        }

        private bool showUpdateNow = false;
        public bool ShowUpdateNow
        {
            get
            {
                return showUpdateNow;
            }
            set
            {
                base.UpdateProperty(ref showUpdateNow, value);
                base.RaisePropertyChanged("LastChangedOn");
                base.RaisePropertyChanged("LastChangedOnDisplay");
                base.RaisePropertyChanged("CurrentFileDate");
                base.RaisePropertyChanged("CurrentFileDateDisplay");
                base.RaisePropertyChanged("ShowUpdateVisibility");
            }
        }

        private bool showOpen = false;
        public bool ShowOpen
        {
            get
            {
                return showOpen;
            }
            set
            {
                base.UpdateProperty(ref showOpen, value);
            }
        }

        public Visibility ShowUpdateVisibility
        {
            get
            {
                return ShowUpdateNow ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private string saveFileTo = string.Empty;
        private Match match;

        public string SaveFileTo
        {
            get
            {
                return saveFileTo;
            }
            set
            {
                base.UpdateProperty(ref saveFileTo, value);
            }
        }

        private void LiveSysInternals_SaveFilesToChanged(string location)
        {
            this.SaveToDirectory = location;
            UpdateCommandBools();
        }

        public void UpdateCommandBools()
        {
            SaveFileTo = Path.Combine(SaveToDirectory, RelativePath.TrimStart('/'));
            DownloadFrom = Path.Combine(LiveSysInternals.LiveSysInternalsUrl, RelativePath.TrimStart('/'));
            ShowDownloadNow = !File.Exists(SaveFileTo) && !IsDownloading;
            ShowUpdateNow = File.Exists(SaveFileTo) && !IsDownloading && LastChangedOn != File.GetCreationTime(SaveFileTo);
            ShowOpen = File.Exists(SaveFileTo) && !IsDownloading;
        }

        private string relativePath = string.Empty;
        public string RelativePath
        {
            get
            {
                return relativePath;
            }
            set
            {
                base.UpdateProperty(ref relativePath, value);
            }
        }

        private DateTime lastChangedOn = DateTime.MinValue;
        public DateTime LastChangedOn
        {
            get
            {
                return lastChangedOn;
            }
            set
            {
                base.UpdateProperty(ref lastChangedOn, value);
            }
        }

        public DateTime? CurrentFileDate
        {
            get
            {
                if (File.Exists(SaveFileTo))
                {
                    return File.GetCreationTime(SaveFileTo);
                }
                return null;
            }
        }

        public string LastChangedOnDisplay
        {
            get
            {
                return LastChangedOn.ToString("dd MMM yyyy");
            }
        }

        public string CurrentFileDateDisplay
        {
            get
            {
                return CurrentFileDate.HasValue ? CurrentFileDate.Value.ToString("dd MMM yyyy") : string.Empty;
            }
        }
    }
}