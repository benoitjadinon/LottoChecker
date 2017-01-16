using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace LottoChecker
{
    public class LottoCheckerViewModel : INotifyPropertyChanged
    {
        public ICommand ScanCommand { get; private set; }

        private string _result = "";
        public string Result 
        { 
            get { return _result; }
            private set { 
                _result = value;
                NotifyPropertyChanged(nameof(Result));
            } 
        }

        private void NotifyPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly VisionServiceClient _ocrClient;
		private readonly IBitmapTools _bitmapTools;
  
		private bool _isInitialized = false;


		public LottoCheckerViewModel(IBitmapTools bitmapTools)
        {
            _ocrClient = new VisionServiceClient(ApiKeys.MicrosoftVisionToken);

			_bitmapTools = bitmapTools;
			
            ScanCommand = new Command (async () => {
                Result = "Loading...";
                Result = await ScanAsync() ? "Winning ticket !" : "Lost.";
            }, () => true);
        }


        private async Task<bool> ScanAsync()
        {
            var winningNumbers = await LoadResultsAsync();
            var scannedNumberLines = await ScanTicketAsync();

            return IsTicketWinning(winningNumbers, scannedNumberLines);
        }

        public bool IsTicketWinning(int[] winningNumbers, IEnumerable<int[]> scannedNumberLines)
            => scannedNumberLines.Any(line => line.Count(winningNumbers.Contains) >= 3);

        private Task<int[]> LoadResultsAsync()
        {
            return Task.Run(() =>
            {
                var xmlDoc = XDocument.Load("https://www.e-lotto.be/cache/dgLastResultForGameWithAddons/FR/Lotto6.xml");
                return xmlDoc.Root
                        .Element ("gameevent")
                        .Element ("gamedraws")
                        .Elements("gamedraw").First()
                        .Element ("resultsets")
                        .Elements("resultset").First()
                        .Element ("mainvalues")
                        .Value
                        .Split(',')
                        .Select(XmlConvert.ToInt32)
                        .ToArray()
                    ;
            });
        }

        private async Task<IEnumerable<int[]>> ScanTicketAsync()
        {
            if (!_isInitialized)
                _isInitialized = await CrossMedia.Current.Initialize();

            MediaFile photo;
            if (CrossMedia.Current.IsCameraAvailable)
                photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    SaveToAlbum = false,
                    AllowCropping = true,
                });
            else
                photo = await CrossMedia.Current.PickPhotoAsync();

			long weight;
            while ((weight = _bitmapTools.GetImageWeight(photo.Path)) > 4000000)
            {
                _bitmapTools.ResizeImage(photo.Path, photo.Path, 0.8f);
            }

            using (var photoStream = photo.GetStream())
			{
                return await RecognizeNumberLines(photoStream);
            }

            //return await Recognize("https://goo.gl/photos/PS6nHuH8Y7LW2oWc6");
        }

        private async Task<IEnumerable<int[]>> RecognizeNumberLines(Stream stream)
        {
            return ExtractNumbers(await _ocrClient.RecognizeTextAsync(stream));
        }

        private async Task<IEnumerable<int[]>> RecognizeNumberLines(string url)
        {
            return ExtractNumbers(await _ocrClient.RecognizeTextAsync(url));
        }

        public IEnumerable<int[]> ExtractNumbers(OcrResults ocrResult)
        {
            return
                from region in ocrResult.Regions
                from lines in region.Lines
                where lines.Words.Length >= 6
                select lines.Words.Select(word => word.Text).Where(IsNumeric)
                    .Select(v => Convert.ToInt32(v))
                    .ToArray()
            ;
        }

        private readonly Func<string, bool> IsNumeric =
            x => !string.IsNullOrEmpty(x) && x.ToCharArray().All(char.IsDigit);
    }
}