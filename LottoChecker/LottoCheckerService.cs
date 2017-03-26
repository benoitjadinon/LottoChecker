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
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;

namespace LottoChecker
{
	public class LottoCheckerService
	{
		private const uint MinimumNumbersToWin = 3;
		private const uint MinimumNumberRows = 6;

		private readonly VisionServiceClient _ocrClient;
		private readonly IBitmapTools _bitmapTools;
		private readonly LottoService _lottoService;

		readonly Subject<bool> _refreshSubject;

		private bool _isInitialized = false;


		public LottoCheckerService(LottoService lottoService, IBitmapTools bitmapTools)
		{
			_lottoService = lottoService;
			_ocrClient = new VisionServiceClient(ApiKeys.MicrosoftVisionToken);
			_bitmapTools = bitmapTools;

			_refreshSubject = new Subject<bool>();
		}


		public IObservable<bool> Load()
		{
			return Observable.FromAsync(ct => ScanTicketAsync(ct))
								 .Where(l => l != null)
								 .CombineLatest(_lottoService.Results,
												(scannedNumberLines, winningNumbers)
													=> IsTicketWinning(winningNumbers.NumberLines, scannedNumberLines)
						     );
		}

		public bool IsTicketWinning(int[] winningNumbers, IEnumerable<int[]> scannedNumberLines)
			=> scannedNumberLines.Any(line => line.Count(winningNumbers.Contains) >= MinimumNumbersToWin);

		public async Task<IEnumerable<int[]>> ScanTicketAsync(CancellationToken ct)
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

			if (photo == null || photo.Path == null)
				return null;

			long weight;
			while ((weight = _bitmapTools.GetImageWeight(photo.Path)) > 4000000)
			{
				Debug.WriteLine($"{weight} is too big, resizing...");
				_bitmapTools.ResizeImage(photo.Path, photo.Path, 0.8f);
			}

			using (var photoStream = photo.GetStream())
				return await RecognizeNumberLines(photoStream);

			//return await Recognize("https://goo.gl/photos/PS6nHuH8Y7LW2oWc6");
		}

		private async Task<IEnumerable<int[]>> RecognizeNumberLines(Stream stream)
			=> ExtractNumbers(await _ocrClient.RecognizeTextAsync(stream));

		private async Task<IEnumerable<int[]>> RecognizeNumberLines(string url)
			=> ExtractNumbers(await _ocrClient.RecognizeTextAsync(url));

		public IEnumerable<int[]> ExtractNumbers(OcrResults ocrResult)
		{
			return
				from region in ocrResult.Regions
				from lines in region.Lines
					where lines.Words.Length >= MinimumNumberRows
				select lines.Words.Select(word => word.Text)
					.Where(IsNumeric)
					.Select(v => Convert.ToInt32(v))
					.ToArray()
				;
		}

		private readonly Func<string, bool> IsNumeric =
			x => !string.IsNullOrEmpty(x) && x.ToCharArray().All(char.IsDigit);
	}
}
