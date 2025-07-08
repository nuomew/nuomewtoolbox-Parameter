using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using VRC.SDK3.Avatars.Components;

namespace NyameauToolbox.Editor
{
    /// <summary>
    /// 模型大小精确计算器
    /// 从画质压缩工具迁移的精确计算逻辑
    /// </summary>
    public static class ModelSizeCalculator
    {
        // 顶点属性格式对应的字节大小
        private static readonly Dictionary<VertexAttributeFormat, int> VertexAttributeByteSize = new Dictionary<VertexAttributeFormat, int>()
        {
            { VertexAttributeFormat.UNorm8, 1},
            { VertexAttributeFormat.SNorm8, 1},
            { VertexAttributeFormat.UInt8, 1},
            { VertexAttributeFormat.SInt8, 1},

            { VertexAttributeFormat.UNorm16, 2},
            { VertexAttributeFormat.SNorm16, 2},
            { VertexAttributeFormat.UInt16, 2},
            { VertexAttributeFormat.SInt16, 2},
            { VertexAttributeFormat.Float16, 2},

            { VertexAttributeFormat.Float32, 4},
            { VertexAttributeFormat.UInt32, 4},
            { VertexAttributeFormat.SInt32, 4},
        };

        // 网格大小缓存
        private static readonly Dictionary<Mesh, long> meshSizeCache = new Dictionary<Mesh, long>();

        /// <summary>
        /// 计算GameObject的总模型大小（包含纹理和网格）
        /// </summary>
        public static ModelSizeResult CalculateTotalModelSize(GameObject avatar)
        {
            if (avatar == null)
                return new ModelSizeResult();

            var result = new ModelSizeResult();
            
            // 计算纹理大小 - 使用简化版本
            result.textureSizeBytes = CalculateTextureMemoryForModel(avatar);
            result.textureSizeMB = result.textureSizeBytes / (1024f * 1024f);

            // 计算网格大小
            var meshResult = CalculateMeshSize(avatar);
            result.meshSizeBytes = meshResult.totalSizeBytes;
            result.meshSizeMB = meshResult.totalSizeMB;
            result.vertexCount = meshResult.vertexCount;
            result.triangleCount = meshResult.triangleCount;
            result.meshInfos = meshResult.meshInfos;

            // 计算总大小（Combined all）
            result.totalSizeBytes = result.textureSizeBytes + result.meshSizeBytes;
            result.totalSizeMB = result.totalSizeBytes / (1024f * 1024f);

            return result;
        }

        /// <summary>
        /// 计算活动状态下的模型大小（仅包含活动的GameObject）
        /// </summary>
        public static ModelSizeResult CalculateActiveModelSize(GameObject avatar)
        {
            if (avatar == null)
                return new ModelSizeResult();

            var result = new ModelSizeResult();
            
            // 计算纹理大小（活动状态）- 使用简化版本
            result.textureSizeBytes = CalculateActiveTextureMemoryForModel(avatar);
            result.textureSizeMB = result.textureSizeBytes / (1024f * 1024f);

            // 计算网格大小（仅活动状态）
            var meshResult = CalculateActiveMeshSize(avatar);
            result.meshSizeBytes = meshResult.totalSizeBytes;
            result.meshSizeMB = meshResult.totalSizeMB;
            result.vertexCount = meshResult.vertexCount;
            result.triangleCount = meshResult.triangleCount;
            result.meshInfos = meshResult.meshInfos;

            // 计算总大小
            result.totalSizeBytes = result.textureSizeBytes + result.meshSizeBytes;
            result.totalSizeMB = result.totalSizeBytes / (1024f * 1024f);

            return result;
        }

