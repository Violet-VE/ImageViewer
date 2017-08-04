﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTKImageViewer.glhelper;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using MatrixMode = OpenTK.Graphics.OpenGL.MatrixMode;
using MessageBox = System.Windows.MessageBox;

namespace OpenTKImageViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GLControl glControl;

        private int _program;
        private int _vertexArray;
        private double _time;

        private string error = "";
        private int iteration = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WinFormsHost_OnInitialized(object sender, EventArgs e)
        {
            try
            {
                var flags = GraphicsContextFlags.Default;
                glControl = new GLControl(new GraphicsMode(32, 24), 4, 2, flags);
                glControl.Paint += GLControl_Paint;
                glControl.Dock = DockStyle.Fill;
                var windowsFormsHost = sender as WindowsFormsHost;
                if (windowsFormsHost != null) windowsFormsHost.Child = glControl;

                glControl.MakeCurrent();
                InitGraphics();
            }
            catch (Exception exception)
            {
                error = exception.Message + ": " + exception.StackTrace;
            }
        }

        private void InitGraphics()
        { 
            _program = CreateProgram();
            GL.GenVertexArrays(1, out _vertexArray);
            GL.BindVertexArray(_vertexArray);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
        }

        private int CreateProgram()
        {
            try
            {
                var program = GL.CreateProgram();
                var shaders = new List<int>();
                shaders.Add(CompileShader(ShaderType.VertexShader,
                    "#version 450 core\n" +
                    "layout (location = 1) in vec4 position;\n" +
                    "void main(void){\n" +
                    "vec4 vertex = vec4(0.0, 0.0, 0.0, 1.0);" +
                    "if(gl_VertexID == 0u) vertex = vec4(1.0, -1.0, 0.0, 1.0);\n" +
                    "if(gl_VertexID == 1u) vertex = vec4(-1.0, -1.0, 0.0, 1.0);\n" +
                    "if(gl_VertexID == 2u) vertex = vec4(1.0, 1.0, 0.0, 1.0);\n" +
                    "if(gl_VertexID == 3u) vertex = vec4(-1.0, 1.0, 0.0, 1.0);\n" +
                    "gl_Position = vertex;\n" +
                    "}\n"
                    ));
                shaders.Add(CompileShader(ShaderType.FragmentShader,
                    "#version 450 core\n" +
                    "out vec4 color;\n" +
                    "void main(void){\n" +
                    "color = vec4(1.0);\n" +
                    "}\n"
                    ));

                foreach (var shader in shaders)
                    GL.AttachShader(program, shader);
                GL.LinkProgram(program);
                var info = GL.GetProgramInfoLog(program);
                if (!string.IsNullOrWhiteSpace(info))
                    throw new Exception($"CompileShaders ProgramLinking had errors: {info}");

                foreach (var shader in shaders)
                {
                    GL.DetachShader(program, shader);
                    GL.DeleteShader(shader);
                }
                return program;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private int CompileShader(ShaderType type, string src)
        {
            var shader = GL.CreateShader(type);
            GL.ShaderSource(shader, src);
            GL.CompileShader(shader);
            var info = GL.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(info))
                throw new Exception($"CompileShader {type} had errors: {info}");
            return shader;
        }

        private float r = 0.0f;
        private void GLControl_Paint(object sender, PaintEventArgs e)
        {
            if (error.Length > 0 && iteration++ > 0)
            {
                MessageBox.Show(error);
                error = "";
            }

            try
            {
                GL.Viewport(0, 0, (int)WinFormsHost.ActualWidth, (int)WinFormsHost.ActualHeight);
                _time += 0.1f;
                Color4 backColor;
                backColor.A = 1.0f;
                backColor.R = 0.1f;
                backColor.G = 0.1f;
                backColor.B = 0.3f;
                GL.ClearColor(backColor);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.UseProgram(_program);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                
                glControl.SwapBuffers();
            }
            catch (Exception exception)
            {
                if (error.Length == 0)
                    error = exception.Message + ": " + exception.StackTrace;
            }

            glControl.Invalidate();
        }

        private void WinFormsHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}