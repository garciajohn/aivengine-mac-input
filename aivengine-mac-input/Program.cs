using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Aiv.Engine.Input
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");

			MacNative.IOHIDDeviceCallback DeviceAddedCallback;
			MacNative.IOHIDDeviceCallback DeviceRemovedCallback;


			System.IntPtr runLoop = MacNative.CFRunLoopGetMain ();
			if (runLoop == IntPtr.Zero) {
				runLoop = MacNative.CFRunLoopGetCurrent ();
			}
			MacNative.CFRetain (runLoop);

			System.IntPtr hidManager = MacNative.IOHIDManagerCreate (IntPtr.Zero, IntPtr.Zero);

			DeviceAddedCallback = DeviceAdded;
			MacNative.IOHIDManagerRegisterDeviceMatchingCallback (hidManager, DeviceAddedCallback, IntPtr.Zero);

			DeviceRemovedCallback = DeviceRemoved;
			MacNative.IOHIDManagerRegisterDeviceRemovalCallback (hidManager, DeviceRemovedCallback, IntPtr.Zero);

			Console.WriteLine (hidManager);

			MacNative.IOHIDManagerScheduleWithRunLoop (hidManager, runLoop, MacNative.CFString ("kCFRunLoopDefaultMode"));

			MacNative.IOHIDManagerSetDeviceMatching (hidManager, IntPtr.Zero);

			MacNative.IOHIDManagerOpen (hidManager, IntPtr.Zero);

			MacNative.CFRunLoopRun ();

		}

		static void DeviceAdded (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			if (MacNative.IOHIDDeviceOpen (device, IntPtr.Zero) == IntPtr.Zero) {
				
				System.IntPtr product = MacNative.IOHIDDeviceGetProperty (device, MacNative.CFString ("Product"));
				string productName = MacNative.CString (product);

				Console.WriteLine ("Device Opened " + productName);
			}


		}

		static void DeviceRemoved (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			Console.WriteLine ("Device Removed");
		}
	}


	class MacNative
	{
		// the osx library exposing IOHID
		const string hidlib = "/System/Library/Frameworks/IOKit.framework/Versions/Current/IOKit";
		// the osx library exposing CoreFoundation basic structures
		const string applib = "/System/Library/Frameworks/ApplicationServices.framework/Versions/Current/ApplicationServices";

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDManagerCreate (System.IntPtr allocator, System.IntPtr options);

		[DllImport (applib)]
		public static extern System.IntPtr CFRunLoopGetMain ();

		[DllImport (applib)]
		public static extern System.IntPtr CFRunLoopGetCurrent ();

		[DllImport (applib)]
		public static extern System.IntPtr CFRetain (System.IntPtr ptr);

		[DllImport (applib)]
		public static extern System.IntPtr __CFStringMakeConstantString (string s);

		[DllImport (applib)]
		public static extern void CFRunLoopRun ();

		[DllImport (applib)]
		public static extern System.IntPtr CFStringGetLength(System.IntPtr s);

		[DllImport (applib)]
		public static extern bool CFStringGetCString(System.IntPtr s, byte[] buffer, System.IntPtr bufferSize, int encoding);

		[DllImport (hidlib)]
		public static extern void IOHIDManagerRegisterDeviceMatchingCallback (System.IntPtr manager, IOHIDDeviceCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern void IOHIDManagerRegisterDeviceRemovalCallback (System.IntPtr manager, IOHIDDeviceCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern void IOHIDManagerScheduleWithRunLoop (System.IntPtr manager, System.IntPtr clalback, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern void IOHIDManagerSetDeviceMatching (System.IntPtr manager, System.IntPtr matching);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDManagerOpen (System.IntPtr manager, System.IntPtr options);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDDeviceOpen (System.IntPtr device, System.IntPtr options);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDDeviceGetProperty (System.IntPtr device, System.IntPtr key);

		public delegate void IOHIDDeviceCallback (IntPtr ctx, IntPtr res, IntPtr sender, IntPtr device);

		public delegate void IOHIDValueCallback (IntPtr ctx, IntPtr res, IntPtr sender, IntPtr val);

		public static System.IntPtr CFString (string s)
		{
			return __CFStringMakeConstantString (s);
		}

		public static string CString (System.IntPtr ptr)
		{
			System.IntPtr length = CFStringGetLength (ptr);
			if (length != IntPtr.Zero) {
				byte[] utf8_bytes = new byte[length.ToInt32() + 1];
				if (CFStringGetCString(ptr, utf8_bytes, new IntPtr(utf8_bytes.Length), 0x08000100)) {
					return Encoding.UTF8.GetString(utf8_bytes);
				}
			}
			return "";
		}
	}
}
