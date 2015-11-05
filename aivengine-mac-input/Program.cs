using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Aiv.Engine.Input
{
	class MainClass
	{

		const int HIDJoystick = 0x04;
		const int HIDGamePad = 0x05;
		const int HIDMultiAxis = 0x08;
		const int HIDWheel = 0x38;

		static MacNative.IOHIDDeviceCallback DeviceAddedCallback;
		static MacNative.IOHIDDeviceCallback DeviceRemovedCallback;
		static MacNative.IOHIDValueCallback DeviceInputCallback;

		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");




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

			DeviceInputCallback = InputReceived;

			Console.WriteLine (hidManager);

			MacNative.IOHIDManagerScheduleWithRunLoop (hidManager, runLoop, MacNative.CFString ("kCFRunLoopDefaultMode"));

			MacNative.IOHIDManagerSetDeviceMatching (hidManager, IntPtr.Zero);

			MacNative.IOHIDManagerOpen (hidManager, IntPtr.Zero);

			MacNative.CFRunLoopRun ();

		}

		static void DeviceAdded (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			if (MacNative.IOHIDDeviceOpen (device, IntPtr.Zero) == IntPtr.Zero) {

				if (MacNative.IOHIDDeviceConformsTo (device, 0x01, HIDJoystick) ||
				    MacNative.IOHIDDeviceConformsTo (device, 0x01, HIDGamePad) ||
				    MacNative.IOHIDDeviceConformsTo (device, 0x01, HIDMultiAxis) ||
				    MacNative.IOHIDDeviceConformsTo (device, 0x01, HIDWheel)) {
				
					System.IntPtr product = MacNative.IOHIDDeviceGetProperty (device, MacNative.CFString ("Product"));
					string productName = MacNative.CString (product);

					Console.WriteLine ("Device Opened " + productName);

					MacNative.IOHIDDeviceRegisterInputValueCallback (device, DeviceInputCallback, device);
					MacNative.IOHIDDeviceScheduleWithRunLoop(device, MacNative.CFRunLoopGetCurrent (), MacNative.CFString ("kCFRunLoopDefaultMode"));
				}
			}


		}

		static void DeviceRemoved (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			Console.WriteLine ("Device Removed");
		}

		static void InputReceived(System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr value) {
			
			System.IntPtr element = MacNative.IOHIDValueGetElement (value);
			System.IntPtr cookie = MacNative.IOHIDElementGetCookie (element);
			ushort page = MacNative.IOHIDElementGetUsagePage (element);
			int usage = MacNative.IOHIDElementGetUsage (element);

			//Console.WriteLine ("Input received from " + context.ToInt64() + " " + page + "/" + usage);
			switch (page) {
			case 0x01:
				//Console.WriteLine ("Axis");
				switch (usage) {
				// X
				case 0x30:
					ManageAxis ("X", cookie, element, value);
					break;
				case 0x31:
					ManageAxis ("Y", cookie, element, value);
					break;
				}
				break;
			case 0x09:
				
				Console.WriteLine ("Buttons [{0}] {1}", cookie, MacNative.IOHIDValueGetIntegerValue(value));
				break;
			}
		}

		static void ManageAxis(string axis, System.IntPtr cookie, System.IntPtr element, System.IntPtr value) {
			int min = MacNative.IOHIDElementGetLogicalMin (element).ToInt32();
			int max = MacNative.IOHIDElementGetLogicalMax (element).ToInt32();
			Console.WriteLine ("Axis {0} [{1}] {2} => {3}/{4}", axis, cookie, MacNative.IOHIDValueGetIntegerValue(value), min, max);
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
		public static extern void IOHIDManagerScheduleWithRunLoop (System.IntPtr manager, System.IntPtr loop, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern void IOHIDManagerSetDeviceMatching (System.IntPtr manager, System.IntPtr matching);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDManagerOpen (System.IntPtr manager, System.IntPtr options);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDDeviceOpen (System.IntPtr device, System.IntPtr options);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDDeviceGetProperty (System.IntPtr device, System.IntPtr key);

		[DllImport (hidlib)]
		public static extern bool IOHIDDeviceConformsTo (System.IntPtr device, ushort page, int usage);

		[DllImport (hidlib)]
		public static extern void IOHIDDeviceRegisterInputValueCallback (System.IntPtr device, IOHIDValueCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern void IOHIDDeviceScheduleWithRunLoop (System.IntPtr device, System.IntPtr loop, System.IntPtr context);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDValueGetElement (System.IntPtr ptr);

		[DllImport (hidlib)]
		public static extern ushort IOHIDElementGetUsagePage (System.IntPtr element);

		[DllImport (hidlib)]
		public static extern int IOHIDElementGetUsage (System.IntPtr element);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDValueGetIntegerValue (System.IntPtr element);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDElementGetCookie(System.IntPtr element);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDElementGetLogicalMax(System.IntPtr element);

		[DllImport (hidlib)]
		public static extern System.IntPtr IOHIDElementGetLogicalMin(System.IntPtr element);

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
