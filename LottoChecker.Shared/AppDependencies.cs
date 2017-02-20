using System;
using Xamarin.Forms;

namespace LottoChecker.Shared
{
	public class AppDependencies
	{
		public void Setup()
		{
			DependencyService.Register<IBitmapTools, BitmapTools>();
		}
	}
}
