using System.Linq;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Diagnostics;

namespace LottoChecker
{
	public class LottoCheckerViewModel
	{
		private readonly LottoCheckerService _lottoCheckerService;


		public LottoCheckerViewModel(IBitmapTools bitmapTools)
		{
			var lotteryService = new LottoService();
			_lottoCheckerService = new LottoCheckerService(lotteryService, bitmapTools);

			ResultsDate = lotteryService.Results
						  .ObserveOn(ReactivePropertyScheduler.Default)
						  .Select(d => d.PublishDate.ToString("dddd d-M-yyyy"))
						  .ToReadOnlyReactiveProperty("loading results...")
                          ;

			ScanCommand = lotteryService.Results
						  .Select(res => res != null)
						  .StartWith(false)
						  .ToAsyncReactiveCommand();

			ScanCommand.Subscribe(async _ => 
			                      Result.Value = (await _lottoCheckerService.Load()) ? "win!" : "nope.");

			//ScanCommand.CanExecuteChanged += (sender, e) => Debug.WriteLine("can");
		}


		public ReadOnlyReactiveProperty<string> ResultsDate { get; }

		public AsyncReactiveCommand ScanCommand { get; private set; }
		//public ReadOnlyReactiveProperty<bool> IsLoading { get; private set; }
		public ReactiveProperty<string> Result { get; } = new ReactiveProperty<string>("...");
	}
}