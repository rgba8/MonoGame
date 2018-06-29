using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#if OPENGL
#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
#elif GLES
#if ANGLE
using OpenTK.Graphics;
#endif
using OpenTK.Graphics.ES30;
using VertexPointerType = OpenTK.Graphics.ES30.All;
using ColorPointerType = OpenTK.Graphics.ES30.All;
using NormalPointerType = OpenTK.Graphics.ES30.All;
using TexCoordPointerType = OpenTK.Graphics.ES30.All;
#endif
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    static class GraphicsExtensions
    {
#if OPENGL
        public static All OpenGL11(CullMode cull)
        {
            switch (cull)
            {
                case CullMode.CullClockwiseFace:
                    return All.Cw;
                case CullMode.CullCounterClockwiseFace:
                    return All.Ccw;
                default:
                    throw new ArgumentException();
            }
        }

        public static int OpenGLNumberOfElements(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return 1;

                case VertexElementFormat.Vector2:
                    return 2;

                case VertexElementFormat.Vector3:
                    return 3;

                case VertexElementFormat.Vector4:
                    return 4;

                case VertexElementFormat.Color:
                    return 4;

                case VertexElementFormat.Byte4:
                    return 4;

                case VertexElementFormat.Short2:
                    return 2;

                case VertexElementFormat.Short4:
                    return 2;

                case VertexElementFormat.NormalizedShort2:
                    return 2;

                case VertexElementFormat.NormalizedShort4:
                    return 4;

                case VertexElementFormat.HalfVector2:
                    return 2;

                case VertexElementFormat.HalfVector4:
                    return 4;
            }

            throw new ArgumentException();
        }

        public static VertexPointerType OpenGLVertexPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector2:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector3:
                    return VertexPointerType.Float;

                case VertexElementFormat.Vector4:
                    return VertexPointerType.Float;

                case VertexElementFormat.Color:
                    return VertexPointerType.Short;

                case VertexElementFormat.Byte4:
                    return VertexPointerType.Short;

                case VertexElementFormat.Short2:
                    return VertexPointerType.Short;

                case VertexElementFormat.Short4:
                    return VertexPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return VertexPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return VertexPointerType.Short;

                case VertexElementFormat.HalfVector2:
                    return VertexPointerType.Float;

                case VertexElementFormat.HalfVector4:
                    return VertexPointerType.Float;
            }

            throw new ArgumentException();
        }

		public static VertexAttribPointerType OpenGLVertexAttribPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector2:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector3:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Vector4:
                    return VertexAttribPointerType.Float;

                case VertexElementFormat.Color:
					return VertexAttribPointerType.UnsignedByte;

                case VertexElementFormat.Byte4:
					return VertexAttribPointerType.UnsignedByte;

                case VertexElementFormat.Short2:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.Short4:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return VertexAttribPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return VertexAttribPointerType.Short;
                
#if MONOMAC || WINDOWS || LINUX
               case VertexElementFormat.HalfVector2:
                    return VertexAttribPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return VertexAttribPointerType.HalfFloat;
#endif
            }

            throw new ArgumentException();
        }

        public static bool OpenGLVertexAttribNormalized(this VertexElement element)
        {
            // TODO: This may or may not be the right behavor.  
            //
            // For instance the VertexElementFormat.Byte4 format is not supposed
            // to be normalized, but this line makes it so.
            //
            // The question is in MS XNA are types normalized based on usage or
            // normalized based to their format?
            //
            if (element.VertexElementUsage == VertexElementUsage.Color)
                return true;

            switch (element.VertexElementFormat)
            {
                case VertexElementFormat.NormalizedShort2:
                case VertexElementFormat.NormalizedShort4:
                    return true;

                default:
                    return false;
            }
        }

        public static ColorPointerType OpenGLColorPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector2:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector3:
                    return ColorPointerType.Float;

                case VertexElementFormat.Vector4:
                    return ColorPointerType.Float;

                case VertexElementFormat.Color:
                    return ColorPointerType.UnsignedByte;

                case VertexElementFormat.Byte4:
                    return ColorPointerType.UnsignedByte;

                case VertexElementFormat.Short2:
                    return ColorPointerType.Short;

                case VertexElementFormat.Short4:
                    return ColorPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return ColorPointerType.UnsignedShort;

                case VertexElementFormat.NormalizedShort4:
                    return ColorPointerType.UnsignedShort;
				
#if MONOMAC
                case VertexElementFormat.HalfVector2:
                    return ColorPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return ColorPointerType.HalfFloat;
