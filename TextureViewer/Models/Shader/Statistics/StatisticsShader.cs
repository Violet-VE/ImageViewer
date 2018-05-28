﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.glhelper;

namespace TextureViewer.Models.Shader.Statistics
{
    public abstract class StatisticsShader
    {
        private readonly Program program;

        public static readonly int LocalSize = 32;

        protected StatisticsShader()
        {
            var shader = new glhelper.Shader(ShaderType.ComputeShader, GetComputeSource()).Compile();
            program = new Program(new List<glhelper.Shader>{shader}, true);
        }

        public void Dispose()
        {
            program.Dispose();
        }

        /// <summary>
        /// executes the shader for the outer image layer
        /// </summary>
        /// <returns></returns>
        public Vector4 Run(TextureArray2D source, Models models)
        {
            var texDst = models.GlData.TextureCache.GetTexture();

            models.GlData.BindSampler(0, false, false);
            source.BindAsTexture2D(0, 0, 0);
            texDst.BindAsImage(1, 0, 0, TextureAccess.WriteOnly);

            // bind and set uniforms
            program.Bind();
            // direction
            GL.Uniform2(0, 1, 0);
            // stride
            var curStride = 2;
            GL.Uniform1(1, curStride);

            var curWidth = models.Images.Width;
            GL.DispatchCompute(curWidth / (LocalSize * 2), models.Images.Height, 1);

            // swap textures
            var texSrc = models.GlData.TextureCache.GetTexture();
            GL.MemoryBarrier(MemoryBarrierFlags.TextureFetchBarrierBit);

            // do invocation until finished
            while (curWidth > 2)
            {
                curWidth /= 2;
                curStride *= 2;

                // swap textures
                Swap(ref texSrc, ref texDst);
                texSrc.BindAsTexture2D(0, 0, 0);
                texDst.BindAsImage(1, 0, 0, TextureAccess.WriteOnly);

                // stride
                GL.Uniform1(1, curStride);

                // dispatch
                GL.DispatchCompute(curWidth / (LocalSize * 2), models.Images.Height, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.TextureFetchBarrierBit);
            }

            // do the scan in y direction
            var curHeight = models.Images.Height;
            curStride = 2;

            // set direction
            GL.Uniform2(0, 0, 1);

            while (curHeight >= 2)
            {
                // swap textures
                Swap(ref texSrc, ref texDst);
                texSrc.BindAsTexture2D(0, 0, 0);
                texDst.BindAsImage(1, 0, 0, TextureAccess.WriteOnly);

                // stride
                GL.Uniform1(1, curStride);

                GL.DispatchCompute(1, curHeight / (LocalSize * 2), 1);
                GL.MemoryBarrier(MemoryBarrierFlags.TextureFetchBarrierBit);

                curHeight /= 2;
                curStride *= 2;
            }

            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            // the result is in pixel 0 0

            texDst.BindAsTexture2D(models.GlData.GetPixelShader.GetTextureLocation(), 0, 0);
            var res = models.GlData.GetPixelShader.GetPixelColor(0, 0, 0);

            // cleanup
            models.GlData.TextureCache.StoreTexture(texSrc);
            models.GlData.TextureCache.StoreTexture(texDst);

            return res.ToVector();
        }

        private void Swap(ref TextureArray2D t1, ref TextureArray2D t2)
        {
            var tmp = t1;
            t1 = t2;
            t2 = tmp;
        }

        private string GetComputeSource()
        {
            return OpenGlContext.ShaderVersion + "\n" +
                   $"layout(local_size_x = {LocalSize}) in;\n" +
                   "layout(binding = 0) uniform sampler2D src_image;\n" +
                   "layout(binding = 1) uniform writeonly image2D dst_image;\n" +
                   "layout(location = 0) uniform ivec2 direction;\n" +
                   "layout(location = 1) uniform int stride;\n" +
                   "vec4 combine(vec4 a, vec4 b) {"
                   + GetCombineFunction() + "}\n" +
                   @"void main() {
                        ivec2 pixel = ivec2(gl_GlobalInvocationID);
                        ivec2 y = dot(pixel, ivec2(1) - direction) * (ivec2(1) - direction);
                        ivec2 x = dot(pixel, direction) * stride * direction;
                        ivec2 x2 = x + direction * (stride - 1);
                        
                        ivec2 pos1 = x + y;
                        ivec2 pos2 = x2 + y;

                        ivec2 size = textureSize(src_image, 0);
                        if(pos1.x >= size.x || pos1.y >= size.y) return;
                        if(pos2.x >= size.x || pos2.y >= size.y)
                        {
                            /* only write the value as is */
                            imageStore(dst_image, pos1, texelFetch(src_image, pos1, 0));
                            return;
                        }
                        vec4 color = combine( texelFetch(src_image, pos1, 0), texelFetch(src_image, pos2, 0) );
                        imageStore(dst_image, pos1, color);
                    }";
        }

        protected abstract string GetCombineFunction();
    }
}
