using System.Linq;
using Reactive.Bindings;
using System.Reactive.Linq;

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
										.Select(_ => true)
										.ToAsyncReactiveCommand()
			                            ;
			/*IsLoading = ScanCommand.CanExecuteChangedAsObservable()
					   .Select(_ => ScanCommand.CanExecute())
					   .ToReadOnlyReactiveProperty();
					   */
			ScanCommand
			           .Subscribe(_ => _lottoCheckerService.ScanTicketAsync(new System.Threading.CancellationToken()))
			           ;

			/*
			ScanCommand = _lottoCheckerService.Load().ToReactiveCommand<bool>();
            ScanCommand.Select(b => b ? "win!" : "nope.")
			           .Subscribe(onNext:s => Result.Value = s);
			           */
		}


		public ReadOnlyReactiveProperty<string> ResultsDate { get; }

		public AsyncReactiveCommand ScanCommand { get; private set; }
		//public ReadOnlyReactiveProperty<bool> IsLoading { get; private set; }
		public ReactiveProperty<string> Result { get; }// = new ReactiveProperty<string>();
	}
}