#endif
			}

            throw new ArgumentException();
        }

       public static NormalPointerType OpenGLNormalPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector2:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector3:
                    return NormalPointerType.Float;

                case VertexElementFormat.Vector4:
                    return NormalPointerType.Float;

                case VertexElementFormat.Color:
                    return NormalPointerType.Byte;

                case VertexElementFormat.Byte4:
                    return NormalPointerType.Byte;

                case VertexElementFormat.Short2:
                    return NormalPointerType.Short;

                case VertexElementFormat.Short4:
                    return NormalPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return NormalPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return NormalPointerType.Short;
				
#if MONOMAC
                case VertexElementFormat.HalfVector2:
                    return NormalPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return NormalPointerType.HalfFloat;
#endif
			}

            throw new ArgumentException();
        }

       public static TexCoordPointerType OpenGLTexCoordPointerType(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector2:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector3:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Vector4:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Color:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Byte4:
                    return TexCoordPointerType.Float;

                case VertexElementFormat.Short2:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.Short4:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.NormalizedShort2:
                    return TexCoordPointerType.Short;

                case VertexElementFormat.NormalizedShort4:
                    return TexCoordPointerType.Short;
				
#if MONOMAC
                case VertexElementFormat.HalfVector2:
                    return TexCoordPointerType.HalfFloat;

                case VertexElementFormat.HalfVector4:
                    return TexCoordPointerType.HalfFloat;
#endif
			}

            throw new ArgumentException();
        }

		
		public static BlendEquationMode GetBlendEquationMode (this BlendFunction function)
		{
			switch (function) {
			case BlendFunction.Add:
				return BlendEquationMode.FuncAdd;
#if IOS || ANDROID
			case BlendFunction.Max:
				return BlendEquationMode.Max;
			case BlendFunction.Min:
				return BlendEquationMode.Min;
#elif MONOMAC || WINDOWS || LINUX
			case BlendFunction.Max:
				return BlendEquationMode.Max;
			case BlendFunction.Min:
				return BlendEquationMode.Min;
#endif
			case BlendFunction.ReverseSubtract:
				return BlendEquationMode.FuncReverseSubtract;
			case BlendFunction.Subtract:
				return BlendEquationMode.FuncSubtract;

			default:
                throw new ArgumentException();
			}
		}

		public static BlendingFactorSrc GetBlendFactorSrc (this Blend blend)
		{
			switch (blend) {
			case Blend.DestinationAlpha:
				return BlendingFactorSrc.DstAlpha;
			case Blend.DestinationColor:
				return BlendingFactorSrc.DstColor;
			case Blend.InverseDestinationAlpha:
				return BlendingFactorSrc.OneMinusDstAlpha;
			case Blend.InverseDestinationColor:
				return BlendingFactorSrc.OneMinusDstColor;
			case Blend.InverseSourceAlpha:
				return BlendingFactorSrc.OneMinusSrcAlpha;
			case Blend.InverseSourceColor:
#if MONOMAC || WINDOWS || LINUX
				return (BlendingFactorSrc)All.OneMinusSrcColor;
#else
				return BlendingFactorSrc.OneMinusSrcColor;
#endif
			case Blend.One:
				return BlendingFactorSrc.One;
			case Blend.SourceAlpha:
				return BlendingFactorSrc.SrcAlpha;
			case Blend.SourceAlphaSaturation:
				return BlendingFactorSrc.SrcAlphaSaturate;
			case Blend.SourceColor:
#if MONOMAC || WINDOWS || LINUX
				return (BlendingFactorSrc)All.SrcColor;
#else
				return BlendingFactorSrc.SrcColor;
#endif
			case Blend.Zero:
				return BlendingFactorSrc.Zero;
			default:
				return BlendingFactorSrc.One;
			}

		}

		public static BlendingFactorDest GetBlendFactorDest (this Blend blend)
		{
			switch (blend) {
			case Blend.DestinationAlpha:
				return BlendingFactorDest.DstAlpha;
//			case Blend.DestinationColor:
//				return BlendingFactorDest.DstColor;
			case Blend.InverseDestinationAlpha:
				return BlendingFactorDest.OneMinusDstAlpha;
//			case Blend.InverseDestinationColor:
//				return BlendingFactorDest.OneMinusDstColor;
			case Blend.InverseSourceAlpha:
				return BlendingFactorDest.OneMinusSrcAlpha;
			case Blend.InverseSourceColor:
#if MONOMAC || WINDOWS
				return (BlendingFactorDest)All.OneMinusSrcColor;
#else
				return BlendingFactorDest.OneMinusSrcColor;
#endif
			case Blend.One:
				return BlendingFactorDest.One;
			case Blend.SourceAlpha:
				return BlendingFactorDest.SrcAlpha;
//			case Blend.SourceAlphaSaturation:
//				return BlendingFactorDest.SrcAlphaSaturate;
			case Blend.SourceColor:
#if MONOMAC || WINDOWS
				return (BlendingFactorDest)All.SrcColor;
#else
				return BlendingFactorDest.SrcColor;
#endif
			case Blend.Zero:
				return BlendingFactorDest.Zero;
			default:
				return BlendingFactorDest.One;
			}

		}

