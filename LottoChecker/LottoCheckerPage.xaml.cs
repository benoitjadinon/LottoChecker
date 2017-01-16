using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace LottoChecker
{
	public partial class LottoCheckerPage : ContentPage
	{
		public LottoCheckerPage()
		{
			InitializeComponent();
		}


		protected override void OnAppearing()
		{
			base.OnAppearing();

			var bitmapTools = DependencyService.Get<IBitmapTools>();

		    this.BindingContext = new LottoCheckerViewModel(bitmapTools);
		}
	}
}
