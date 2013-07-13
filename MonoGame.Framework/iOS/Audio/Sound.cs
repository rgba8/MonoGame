/*
	Sound.cs
	 
	Author:
	      Christian Beaumont chris@foundation42.org (http://www.foundation42.com)
	
	Copyright (c) 2009 Foundation42 LLC
	
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using MonoTouch;
using MonoTouch.Foundation;
using MonoTouch.AVFoundation;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
    public class Sound
    {
        byte[] buffer = null;
        string filename = string.Empty;
        int Channels;
        int Bits;
        int Rate;
        int source = -1;
        int bufferID = -1;

        public Sound()
        {
        }

        private static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            using (BinaryReader reader = new BinaryReader(stream))
            {
                // RIFF header
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                int riff_chunck_size = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                {
                    throw new NotSupportedException("Specified stream is not a wave file.");
                }

                // WAVE header
                string format_signature = new string(reader.ReadChars(4));
                if (format_signature != "fmt ")
                {
                    throw new NotSupportedException("Specified wave file is not supported.");
                }

                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();
                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();
                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();

                string data_signature = new string(reader.ReadChars(4));
                if (data_signature != "data")
                {
                    throw new NotSupportedException("Specified wave file is not supported.");
                }

                int data_chunk_size = reader.ReadInt32();

                channels = num_channels;
                bits = bits_per_sample;
                rate = sample_rate;

                return reader.ReadBytes((int)reader.BaseStream.Length);
            }
        }


        public Sound(string filename, float volume, bool looping)
        {
            this.filename = filename;

            Stream stream = new StreamReader(filename).BaseStream;
            if (stream.ReadByte() == 'R'
                && stream.ReadByte() == 'I'
                && stream.ReadByte() == 'F'
                && stream.ReadByte() == 'F')
            {
                stream.Position = 0;
                buffer = LoadWave(stream, out Channels, out Bits, out Rate);

            }

            // reserve 2 Handles
            /*uint[] MyBuffers = new uint[2]; 
            AL.GenBuffers( 2, out MyBuffers ); 

            AudioReader sound = new AudioReader(url)
            AL.BufferData(MyBuffers[0], sound.ReadToEnd());
			
            AL
            */


            /*var mediaFile = NSUrl.FromFilename(url);			
            _audioPlayer =  AVAudioPlayer.FromUrl(mediaFile); 
            _audioPlayer.Volume = volume;
            if ( looping )
            {
                _audioPlayer.NumberOfLoops = -1;
            }
            else
            {
                _audioPlayer.NumberOfLoops = 0;
            }
			
            if (!_audioPlayer.PrepareToPlay())
            {
                throw new Exception("Unable to Prepare sound for playback!");
            } */
        }

        public Sound(byte[] audiodata, float volume, bool looping)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            AL.DeleteSource(source);
            AL.DeleteBuffer(bufferID);
        }

        public double Duration
        {
            get
            {
                return 0;
            }
        }

        public double CurrentPosition
        {
            get
            {
                return 0;
            }
        }

        public bool Looping
        {
            get;
            set;

            /*get 
            {  
                //return this._Looping; 
                return (_audioPlayer.NumberOfLoops == -1 );
            }
            set
            {
                if ( value )
                {
                    _audioPlayer.NumberOfLoops = -1;
                }
                else
                {
                    _audioPlayer.NumberOfLoops = 0;
                }
            } */
        }

        public float Pan
        {
            get;
            set;
        }

        private float pitch;

        public float Pitch
        {
            get
            {
                return pitch;
            }
            set
            {
                pitch = value;
                if (source != -1)
                {
                    // pitch in XNA is -1.0 -> 1.0, reformat is to openAL
                    var newPitch = (value + 1.55f) * 0.75f;
                    AL.Source(source, ALSourcef.Pitch, newPitch);
                }
            }
        }

        public bool Playing
        {
            get;
            private set;
        }

        // Used to make sure we set the 3D/2D params for everyone
        private bool is3DSet = false;
        private bool is3D = false;

        public bool Is3D
        {
            get { return is3D; }
            set
            {
                is3D = value;
                //if (changed || is3DSet)
                {
                    is3DSet = true;

                    if (is3D)
                    {
                        AL.Source(source, ALSourceb.SourceRelative, false);
                        AL.Source(source, ALSourcef.ReferenceDistance, 50.0f);
                    }
                    else
                    {
                        tmpVector.X = 0;
                        tmpVector.Y = 0;
                        tmpVector.Z = 0;
                        AL.Source(source, ALSource3f.Position, ref tmpVector);
                        AL.Source(source, ALSourceb.SourceRelative, true);
                    }
                }
            }
        }

        public void Pause()
        {
            AL.SourcePause(source);

            Playing = false;
        }

        public void Play()
        {
            Playing = true;

            if (source == -1)
            {
                source = OpenTK.Audio.OpenAL.AL.GenSource();
                bufferID = OpenTK.Audio.OpenAL.AL.GenBuffer();

                AL.BufferData(bufferID, GetSoundFormat(Channels, Bits), buffer, buffer.Length, Rate);
                AL.Source(source, OpenTK.Audio.OpenAL.ALSourcei.Buffer, bufferID);
                AL.Source(source, ALSourceb.Looping, Looping);

                // Set the initial volume here (can't do it before since we don't have the source ID)
                AL.Source(source, ALSourcef.Gain, volume);

                // Force the pitch to be re-evaluated and re-applied now that we have the source ID
                if (this.pitch != 0.0f)
                {
                    this.Pitch = this.pitch;
                }

                Is3D = false;
            }

            AL.SourcePlay(source);
        }

        private static OpenTK.Audio.OpenAL.ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1: return bits == 8 ? OpenTK.Audio.OpenAL.ALFormat.Mono8 : OpenTK.Audio.OpenAL.ALFormat.Mono16;
                case 2: return bits == 8 ? OpenTK.Audio.OpenAL.ALFormat.Stereo8 : OpenTK.Audio.OpenAL.ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }

        private void DoPlay()
        {
            Console.WriteLine("Playing sound in thread...");

            int state;

            AL.SourcePlay(source);

            // Query the source to find out when it stops playing.
            for (; ; )
            {
                AL.GetSource(source, OpenTK.Audio.OpenAL.ALGetSourcei.SourceState, out state);
                if ((!Looping) && (OpenTK.Audio.OpenAL.ALSourceState)state != ALSourceState.Playing)
                {
                    break;
                }
                if (Looping)
                {
                    /* if (state == (int)OpenTK.Audio.OpenAL.ALSourceState.Playing && (!ShouldPlay))
                     {
                         OpenTK.Audio.OpenAL.AL.SourcePause(source);
                     }*/
                    if (state != (int)OpenTK.Audio.OpenAL.ALSourceState.Playing)// && (ShouldPlay))
                    {
                        //if (restart)
                        //{
                        //    OpenTK.Audio.OpenAL.AL.SourceRewind(source);
                        //    restart = false;
                        //}
                        OpenTK.Audio.OpenAL.AL.SourcePlay(source);
                    }
                }
                /*
                if (stop)
                {
                    AL.SourcePause(source);
                    resume = false;
                }
                if (resume)
                {
                    AL.SourcePlay(source);
                    resume = false;
                }
                */
                Thread.Sleep(1);
            }
            AL.SourceStop(source);
            AL.DeleteSource(source);
            AL.DeleteBuffer(bufferID);
        }


        public void Stop()
        {
            Playing = false;
            AL.SourceStop(source);
        }

        private float volume = 1;

        public float Volume
        {
            get
            {

                return volume;
            }
            set
            {
                volume = value;
                AL.Source(source, ALSourcef.Gain, volume);
            }
        }

        OpenTK.Vector3 tmpVector = new OpenTK.Vector3();
        OpenTK.Vector3 tmpVector2 = new OpenTK.Vector3();

        private Vector3 position = Vector3.Zero;

        public Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                tmpVector.X = position.X;
                tmpVector.Y = position.Y;
                tmpVector.Z = position.Z;
                Is3D = true;
                AL.Source(source, ALSource3f.Position, ref tmpVector);
            }
        }


        private Vector3 velocity = Vector3.Zero;

        public Vector3 Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                velocity = value;

                tmpVector.X = velocity.X;
                tmpVector.Y = velocity.Y;
                tmpVector.Z = velocity.Z;
                Is3D = true;
                AL.Source(source, ALSource3f.Velocity, ref tmpVector);
            }
        }

        public void SetListener(Vector3 position, Vector3 direction, Vector3 up, Vector3 velocity)
        {
            tmpVector.X = position.X;
            tmpVector.Y = position.Y;
            tmpVector.Z = position.Z;
            AL.Listener(ALListener3f.Position, ref tmpVector);

            tmpVector.X = direction.X;
            tmpVector.Y = direction.Y;
            tmpVector.Z = direction.Z;

            tmpVector2.X = up.X;
            tmpVector2.Y = up.Y;
            tmpVector2.Z = up.Z;
            AL.Listener(ALListenerfv.Orientation, ref tmpVector, ref tmpVector2);

            tmpVector.X = velocity.X;
            tmpVector.Y = velocity.Y;
            tmpVector.Z = velocity.Z;
            AL.Listener(ALListener3f.Velocity, ref tmpVector);
        }

        public static Sound CreateAndPlay(string url, float volume, bool looping)
        {
            Sound sound = new Sound(url, volume, looping);

            sound.Play();

            return sound;
        }
    }
}