#if WINDOWS || LINUX || ANGLE
        /// <summary>
        /// Convert a <see cref="SurfaceFormat"/> to an OpenTK.Graphics.ColorFormat.
        /// This is used for setting up the backbuffer format of the OpenGL context.
        /// </summary>
        /// <returns>An OpenTK.Graphics.ColorFormat instance.</returns>
        /// <param name="format">The <see cref="SurfaceFormat"/> to convert.</param>
        internal static ColorFormat GetColorFormat(this SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Alpha8:
                    return new ColorFormat(0, 0, 0, 8);
                case SurfaceFormat.Bgr565:
                    return new ColorFormat(5, 6, 5, 0);
                case SurfaceFormat.Bgra4444:
                    return new ColorFormat(4, 4, 4, 4);
                case SurfaceFormat.Bgra5551:
                    return new ColorFormat(5, 5, 5, 1);
                case SurfaceFormat.Bgr32:
                    return new ColorFormat(8, 8, 8, 0);
                case SurfaceFormat.Bgra32:
                case SurfaceFormat.Color:
                    return new ColorFormat(8, 8, 8, 8);
                case SurfaceFormat.Rgba1010102:
                    return new ColorFormat(10, 10, 10, 2);
                default:
                    // Floating point backbuffers formats could be implemented
                    // but they are not typically used on the backbuffer. In
                    // those cases it is better to create a render target instead.
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Converts <see cref="PresentInterval"/> to OpenGL swap interval.
        /// </summary>
        /// <returns>A value according to EXT_swap_control</returns>
        /// <param name="interval">The <see cref="PresentInterval"/> to convert.</param>
        internal static int GetSwapInterval(this PresentInterval interval)
        {
            // See http://www.opengl.org/registry/specs/EXT/swap_control.txt
            // and https://www.opengl.org/registry/specs/EXT/glx_swap_control_tear.txt
            // OpenTK checks for EXT_swap_control_tear:
            // if supported, a swap interval of -1 enables adaptive vsync;
            // otherwise -1 is converted to 1 (vsync enabled.)

            switch (interval)
            {

                case PresentInterval.Immediate:
                    return 0;
                case PresentInterval.One:
                    return 1;
                case PresentInterval.Two:
                    return 2;
                case PresentInterval.Default:
                default:
                    return -1;
            }
        }
#endif
		
		
		internal static void GetGLFormat (this SurfaceFormat format,
		                                 out PixelInternalFormat glInternalFormat,
		                                 out PixelFormat glFormat,
		                                 out PixelType glType)
		{
			glInternalFormat = PixelInternalFormat.Rgba;
			glFormat = PixelFormat.Rgba;
			glType = PixelType.UnsignedByte;
			
			switch (format) {
			case SurfaceFormat.Color:
				glInternalFormat = PixelInternalFormat.Rgba;
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedByte;
				break;
            case SurfaceFormat.U248:
                glInternalFormat = (PixelInternalFormat)35056;
                glFormat = PixelFormat.DepthStencil;
                glType = PixelType.UnsignedInt248;
                break;
			case SurfaceFormat.Bgr565:
				glInternalFormat = PixelInternalFormat.Rgb;
				glFormat = PixelFormat.Rgb;
				glType = PixelType.UnsignedShort565;
				break;
			case SurfaceFormat.Bgra4444:
#if IOS || ANDROID
				glInternalFormat = PixelInternalFormat.Rgba;
#else
				glInternalFormat = PixelInternalFormat.Rgba4;
#endif
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedShort4444;
				break;
			case SurfaceFormat.Bgra5551:
				glInternalFormat = PixelInternalFormat.Rgba;
				glFormat = PixelFormat.Rgba;
				glType = PixelType.UnsignedShort5551;
				break;
			case SurfaceFormat.Alpha8:
                    glInternalFormat = (PixelInternalFormat)0x1903;// PixelInternalFormat.R8;
				glFormat = PixelFormat.Red;
				glType = PixelType.UnsignedByte;
				break;
			case SurfaceFormat.Dxt1:
                glInternalFormat = (PixelInternalFormat)0x83F0; // GL_COMPRESSED_RGB_S3TC_DXT1_EXT
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
            case SurfaceFormat.Dxt1a:
                glInternalFormat = (PixelInternalFormat)0x83F1; // GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
                glFormat = (PixelFormat)All.CompressedTextureFormats;
                break;
            case SurfaceFormat.Dxt3:
                glInternalFormat = (PixelInternalFormat)0x83F2; // GL_COMPRESSED_RGBA_S3TC_DXT3_EXT
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.Dxt5:
                glInternalFormat = (PixelInternalFormat)0x83F3; // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.Single:
				glInternalFormat = (PixelInternalFormat)All.R32f;
				glFormat = PixelFormat.Red;
				glType = PixelType.Float;
				break;
            case SurfaceFormat.HalfVector2:
                glInternalFormat = (PixelInternalFormat)All.Rg16f;
				glFormat = PixelFormat.Rg;
				glType = PixelType.HalfFloat;
                break;
            // HdrBlendable implemented as HalfVector4 (see http://blogs.msdn.com/b/shawnhar/archive/2010/07/09/surfaceformat-hdrblendable.aspx)
            case SurfaceFormat.HdrBlendable:
            case SurfaceFormat.HalfVector4:
                glInternalFormat = (PixelInternalFormat)All.Rgba16f;
                glFormat = PixelFormat.Rgba;
                glType = PixelType.HalfFloat;
                break;
            case SurfaceFormat.HalfVector4Oes:
                glInternalFormat = PixelInternalFormat.Rgba;
                glFormat = PixelFormat.Rgba;
                glType = (PixelType)0x8D61; // GL_HALF_FLOAT_OES
                break;
            case SurfaceFormat.HalfSingle:
                glInternalFormat = (PixelInternalFormat)All.R16f;
                glFormat = PixelFormat.Red;
                glType = PixelType.HalfFloat;
                break;

            case SurfaceFormat.Vector2:
                glInternalFormat = (PixelInternalFormat)All.Rg32f;
                glFormat = PixelFormat.Rg;
                glType = PixelType.Float;
                break;

            case SurfaceFormat.Vector4:
                glInternalFormat = (PixelInternalFormat)All.Rgba32f;
                glFormat = PixelFormat.Rgba;
                glType = PixelType.Float;
                break;

            case SurfaceFormat.NormalizedByte2:
                glInternalFormat = (PixelInternalFormat)All.Rg8i;
                glFormat = PixelFormat.Rg;
                glType = PixelType.Byte;
                break;

            case SurfaceFormat.NormalizedByte4:
                glInternalFormat = (PixelInternalFormat)All.Rgba8i;
                glFormat = PixelFormat.Rgba;
                glType = PixelType.Byte;
                break;
            case SurfaceFormat.Rg32:
                glInternalFormat = (PixelInternalFormat)All.Rg16ui;
                glFormat = PixelFormat.Rg;
                glType = PixelType.UnsignedShort;
                break;
            case SurfaceFormat.Rgba64:
                glInternalFormat = (PixelInternalFormat)All.Rgba16ui;
                glFormat = PixelFormat.Rgba;
                glType = PixelType.UnsignedShort;
                break;
            case SurfaceFormat.Rgba1010102:
#if GLES
                throw new NotSupportedException();
#else
                glInternalFormat = (PixelInternalFormat)All.Rgb10A2ui;
                glFormat = PixelFormat.Rgba;
                glType = PixelType.UnsignedInt1010102;
                break;
#endif
            case SurfaceFormat.RgbEtc1:
                glInternalFormat = (PixelInternalFormat)0x8D64; // GL_ETC1_RGB8_OES
                glFormat = (PixelFormat)All.CompressedTextureFormats;
                break;
            case SurfaceFormat.RgbEtc2:
                glInternalFormat = (PixelInternalFormat)All.CompressedRgb8Etc2;
                glFormat = (PixelFormat)All.CompressedTextureFormats;
                break;
            case SurfaceFormat.RgbaEtc2:
                glInternalFormat = (PixelInternalFormat)All.CompressedRgba8Etc2Eac;
                glFormat = (PixelFormat)All.CompressedTextureFormats;
                break;
			case SurfaceFormat.RgbPvrtc2Bpp:
				glInternalFormat = (PixelInternalFormat)OpenTK.Graphics.ES20.All.CompressedRgbPvrtc2Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbPvrtc4Bpp:
                glInternalFormat = (PixelInternalFormat)OpenTK.Graphics.ES20.All.CompressedRgbPvrtc4Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbaPvrtc2Bpp:
				glInternalFormat = (PixelInternalFormat)OpenTK.Graphics.ES20.All.CompressedRgbaPvrtc2Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
			case SurfaceFormat.RgbaPvrtc4Bpp:
                glInternalFormat = (PixelInternalFormat)OpenTK.Graphics.ES20.All.CompressedRgbaPvrtc4Bppv1Img;
				glFormat = (PixelFormat)All.CompressedTextureFormats;
				break;
                case SurfaceFormat.NONE:
                    break;
			default:
				throw new NotSupportedException();
			}
		}

#endif // OPENGL

        public static int GetFrameLatency(this PresentInterval interval)
        {
            switch (interval)
            {
                case PresentInterval.Immediate:
                    return 0;

                case PresentInterval.Two:
                    return 2;

                default:
                    return 1;
            }
        }

        public static int GetSize(this SurfaceFormat surfaceFormat)
        {
            switch (surfaceFormat)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.RgbPvrtc2Bpp:
                case SurfaceFormat.RgbaPvrtc2Bpp:
                case SurfaceFormat.RgbEtc1:
                case SurfaceFormat.RgbEtc2:
                    // One texel in DXT1, PVRTC 2bpp and ETC1 is a minimum 4x4 block, which is 8 bytes
                    return 8;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.RgbPvrtc4Bpp:
                case SurfaceFormat.RgbaPvrtc4Bpp:
                case SurfaceFormat.RgbaEtc2:
                    // One texel in DXT3, DXT5 and PVRTC 4bpp is a minimum 4x4 block, which is 16 bytes
                    return 16;
                case SurfaceFormat.Alpha8:
                    return 1;
                case SurfaceFormat.Bgr565:
                case SurfaceFormat.Bgra4444:
                case SurfaceFormat.Bgra5551:
                case SurfaceFormat.HalfSingle:
                case SurfaceFormat.NormalizedByte2:
                    return 2;
                case SurfaceFormat.Color:
                case SurfaceFormat.Single:
                case SurfaceFormat.Rg32:
                case SurfaceFormat.HalfVector2:
                case SurfaceFormat.NormalizedByte4:
                case SurfaceFormat.Rgba1010102:
                case SurfaceFormat.Bgra32:
                case SurfaceFormat.Bgr32:
                case SurfaceFormat.U248:
                    return 4;
                case SurfaceFormat.HalfVector4:
                case SurfaceFormat.HalfVector4Oes:
                case SurfaceFormat.Rgba64:
                case SurfaceFormat.Vector2:
                    return 8;
                case SurfaceFormat.Vector4:
                    return 16;
                default:
                    throw new ArgumentException();
            }
        }

        public static int GetSize(this VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Single:
                    return 4;

                case VertexElementFormat.Vector2:
                    return 8;

                case VertexElementFormat.Vector3:
                    return 12;

                case VertexElementFormat.Vector4:
                    return 16;

                case VertexElementFormat.Color:
                    return 4;

                case VertexElementFormat.Byte4:
                    return 4;

                case VertexElementFormat.Short2:
                    return 4;

                case VertexElementFormat.Short4:
                    return 8;

                case VertexElementFormat.NormalizedShort2:
                    return 4;

                case VertexElementFormat.NormalizedShort4:
                    return 8;

                case VertexElementFormat.HalfVector2:
                    return 4;

                case VertexElementFormat.HalfVector4:
                    return 8;
            }
            return 0;
        }

