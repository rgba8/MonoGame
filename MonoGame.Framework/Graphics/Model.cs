using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Model
	{
		private static Matrix[] sharedDrawBoneMatrices;
		
		private GraphicsDevice graphicsDevice;
        private GraphicsDevice GraphicsDevice { get { return this.graphicsDevice; } }

		// Summary:
		//     Gets a collection of ModelBone objects which describe how each mesh in the
		//     Meshes collection for this model relates to its parent mesh.
		public ModelBoneCollection Bones { get; private set; }
		//
		// Summary:
		//     Gets a collection of ModelMesh objects which compose the model. Each ModelMesh
		//     in a model may be moved independently and may be composed of multiple materials
		//     identified as ModelMeshPart objects.
		public ModelMeshCollection Meshes { get; private set; }
		//
		// Summary:
		//     Gets the root bone for this model.
		public ModelBone Root { get; set; }
		//
		// Summary:
		//     Gets or sets an object identifying this model.
		public object Tag { get; set; }

		public Model()
		{

		}

		public Model(GraphicsDevice graphicsDevice, List<ModelBone> bones, List<ModelMesh> meshes)
		{
			// TODO: Complete member initialization
			this.graphicsDevice = graphicsDevice;

			Bones = new ModelBoneCollection(bones);
			Meshes = new ModelMeshCollection(meshes);
		}


        struct IV
        {
            public VertexBuffer v;
            public IndexBuffer i;
            public List<ModelMeshPart> parts;
        };
        public void RebuildBuffers()
        {
            List<IV> ivs = new List<IV>();
            {
                var meshCount = this.Meshes.Count;
                for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
                {
                    var mesh = this.Meshes[meshIndex];
                    var partCount = mesh.MeshParts.Count;
                    for (var partIndex = 0; partIndex < partCount; partIndex++)
                    {
                        var part = mesh.MeshParts[partIndex];
                        var ivCount = ivs.Count;
                        var ivFound = -1;
                        for (var ivIndex = 0; ivIndex < ivCount; ivIndex++)
                        {
                            if (ivs[ivIndex].v == part.VertexBuffer && ivs[ivIndex].i == part.IndexBuffer)
                            {
                                ivFound = ivIndex;
                                break;
                            }
                        }
                        IV iv;
                        if (ivFound < 0)
                        {
                            ivFound = ivCount;
                            iv.parts = new List<ModelMeshPart>();
                            iv.v = part.VertexBuffer;
                            iv.i = part.IndexBuffer;
                            ivs.Add(iv);
                        }
                        else
                        {
                            iv = ivs[ivFound];
                        }

                        iv.parts.Add(part);
                        ivs[ivFound] = iv;
                    }
                }
            }

            {
                var ivCount = ivs.Count;
                for (var ivIndex = 0; ivIndex < ivCount; ivIndex++)
                {
                    var v = ivs[ivIndex].v;
                    var i = ivs[ivIndex].i;
                    var parts = ivs[ivIndex].parts;
                    var partCount = parts.Count;

                    var data = i._data;
                    var dataCount = data.Length;
                    var newData = data;

                    var newI = new IndexBuffer(i.GraphicsDevice, IndexElementSize.ThirtyTwoBits, i.IndexCount, i.BufferUsage);
                    if (i.IndexElementSize == IndexElementSize.SixteenBits)
                    {
                        newData = new byte[data.Length * 2];
                    }


                    for (var partIndex = 0; partIndex < partCount; partIndex++)
                    {
                        var part = parts[partIndex];
                        var startIndex = part.StartIndex;
                        var vertexOffset = part.VertexOffset;
                        part.VertexOffset = 0;
                        part.IndexBuffer = newI;
                        var indexCount = part.PrimitiveCount * 3;
                        if (i.IndexElementSize == IndexElementSize.SixteenBits)
                        {
                            for (var index = 0; index < indexCount; index++)
                            {
                                var offset = (startIndex + index) * 2;
                                var b0 = data[(startIndex + index) * 2];
                                var b1 = data[(startIndex + index) * 2 + 1];

                                int newIndex = (b1 << 8 | b0) + vertexOffset;
                                var newOffset = (startIndex + index) * 4;
                                newData[newOffset] = (byte)(newIndex & 0xFF);
                                newData[newOffset + 1] = (byte)((newIndex >> 8) & 0xFF);
                                newData[newOffset + 2] = (byte)((newIndex >> 16) & 0xFF);
                                newData[newOffset + 3] = (byte)((newIndex >> 24) & 0xFF);
                            }
                        }
                        else if (i.IndexElementSize == IndexElementSize.ThirtyTwoBits)
                        {
                            for (var index = 0; index < indexCount; index++)
                            {
                                var newOffset = (startIndex + index) * 4;
                                var b0 = data[newOffset];
                                var b1 = data[newOffset + 1];
                                var b2 = data[newOffset + 2];
                                var b3 = data[newOffset + 3];

                                int newIndex = (b3 << 24 | b2 << 16 | b1 << 8 | b0) + vertexOffset;

                                newData[newOffset] = (byte)(vertexOffset & 0xFF);
                                newData[newOffset + 1] = (byte)((vertexOffset >> 8) & 0xFF);
                                newData[newOffset + 2] = (byte)((vertexOffset >> 16) & 0xFF);
                                newData[newOffset + 3] = (byte)((vertexOffset >> 24) & 0xFF);

                            }
                        }
                    }

                    newI.SetData(newData);
                    newI._data = null;
                    i._data = null;
                    i.Dispose();
                    newData = null;
                    data = null;
                }
            }
        }


        public void BuildHierarchy()
		{
			var globalScale = Matrix.CreateScale(0.01f);
			
			foreach(var node in this.Root.Children)
			{
				BuildHierarchy(node, this.Root.Transform * globalScale, 0);
			}
		}
		
		private void BuildHierarchy(ModelBone node, Matrix parentTransform, int level)
		{
			node.ModelTransform = node.Transform * parentTransform;
			
			foreach (var child in node.Children) 
			{
				BuildHierarchy(child, node.ModelTransform, level + 1);
			}
			
			//string s = string.Empty;
			//
			//for (int i = 0; i < level; i++) 
			//{
			//	s += "\t";
			//}
			//
			//Debug.WriteLine("{0}:{1}", s, node.Name);
		}
		
		public void Draw(Matrix world, Matrix view, Matrix projection) 
		{       
            int boneCount = this.Bones.Count;
			
			if (sharedDrawBoneMatrices == null ||
				sharedDrawBoneMatrices.Length < boneCount)
			{
				sharedDrawBoneMatrices = new Matrix[boneCount];    
			}
			
			// Look up combined bone matrices for the entire model.            
			CopyAbsoluteBoneTransformsTo(sharedDrawBoneMatrices);

            // Draw the model.
            foreach (ModelMesh mesh in Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
					IEffectMatrices effectMatricies = effect as IEffectMatrices;
					if (effectMatricies == null) {
						throw new InvalidOperationException();
					}
                    effectMatricies.World = sharedDrawBoneMatrices[mesh.ParentBone.Index] * world;
                    effectMatricies.View = view;
                    effectMatricies.Projection = projection;
                }

                mesh.Draw();
            }
		}
		
		public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
		{
			if (destinationBoneTransforms == null)
				throw new ArgumentNullException("destinationBoneTransforms");
            if (destinationBoneTransforms.Length < this.Bones.Count)
				throw new ArgumentOutOfRangeException("destinationBoneTransforms");
            int count = this.Bones.Count;
			for (int index1 = 0; index1 < count; ++index1)
			{
                ModelBone modelBone = (this.Bones)[index1];
				if (modelBone.Parent == null)
				{
					destinationBoneTransforms[index1] = modelBone.transform;
				}
				else
				{
					int index2 = modelBone.Parent.Index;
					Matrix.Multiply(ref modelBone.transform, ref destinationBoneTransforms[index2], out destinationBoneTransforms[index1]);
				}
			}
		}
	}

	//// Summary:
	////     Represents a 3D model composed of multiple ModelMesh objects which may be
	////     moved independently.
	//public sealed class Model
	//{
	//    // Summary:
	//    //     Gets a collection of ModelBone objects which describe how each mesh in the
	//    //     Meshes collection for this model relates to its parent mesh.
	//    public ModelBoneCollection Bones { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets a collection of ModelMesh objects which compose the model. Each ModelMesh
	//    //     in a model may be moved independently and may be composed of multiple materials
	//    //     identified as ModelMeshPart objects.
	//    public ModelMeshCollection Meshes { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets the root bone for this model.
	//    public ModelBone Root { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets or sets an object identifying this model.
	//    public object Tag { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

	//    // Summary:
	//    //     Copies a transform of each bone in a model relative to all parent bones of
	//    //     the bone into a given array.
	//    //
	//    // Parameters:
	//    //   destinationBoneTransforms:
	//    //     The array to receive bone transforms.
	//    public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms) { throw new NotImplementedException(); }
	//    //
	//    // Summary:
	//    //     Copies an array of transforms into each bone in the model.
	//    //
	//    // Parameters:
	//    //   sourceBoneTransforms:
	//    //     An array containing new bone transforms.
	//    public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms) { throw new NotImplementedException(); }
	//    //
	//    // Summary:
	//    //     Copies each bone transform relative only to the parent bone of the model
	//    //     to a given array.
	//    //
	//    // Parameters:
	//    //   destinationBoneTransforms:
	//    //     The array to receive bone transforms.
	//    public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms) { throw new NotImplementedException(); }
	//    //
	//    // Summary:
	//    //     Render a model after applying the matrix transformations.
	//    //
	//    // Parameters:
	//    //   world:
	//    //     A world transformation matrix.
	//    //
	//    //   view:
	//    //     A view transformation matrix.
	//    //
	//    //   projection:
	//    //     A projection transformation matrix.
	//    public void Draw(Matrix world, Matrix view, Matrix projection) { throw new NotImplementedException(); }
	//}
}