        /// <summary>
        /// 计算活动状态下的网格大小
        /// </summary>
        public static MeshSizeResult CalculateActiveMeshSize(GameObject avatar)
        {
            if (avatar == null)
                return new MeshSizeResult();

            var result = new MeshSizeResult();
            var meshInfos = new List<MeshInfo>();
            
            // 获取所有网格和活动网格（与画质压缩工具逻辑一致）
            Dictionary<Mesh, bool> meshDict = new Dictionary<Mesh, bool>();
            
            // 获取所有网格（包括非活动的）
            var allMeshes = avatar.GetComponentsInChildren<Renderer>(true)
                .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                       r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                .Where(m => m != null);
                
            // 获取活动状态的网格
            var activeMeshes = avatar.GetComponentsInChildren<Renderer>(false)
                .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                       r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                .Where(m => m != null);
                
            // 构建网格字典，标记每个网格是否活动
            foreach (var mesh in allMeshes)
            {
                bool isActive = activeMeshes.Contains(mesh);
                if (meshDict.ContainsKey(mesh))
                {
                    if (!meshDict[mesh] && isActive)
                        meshDict[mesh] = true;
                }
                else
                {
                    meshDict.Add(mesh, isActive);
                }
            }
            
            // 计算活动网格的大小
            foreach (var kvp in meshDict)
            {
                var mesh = kvp.Key;
                bool isActive = kvp.Value;
                
                var meshSize = CalculateSingleMeshSize(mesh);
                
                // 只计算活动网格的大小
                if (isActive)
                {
                    result.totalSizeBytes += meshSize;
                    result.vertexCount += mesh.vertexCount;
                    result.triangleCount += mesh.triangles.Length / 3;
                }
                
                meshInfos.Add(new MeshInfo
                {
                    mesh = mesh,
                    sizeBytes = meshSize,
                    sizeMB = meshSize / (1024f * 1024f),
                    isActive = isActive,
                    vertexCount = mesh.vertexCount,
                    triangleCount = mesh.triangles.Length / 3
                });
            }
            
            // 按大小排序，如果大小相同则随机排列
            var random = new System.Random();
            meshInfos.Sort((m1, m2) => 
            {
                int sizeComparison = m2.sizeBytes.CompareTo(m1.sizeBytes);
                if (sizeComparison == 0)
                {
                    // 大小相同时随机排列
                    return random.Next(-1, 2);
                }
                return sizeComparison;
            });
            
            result.totalSizeMB = result.totalSizeBytes / (1024f * 1024f);
            result.meshInfos = meshInfos;
            
            return result;
        }

        /// <summary>
        /// 计算单个网格的大小
        /// </summary>
        public static MeshSizeResult CalculateMeshSize(GameObject avatar)
        {
            if (avatar == null)
                return new MeshSizeResult();

            var result = new MeshSizeResult();
            var meshInfos = new List<MeshInfo>();
            
            // 获取所有网格（包括活动和非活动的）
            var allRenderers = avatar.GetComponentsInChildren<Renderer>(true);
            var activeRenderers = avatar.GetComponentsInChildren<Renderer>(false);
            
            var meshes = new Dictionary<Mesh, bool>();
            
            // 收集所有网格
            var allMeshes = allRenderers.Select(r => 
                r is SkinnedMeshRenderer skinnedRenderer ? skinnedRenderer.sharedMesh : 
                r is MeshRenderer meshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null
            ).Where(m => m != null);
            
            var activeMeshes = activeRenderers.Select(r => 
                r is SkinnedMeshRenderer skinnedRenderer ? skinnedRenderer.sharedMesh : 
                r is MeshRenderer meshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null
            ).Where(m => m != null);

            // 建立网格字典
            foreach (var mesh in allMeshes)
            {
                bool isActive = activeMeshes.Contains(mesh);
                if (meshes.ContainsKey(mesh))
                {
                    if (meshes[mesh] == false && isActive) 
                        meshes[mesh] = true;
                }
                else
                {
                    meshes.Add(mesh, isActive);
                }
            }

            long totalSizeBytes = 0;
            int totalVertices = 0;
            int totalTriangles = 0;

            // 计算每个网格的大小
            foreach (var kvp in meshes)
            {
                var mesh = kvp.Key;
                var isActive = kvp.Value;
                
                long meshSize = CalculateSingleMeshSize(mesh);
                totalSizeBytes += meshSize;
                totalVertices += mesh.vertexCount;
                totalTriangles += mesh.triangles.Length / 3;

                var meshInfo = new MeshInfo
                {
                    mesh = mesh,
                    sizeBytes = meshSize,
                    sizeMB = meshSize / (1024f * 1024f),
                    isActive = isActive,
                    vertexCount = mesh.vertexCount,
                    triangleCount = mesh.triangles.Length / 3
                };
                
                meshInfos.Add(meshInfo);
            }

            // 按大小排序，如果大小相同则随机排列
            var random = new System.Random();
            meshInfos.Sort((m1, m2) => 
            {
                int sizeComparison = m2.sizeBytes.CompareTo(m1.sizeBytes);
                if (sizeComparison == 0)
                {
                    // 大小相同时随机排列
                    return random.Next(-1, 2);
                }
                return sizeComparison;
            });

            result.totalSizeBytes = totalSizeBytes;
            result.totalSizeMB = totalSizeBytes / (1024f * 1024f);
            result.vertexCount = totalVertices;
            result.triangleCount = totalTriangles;
            result.meshInfos = meshInfos;

            return result;
        }

