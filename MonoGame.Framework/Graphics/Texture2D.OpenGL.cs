// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
#endif

#if IOS
using UIKit;
using CoreGraphics;
using Foundation;
#endif

#if OPENGL
#if MONOMAC
using MonoMac.OpenGL;
using GLPixelFormat = MonoMac.OpenGL.PixelFormat;
#endif

#if WINDOWS || LINUX
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using GLPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using GLTextureTarget3D = OpenTK.Graphics.OpenGL.TextureTarget;
using GLCompressedInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat;
using GLTextureComponentCount = OpenTK.Graphics.OpenGL.PixelInternalFormat;
using GLFramebufferAttachment = OpenTK.Graphics.OpenGL.FramebufferAttachment;
#endif

#if GLES
using OpenTK.Graphics.ES30;
using GLPixelFormat = OpenTK.Graphics.ES30.PixelFormat;
using GLTextureTarget3D = OpenTK.Graphics.ES30.TextureTarget3D;
using GLCompressedInternalFormat = OpenTK.Graphics.ES30.CompressedInternalFormat;
using GLTextureComponentCount = OpenTK.Graphics.ES30.TextureComponentCount;
using GLFramebufferAttachment = OpenTK.Graphics.ES30.FramebufferSlot;
#endif

#if ANDROID
using Android.Graphics;
#endif
#endif // OPENGL

