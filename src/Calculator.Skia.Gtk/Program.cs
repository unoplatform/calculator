using System;
using System.Reflection;
using System.Runtime.InteropServices;
using CalculatorApp;
using GLib;
using Uno.UI.Runtime.Skia.Gtk;

namespace Calculator.Skia.Gtk
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				NativeLibrary.SetDllImportResolver(typeof(CalculationManager.NativeDispatch).Assembly, ImportResolver);
			}

			ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
			{
				Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
				expArgs.ExitApplication = true;
			};

			var host = new GtkHost(() => new App());

			host.Run();
		}

		private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			IntPtr libHandle = IntPtr.Zero;

			if (libraryName == "CalcManager")
			{
				NativeLibrary.TryLoad("CalcManager.Win32.dll", assembly, DllImportSearchPath.AssemblyDirectory, out libHandle);
			}

			return libHandle;
		}

	}
}
