using NUnit.Framework;
using System;
using System.Collections.Generic;
using Microsoft.ProjectOxford.Vision.Contract;

namespace LottoChecker.Test
{
	[TestFixture()]
	public class Test
	{
		[Test()]
		public void TestHasAWinningLine()
		{
			var vm = new LottoCheckerViewModel();

			var winningNumbers = new int[] { 18, 21, 26, 39, 42, 43 };
			var playedLines = new List<int[]>
			{
				new int[] { 1, 2, 3, 4, 5, 6 },
				new int[] { 1, 1, 26, 33, 42, 43 },
			};

			Assert.IsTrue(vm.IsTicketWinning(winningNumbers, playedLines));
		}

	    [Test()]
	    public void TestHasNoWinningLine()
	    {
	        var vm = new LottoCheckerViewModel();

	        var winningNumbers = new int[] { 18, 21, 26, 39, 42, 43 };
	        var playedLines = new List<int[]>
	        {
	            new int[] { 1, 2, 3, 4, 5, 6 },
	            new int[] { 9, 8, 7, 6, 5, 4 },
	        };

	        Assert.IsFalse(vm.IsTicketWinning(winningNumbers, playedLines));
	    }

	    [Test()]
	    public void TestHasOnlyTwoNumbers()
	    {
	        var vm = new LottoCheckerViewModel();

	        var winningNumbers = new int[] { 18, 21, 26, 39, 42, 43 };
	        var playedLines = new List<int[]>
	        {
	            new int[] { 1, 2, 3, 4, 5, 6 },
	            new int[] { 18, 26, 7, 6, 5, 4 },
	        };

	        Assert.IsFalse(vm.IsTicketWinning(winningNumbers, playedLines));
	    }


	    [Test()]
	    public void TestExtractNumbers()
	    {
	        var vm = new LottoCheckerViewModel();

	        var results = new OcrResults
	        {
	            Regions = new[]
	            {
	                new Region
	                {
	                    Lines = new []
	                    {
	                        new Line
	                        {
	                            Words = new []
	                            {
	                                new Word { Text = "01" },
	                                new Word { Text = "02" },
	                                new Word { Text = "03" },
	                                new Word { Text = "04" },
	                                new Word { Text = "05" },
									new Word { Text = "06" },
									new Word { Text = "07" },
									new Word { Text = "08" },
									new Word { Text = "09" },
	                            }
	                        },
	                        new Line
	                        {
	                            Words = new []
	                            {
	                                new Word { Text = "01." },
	                                new Word { Text = "18" },
	                                new Word { Text = "21" },
	                                new Word { Text = "26" },
	                                new Word { Text = "39" },
	                                new Word { Text = "42" },
	                                new Word { Text = "43" },
	                            }
	                        },
	                    }
	                }
	            }
	        };

	        var expectedLines = new List<int[]>
	        {
	            new [] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
	            new [] { 18, 21, 26, 39, 42, 43 },
	        };

	        Assert.AreEqual(expectedLines, vm.ExtractNumbers(results));
	    }
	}
}
