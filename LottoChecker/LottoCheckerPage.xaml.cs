using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace LottoChecker
{
	public partial class LottoCheckerPage : ContentPage
	{
		public LottoCheckerPage()
		{
			InitializeComponent();
		}


		protected override async void OnAppearing()
		{
			base.OnAppearing();

		    this.BindingContext = new LottoCheckerViewModel();
		}
	}

    public class LottoCheckerViewModel
    {
        public ICommand ScanCommand { get; private set; }

        private readonly VisionServiceClient _ocrClient = new VisionServiceClient("_API_KEY_HERE_");
        private bool _isInitialized = false;


        public LottoCheckerViewModel()
        {
            ScanCommand = new Command (async () => await ScanAsync(), () => true);
        }


        private async Task<bool> ScanAsync()
        {
            var winningNumbers = await LoadResultsAsync();
            var scannedNumberLines = await ScanTicketAsync();

            return IsTicketWinning(winningNumbers, scannedNumberLines);
        }

        private bool IsTicketWinning(int[] winningNumbers, IEnumerable<int[]> scannedNumberLines)
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
            if (!_isInitialized);
                _isInitialized = await CrossMedia.Current.Initialize();

            MediaFile photo;
            if (CrossMedia.Current.IsCameraAvailable)
                photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    SaveToAlbum = false,
                });
            else
                photo = await CrossMedia.Current.PickPhotoAsync();

            using (var photoStream = photo.GetStream())
                return await RecognizeNumberLines(photoStream); // 413 req entity too large

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

        private IEnumerable<int[]> ExtractNumbers(OcrResults result)
        {
            return
                from r in result.Regions
                from l in r.Lines
                from w in l.Words
                where l.Words.Length >= 6 && l.Words.Length <= 7
                select l.Words.Reverse().Take(6).Reverse().Select(Convert.ToInt32).ToArray()
            ;
        }
    }
}