#if WINDOWS || LINUX || MONOMAC || ANGLE
using System.Drawing.Imaging;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture2D : Texture
    {
        private void PlatformConstruct(int width, int height, bool mipmap, SurfaceFormat format, SurfaceType type, bool shared)
        {
            this.glTarget = TextureTarget.Texture2D;
            this.glLastSamplerStates = new SamplerState[GraphicsDevice.MaxTextureSlots];
            format.GetGLFormat(out glInternalFormat, out glFormat, out glType);

            Threading.BlockOnUIThread(() =>
            {
                GenerateGLTextureIfRequired();
#if GLES
                GL.TexStorage2D(TextureTarget2D.Texture2D, this._levelCount, (SizedInternalFormat)glInternalFormat, width, height);
#else
                GL.TexStorage2D(TextureTarget2d.Texture2D, this._levelCount, (SizedInternalFormat)glInternalFormat, width, height);
#endif
                GraphicsExtensions.CheckGLError();
                
      
            });
        }

        public static int GetImageSize(SurfaceFormat format, int width, int height)
        {
            var imageSize = 0;
            switch (format)
            {
                case SurfaceFormat.RgbPvrtc2Bpp:
                case SurfaceFormat.RgbaPvrtc2Bpp:
                    imageSize = (Math.Max(width, 16) * Math.Max(height, 8) * 2 + 7) / 8;
                    break;
                case SurfaceFormat.RgbPvrtc4Bpp:
                case SurfaceFormat.RgbaPvrtc4Bpp:
                    imageSize = (Math.Max(width, 8) * Math.Max(height, 8) * 4 + 7) / 8;
                    break;
                case SurfaceFormat.RgbEtc1:
                case SurfaceFormat.RgbEtc2:
                    imageSize = (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0) * 8;
                    break;
                case SurfaceFormat.RgbaEtc2:
                    imageSize = (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0) * 16;
                    break;
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                    imageSize = ((width + 3) / 4) * ((height + 3) / 4) * GraphicsExtensions.GetSize(format);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return imageSize;
        }

        private void PlatformSetData<T>(int level, int arraySlice, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            Threading.BlockOnUIThread(() =>
            {
                var elementSizeInByte = Marshal.SizeOf(typeof(T));
                var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                // Use try..finally to make sure dataHandle is freed in case of an error
                try
                {
                    var startBytes = startIndex * elementSizeInByte;
                    var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);
                    int x, y, w, h;
                    if (rect.HasValue)
                    {
                        x = rect.Value.X;
                        y = rect.Value.Y;
                        w = rect.Value.Width;
                        h = rect.Value.Height;
                    }
                    else
                    {
                        x = 0;
                        y = 0;
                        w = Math.Max(width >> level, 1);
                        h = Math.Max(height >> level, 1);

                        // For DXT textures the width and height of each level is a multiple of 4.
                        // OpenGL only: The last two mip levels require the width and height to be 
                        // passed as 2x2 and 1x1, but there needs to be enough data passed to occupy 
                        // a 4x4 block. 
                        // Ref: http://www.mentby.com/Group/mac-opengl/issue-with-dxt-mipmapped-textures.html 
                        if (_format == SurfaceFormat.Dxt1 ||
                            _format == SurfaceFormat.Dxt1a ||
                            _format == SurfaceFormat.Dxt3 ||
                            _format == SurfaceFormat.Dxt5)
                        {
                            if (w > 4)
                                w = (w + 3) & ~3;
                            if (h > 4)
                                h = (h + 3) & ~3;
                        }
                    }
                    GenerateGLTextureIfRequired();

                    GL.BindTexture(this.glTarget, this.glTexture);
                    GraphicsExtensions.CheckGLError();

                    // Set pixel alignment to match texel size in bytes
                    GL.PixelStore(PixelStoreParameter.UnpackAlignment, Math.Min(GraphicsExtensions.GetSize(this.Format), 8));
                    GraphicsExtensions.CheckGLError();
                    if (glFormat == (GLPixelFormat)All.CompressedTextureFormats)
                    {
                        var imageSize = GetImageSize(this._format, w, h);
                        if (this.glTarget == TextureTarget.Texture2DArray)
                        {
#if GLES
                        GL.CompressedTexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, w, h, 1, (GLCompressedInternalFormat)glInternalFormat, imageSize, dataPtr);
#else
                            GL.CompressedTexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, w, h, 1, (GLPixelFormat)glInternalFormat, imageSize, dataPtr);
#endif
                        }
                        else
                        {
                            GL.CompressedTexSubImage2D(this.glTarget, level, x, y, w, h, (GLPixelFormat)glInternalFormat, imageSize, dataPtr);
                        }
                        GraphicsExtensions.CheckGLError();

                    }
                    else
                    {

                        if (this.glTarget == TextureTarget.Texture2DArray)
                        {
                            GL.TexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, w, h, 1, glFormat, glType, dataPtr);
                        }
                        else
                        {
                            GL.TexSubImage2D(this.glTarget, level, x, y, w, h, glFormat, glType, dataPtr);
                        }
                        GraphicsExtensions.CheckGLError();

                    }
                    // Return to default pixel alignment
                    GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
                    GraphicsExtensions.CheckGLError();
                }
                finally
                {
                    dataHandle.Free();
                }
            });
        }

        private int glFbo = -1;

        private void PlatformGetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            // TODO: check for data size and for non renderable formats (formats that can't be attached to FBO)
            var x = 0;
            var y = 0;
            var width = this.width / (1 << level);
            var height = this.height / (1 << level);
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                width = rect.Value.Width;
                height = rect.Value.Height;
            }

            if (this.glFbo == -1)
            {
                GL.GenFramebuffers(1, out this.glFbo);
                GraphicsExtensions.CheckGLError();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.glFbo);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, GLFramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, this.glTexture, 0);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.glFbo);
            }

            GL.ReadPixels(x, y, width, height, this.glFormat, this.glType, data);
            GraphicsExtensions.CheckGLError();
        }

        internal int glPbo = -1;
        internal IntPtr glSync = IntPtr.Zero;
        private GCHandle dataGCHandle;
        private int dataSizeInBytes;

        private void PlatformBeginGetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            var x = 0;
            var y = 0;
            var width = this.width / (1 << level);
            var height = this.height / (1 << level);
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                width = this.Width;
                height = this.Height;
            }

            this.dataGCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            this.dataSizeInBytes = elementCount * Marshal.SizeOf<T>();

            if (this.glPbo == -1)
            {
                GL.GenBuffers(1, out this.glPbo);
                GraphicsExtensions.CheckGLError();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, this.glPbo);
                GraphicsExtensions.CheckGLError();
                var dataSize = _format.GetSize() * this.width * this.Height;
