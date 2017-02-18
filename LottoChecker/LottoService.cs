using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using Reactive.Bindings;

namespace LottoChecker
{
	public class LottoService
	{
		private readonly VisionServiceClient _ocrClient;
		private readonly IBitmapTools _bitmapTools;

		private bool _isInitialized = false;

		public LottoService(IBitmapTools bitmapTools)
		{
			_ocrClient = new VisionServiceClient(ApiKeys.MicrosoftVisionToken);
			_bitmapTools = bitmapTools;

			Observable.Timer(TimeSpan.FromSeconds(2)) //TODO : when network is ready instead
			          .SubscribeOn(TaskPoolScheduler.Default)
			          .Select(_ => LoadResultsAsync())
			          //.ObserveOn(ReactivePropertyScheduler.Default)
			          .Subscribe(_resultsDate);
		}


		private readonly Subject<LottoResults> _resultsDate = new Subject<LottoResults>();
		public IObservable<LottoResults> ResultsDate => _resultsDate.AsObservable();


		public async Task<bool> ScanAsync()
		{
			var scannedNumberLines = await ScanTicketAsync();

			return false;//IsTicketWinning(winningNumbers, scannedNumberLines);
		}

		public bool IsTicketWinning(int[] winningNumbers, IEnumerable<int[]> scannedNumberLines)
			=> scannedNumberLines.Any(line => line.Count(winningNumbers.Contains) >= 3);

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
				select lines.Words.Select(word => word.Text)
					.Where(IsNumeric)
					.Select(v => Convert.ToInt32(v))
					.ToArray()
				;
		}

		private readonly Func<string, bool> IsNumeric =
			x => !string.IsNullOrEmpty(x) && x.ToCharArray().All(char.IsDigit);
		IBitmapTools bitmapTools;

		public LottoResults LoadResultsAsync()
		{
			var xmlDoc = //await Task.Run(() => 
                XDocument.Load("https://www.e-lotto.be/cache/dgLastResultForGameWithAddons/FR/Lotto6.xml");

			return new LottoResults
			{
				PublishDate = DateTime.ParseExact(xmlDoc.Root
					.Element("gameevent")
					.Element("resulttime")
					.Value, "s", null)// 2017-02-18T20:02:28
				,
				NumberLines = xmlDoc.Root
					.Element("gameevent")
					.Element("gamedraws")
					.Elements("gamedraw")
					.First()
					.Element("resultsets")
					.Elements("resultset")
					.First()
					.Element("mainvalues")
					.Value
					.Split(',')
					.Select(x => XmlConvert.ToInt32(x))
					.ToArray()
			};
		}
	}
}
