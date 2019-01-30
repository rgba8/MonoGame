// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#elif GLES
using OpenTK.Graphics.ES30;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
	public partial class TextureCube
	{
        private void PlatformConstruct(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format, bool renderTarget)
        {
            this.glTarget = TextureTarget.TextureCubeMap;
            this.glLastSamplerStates = new SamplerState[GraphicsDevice.MaxTextureSlots];

            Threading.BlockOnUIThread(() =>
            {
			    GL.GenTextures(1, out this.glTexture);
                GraphicsExtensions.CheckGLError();
                GL.BindTexture(TextureTarget.TextureCubeMap, this.glTexture);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                                mipMap ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
                                (int)TextureMagFilter.Linear);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
                                (int)TextureWrapMode.ClampToEdge);
                GraphicsExtensions.CheckGLError();
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
                                (int)TextureWrapMode.ClampToEdge);
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

                format.GetGLFormat(out glInternalFormat, out glFormat, out glType);
#if GLES
                GL.TexStorage2D(TextureTarget2D.TextureCubeMap, this._levelCount, (SizedInternalFormat)glInternalFormat, size, size);
#else
                GL.TexStorage2D(TextureTarget2d.TextureCubeMap, this._levelCount, (SizedInternalFormat)glInternalFormat, size, size);
#endif
                GraphicsExtensions.CheckGLError();
            });
        }

        private void PlatformGetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
        {
#if OPENGL && MONOMAC
            TextureTarget target = GetGLCubeFace(cubeMapFace);
            GL.BindTexture(target, this.glTexture);
            // 4 bytes per pixel
            if (data.Length < size * size * 4)
                throw new ArgumentException("data");

            GL.GetTexImage<T>(target, 0, PixelFormat.Bgra,
                PixelType.UnsignedByte, data);
#else
            throw new NotImplementedException();
#endif
        }

        private void PlatformSetData<T>(CubeMapFace face, int level, IntPtr dataPtr, int xOffset, int yOffset, int width, int height)
        {
            Threading.BlockOnUIThread(() =>
            {
                GL.BindTexture(TextureTarget.TextureCubeMap, this.glTexture);
                GraphicsExtensions.CheckGLError();

                TextureTarget target = GetGLCubeFace(face);
                if (glFormat == (PixelFormat)All.CompressedTextureFormats)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.TexSubImage2D(target, level, xOffset, yOffset, width, height, glFormat, glType, dataPtr);
                    GraphicsExtensions.CheckGLError();
                }
            });
        }

		private TextureTarget GetGLCubeFace(CubeMapFace face) 
        {
			switch (face) 
            {
			case CubeMapFace.PositiveX: return TextureTarget.TextureCubeMapPositiveX;
			case CubeMapFace.NegativeX: return TextureTarget.TextureCubeMapNegativeX;
			case CubeMapFace.PositiveY: return TextureTarget.TextureCubeMapPositiveY;
			case CubeMapFace.NegativeY: return TextureTarget.TextureCubeMapNegativeY;
			case CubeMapFace.PositiveZ: return TextureTarget.TextureCubeMapPositiveZ;
			case CubeMapFace.NegativeZ: return TextureTarget.TextureCubeMapNegativeZ;
			}
			throw new ArgumentException();
		}
	}
}

