using System;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.Util;

namespace Microsoft.Xna.Framework.Audio
{
    public class Sound : IDisposable
    {
		/// <summary>
		/// Fired when the sound pool is done loading the sample so SoundEffect/SoundEffectInstance can start playing
		/// is Play() was called before async loading was done.
		/// </summary>
		public event EventHandler LoadCompleted = delegate { };

        private const int MAX_SIMULTANEOUS_SOUNDS = 6;
        private static SoundPool s_soundPool = new SoundPool(MAX_SIMULTANEOUS_SOUNDS, Stream.Music, 0);
		private int _soundId;

		public int SoundId
		{
			get { return _soundId; }
			set { _soundId = value; }
		}

        bool disposed;
		bool loaded = false;
		private string _filename;

		public string Filename
		{
			get { return _filename; }
			set { _filename = value; }
		}

		public bool Loaded
		{
			get { return loaded; }
			set { loaded = value; }
		}


		internal static SoundPool SoundPool
		{
			get {
				return s_soundPool;
			}
			
		}
		
		internal static void PauseAll()
		{
			s_soundPool.AutoPause();
		}
		
		internal static void ResumeAll()
		{
			s_soundPool.AutoResume();
		}

        ~Sound()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (_soundId != 0)
                    s_soundPool.Unload(_soundId);
                _soundId = 0;
				
                disposed = true;
            }
        }

        public double Duration
        {
            get { return 0; } // cant get this from soundpool.
        }

        public double CurrentPosition
        {
            get { return 0; } // cant get this from soundpool.
        }

        public bool Playing
        {
            get
            {
                return false; // cant get this from soundpool.
            }
        }

        public Sound(string filename)
        {
			// Sound load is async... make sure we know when it's done in case a request for Play() is initiated before loading is complete...
			// There are probably better ways to do this though...
			s_soundPool.LoadComplete += LoadComplete;
			_filename = filename;

			using (AssetFileDescriptor fd = Game.Activity.Assets.OpenFd(filename))
			{
				_soundId = s_soundPool.Load(fd.FileDescriptor, fd.StartOffset, fd.Length, 1);
			}
        }

		void LoadComplete(object sender, SoundPool.LoadCompleteEventArgs e)
		{
			s_soundPool.LoadComplete -= LoadComplete;

			if (e.SampleId == _soundId)
			{
				loaded = true;
				Console.WriteLine("Sound Loaded: {0} - {1}", e.SampleId, e.Status);

				LoadCompleted(this, EventArgs.Empty);
			}
		}

        public Sound(byte[] audiodata)
        {
            _soundId = 0;
            //throw new NotImplementedException();
        }

        internal static void IncreaseMediaVolume()
        {
            AudioManager audioManager = (AudioManager)Game.Activity.GetSystemService(Context.AudioService);

            audioManager.AdjustStreamVolume(Stream.Music, Adjust.Raise, VolumeNotificationFlags.ShowUi);
        }

        internal static void DecreaseMediaVolume()
        {
            AudioManager audioManager = (AudioManager)Game.Activity.GetSystemService(Context.AudioService);

            audioManager.AdjustStreamVolume(Stream.Music, Adjust.Lower, VolumeNotificationFlags.ShowUi);
        }
	}
}