        /// <summary>
        /// 计算单个网格的精确大小（从画质压缩工具迁移）
        /// </summary>
        public static long CalculateSingleMeshSize(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            if (meshSizeCache.ContainsKey(mesh))
                return meshSizeCache[mesh];
            
            long bytes = 0;

            // 计算顶点属性大小
            var vertexAttributes = mesh.GetVertexAttributes();
            long vertexAttributeVRAMSize = 0;
            
            foreach (var vertexAttribute in vertexAttributes)
            {
                int skinnedMeshPositionNormalTangentMultiplier = 1;
                
                // 蒙皮网格的位置、法线和切线数据会有2倍大小，因为需要存储蒙皮前后的数据
                if (mesh.HasVertexAttribute(VertexAttribute.BlendIndices) && 
                    mesh.HasVertexAttribute(VertexAttribute.BlendWeight) &&
                    (vertexAttribute.attribute == VertexAttribute.Position || 
                     vertexAttribute.attribute == VertexAttribute.Normal || 
                     vertexAttribute.attribute == VertexAttribute.Tangent))
                {
                    skinnedMeshPositionNormalTangentMultiplier = 2;
                }
                
                if (VertexAttributeByteSize.TryGetValue(vertexAttribute.format, out int byteSize))
                {
                    vertexAttributeVRAMSize += byteSize * vertexAttribute.dimension * skinnedMeshPositionNormalTangentMultiplier;
                }
            }

            // 计算BlendShape大小
            var deltaPositions = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            long blendShapeVRAMSize = 0;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var blendShapeFrameCount = mesh.GetBlendShapeFrameCount(i);
                for (int j = 0; j < blendShapeFrameCount; j++)
                {
                    mesh.GetBlendShapeFrameVertices(i, j, deltaPositions, deltaNormals, deltaTangents);
                    for (int k = 0; k < deltaPositions.Length; k++)
                    {
                        if (deltaPositions[k] != Vector3.zero || 
                            deltaNormals[k] != Vector3.zero || 
                            deltaTangents[k] != Vector3.zero)
                        {
                            // 每个受影响的顶点有：1个uint索引 + 3个float位置 + 3个float法线 + 3个float切线
                            // 即使所有法线或切线在所有BlendShape中都为零，这也是正确的
                            blendShapeVRAMSize += 40;
                        }
                    }
                }
            }

            bytes = vertexAttributeVRAMSize * mesh.vertexCount + blendShapeVRAMSize;
            meshSizeCache[mesh] = bytes;
            return bytes;
        }

        /// <summary>
        /// 计算所有纹理内存占用（简化版本）
        /// </summary>
        private static long CalculateTextureMemoryForModel(GameObject avatar)
        {
            if (avatar == null) return 0;
            
            try
            {
                var allTextures = avatar.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null)
                    .SelectMany(m => GetTexturesFromMaterial(m))
                    .Distinct()
                    .Where(t => t != null);

                long totalSize = 0;
                foreach (var texture in allTextures)
                {
                    totalSize += Profiler.GetRuntimeMemorySizeLong(texture);
                }
                
                return totalSize;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModelSizeCalculator] 计算纹理内存时出错: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 计算活动纹理内存占用（简化版本）
        /// </summary>
        private static long CalculateActiveTextureMemoryForModel(GameObject avatar)
        {
            if (avatar == null) return 0;
            
            try
            {
                var activeTextures = avatar.GetComponentsInChildren<Renderer>(false)
                    .SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null)
                    .SelectMany(m => GetTexturesFromMaterial(m))
                    .Distinct()
                    .Where(t => t != null);

                long totalSize = 0;
                foreach (var texture in activeTextures)
                {
                    totalSize += Profiler.GetRuntimeMemorySizeLong(texture);
                }
                
                return totalSize;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModelSizeCalculator] 计算活动纹理内存时出错: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 从材质中获取所有纹理
        /// </summary>
        private static IEnumerable<Texture> GetTexturesFromMaterial(Material material)
        {
            if (material == null || material.shader == null) yield break;
            
            int[] textureIds = material.GetTexturePropertyNameIDs();
            foreach (int id in textureIds)
            {
                if (material.HasProperty(id))
                {
                    Texture texture = material.GetTexture(id);
                    if (texture != null)
                        yield return texture;
                }
            }
        }

        /// <summary>
        /// 清除网格大小缓存
        /// </summary>
        public static void ClearCache()
        {
            meshSizeCache.Clear();
        }
    }

    /// <summary>
    /// 模型大小计算结果
    /// </summary>
    [System.Serializable]
    public class ModelSizeResult
    {
        public long totalSizeBytes;
        public float totalSizeMB;
        public long textureSizeBytes;
        public float textureSizeMB;
        public long meshSizeBytes;
        public float meshSizeMB;
        public int vertexCount;
        public int triangleCount;
        public List<MeshInfo> meshInfos = new List<MeshInfo>();
    }

    /// <summary>
    /// 网格大小计算结果
    /// </summary>
    [System.Serializable]
    public class MeshSizeResult
    {
        public long totalSizeBytes;
        public float totalSizeMB;
        public int vertexCount;
        public int triangleCount;
        public List<MeshInfo> meshInfos = new List<MeshInfo>();
    }

    /// <summary>
    /// 网格信息
    /// </summary>
    [System.Serializable]
    public class MeshInfo
    {
        public Mesh mesh;
        public long sizeBytes;
        public float sizeMB;
        public bool isActive;
        public int vertexCount;
        public int triangleCount;
    }
}