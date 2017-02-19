using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;

namespace LottoChecker
{
	public class Reachability
	{
		public virtual bool IsConnected => GetIsConnected();

		public IObservable<bool> WhenConnectionChanged { get; protected set; }
		//public IObservable<Unit> WaitForConnection    { get; protected set; }

		public Reachability()
		{
			WhenConnectionChanged =
				Observable.FromEventPattern<ConnectivityChangedEventHandler, ConnectivityChangedEventArgs>(
					e => CrossConnectivity.Current.ConnectivityChanged += e,
					e => CrossConnectivity.Current.ConnectivityChanged -= e
				)
				.Select(r => r.EventArgs.IsConnected)
				.StartWith(GetIsConnected())
			;
			/*
		    WaitForConnection = WhenConnectionChanged
                .DistinctUntilChanged()
                .WhereIsTrue()
                .Take(1) // TODO sends complete after first true
		        .Select(_ => Unit.Default) // returns Unit instead of unecessary bool
		        ;
		        */
		}

		private bool GetIsConnected()
			=> CrossConnectivity.Current.IsConnected;

		public List<string> ConnectionTypes
			=> CrossConnectivity.Current.ConnectionTypes?.Select(x => x.ToString()).ToList() ?? new List<string>();
	}
}
