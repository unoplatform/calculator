using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculatorApp.Common;
using Windows.ApplicationModel.DataTransfer;

namespace CalculatorApp
{
    public class CopyPasteManager
    {
        public const string PasteErrorString = "NoOp";

        public const int QwordType = 1;
        public const int DwordType = 2;
        public const int WordType = 3;
        public const int ByteType = 4;
        public const int HexBase = 5;
        public const int DecBase = 6;
        public const int OctBase = 7;
        public const int BinBase = 8;

        internal static bool HasStringToPaste() => Clipboard.GetContent().Contains(StandardDataFormats.Text);
		internal static async Task<string> GetStringToPaste(ViewMode mode, CategoryGroupType categoryGroupType, int numberBase, int bitLengthType)
		{
			// Retrieve the text in the clipboard
			var dataPackageView = Clipboard.GetContent();

			// TODO: Support all formats supported by ClipboardHasText
			//-- add support to avoid pasting of expressions like 12 34(as of now we allow 1234)
			//-- add support to allow pasting for expressions like .2 , -.2
			//-- add support to allow pasting for expressions like 1.3e12(as of now we allow 1.3e+12)
			return await dataPackageView.GetTextAsync();
		}

		internal static void CopyToClipboard(string stringToCopy)
		{
			// Copy the string to the clipboard
			var dataPackage = new DataPackage();
			dataPackage.SetText(stringToCopy);
			Clipboard.SetContent(dataPackage);
		}
    }
}
