using System.Collections.Generic;
using System.Linq;
using Android.OS;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// Properties that change from how XNA works by default
    /// </summary>
    public static class AndroidCompatibility
    {
		/// <summary>
		/// Becaue the Kindle Fire devices default orientation is fliped by 180 degrees from all the other android devices
		/// on the market we need to do some special processing to make sure that LandscapeLeft is the correct way round.
		/// This list contains all the Build.Model strings of the effected devices, it should be added to if and when
		/// more devices exxhibit the same issues
		/// </summary>
		private static readonly string[] Kindles = new[] { "KFTT", "KFJWI", "KFJWA", };

        public enum ESVersions
        {
            v1_1,
            v2_0
        }

        static AndroidCompatibility()
        {
            ScaleImageToPowerOf2 = true;
            ESVersion = ESVersions.v2_0;	
			//FlipLandscape = Kindles.Contains(Build.Model);

			var deviceCompatList = new Dictionary<string, DeviceCompatibilitySettings>();

			#region Nexus 7
			var item = new DeviceCompatibilitySettings 
			{
				Model = "Nexus_7",
				DisplayOrientationMapping = new Dictionary<int, OrientationSettings> 
				{
					{ 0, new OrientationSettings { Orientation = DisplayOrientation.Portrait }},
					{ 90, new OrientationSettings { Orientation = DisplayOrientation.LandscapeRight, AccelerometerInvertX = true, AccelerometerInvertY = true }},
					{ 180, new OrientationSettings { Orientation = DisplayOrientation.PortraitDown }},
					{ 270, new OrientationSettings { Orientation = DisplayOrientation.LandscapeLeft }}
				} 
			};
			deviceCompatList[item.Model] = item;
			#endregion

			#region Samsung Galaxy Tab 10.1
			item = new DeviceCompatibilitySettings
			{
				Model = "GT-P7510",
				DisplayOrientationMapping = new Dictionary<int, OrientationSettings> 
				{
					{ 0, new OrientationSettings { Orientation = DisplayOrientation.LandscapeLeft, AccelerometerFlipXY = true, AccelerometerInvertX = true }},
					{ 90, new OrientationSettings { Orientation = DisplayOrientation.Portrait }},
					{ 180, new OrientationSettings { Orientation = DisplayOrientation.LandscapeRight, AccelerometerFlipXY = true, AccelerometerInvertY = true }},
					{ 270, new OrientationSettings { Orientation = DisplayOrientation.PortraitDown }}
				}
			};
			deviceCompatList[item.Model] = item;
			#endregion

			#region Kindle Fire
			var kindleDisplayOrientationMapping = new Dictionary<int, OrientationSettings> 
			{
				{ 0, new OrientationSettings { Orientation = DisplayOrientation.Portrait }},
				{ 90, new OrientationSettings { Orientation = DisplayOrientation.LandscapeLeft, AccelerometerInvertX = true, AccelerometerInvertY = true }},
				{ 180, new OrientationSettings { Orientation = DisplayOrientation.PortraitDown }},
				{ 270, new OrientationSettings { Orientation = DisplayOrientation.LandscapeRight }}
			};

			item = new DeviceCompatibilitySettings
			{
				Model = "KFTT",
				DisplayOrientationMapping = kindleDisplayOrientationMapping
			};
			deviceCompatList[item.Model] = item;
			
			item = new DeviceCompatibilitySettings
			{
				Model = "KFJWI",
				DisplayOrientationMapping = kindleDisplayOrientationMapping
			};
			deviceCompatList[item.Model] = item;
			
			item = new DeviceCompatibilitySettings
			{
				Model = "KFJWA",
				DisplayOrientationMapping = kindleDisplayOrientationMapping
			};
			deviceCompatList[item.Model] = item;

			#endregion
			
			if (deviceCompatList.ContainsKey(Build.Model))
			{
				CompatibilitySettings = deviceCompatList[Build.Model];
			}
			else
			{
				CompatibilitySettings = new DeviceCompatibilitySettings
				{
					Model = Build.Model,
					DisplayOrientationMapping = new Dictionary<int, OrientationSettings> 
					{
						{ 0, new OrientationSettings { Orientation = DisplayOrientation.Portrait }},
						{ 90, new OrientationSettings { Orientation = DisplayOrientation.LandscapeRight }},
						{ 180, new OrientationSettings { Orientation = DisplayOrientation.PortraitDown }},
						{ 270, new OrientationSettings { Orientation = DisplayOrientation.LandscapeLeft }}
					}
				};
			}
		}

		public static OrientationSettings CurrentOrientationSettings { get; set; }
		public static DeviceCompatibilitySettings CompatibilitySettings { get; set; }

        public static bool ScaleImageToPowerOf2 { get; set; }
        public static ESVersions ESVersion { get; set; }

    }

	public class DeviceCompatibilitySettings
	{
		public string Model { get; set; }

		public Dictionary<int, OrientationSettings> DisplayOrientationMapping { get; set; }
	}

	public class OrientationSettings
	{
		public DisplayOrientation Orientation { get; set; }
		public bool AccelerometerFlipXY { get; set; }
		public bool AccelerometerInvertX { get; set; }
		public bool AccelerometerInvertY { get; set; }
	}
}
