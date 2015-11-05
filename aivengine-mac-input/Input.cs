using System;
using System.Runtime.InteropServices;
using System.Text;
using Aiv.Engine;

namespace Aiv.Engine
{
	public class Input
	{

		private const int HIDDesktop = 0x01;
		private const int HIDButtons = 0x09;
		private const int HIDJoystick = 0x04;
		private const int HIDGamePad = 0x05;
		private const int HIDMultiAxis = 0x08;
		private const int HIDWheel = 0x38;
		private const int HIDX = 0x30;
		private const int HIDY = 0x31;

		private static MacNative.IOHIDDeviceCallback DeviceAddedCallback;
		private static MacNative.IOHIDDeviceCallback DeviceRemovedCallback;
		private static MacNative.IOHIDValueCallback DeviceInputCallback;

		private static System.IntPtr defaultLoopMode = MacNative.CFString ("kCFRunLoopDefaultMode");

		private static Engine engine;
		private static System.IntPtr runLoop;

		public static void Initialize (Engine _engine)
		{
			

			runLoop = MacNative.CFRunLoopGetMain ();
			if (runLoop == IntPtr.Zero) {
				runLoop = MacNative.CFRunLoopGetCurrent ();
			}
			if (runLoop == IntPtr.Zero) {
				throw(new Exception ("[mac-input] unable to get a RunLoop"));
			}
			// avoid the RunLoop to be garbaged
			MacNative.CFRetain (runLoop);

			System.IntPtr hidManager = MacNative.IOHIDManagerCreate (IntPtr.Zero, IntPtr.Zero);
			if (hidManager == IntPtr.Zero) {
				throw(new Exception ("[mac-input] unable to get a HidManager()"));
			}


			DeviceAddedCallback = DeviceAdded;
			MacNative.IOHIDManagerRegisterDeviceMatchingCallback (hidManager, DeviceAddedCallback, IntPtr.Zero);

			DeviceRemovedCallback = DeviceRemoved;
			MacNative.IOHIDManagerRegisterDeviceRemovalCallback (hidManager, DeviceRemovedCallback, IntPtr.Zero);

			DeviceInputCallback = InputReceived;

			MacNative.IOHIDManagerScheduleWithRunLoop (hidManager, runLoop, defaultLoopMode);

			MacNative.IOHIDManagerSetDeviceMatching (hidManager, IntPtr.Zero);

			MacNative.IOHIDManagerOpen (hidManager, IntPtr.Zero);

			engine = _engine;
			engine.OnAfterUpdate += new Engine.AfterUpdateEventHandler (RunLoopStep);


		}

		// this is run at each refresh
		private static void RunLoopStep (object sender)
		{
			MacNative.CFRunLoopRunInMode (defaultLoopMode, 0, false);
		}

		private static void DeviceAdded (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			if (MacNative.IOHIDDeviceOpen (device, IntPtr.Zero) == IntPtr.Zero) {

				if (MacNative.IOHIDDeviceConformsTo (device, HIDDesktop, HIDJoystick) ||
				    MacNative.IOHIDDeviceConformsTo (device, HIDDesktop, HIDGamePad) ||
				    MacNative.IOHIDDeviceConformsTo (device, HIDDesktop, HIDMultiAxis) ||
				    MacNative.IOHIDDeviceConformsTo (device, HIDDesktop, HIDWheel)) {
				
					System.IntPtr product = MacNative.IOHIDDeviceGetProperty (device, MacNative.CFString ("Product"));
					string productName = MacNative.CString (product);

					int index = 0;
					foreach (Engine.Joystick joystick in engine.joysticks) {
						if (joystick == null) {
							engine.joysticks [index] = new Engine.Joystick ();
							engine.joysticks [index].index = index;
							engine.joysticks [index].id = device.ToInt64 ();
							engine.joysticks [index].name = productName;
							MacNative.IOHIDDeviceRegisterInputValueCallback (device, DeviceInputCallback, device);
							MacNative.IOHIDDeviceScheduleWithRunLoop (device, runLoop, defaultLoopMode);
							return;
						}
						index++;
					}

				}
				MacNative.IOHIDDeviceClose (device, IntPtr.Zero);
			}


		}

