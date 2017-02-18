using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Interactivity;

namespace LottoChecker
{
	public class LottoCheckerViewModel
	{
		private readonly LottoService _lottoService;

		public LottoCheckerViewModel(IBitmapTools bitmapTools)
		{
			_lottoService = new LottoService(bitmapTools);

			ResultsDate = _lottoService.Results
			                           .ObserveOn(ReactivePropertyScheduler.Default)
			                           .Select(d => d.PublishDate.ToString("dddd d-M-yyyy"))
			                           .ToReadOnlyReactiveProperty("?-?-?");

			ScanCommand = new AsyncReactiveCommand();
			ScanCommand
				.Subscribe(async _ =>
				{
					IsLoading.Value = true;
					Result.Value = "Loading...";
					Result.Value = await _lottoService.ScanAsync() ? "Winning ticket !" : "Lost.";
					IsLoading.Value = false;
				}
        	);
		}


		public AsyncReactiveCommand ScanCommand { get; private set; }

		public ReactiveProperty<bool> IsLoading => new ReactiveProperty<bool>();
		public ReactiveProperty<string> Result => new ReactiveProperty<string>();
		public ReadOnlyReactiveProperty<string> ResultsDate { get; }
	}
}