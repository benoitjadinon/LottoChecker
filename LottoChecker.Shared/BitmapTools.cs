using System;
using System.IO;
#if __IOS__
using CoreGraphics;
using UIKit;
#elif __ANDROID__
using Android.Graphics;
#endif
using Xamarin.Forms;

namespace LottoChecker.Shared
{
    public class BitmapTools : IBitmapTools
    {
#if __IOS__

		public void ResizeImage(string sourceFile, string targetFile, float maxResizeFactor)
		{
			if (File.Exists(sourceFile) /*&& !File.Exists(targetFile)*/)
			{
				using (UIImage sourceImage = UIImage.FromFile(sourceFile))
				{
					var sourceSize = sourceImage.Size;

					if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
						Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

					if (maxResizeFactor > 0.9)
					{
						File.Copy(sourceFile, targetFile);
					}
					else
					{
						var width = maxResizeFactor * sourceSize.Width;
						var height = maxResizeFactor * sourceSize.Height;

						UIGraphics.BeginImageContextWithOptions(new CGSize((float)width, (float)height), true, 1.0f);
						//  UIGraphics.GetCurrentContext().RotateCTM(90 / Math.PI);
						sourceImage.Draw(new CGRect(0, 0, (float)width, (float)height));

						var resultImage = UIGraphics.GetImageFromCurrentImageContext();
						UIGraphics.EndImageContext();

						if (targetFile.ToLower().EndsWith("png"))
							resultImage.AsPNG().Save(targetFile, true);
						else
							resultImage.AsJPEG().Save(targetFile, true);
					}
				}
			}
		}

		#elif __ANDROID__

        public void ResizeImage(string sourceFile, string targetFile, float ratio)
        {
            if (!File.Exists(targetFile) && File.Exists(sourceFile))
            {
                var downImg = decodeSampledBitmapFromFile(sourceFile, ratio);
                using (var outStream = File.Create(targetFile))
                {
                    if (targetFile.ToLower().EndsWith("png"))
                        downImg.Compress(Bitmap.CompressFormat.Png, 100, outStream);
                    else
                        downImg.Compress(Bitmap.CompressFormat.Jpeg, 95, outStream);
                }
                downImg.Recycle();
            }
        }

        public static Bitmap decodeSampledBitmapFromFile(string path, float ratio)
        {
            // First decode with inJustDecodeBounds=true to check dimensions
            var options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeFile(path, options);

            // Calculate inSampleSize
            options.InSampleSize = calculateInSampleSize(options, (float) options.OutWidth * ratio,
                (float) options.OutHeight * ratio);

            // Decode bitmap with inSampleSize set
            options.InJustDecodeBounds = false;
            return BitmapFactory.DecodeFile(path, options);
        }

        public static int calculateInSampleSize(BitmapFactory.Options options, float reqWidth, float reqHeight)
        {
            // Raw height and width of image
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                // height and width larger than the requested height and width.
                while ((halfHeight / inSampleSize) > reqHeight
                       && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }

#endif


        public long GetImageWeight(string sourceFile)
        {
            var info = new FileInfo(sourceFile);
            if (info.Exists)
                return info.Length;

            return -1;
        }
    }
}