		private static void DeviceRemoved (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr device)
		{
			foreach (Engine.Joystick joystick in engine.joysticks) {
				if (joystick != null) {
					if (joystick.id == device.ToInt64 ()) {
						engine.joysticks [joystick.index] = null;
						MacNative.IOHIDDeviceRegisterInputValueCallback (device, IntPtr.Zero, IntPtr.Zero);
						MacNative.IOHIDDeviceUnscheduleFromRunLoop (device, runLoop, defaultLoopMode);
						MacNative.IOHIDDeviceClose (device, IntPtr.Zero);
						return;
					}
				}
			}
		}

		private static void InputReceived (System.IntPtr context, System.IntPtr res, System.IntPtr sender, System.IntPtr value)
		{
			
			System.IntPtr element = MacNative.IOHIDValueGetElement (value);
			System.IntPtr cookie = MacNative.IOHIDElementGetCookie (element);
			ushort page = MacNative.IOHIDElementGetUsagePage (element);
			int usage = MacNative.IOHIDElementGetUsage (element);

			switch (page) {
			case HIDDesktop:
				switch (usage) {
				// X
				case HIDX:
					ManageXAxis (context, element, value);
					break;
				case HIDY:
					ManageYAxis (context, element, value);
					break;
				}
				break;
			case HIDButtons:
				ManageButtons (context, cookie, value);
				break;
			}
		}

		private static void ManageXAxis (System.IntPtr device, System.IntPtr element, System.IntPtr value)
		{
			Engine.Joystick joystick = GetJoystick (device);
			if (joystick == null)
				return;
			int min = MacNative.IOHIDElementGetLogicalMin (element).ToInt32 ();
			int max = MacNative.IOHIDElementGetLogicalMax (element).ToInt32 ();
			int axis = MacNative.IOHIDValueGetIntegerValue (value).ToInt32();
			joystick.x = Normalize (axis, min, max);
		}

		private static void ManageYAxis (System.IntPtr device, System.IntPtr element, System.IntPtr value)
		{
			Engine.Joystick joystick = GetJoystick (device);
			if (joystick == null)
				return;
			int min = MacNative.IOHIDElementGetLogicalMin (element).ToInt32 ();
			int max = MacNative.IOHIDElementGetLogicalMax (element).ToInt32 ();
			int axis = MacNative.IOHIDValueGetIntegerValue (value).ToInt32();
			joystick.y = Normalize (axis, min, max);
		}

		// this is not a true normalization and it is pretty hardcoded, but is more than enough for axis
		private static int Normalize(int value, int min, int max) {
			int n = (value - min) * 255;
			return (n / 255) - 128;
		}

		private static void ManageButtons (System.IntPtr device, System.IntPtr cookie, System.IntPtr value)
		{
			if (cookie.ToInt32() < 0 || cookie.ToInt32() > 19)
				return;
			Engine.Joystick joystick = GetJoystick (device);
			if (joystick == null)
				return;
			int buttonPressed = MacNative.IOHIDValueGetIntegerValue (value).ToInt32 ();
			joystick.buttons [cookie.ToInt32()] =  buttonPressed >= 1;
		}

		private static Engine.Joystick GetJoystick (System.IntPtr device)
		{
			foreach (Engine.Joystick joystick in engine.joysticks) {
				if (joystick != null) {
					if (joystick.id == device.ToInt64 ()) {
						return joystick;
					}
				}
			}
			return null;
		}

	}


