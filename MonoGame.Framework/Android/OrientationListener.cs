using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Microsoft.Xna.Framework
{
    internal class OrientationListener : OrientationEventListener
    {
        AndroidGameActivity activity;
        private bool isChanging = false;
        private bool nativeOrientationIsLandscape = false;

        /// <summary>
        /// Constructor. SensorDelay.Ui is passed to the base class as this orientation listener 
        /// is just used for flipping the screen orientation, therefore high frequency data is not required.
        /// </summary>
        public OrientationListener(AndroidGameActivity activity)
            : base(activity, SensorDelay.Ui)
        {
            this.activity = activity;
            var display = this.activity.WindowManager.DefaultDisplay;
            this.nativeOrientationIsLandscape = display.Width > display.Height;
        }

        public override void OnOrientationChanged(int orientation)
        {
            // Avoid changing orientation whilst the screen is locked
            if (ScreenReceiver.ScreenLocked)
                return;

            if (!isChanging)
            {
                isChanging = true;
                // Divide by 90 into an int to round, then multiply out to one of 5 positions, either 0,90,180,270,360. 
                int ort = (90 * (int)Math.Round(orientation / 90f)) % 360;

                // Convert 360 to 0
                if (ort == 360)
                {
                    ort = 0;
                }

                var disporientation = DisplayOrientation.Unknown;

                switch (ort)
                {
                  case 0: disporientation = this.nativeOrientationIsLandscape ? DisplayOrientation.LandscapeLeft : DisplayOrientation.Portrait;
                    break;
                  case 90: disporientation = this.nativeOrientationIsLandscape ? DisplayOrientation.Portrait : DisplayOrientation.LandscapeLeft;
                    break;
                  case 180: disporientation = this.nativeOrientationIsLandscape ? DisplayOrientation.LandscapeRight : DisplayOrientation.PortraitDown;
                    break;
                  case 270: disporientation = this.nativeOrientationIsLandscape ? DisplayOrientation.PortraitDown : DisplayOrientation.LandscapeRight;
                    break;
                  default:
                    disporientation =  this.nativeOrientationIsLandscape ? DisplayOrientation.LandscapeLeft : DisplayOrientation.Portrait;
                    break;
                }

                // Only auto-rotate if target orientation is supported and not current
                if ((AndroidGameActivity.Game.Window.GetEffectiveSupportedOrientations() & disporientation) != 0 &&
                     disporientation != AndroidGameActivity.Game.Window.CurrentOrientation)
                {
                    AndroidGameActivity.Game.Window.SetOrientation(disporientation, true);
                }
                isChanging = false;
            }
        }
    }
}