using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace LottoChecker
{
	public class LottoService
	{
		public IObservable<DateTime> ResultsDate { get; internal set; }

		public Task<int[]> LoadResultsAsync()
		{
			return Task.Run(() =>
			{
				var xmlDoc = XDocument.Load("https://www.e-lotto.be/cache/dgLastResultForGameWithAddons/FR/Lotto6.xml");
				return xmlDoc.Root
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
					;
			});
		}

	}
}
