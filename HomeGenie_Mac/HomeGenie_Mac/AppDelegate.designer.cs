// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace HomeGenie_Mac
{
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		MonoMac.AppKit.NSMenu statusMenu { get; set; }

		[Action ("openHome:")]
		partial void openHome (MonoMac.Foundation.NSObject sender);

		[Action ("openWebsite:")]
		partial void openWebsite (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (statusMenu != null) {
				statusMenu.Dispose ();
				statusMenu = null;
			}
		}
	}
}