#if GLES
                GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)dataSize, IntPtr.Zero, (OpenTK.Graphics.ES30.BufferUsage)All.StreamRead);
#else
                GL.BufferData(BufferTarget.PixelPackBuffer, (IntPtr)dataSize, IntPtr.Zero, BufferUsageHint.StreamRead);
#endif
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, this.glPbo);
                GraphicsExtensions.CheckGLError();
            }

            if (this.glFbo == -1)
            {
                GL.GenFramebuffers(1, out this.glFbo);
                GraphicsExtensions.CheckGLError();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.glFbo);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, GLFramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, this.glTexture, level);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this.glFbo);
                GraphicsExtensions.CheckGLError();
            }

            GL.ReadPixels(x, y, width, height, this.glFormat, this.glType, IntPtr.Zero);
            GraphicsExtensions.CheckGLError();
            this.glSync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, 0);
            GraphicsExtensions.CheckGLError();

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GraphicsExtensions.CheckGLError();
        }

        private bool PlatformIsGetDataCompleted()
        {
            if (this.glSync != IntPtr.Zero)
            {
                var length = 0;
                var value = 0;
                GL.GetSync(this.glSync, SyncParameterName.SyncStatus, 1, out length, out value);
                if (value == (int)All.Signaled)
                    return true;
            }
            return false;
        }

        private void PlatformEndGetData()
        {
#if GLES
            var status = GL.ClientWaitSync(this.glSync, (int)All.SyncFlushCommandsBit, 1000000);
            if (status == All.AlreadySignaled || status == All.ConditionSatisfied)
#else
            var status = GL.ClientWaitSync(this.glSync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000);
            if (status == WaitSyncStatus.AlreadySignaled || status == WaitSyncStatus.ConditionSatisfied)
#endif
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer, this.glPbo);
                var pboPtr = GL.MapBufferRange(BufferTarget.PixelPackBuffer, IntPtr.Zero, (IntPtr)dataSizeInBytes, BufferAccessMask.MapReadBit);
                unsafe
                {
                    Buffer.MemoryCopy(pboPtr.ToPointer(), dataGCHandle.AddrOfPinnedObject().ToPointer(), dataSizeInBytes, dataSizeInBytes);
                }
                GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
                GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            }
            GL.DeleteSync(this.glSync);
            this.glSync = IntPtr.Zero;
        }

        internal IntPtr glUnpackSync = IntPtr.Zero;

        private void PlatformBeginSetData<T>(int level, int arraySlice, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            var x = 0;
            var y = 0;
            var width = this.width / (1 << level);
            var height = this.height / (1 << level);
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                width = this.Width;
                height = this.Height;
            }

            if (this.glPbo == -1)
            {
                GL.GenBuffers(1, out this.glPbo);
                GraphicsExtensions.CheckGLError();
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, this.glPbo);
                GraphicsExtensions.CheckGLError();
                var pboSizeInBytes = _format.GetSize() * this.width * this.Height;
#if GLES
                GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)pboSizeInBytes, IntPtr.Zero, (OpenTK.Graphics.ES30.BufferUsage)All.StreamDraw);
#else
                GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)pboSizeInBytes, IntPtr.Zero, BufferUsageHint.StreamRead);
