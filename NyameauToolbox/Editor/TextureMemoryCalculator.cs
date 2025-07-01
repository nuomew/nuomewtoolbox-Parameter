// 纹理显存计算器 - 从Thry's Avatar Tools迁移的纹理内存计算功能
// 用于计算VRChat模型的纹理显存占用
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace NyameauToolbox.Editor
{
    /// <summary>
    /// 纹理内存计算器 - 计算VRChat模型的纹理显存占用
    /// </summary>
    public static class TextureMemoryCalculator
    {
        // 纹理格式对应的每像素位数(BPP)字典
        private static readonly Dictionary<TextureFormat, float> BPP = new Dictionary<TextureFormat, float>()
        {
            { TextureFormat.Alpha8, 8 },
            { TextureFormat.ARGB4444, 16 },
            { TextureFormat.RGB24, 24 },
            { TextureFormat.RGBA32, 32 },
            { TextureFormat.ARGB32, 32 },
            { TextureFormat.RGB565, 16 },
            { TextureFormat.R16, 16 },
            { TextureFormat.DXT1, 4 },
            { TextureFormat.DXT5, 8 },
            { TextureFormat.RGBA4444, 16 },
            { TextureFormat.BGRA32, 32 },
            { TextureFormat.RHalf, 16 },
            { TextureFormat.RGHalf, 32 },
            { TextureFormat.RGBAHalf, 64 },
            { TextureFormat.RFloat, 32 },
            { TextureFormat.RGFloat, 64 },
            { TextureFormat.RGBAFloat, 128 },
            { TextureFormat.YUY2, 16 },
            { TextureFormat.RGB9e5Float, 32 },
            { TextureFormat.BC6H, 8 },
            { TextureFormat.BC7, 8 },
            { TextureFormat.BC4, 4 },
            { TextureFormat.BC5, 8 },
            { TextureFormat.DXT1Crunched, 4 },
            { TextureFormat.DXT5Crunched, 8 },
            { TextureFormat.PVRTC_RGB2, 2 },
            { TextureFormat.PVRTC_RGBA2, 2 },
            { TextureFormat.PVRTC_RGB4, 4 },
            { TextureFormat.PVRTC_RGBA4, 4 },
            { TextureFormat.ETC_RGB4, 4 },
            { TextureFormat.EAC_R, 4 },
            { TextureFormat.EAC_R_SIGNED, 4 },
            { TextureFormat.EAC_RG, 8 },
            { TextureFormat.EAC_RG_SIGNED, 8 },
            { TextureFormat.ETC2_RGB, 4 },
            { TextureFormat.ETC2_RGBA1, 4 },
            { TextureFormat.ETC2_RGBA8, 8 },
            { TextureFormat.ASTC_4x4, 8 },
            { TextureFormat.ASTC_5x5, 5.12f },
            { TextureFormat.ASTC_6x6, 3.55f },
            { TextureFormat.ASTC_8x8, 2 },
            { TextureFormat.ASTC_10x10, 1.28f },
            { TextureFormat.ASTC_12x12, 1 },
            { TextureFormat.RG16, 16 },
            { TextureFormat.R8, 8 },
            { TextureFormat.ETC_RGB4Crunched, 4 },
            { TextureFormat.ETC2_RGBA8Crunched, 8 },
            { TextureFormat.ASTC_HDR_4x4, 8 },
            { TextureFormat.ASTC_HDR_5x5, 5.12f },
            { TextureFormat.ASTC_HDR_6x6, 3.55f },
            { TextureFormat.ASTC_HDR_8x8, 2 },
            { TextureFormat.ASTC_HDR_10x10, 1.28f },
            { TextureFormat.ASTC_HDR_12x12, 1 },
            { TextureFormat.RG32, 32 },
            { TextureFormat.RGB48, 48 },
            { TextureFormat.RGBA64, 64 }
        };

        // RenderTexture格式对应的每像素位数字典
        private static readonly Dictionary<RenderTextureFormat, float> RT_BPP = new Dictionary<RenderTextureFormat, float>()
        {
            { RenderTextureFormat.ARGB32, 32 },
            { RenderTextureFormat.Depth, 0 },
            { RenderTextureFormat.ARGBHalf, 64 },
            { RenderTextureFormat.Shadowmap, 8 },
            { RenderTextureFormat.RGB565, 16 },
            { RenderTextureFormat.ARGB4444, 16 },
            { RenderTextureFormat.ARGB1555, 16 },
            { RenderTextureFormat.Default, 32 },
            { RenderTextureFormat.ARGB2101010, 32 },
            { RenderTextureFormat.DefaultHDR, 128 },
            { RenderTextureFormat.ARGB64, 64 },
            { RenderTextureFormat.ARGBFloat, 128 },
            { RenderTextureFormat.RGFloat, 64 },
            { RenderTextureFormat.RGHalf, 32 },
            { RenderTextureFormat.RFloat, 32 },
            { RenderTextureFormat.RHalf, 16 },
            { RenderTextureFormat.R8, 8 },
            { RenderTextureFormat.ARGBInt, 128 },
            { RenderTextureFormat.RGInt, 64 },
            { RenderTextureFormat.RInt, 32 },
            { RenderTextureFormat.BGRA32, 32 },
            { RenderTextureFormat.RGB111110Float, 32 },
            { RenderTextureFormat.RG32, 32 },
            { RenderTextureFormat.RGBAUShort, 64 },
            { RenderTextureFormat.RG16, 16 },
            { RenderTextureFormat.BGRA10101010_XR, 40 },
            { RenderTextureFormat.BGR101010_XR, 30 },
            { RenderTextureFormat.R16, 16 }
        };

        /// <summary>
        /// 纹理信息结构体
        /// </summary>
        public struct TextureInfo
        {
            public Texture texture;          // 纹理对象
            public string name;              // 纹理名称
            public long sizeBytes;           // 大小(字节)
            public float sizeMB;             // 大小(MB)
            public bool isActive;            // 是否激活
            public float BPP;                // 每像素位数
            public string formatString;     // 格式字符串
            public TextureFormat format;    // 纹理格式
            public bool hasAlpha;            // 是否有Alpha通道
            public int width;                // 宽度
            public int height;               // 高度
            public List<Material> materials; // 使用该纹理的材质列表
        }

        /// <summary>
        /// 纹理内存计算结果
        /// </summary>
        public struct TextureMemoryResult
        {
            public List<TextureInfo> textures;    // 纹理列表
            public long totalMemoryBytes;         // 总内存(字节)
            public float totalMemoryMB;           // 总内存(MB)
            public long activeMemoryBytes;        // 激活内存(字节)
            public float activeMemoryMB;          // 激活内存(MB)
            public int textureCount;              // 纹理数量
            public int activeTextureCount;        // 激活纹理数量
        }

        /// <summary>
        /// 计算GameObject的纹理内存占用
        /// </summary>
        /// <param name="avatar">要分析的GameObject</param>
        /// <returns>纹理内存计算结果</returns>
        public static TextureMemoryResult CalculateTextureMemory(GameObject avatar)
        {
            if (avatar == null)
            {
                return new TextureMemoryResult();
            }

            try
            {
                // 获取所有纹理
                Dictionary<Texture, bool> textureDict = GetTextures(avatar);
                
                // 获取所有材质用于查找纹理使用情况
                List<Material> allMaterials = avatar.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials)
                    .Where(mat => mat != null)
                    .Distinct()
                    .ToList();

                List<TextureInfo> textureInfos = new List<TextureInfo>();
                long totalBytes = 0;
                long activeBytes = 0;
                int activeCount = 0;

                foreach (var kvp in textureDict)
                {
                    Texture texture = kvp.Key;
                    bool isActive = kvp.Value;

                    TextureInfo info = CalculateTextureSize(texture);
                    info.isActive = isActive;
                    info.materials = GetMaterialsUsingTexture(texture, allMaterials);

                    textureInfos.Add(info);
                    totalBytes += info.sizeBytes;
                    
                    if (isActive)
                    {
                        activeBytes += info.sizeBytes;
                        activeCount++;
                    }
                }

                // 按大小排序
                textureInfos.Sort((t1, t2) => t2.sizeBytes.CompareTo(t1.sizeBytes));

                return new TextureMemoryResult
                {
                    textures = textureInfos,
                    totalMemoryBytes = totalBytes,
                    totalMemoryMB = totalBytes / (1024f * 1024f),
                    activeMemoryBytes = activeBytes,
                    activeMemoryMB = activeBytes / (1024f * 1024f),
                    textureCount = textureInfos.Count,
                    activeTextureCount = activeCount
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"计算纹理内存时出错: {e.Message}");
                return new TextureMemoryResult();
            }
        }

        /// <summary>
        /// 快速计算纹理内存占用(仅返回总大小)
        /// </summary>
        /// <param name="avatar">要分析的GameObject</param>
        /// <returns>总内存大小(字节)</returns>
        public static long QuickCalculateTextureMemory(GameObject avatar)
        {
            if (avatar == null) return 0;

            try
            {
                Dictionary<Texture, bool> textures = GetTextures(avatar);
                long totalSize = 0;

                foreach (var kvp in textures)
                {
                    TextureInfo info = CalculateTextureSize(kvp.Key);
                    totalSize += info.sizeBytes;
                }

                return totalSize;
            }
            catch (Exception e)
            {
                Debug.LogError($"快速计算纹理内存时出错: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取GameObject中的所有纹理
        /// </summary>
        /// <param name="avatar">要分析的GameObject</param>
        /// <returns>纹理字典(纹理, 是否激活)</returns>
        private static Dictionary<Texture, bool> GetTextures(GameObject avatar)
        {
            // 获取激活和非激活的材质
            var activeMaterials = avatar.GetComponentsInChildren<Renderer>(false)
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .ToHashSet();

            var allMaterials = avatar.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .ToList();

            Dictionary<Texture, bool> textures = new Dictionary<Texture, bool>();

            foreach (Material material in allMaterials)
            {
                if (material == null) continue;

                bool isActive = activeMaterials.Contains(material);
                int[] textureIds = material.GetTexturePropertyNameIDs();

                foreach (int id in textureIds)
                {
                    if (!material.HasProperty(id)) continue;
                    
                    Texture texture = material.GetTexture(id);
                    if (texture == null) continue;

                    if (textures.ContainsKey(texture))
                    {
                        // 如果纹理已存在且当前材质是激活的，则标记为激活
                        if (!textures[texture] && isActive)
                        {
                            textures[texture] = true;
                        }
                    }
                    else
                    {
                        textures.Add(texture, isActive);
                    }
                }
            }

            return textures;
        }

        /// <summary>
        /// 计算单个纹理的大小
        /// </summary>
        /// <param name="texture">要计算的纹理</param>
        /// <returns>纹理信息</returns>
        public static TextureInfo CalculateTextureSize(Texture texture)
        {
            TextureInfo info = new TextureInfo
            {
                texture = texture,
                name = texture.name,
                width = texture.width,
                height = texture.height
            };

            if (texture is Texture2D tex2D)
            {
                TextureFormat format = tex2D.format;
                if (!BPP.TryGetValue(format, out info.BPP))
                {
                    info.BPP = 16; // 默认值
                }
                
                info.formatString = format.ToString();
                info.format = format;
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);

                // 检查是否有Alpha通道
                string path = AssetDatabase.GetAssetPath(texture);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    if (importer is TextureImporter texImporter)
                    {
                        info.hasAlpha = texImporter.DoesSourceTextureHaveAlpha();
                    }
                }
            }
            else if (texture is Texture2DArray tex2DArray)
            {
                if (!BPP.TryGetValue(tex2DArray.format, out info.BPP))
                {
                    info.BPP = 16;
                }
                info.formatString = tex2DArray.format.ToString();
                info.format = tex2DArray.format;
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP) * tex2DArray.depth;
            }
            else if (texture is Cubemap cubemap)
            {
                if (!BPP.TryGetValue(cubemap.format, out info.BPP))
                {
                    info.BPP = 16;
                }
                info.formatString = cubemap.format.ToString();
                info.format = cubemap.format;
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);
                if (cubemap.dimension == TextureDimension.Tex3D)
                {
                    info.sizeBytes *= 6;
                }
            }
            else if (texture is RenderTexture renderTexture)
            {
                if (!RT_BPP.TryGetValue(renderTexture.format, out info.BPP))
                {
                    info.BPP = 16;
                }
                info.BPP += renderTexture.depth;
                info.formatString = renderTexture.format.ToString();
                info.hasAlpha = renderTexture.format == RenderTextureFormat.ARGB32 || 
                               renderTexture.format == RenderTextureFormat.ARGBHalf || 
                               renderTexture.format == RenderTextureFormat.ARGBFloat;
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);
            }
            else
            {
                // 其他类型纹理使用Unity的内存分析器
                info.sizeBytes = Profiler.GetRuntimeMemorySizeLong(texture);
                info.formatString = "Unknown";
            }

            info.sizeMB = info.sizeBytes / (1024f * 1024f);
            return info;
        }

        /// <summary>
        /// 根据BPP计算纹理字节大小
        /// </summary>
        /// <param name="texture">纹理</param>
        /// <param name="bpp">每像素位数</param>
        /// <param name="resolutionScale">分辨率缩放</param>
        /// <returns>字节大小</returns>
        private static long TextureToBytesUsingBPP(Texture texture, float bpp, float resolutionScale = 1f)
        {
            int width = (int)(texture.width * resolutionScale);
            int height = (int)(texture.height * resolutionScale);
            long bytes = 0;

            if (texture is Texture2D || texture is Texture2DArray || texture is Cubemap)
            {
                // 计算包含mipmap的总大小
                for (int mipLevel = 0; mipLevel < texture.mipmapCount; mipLevel++)
                {
                    int mipWidth = Mathf.Max(1, width >> mipLevel);
                    int mipHeight = Mathf.Max(1, height >> mipLevel);
                    bytes += (long)Mathf.RoundToInt(mipWidth * mipHeight * bpp / 8f);
                }
            }
            else if (texture is RenderTexture renderTexture)
            {
                double mipmapMultiplier = 1.0;
                if (renderTexture.useMipMap)
                {
                    // 计算mipmap链的总大小倍数
                    for (int i = 0; i < renderTexture.mipmapCount; i++)
                    {
                        mipmapMultiplier += Math.Pow(0.25, i + 1);
                    }
                }
                bytes = (long)((RT_BPP[renderTexture.format] + renderTexture.depth) * width * height * mipmapMultiplier / 8);
            }
            else
            {
                bytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }

            return bytes;
        }

        /// <summary>
        /// 获取使用指定纹理的材质列表
        /// </summary>
        /// <param name="texture">纹理</param>
        /// <param name="materialsToSearch">要搜索的材质列表</param>
        /// <returns>使用该纹理的材质列表</returns>
        private static List<Material> GetMaterialsUsingTexture(Texture texture, List<Material> materialsToSearch)
        {
            List<Material> materials = new List<Material>();

            foreach (Material material in materialsToSearch)
            {
                if (material == null) continue;

                foreach (string propName in material.GetTexturePropertyNames())
                {
                    Texture matTexture = material.GetTexture(propName);
                    if (matTexture != null && matTexture == texture)
                    {
                        materials.Add(material);
                        break;
                    }
                }
            }

            return materials;
        }

        /// <summary>
        /// 将字节转换为可读的字符串格式
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化的字符串</returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KiB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F1} MiB";
            else
                return $"{bytes / (1024f * 1024f * 1024f):F1} GiB";
        }
    }
}