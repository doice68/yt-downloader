using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ReactiveUI;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using yt_downloader.Views;

namespace yt_downloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        MainWindow window;

        public MainWindowViewModel(MainWindow window)
        {
            this.window = window;
        }

        private string _thumbnailUrl = @"avares://yt downloader/Assets/unknown.png";
        public string thumbnailUrl
        {
            get => _thumbnailUrl;
            set
            {
                _thumbnailUrl = value;
                this.RaisePropertyChanged();
            }
        }

        string _searchText = "";
        public string searchText 
        {
            get => _searchText;
            set 
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
            }
        }

        bool _infoVisible = false;
        public bool infoVisible
        {
            get => _infoVisible;
            set
            {
                this.RaiseAndSetIfChanged(ref _infoVisible, value);

            }
        }

        string _videoTitle = "None";
        public string videoTitle
        {
            get => _videoTitle;
            set 
            {
                this.RaiseAndSetIfChanged(ref _videoTitle, value);

            }
        }

        string _videoArthur = "None";
        public string videoArthur
        {
            get => _videoArthur;
            set
            {
                this.RaiseAndSetIfChanged(ref _videoArthur, value);

            }
        }

        string _videoDuration = "None";
        public string videoDuration
        {
            get => _videoDuration;
            set
            {
                this.RaiseAndSetIfChanged(ref _videoDuration, value);

            }
        }

        #region popups
        //error popup
        string _errorPopupText = "Error";
        public string errorPopupText
        {
            get => _errorPopupText;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorPopupText, value);
            }
        }
        bool _errorPopupOpen = false;
        public bool errorPopupOpen
        {
            get => _errorPopupOpen;
            set 
            {
                this.RaiseAndSetIfChanged(ref _errorPopupOpen, value);
            }
        }
        public void ErrorOk()
        {
            errorPopupOpen = false;
        }
        //loading popup
        string _loadingPopupText = "";
        public string loadingPopupText
        {
            get => _loadingPopupText;
            set
            {
                this.RaiseAndSetIfChanged(ref _loadingPopupText, value);
            }
        }
        bool _loadingPopupOpen = false;
        public bool loadingPopupOpen
        {
            get => _loadingPopupOpen;
            set
            {
                this.RaiseAndSetIfChanged(ref _loadingPopupOpen, value);
            }
        }
        int _loadingProgress = 0;
        public int loadingProgress
        {
            get => _loadingProgress;
            set
            {
                this.RaiseAndSetIfChanged(ref _loadingProgress, value);
            }
        }

        bool _downloadPopupOpen = false;
        public bool downloadPopupOpen
        {
            get => _downloadPopupOpen;
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadPopupOpen, value);
            }
        }
        #endregion

        async void DownloadThumbnail(string url)
        {
            loadingPopupText = "Getting video information.";
            var youtube = new YoutubeClient();

            // You can specify either the video URL or its ID
            var videoUrl = url;
            Video video = null;
            try
            {
                loadingPopupOpen = true;
                video = await youtube.Videos.GetAsync(videoUrl);
            }
            catch (System.ArgumentException)
            {
                loadingPopupOpen = false;
                errorPopupOpen = true;
                errorPopupText = "Something went wrong.";
                return;
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadDataAsync(new Uri(video.Thumbnails[1].Url));
                client.DownloadProgressChanged += (s, args) =>
                {
                    loadingProgress = args.ProgressPercentage;
                };
                client.DownloadDataCompleted += (s, args) => 
                {
                    Stream stream = new MemoryStream(args.Result);
                    Bitmap bitmap = new Bitmap(stream);
                    File.Delete("thumbnail.png");
                    bitmap.Save("thumbnail.png");
                    thumbnailUrl = "thumbnail.png";

                    videoTitle = video.Title;
                    videoArthur = video.Author.ChannelTitle;
                    videoDuration = video.Duration.ToString();
                    loadingPopupOpen = false;
                    infoVisible = true;
                };
            }
        }
        public void Search() 
        {
            if (Design.IsDesignMode)
                return;

            DownloadThumbnail(searchText);
        }
        public void OpenDownloadPopup() 
        {
            if (string.IsNullOrWhiteSpace(searchText) == true)
            {
                errorPopupOpen = true;
                return;
            }
            downloadPopupOpen = true;
        }
        public async void Download(string parameter) 
        {
            downloadPopupOpen = false;
            loadingPopupOpen = true;
            loadingPopupText = "Downloading media.";

            var parameters = parameter.Split(' ');
            var fileType = parameters[0];
            var quality = parameters[1];

            var youtube = new YoutubeClient();

            var videoUrl = searchText;
            var video = await youtube.Videos.GetAsync(videoUrl);
            
            var manifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            Stream stream;

            if (fileType == "mp4") 
            {
                var streamInfo = manifest
                    .GetVideoOnlyStreams()
                    .Where(s => s.VideoQuality.Label == quality)
                    .GetWithHighestVideoQuality();

                stream = await youtube.Videos.Streams.GetAsync(streamInfo);

                OpenFolderDialog dialog = new OpenFolderDialog();

                var dir = await dialog.ShowAsync(window);
                if (dir != null)
                {
                    await youtube.Videos.Streams.DownloadAsync(
                        streamInfo,
                        $@"{dir}\{video.Title}.{fileType}");
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", $@"{dir}\{video.Title}.{fileType}"));
                }
            }
            else if(fileType == "mp3")
            {
                var streamInfo = manifest
                    .GetAudioOnlyStreams()
                    .GetWithHighestBitrate();
                try
                {
                    stream = await youtube.Videos.Streams.GetAsync(streamInfo);
                }
                catch (Exception)
                {
                    errorPopupText = "Something went wrong.";
                    errorPopupOpen = true;
                }

                OpenFolderDialog dialog = new OpenFolderDialog();

                var dir = await dialog.ShowAsync(window);
                if (dir != null)
                {
                    await youtube.Videos.Streams.DownloadAsync(
                        streamInfo,
                        $@"{dir}\{video.Title}.{fileType}");
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", $@"{dir}\{video.Title}.{fileType}"));
                }
            }
            loadingPopupOpen = false;
        }
        public void Cancel() 
        {
            downloadPopupOpen = false;
        }
        public void About() 
        {
            var uri = "https://github.com/doice68/yt-downloader";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = uri
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}