#endif
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, this.glPbo);
                GraphicsExtensions.CheckGLError();
            }

            var elementSizeInBytes = Marshal.SizeOf<T>();
            var dataSizeInBytes = elementCount * elementSizeInBytes;
            var pboPtr = GL.MapBufferRange(BufferTarget.PixelUnpackBuffer, IntPtr.Zero, (IntPtr)dataSizeInBytes, BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateRangeBit);
            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            unsafe
            {
                Buffer.MemoryCopy(Marshal.UnsafeAddrOfPinnedArrayElement(data, startIndex).ToPointer(), pboPtr.ToPointer(), dataSizeInBytes, dataSizeInBytes);
            }
            gcHandle.Free();
            GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);

            GL.BindTexture(this.glTarget, this.glTexture);
            GraphicsExtensions.CheckGLError();

            // Set pixel alignment to match texel size in bytes
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, Math.Min(GraphicsExtensions.GetSize(this.Format), 8));
            GraphicsExtensions.CheckGLError();
            if (glFormat == (GLPixelFormat)All.CompressedTextureFormats)
            {
                // Partially updating ETC2/EAC texture is not supported according to the spec. iOS seems to still allow it, Android usually doesn't (crash or corrupted textures).
                var imageSize = GetImageSize(this._format, width, height);
                if (this.glTarget == TextureTarget.Texture2DArray)
                {
#if GLES
                    GL.CompressedTexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, width, height, 1, (GLCompressedInternalFormat)glInternalFormat, imageSize, IntPtr.Zero);
#else
                    GL.CompressedTexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, width, height, 1, (GLPixelFormat)glInternalFormat, imageSize, IntPtr.Zero);
#endif
                }
                else
                {
                    GL.CompressedTexSubImage2D(this.glTarget, level, x, y, width, height, (GLPixelFormat)glInternalFormat, imageSize, IntPtr.Zero);
                }
                GraphicsExtensions.CheckGLError();

            }
            else
            {
                if (this.glTarget == TextureTarget.Texture2DArray)
                {
                    GL.TexSubImage3D((GLTextureTarget3D)this.glTarget, level, x, y, arraySlice, width, height, 1, glFormat, glType, IntPtr.Zero);
                }
                else
                {
                    GL.TexSubImage2D(this.glTarget, level, x, y, width, height, glFormat, glType, IntPtr.Zero);
                }
            }

            GraphicsExtensions.CheckGLError();
            this.glUnpackSync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, 0);
            GraphicsExtensions.CheckGLError();

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            GraphicsExtensions.CheckGLError();
        }

        private bool PlatformIsSetDataCompleted()
        {
            if (this.glUnpackSync != IntPtr.Zero)
            {
                var length = 0;
                var value = 0;
                GL.GetSync(this.glUnpackSync, SyncParameterName.SyncStatus, 1, out length, out value);
                if (value == (int)All.Signaled)
                    return true;
            }
            return false;
        }

        private static Texture2D PlatformFromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
#if IOS || MONOMAC

#if IOS
			using (var uiImage = UIImage.LoadFromData(NSData.FromStream(stream)))
#elif MONOMAC
			using (var nsImage = NSImage.FromStream (stream))
#endif
			{
#if IOS
				var cgImage = uiImage.CGImage;
#elif MONOMAC
				var rectangle = RectangleF.Empty;
				var cgImage = nsImage.AsCGImage (ref rectangle, null, null);
#endif

			    return PlatformFromStream(graphicsDevice, cgImage);
			}
#endif
#if ANDROID
            using (Bitmap image = BitmapFactory.DecodeStream(stream, null, new BitmapFactory.Options
            {
                InScaled = false,
                InDither = false,
                InJustDecodeBounds = false,
                InPurgeable = true,
                InInputShareable = true,
            }))
            {
                return PlatformFromStream(graphicsDevice, image);
            }
#endif
#if WINDOWS || LINUX || ANGLE
            Bitmap image = (Bitmap)Bitmap.FromStream(stream);
            try
            {
                var data = new byte[image.Width * image.Height * 4];
                unsafe
                {
                    fixed (byte* dataPtr = &data[0])
                    {
                        var bitmapData = new BitmapData { Width = image.Width, Height = image.Height, PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb, Stride = image.Width * 4 };
                        bitmapData.Scan0 = (IntPtr)dataPtr;
                        var rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
                        image.LockBits(rect, ImageLockMode.UserInputBuffer | ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, bitmapData);
                        image.UnlockBits(bitmapData);
                    }
                }
                Texture2D texture = null;
                texture = new Texture2D(graphicsDevice, image.Width, image.Height, false, SurfaceFormat.Bgra32);
                texture.SetData(data);

                return texture;
            }
            finally
            {
                image.Dispose();
            }
