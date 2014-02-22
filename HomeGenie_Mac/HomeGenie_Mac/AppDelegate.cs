using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using HomeGenie;

namespace HomeGenie_Mac
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;
		public NSStatusItem statusItem;
		public static HomeGenie.Service.HomeGenieService hg;
		
		public AppDelegate ()
		{
			hg = new HomeGenie.Service.HomeGenieService (); 
		}

		public override void AwakeFromNib ()
		{
			statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (-1);
			statusItem.Menu = statusMenu;
			statusItem.Image = NSImage.ImageNamed ("homestatus");
			statusItem.HighlightMode = true;
		}

		public override void FinishedLaunching (NSObject notification)
		{
			mainWindowController = new MainWindowController ();
			//mainWindowController.Window.MakeKeyAndOrderFront (this);
		}

		partial void openHome (MonoMac.Foundation.NSObject sender)
		{
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}

		partial void openWebsite (MonoMac.Foundation.NSObject sender)
		{
			NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl ("http://localhost/"));
		}
	}
}

