using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;

namespace LottoChecker
{
	public class LottoService
	{
		public LottoService()
		{
			var retry = TimeSpan.FromHours(1);

			new Reachability().WhenConnectionChanged.Where(b => b) // wait for connection
			                  .Merge(Observable.Timer(retry, retry).Select(_ => true)) // also refresh every hour
							  .SubscribeOn(TaskPoolScheduler.Default)
							  .Select(_ => LoadResultsAsync())
							  //.ObserveOn(ReactivePropertyScheduler.Default)
					          .Subscribe(_results);
		}


		private readonly Subject<LottoResults> _results = new Subject<LottoResults>();
		public IObservable<LottoResults> Results => _results.AsObservable();


		private LottoResults LoadResultsAsync()
		{
			var xmlDoc = 
				XDocument.Load("https://www.e-lotto.be/cache/dgLastResultForGameWithAddons/FR/Lotto6.xml");

			var xmlRoot = xmlDoc.Root
							 .Element("gameevent");

			return new LottoResults
			{
				PublishDate = DateTime.ParseExact(xmlRoot
					.Element("resulttime")
					.Value, "s", null)// 2017-02-18T20:02:28
				,
				NumberLines = xmlRoot
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