#endif
        }

#if IOS
        [CLSCompliant(false)]
        public static Texture2D FromStream(GraphicsDevice graphicsDevice, UIImage uiImage)
        {
            return PlatformFromStream(graphicsDevice, uiImage.CGImage);
        }
#elif ANDROID
        [CLSCompliant(false)]
        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Bitmap bitmap)
        {
            return PlatformFromStream(graphicsDevice, bitmap);
        }

        [CLSCompliant(false)]
        public void Reload(Bitmap image)
        {
            var width = image.Width;
            var height = image.Height;

            int[] pixels = new int[width * height];
            if ((width != image.Width) || (height != image.Height))
            {
                using (Bitmap imagePadded = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
                {
                    Canvas canvas = new Canvas(imagePadded);
                    canvas.DrawARGB(0, 0, 0, 0);
                    canvas.DrawBitmap(image, 0, 0, null);
                    imagePadded.GetPixels(pixels, 0, width, 0, 0, width, height);
                    imagePadded.Recycle();
                }
            }
            else
            {
                image.GetPixels(pixels, 0, width, 0, 0, width, height);
            }

            image.Recycle();

            this.SetData<int>(pixels);
        }
#endif

#if MONOMAC
        public static Texture2D FromStream(GraphicsDevice graphicsDevice, NSImage nsImage)
        {
            var rectangle = RectangleF.Empty;
		    var cgImage = nsImage.AsCGImage (ref rectangle, null, null);
            return PlatformFromStream(graphicsDevice, cgImage);
        }
#endif

#if IOS || MONOMAC
        private static Texture2D PlatformFromStream(GraphicsDevice graphicsDevice, CGImage cgImage)
        {
			var width = cgImage.Width;
			var height = cgImage.Height;

            var data = new byte[width * height * 4];

            var colorSpace = CGColorSpace.CreateDeviceRGB();
            var bitmapContext = new CGBitmapContext(data, width, height, 8, width * 4, colorSpace, CGBitmapFlags.PremultipliedLast);
            bitmapContext.DrawImage(new CGRect(0, 0, width, height), cgImage);
            bitmapContext.Dispose();
            colorSpace.Dispose();

            Texture2D texture = null;
            texture = new Texture2D(graphicsDevice, (int)width, (int)height, false, SurfaceFormat.Color);
            texture.SetData(data);

            return texture;
        }
#elif ANDROID
        private static Texture2D PlatformFromStream(GraphicsDevice graphicsDevice, Bitmap image)
        {
            var width = image.Width;
            var height = image.Height;

            int[] pixels = new int[width * height];
            if ((width != image.Width) || (height != image.Height))
            {
                using (Bitmap imagePadded = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
                {
                    Canvas canvas = new Canvas(imagePadded);
                    canvas.DrawARGB(0, 0, 0, 0);
                    canvas.DrawBitmap(image, 0, 0, null);
                    imagePadded.GetPixels(pixels, 0, width, 0, 0, width, height);
                    imagePadded.Recycle();
                }
            }
            else
            {
                image.GetPixels(pixels, 0, width, 0, 0, width, height);
            }
            image.Recycle();

            Texture2D texture = null;
            texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Bgra32);
            texture.SetData<int>(pixels);

            return texture;
        }
#endif

        private void FillTextureFromStream(Stream stream)
        {
#if ANDROID
            using (Bitmap image = BitmapFactory.DecodeStream(stream, null, new BitmapFactory.Options
            {
                InScaled = false,
                InDither = false,
                InJustDecodeBounds = false,
                InPurgeable = true,
                InInputShareable = true,
            }))
            {
                var width = image.Width;
                var height = image.Height;

                int[] pixels = new int[width * height];
                image.GetPixels(pixels, 0, width, 0, 0, width, height);

                this.SetData<int>(pixels);
                image.Recycle();
            }
#endif
        }

        private void PlatformSaveAsJpeg(Stream stream, int width, int height)
        {
#if MONOMAC || WINDOWS
            SaveAsImage(stream, width, height, ImageFormat.Jpeg);
#elif ANDROID
            SaveAsImage(stream, width, height, Bitmap.CompressFormat.Jpeg);
#else
            throw new NotImplementedException();
#endif
        }

        private void PlatformSaveAsPng(Stream stream, int width, int height)
        {
#if MONOMAC || WINDOWS
            SaveAsImage(stream, width, height, ImageFormat.Png);
#elif ANDROID
            SaveAsImage(stream, width, height, Bitmap.CompressFormat.Png);
#else
            throw new NotImplementedException();
#endif
        }

#if MONOMAC || WINDOWS
        private void SaveAsImage(Stream stream, int width, int height, ImageFormat format)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream", "'stream' cannot be null (Nothing in Visual Basic)");
            }
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width", width, "'width' cannot be less than or equal to zero");
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height", height, "'height' cannot be less than or equal to zero");
            }
            if (format == null)
            {
                throw new ArgumentNullException("format", "'format' cannot be null (Nothing in Visual Basic)");
            }

            byte[] data = null;
            GCHandle? handle = null;
            Bitmap bitmap = null;
            try
            {
                data = new byte[width * height * 4];
                handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                GetData(data);

                // internal structure is BGR while bitmap expects RGB
                for (int i = 0; i < data.Length; i += 4)
                {
                    byte temp = data[i + 0];
                    data[i + 0] = data[i + 2];
                    data[i + 2] = temp;
                }

                bitmap = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, handle.Value.AddrOfPinnedObject());

                bitmap.Save(stream, format);
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
                if (handle.HasValue)
                {
                    handle.Value.Free();
                }
                if (data != null)
                {
                    data = null;
                }
            }
        }
