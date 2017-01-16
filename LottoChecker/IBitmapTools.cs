using Xamarin.Forms;

namespace LottoChecker
{
	public interface IBitmapTools
    {
		void ResizeImage(string sourceFile, string targetFile, float ratio);
		long GetImageWeight(string sourceFile);
    }
}