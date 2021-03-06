﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using ImageFramework.DirectX;

namespace ImageFramework.Model.Shader
{
    public class ShaderBuilder2D : IShaderBuilder
    {
        public string Type { get; }

        public ShaderBuilder2D(string type = "float4")
        {
            this.Type = type;
        }

        public string SrvType => $"Texture2DArray<{Type}>";
        public string SrvSingleType => $"Texture2D<{Type}>";

        public string UavType => $"RWTexture2DArray<{Type}>";

        public int LocalSizeX => Device.Get().IsLowEndDevice ? 16 : 32;
        public int LocalSizeY => Device.Get().IsLowEndDevice ? 16 : 32;
        public int LocalSizeZ => 1;

        public bool Is3D => false;

        public string Is3DString => "false";
        public int Is3DInt => 0;

        public string TexelHelperFunctions => @"
int2 texel(int3 coord) {{ return coord.xy; }}
int3 texel(int3 coord, int layer) {{ return uint3(coord.xy, layer); }}
float2 texel(float3 coord) {{ return coord.xy; }}
";

        public string Double => Device.Get().SupportsDouble ? "double" : "float";
        public string IntVec => "int2";
    }
}
