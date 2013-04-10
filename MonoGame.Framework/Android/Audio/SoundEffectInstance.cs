#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

#region Using Statements
using System;
using Android.Content;
using Android.Media;
#if DIRECTX
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Multimedia;
#endif
#endregion Statements

using OpenTK.Audio.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
	public sealed class SoundEffectInstance : IDisposable
	{
		private bool isDisposed = false;
        private SoundState soundState = SoundState.Stopped;
		private int _streamId = -1;
		private bool _loop;
		private float _pitch = 0.0f;
		private float _3dPitch = 1.0f;
		private float _3dAttenuation = 1.0f;

		private float _maxDistance = 343.0f;
		private float _referenceDistance = 50.0f;
		private float _rolloffFactor = 1.0f;

		/// <summary>
		/// Used to let the instance know Play() has been called when the SoundPool had not finished loading the sound yet
		/// </summary>
		bool playRequested = false;

		private Sound _sound;
		public Sound Sound 
		{ 
			get
			{
				return _sound;
			} 
			
			set
			{
				_sound = value;
				if (_sound != null)
					_sound.LoadCompleted += Sound_LoadCompleted;
			} 
		}

        public void Dispose()
        {
            if (_streamId >= 0)
                Sound.SoundPool.Stop(_streamId);
			isDisposed = true;
		}
		
		public void Apply3D(AudioListener listener, AudioEmitter emitter)
		{
			// Calculate
			var distance = (emitter.Position - listener.Position).Length();
			distance = Math.Max(distance,_referenceDistance);
			distance = Math.Min(distance,_maxDistance);
			_3dAttenuation = _referenceDistance / (_referenceDistance + _rolloffFactor * (distance - _referenceDistance));

			var SL = emitter.Position - listener.Position;
			var SV = emitter.Velocity;
			var LV = listener.Velocity;
			var dopplerFactor = 1.0f;
 
			var vls = Vector3.Dot(SL, LV) / SL.Length();
			var vss = Vector3.Dot(SL, SV) / SL.Length();

			vss = Math.Min(vss, SoundEffect.SpeedOfSound / dopplerFactor);
			vls = Math.Min(vls, SoundEffect.SpeedOfSound / dopplerFactor);
			_3dPitch = (speedOfSound - dopplerFactor * vls) / (SoundEffect.SpeedOfSound - SoundEffect.DopplerScale * vss);

			UpdatePitch();
			UpdateVolume();
		}

		
		public void Apply3D (AudioListener[] listeners,AudioEmitter emitter)
		{
            foreach ( var l in listeners )
                Apply3D(l, emitter);            
		}		
		
		public void Pause ()
        {
            if ( _sound != null )
			{
				Sound.SoundPool.Pause(_streamId);
                soundState = SoundState.Paused;
			}
		}

		void Sound_LoadCompleted(object sender, EventArgs e)
		{
			Console.WriteLine("Sound load completed");
			if (playRequested)
			{
				_sound.LoadCompleted -= Sound_LoadCompleted;
				playRequested = false;
				DoPlay();
			}
		}
		
		public void Play ()
        {
			if (!Sound.Loaded && playRequested)
			{
				Console.WriteLine("Play request pending for: {0}", Sound.SoundId);
				return;
			}

            if (State == SoundState.Playing)
                return;
			else if (State == SoundState.Paused && _streamId != -1)
			{
				Resume();				
			}
            else if ( Sound != null )
			{
				Console.WriteLine("Playing Sound ID: {0}", Sound.SoundId);

				if (!Sound.Loaded && playRequested)
				{
					Console.WriteLine("Play request pending for: {0}", Sound.SoundId);
					return;
				}

				DoPlay();
			}
		}

		private static int instanceCount = 0;

		private void DoPlay()
		{
			Console.WriteLine("DoPlay(), ID: {0}", Sound.SoundId);

			AudioManager audioManager = (AudioManager)Game.Activity.GetSystemService(Context.AudioService);
			
			float panRatio = (this.Pan + 1.0f) / 2.0f;
			float volumeTotal = SoundEffect.MasterVolume * this.Volume;
			float volumeLeft = volumeTotal * (1.0f - panRatio);
			float volumeRight = volumeTotal * panRatio;
			int priority = 1;

			var finalRate = 0.75f * (Pitch + 1.0f) + 0.5f;
			if (finalRate == 1.0f)
				finalRate = 0.99f; // set initial rate to 0.99 otherwise setRate won't work afterwards... no idea why...
			
			
			Console.WriteLine("Volume: {0} L: {1} / R: {2} - Rate: {3} - Looping: {4} - File: {5} - Priority: {6}", 
				volumeTotal, volumeLeft, volumeRight, finalRate, _loop, Sound.Filename, priority);
			
			_streamId = Sound.SoundPool.Play(Sound.SoundId, volumeLeft, volumeRight, 1, _loop ? -1 : 0, finalRate);
			instanceCount++;
			
			if (_streamId == 0)
			{
				playRequested = true;
				Console.WriteLine("Play Requested for: {0}", Sound.SoundId);
			}
			else if (_streamId != -1)
			{
				this.playRequested = false;
				Console.WriteLine("Playing Stream ID: {0}", _streamId);
				soundState = SoundState.Playing;
			}
			else
			{
				Console.WriteLine("Error playing sound!");
			}
		}

		public void Resume()
		{
			if (soundState == SoundState.Paused)
			{
				Sound.SoundPool.Resume(_streamId);
			}
			soundState = SoundState.Playing;
		}

		public void Stop()
		{
			Sound.SoundPool.Stop(_streamId);
			Sound.SoundPool.Release();
			_streamId = -1;
			soundState = SoundState.Stopped;
		}

		public void Stop(bool immediate)
		{
			Sound.SoundPool.Stop(_streamId);
			_streamId = -1;
			soundState = SoundState.Stopped;
		}

		public bool IsDisposed
		{ 
			get
			{
				return isDisposed;
			}
		}
		
		public bool IsLooped 
		{ 
			get
            {
				return _loop;
			}
			
			set
            {
				if (this._loop != value)
				{
					if (_loop)
					{
						Sound.SoundPool.SetLoop(_streamId, -1);
					}
					else
					{
						Sound.SoundPool.SetLoop(_streamId, 0);
					}
					_loop = value;
				}
			}
		}

		private float _pan;

        public float Pan 
		{ 
			get
            {
                if ( _sound != null )
				{
					return _pan;
				}
				else
				{
					return 0.0f;
				}
			}
			
			set
			{
				if (_pan != value)
				{
					// actually set this
					_pan = value;
				}
			}
		}

		public float Pitch
		{
			get
			{
				return _pitch;
			}
			set
			{
				if (_pitch != value)
				{
					_pitch = value;
					UpdatePitch();
				}
			}
		}

		private void UpdatePitch()
		{
			var diff = 1.0f - _3dPitch;

			// Convert from -1/1 range to 0.5/2.0 range of SoundPool...
			var finalRate = 0.75f * ((_pitch + diff) + 1.0f) + 0.5f;
			Sound.SoundPool.SetRate(_streamId, finalRate);
		}
		
		public SoundState State 
		{ 
			get
            {
                // Android SoundPool can't tell us when a sound is finished playing.
                // TODO: Remove this code when OpenAL for Android is implemented
                if (_sound != null && IsLooped)
                {
                    // Looping sounds use our stored state
                    return soundState;
                }
                else
                {
                    // Non looping sounds always return Stopped
                    return SoundState.Stopped;
                }
			} 
		}

		private float _volume = 1.0f;

		public float Volume
		{ 
			get
            {
				return _volume;
			}
			
			set
            {
				_volume = value;
				UpdateVolume();
			}
		}

		/// <summary>
		/// Update the volume after a change to SoundEffect.MasterVolume or this.Volume
		/// This is sort of needed because there isn't no SoundPool.MasterVolume to set the volume on all
		/// sounds in one call.
		/// </summary>
		internal void UpdateVolume()
		{
			float panRatio = (this.Pan + 1.0f) / 2.0f;
			float volumeTotal = SoundEffect.MasterVolume * _volume * _3dAttenuation;
			float volumeLeft = volumeTotal * (1.0f - panRatio);
			float volumeRight = volumeTotal * panRatio;
			Sound.SoundPool.SetVolume(_streamId, volumeLeft, volumeRight);
		}	
	}
}
