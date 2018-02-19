﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FileFormatWavefront.Model;
using GlmNet;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.Shaders;
using SharpGL.Textures;
using SharpGL.VertexBuffers;

namespace PSSL_Environment
{
    public static class VertexAttributes
    {
        public const uint Position = 0;
        public const uint Normal = 1;
        public const uint TexCoord = 2;
    }
    /// <summary>
    /// A class that represents the scene for this sample.
    /// </summary>
    public class Scene
    {
        /// <summary>
        /// Initialises the Scene.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        public void Initialise(OpenGL gl)
        {
            //  We're going to specify the attribute locations for the position and normal, 
            //  so that we can force both shaders to explicitly have the same locations.
            const uint positionAttribute = 0;
            const uint normalAttribute = 1;
            var attributeLocations = new Dictionary<uint, string>
            {
                {positionAttribute, "Position"},
                {normalAttribute, "Normal"},
            };

            //  Create the per pixel shader.
            shaderPerPixel = new ShaderProgram();
            shaderPerPixel.Create(gl,
                ManifestResourceLoader.LoadTextFile(@"Shaders\PerPixel.vert"),
                ManifestResourceLoader.LoadTextFile(@"Shaders\PerPixel.frag"), attributeLocations);

            //  Create the toon shader.
            shaderToon = new ShaderProgram();
            shaderToon.Create(gl,
                ManifestResourceLoader.LoadTextFile(@"Shaders\Toon.vert"),
                ManifestResourceLoader.LoadTextFile(@"Shaders\Toon.frag"), attributeLocations);

            gl.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);

            //  Generate the geometry and it's buffers.
            trefoilKnot.GenerateGeometry(gl, positionAttribute, normalAttribute);
        }

        /// <summary>
        /// Creates the projection matrix for the given screen size.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screen.</param>
        public void CreateProjectionMatrix(OpenGL gl, float screenWidth, float screenHeight)
        {
            //  Create the projection matrix for our screen size.
            const float S = 0.46f;
            float H = S * screenHeight / screenWidth;
            projectionMatrix = glm.frustum(-S, S, -H, H, 1, 100);

            //  When we do immediate mode drawing, OpenGL needs to know what our projection matrix
            //  is, so set it now.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.MultMatrix(projectionMatrix.to_array());
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        /// <summary>
        /// Creates the modelview and normal matrix. Also rotates the sceen by a specified amount.
        /// </summary>
        /// <param name="rotationAngle">The rotation angle, in radians.</param>
        public void CreateModelviewAndNormalMatrix(float rotationAngle)
        {
            //  Create the modelview and normal matrix. We'll also rotate the scene
            //  by the provided rotation angle, which means things that draw it 
            //  can make the scene rotate easily.
            mat4 rotation = glm.rotate(mat4.identity(), rotationAngle, new vec3(0, 1, 0));
            mat4 translation = glm.translate(mat4.identity(), new vec3(0, 0, -4));
            modelviewMatrix = rotation * translation;
            normalMatrix = modelviewMatrix.to_mat3();
        }

        /// <summary>
        /// Renders the scene in immediate mode.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        public void RenderImmediateMode(OpenGL gl)
        {
            //  Setup the modelview matrix.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();
            gl.MultMatrix(modelviewMatrix.to_array());

            //  Push the polygon attributes and set line mode.
            gl.PushAttrib(OpenGL.GL_POLYGON_BIT);
            gl.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Lines);

            //  Render the trefoil.
            var vertices = trefoilKnot.Vertices;
            gl.Begin(BeginMode.Triangles);
            foreach (var index in trefoilKnot.Indices)
                gl.Vertex(vertices[index].x, vertices[index].y, vertices[index].z);
            gl.End();

            //  Pop the attributes, restoring all polygon state.
            gl.PopAttrib();
        }

        /// <summary>
        /// Renders the scene in retained mode.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        /// <param name="useToonShader">if set to <c>true</c> use the toon shader, otherwise use a per-pixel shader.</param>
        public void RenderRetainedMode(OpenGL gl, bool useToonShader)
        {

            //  Get a reference to the appropriate shader.
            var shader = useToonShader ? shaderToon : shaderPerPixel;

            //  Use the shader program.
            shader.Bind(gl);

            //  Set the variables for the shader program.
            shader.SetUniform3(gl, "DiffuseMaterial", 0f, 0.75f, 0.75f);
            shader.SetUniform3(gl, "AmbientMaterial", 0.04f, 0.04f, 0.04f);
            shader.SetUniform3(gl, "SpecularMaterial", 0.5f, 0.5f, 0.5f);
            shader.SetUniform1(gl, "Shininess", 50f);

            //  Set the light position.
            shader.SetUniform3(gl, "LightPosition", 0.25f, 0.25f, 1f);

            //  Set the matrices.
            shader.SetUniformMatrix4(gl, "Projection", projectionMatrix.to_array());
            shader.SetUniformMatrix4(gl, "Modelview", modelviewMatrix.to_array());
            shader.SetUniformMatrix3(gl, "NormalMatrix", normalMatrix.to_array());

            //  Bind the vertex buffer array.
            trefoilKnot.VertexBufferArray.Bind(gl);

            //  Draw the elements.
            gl.DrawElements(OpenGL.GL_TRIANGLES, trefoilKnot.Indices.Length, OpenGL.GL_UNSIGNED_SHORT, IntPtr.Zero);

            //  Unbind the shader.
            shader.Unbind(gl);
        }

