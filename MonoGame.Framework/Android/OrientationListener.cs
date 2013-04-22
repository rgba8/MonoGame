using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Microsoft.Xna.Framework
{
    internal class OrientationListener : OrientationEventListener
    {
        AndroidGameActivity activity;
        private bool inprogress = false;

        /// <summary>
        /// Constructor. SensorDelay.Ui is passed to the base class as this orientation listener 
        /// is just used for flipping the screen orientation, therefore high frequency data is not required.
        /// </summary>
        public OrientationListener(AndroidGameActivity activity)
            : base(activity, SensorDelay.Ui)
        {
            this.activity = activity;
        }

        public override void OnOrientationChanged(int orientation)
        {
			// Ignoring this for now...
			return;

			//Console.WriteLine("Orientation Changed: {0}", orientation);

			// ignore -1?
			if (orientation == -1)
				return;
			
            // Avoid changing orientation whilst the screen is locked
            if (ScreenReceiver.ScreenLocked)
                return;

            if (!inprogress)
            {
                inprogress = true;

				// Divide by 90 into an int to round, then multiply out to one of 5 positions, either 0,90,180,270,360. 
				int ort = (90 * (int)Math.Round(orientation / 90f)) % 360;

				// Convert 360 to 0
				if (ort == 360)
				{
					ort = 0;
				}

				//Console.WriteLine("Final Orientation: {0}", ort);

				var displayOrientation = DisplayOrientation.Unknown;

				var currentOrientationSettings = AndroidCompatibility.CompatibilitySettings.DisplayOrientationMapping[ort];
				displayOrientation = currentOrientationSettings.Orientation;

				// Only auto-rotate if target orientation is supported and not current
				if ((AndroidGameActivity.Game.Window.GetEffectiveSupportedOrientations() & displayOrientation) != 0 &&
					 displayOrientation != AndroidGameActivity.Game.Window.CurrentOrientation)
				{
					// Get the settings for the current orientation
					AndroidCompatibility.CurrentOrientationSettings = currentOrientationSettings;

					Console.WriteLine("MG Orientation: {0}", displayOrientation);

					AndroidGameActivity.Game.Window.SetOrientation(displayOrientation, true);
				}
                inprogress = false;
            }
        }
    }
}