#elif ANDROID
        private void SaveAsImage(Stream stream, int width, int height, Bitmap.CompressFormat format)
        {
            int[] data = new int[width * height];
            GetData(data);
            // internal structure is BGR while bitmap expects RGB
            for (int i = 0; i < data.Length; ++i)
            {
                uint pixel = (uint)data[i];
                data[i] = (int)((pixel & 0xFF00FF00) | ((pixel & 0x00FF0000) >> 16) | ((pixel & 0x000000FF) << 16));
            }
            using (Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
            {
                bitmap.SetPixels(data, 0, width, 0, 0, width, height);
                bitmap.Compress(format, 100, stream);
                bitmap.Recycle();
            }
        }
#endif

        // This method allows games that use Texture2D.FromStream 
        // to reload their textures after the GL context is lost.
        private void PlatformReload(Stream textureStream)
        {
            GenerateGLTextureIfRequired();
            FillTextureFromStream(textureStream);
        }

        private void GenerateGLTextureIfRequired()
        {
            if (this.glTexture < 0)
            {
                GL.GenTextures(1, out this.glTexture);
                GraphicsExtensions.CheckGLError();

                // For best compatibility and to keep the default wrap mode of XNA, only set ClampToEdge if either
                // dimension is not a power of two.
                var wrap = TextureWrapMode.Repeat;
                if (((width & (width - 1)) != 0) || ((height & (height - 1)) != 0))
                    wrap = TextureWrapMode.ClampToEdge;

                GL.BindTexture(this.glTarget, this.glTexture);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(this.glTarget, TextureParameterName.TextureMinFilter,
                                (_levelCount > 1) ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(this.glTarget, TextureParameterName.TextureMagFilter,
                                (int)TextureMagFilter.Linear);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(this.glTarget, TextureParameterName.TextureWrapS, (int)wrap);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(this.glTarget, TextureParameterName.TextureWrapT, (int)wrap);
                GraphicsExtensions.CheckGLError();

                if (this.Format == SurfaceFormat.Bgra32)
                {
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int)All.Blue);
                    GraphicsExtensions.CheckGLError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int)All.Green);
                    GraphicsExtensions.CheckGLError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int)All.Red);
                    GraphicsExtensions.CheckGLError();
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int)All.Alpha);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }
    }
}