        private void CreateVertexBufferArray(OpenGL gl, Mesh mesh)
        {
            //  Create and bind a vertex buffer array.
            var vertexBufferArray = new VertexBufferArray();
            vertexBufferArray.Create(gl);
            vertexBufferArray.Bind(gl);

            //  Create a vertex buffer for the vertices.
            var verticesVertexBuffer = new VertexBuffer();
            verticesVertexBuffer.Create(gl);
            verticesVertexBuffer.Bind(gl);
            verticesVertexBuffer.SetData(gl, VertexAttributes.Position,
                                 mesh.vertices.SelectMany(v => v.to_array()).ToArray(),
                                 false, 3);
            if (mesh.normals != null)
            {
                var normalsVertexBuffer = new VertexBuffer();
                normalsVertexBuffer.Create(gl);
                normalsVertexBuffer.Bind(gl);
                normalsVertexBuffer.SetData(gl, VertexAttributes.Normal,
                                            mesh.normals.SelectMany(v => v.to_array()).ToArray(),
                                            false, 3);
            }

            if (mesh.uvs != null)
            {
                var texCoordsVertexBuffer = new VertexBuffer();
                texCoordsVertexBuffer.Create(gl);
                texCoordsVertexBuffer.Bind(gl);
                texCoordsVertexBuffer.SetData(gl, VertexAttributes.TexCoord,
                                              mesh.uvs.SelectMany(v => v.to_array()).ToArray(),
                                              false, 2);
            }
            //  We're done creating the vertex buffer array - unbind it and add it to the dictionary.
            verticesVertexBuffer.Unbind(gl);
            meshVertexBufferArrays[mesh] = vertexBufferArray;
        }

        public void Load(OpenGL gl, string objectFilePath)
        {
            //  TODO: cleanup old files.

            //  Destroy all of the vertex buffer arrays in the meshes.
            foreach (var vertexBufferArray in meshVertexBufferArrays.Values)
                vertexBufferArray.Delete(gl);
            meshes.Clear();
            meshVertexBufferArrays.Clear();

            //  Load the object file.
            var result = FileFormatWavefront.FileFormatObj.Load(objectFilePath, true);

            meshes.AddRange(SceneDenormaliser.Denormalize(result.Model));

            //  Create a vertex buffer array for each mesh.
            meshes.ForEach(m => CreateVertexBufferArray(gl, m));

            //  Create textures for each texture map.
            CreateTextures(gl, meshes);

            //  TODO: handle errors and warnings.

            //  TODO: cleanup

        }

        private void CreateTextures(OpenGL gl, IEnumerable<Mesh> meshes)
        {
            foreach (var mesh in meshes.Where(m => m.material != null && m.material.TextureMapDiffuse != null))
            {
                //  Create a new texture and bind it.
                var texture = new Texture2D();
                texture.Create(gl);
                texture.Bind(gl);
                texture.SetParameter(gl, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
                texture.SetParameter(gl, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
                texture.SetParameter(gl, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
                texture.SetParameter(gl, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
                texture.SetImage(gl, (Bitmap)mesh.material.TextureMapDiffuse.Image);
                texture.Unbind(gl);
                meshTextures[mesh] = texture;
            }
        }

        /// <summary>
        /// Gets or sets the scale factor.
        /// </summary>
        public float ScaleFactor
        {
            get { return scaleFactor; }
            set { scaleFactor = value; }
        }

        /// <summary>
        /// Gets the projection matrix.
        /// </summary>
        public mat4 ProjectionMatrix
        {
            get { return projectionMatrix; }
        }


        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Dictionary<Mesh, VertexBufferArray> meshVertexBufferArrays = new Dictionary<Mesh, VertexBufferArray>();
        private readonly Dictionary<Mesh, Texture2D> meshTextures = new Dictionary<Mesh, Texture2D>();

        //  The shaders we use.
        private ShaderProgram shaderPerPixel;
        private ShaderProgram shaderToon;

        //  The modelview, projection and normal matrices.
        private mat4 modelviewMatrix = mat4.identity();
        private mat4 projectionMatrix = mat4.identity();
        private mat3 normalMatrix = mat3.identity();

        private float scaleFactor = 1.0f;

        //  Scene geometry - a trefoil knot.
        private readonly TrefoilKnot trefoilKnot = new TrefoilKnot();
    }

}