#if OPENGL

        public static int GetBoundTexture2D()
        {
            var prevTexture = 0;
            GL.GetInteger(GetPName.TextureBinding2D, out prevTexture);
            GraphicsExtensions.LogGLError("GraphicsExtensions.GetBoundTexture2D() GL.GetInteger");
            return prevTexture;
        }

        [Conditional("DEBUG")]
        public static void CheckGLError()
        {
#if GLES
            var error = GL.GetErrorCode();
#else
            var error = GL.GetError();
#endif
            if (error != ErrorCode.NoError)
                throw new MonoGameGLException("GL.GetError() returned " + error.ToString());
        }
#endif

#if OPENGL
        [Conditional("DEBUG")]
        public static void LogGLError(string location)
        {
            try
            {
                GraphicsExtensions.CheckGLError();
            }
            catch (MonoGameGLException ex)
            {
#if ANDROID
                // Todo: Add generic MonoGame logging interface
                Android.Util.Log.Debug("MonoGame", "MonoGameGLException at " + location + " - " + ex.Message);
#else
                Debug.WriteLine("MonoGameGLException at " + location + " - " + ex.Message);
#endif
            }
        }
#endif
    }

    internal class MonoGameGLException : Exception
    {
        public MonoGameGLException(string message)
            : base(message)
        {
        }
    }
}