	internal class MacNative
	{
		// the osx library exposing IOHID
		const string hidlib = "/System/Library/Frameworks/IOKit.framework/Versions/Current/IOKit";
		// the osx library exposing CoreFoundation basic structures
		const string applib = "/System/Library/Frameworks/ApplicationServices.framework/Versions/Current/ApplicationServices";

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDManagerCreate (System.IntPtr allocator, System.IntPtr options);

		[DllImport (applib)]
		internal static extern System.IntPtr CFRunLoopGetMain ();

		[DllImport (applib)]
		internal static extern System.IntPtr CFRunLoopGetCurrent ();

		[DllImport (applib)]
		internal static extern System.IntPtr CFRetain (System.IntPtr ptr);

		[DllImport (applib)]
		internal static extern System.IntPtr __CFStringMakeConstantString (string s);

		[DllImport (applib)]
		internal static extern void CFRunLoopRunInMode (System.IntPtr mode, double seconds, bool oneStep);

		[DllImport (applib)]
		internal static extern System.IntPtr CFStringGetLength (System.IntPtr s);

		[DllImport (applib)]
		internal static extern bool CFStringGetCString (System.IntPtr s, byte[] buffer, System.IntPtr bufferSize, int encoding);

		[DllImport (hidlib)]
		internal static extern void IOHIDManagerRegisterDeviceMatchingCallback (System.IntPtr manager, IOHIDDeviceCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern void IOHIDManagerRegisterDeviceRemovalCallback (System.IntPtr manager, IOHIDDeviceCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern void IOHIDManagerScheduleWithRunLoop (System.IntPtr manager, System.IntPtr loop, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern void IOHIDDeviceUnscheduleFromRunLoop (System.IntPtr device, System.IntPtr loop, System.IntPtr mode);

		[DllImport (hidlib)]
		internal static extern void IOHIDManagerSetDeviceMatching (System.IntPtr manager, System.IntPtr matching);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDManagerOpen (System.IntPtr manager, System.IntPtr options);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDDeviceOpen (System.IntPtr device, System.IntPtr options);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDDeviceGetProperty (System.IntPtr device, System.IntPtr key);

		[DllImport (hidlib)]
		internal static extern bool IOHIDDeviceConformsTo (System.IntPtr device, ushort page, int usage);

		[DllImport (hidlib)]
		internal static extern void IOHIDDeviceRegisterInputValueCallback (System.IntPtr device, IOHIDValueCallback callback, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern void IOHIDDeviceRegisterInputValueCallback (System.IntPtr device, System.IntPtr callback, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern void IOHIDDeviceScheduleWithRunLoop (System.IntPtr device, System.IntPtr loop, System.IntPtr context);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDValueGetElement (System.IntPtr ptr);

		[DllImport (hidlib)]
		internal static extern ushort IOHIDElementGetUsagePage (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern int IOHIDElementGetUsage (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDValueGetIntegerValue (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDElementGetCookie (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDElementGetLogicalMax (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDElementGetLogicalMin (System.IntPtr element);

		[DllImport (hidlib)]
		internal static extern System.IntPtr IOHIDDeviceClose (System.IntPtr device, System.IntPtr options);

		internal delegate void IOHIDDeviceCallback (IntPtr ctx, IntPtr res, IntPtr sender, IntPtr device);

		internal delegate void IOHIDValueCallback (IntPtr ctx, IntPtr res, IntPtr sender, IntPtr val);

		internal static System.IntPtr CFString (string s)
		{
			return __CFStringMakeConstantString (s);
		}

		internal static string CString (System.IntPtr ptr)
		{
			System.IntPtr length = CFStringGetLength (ptr);
			if (length != IntPtr.Zero) {
				byte[] utf8_bytes = new byte[length.ToInt32 () + 1];
				if (CFStringGetCString (ptr, utf8_bytes, new IntPtr (utf8_bytes.Length), 0x08000100)) {
					return Encoding.UTF8.GetString (utf8_bytes);
				}
			}
			return "";
		}
	}
}
