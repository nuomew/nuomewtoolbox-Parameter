using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using System;
using UnityEngine;

namespace NyameauToolbox.Editor
{
    /// <summary>
    /// VRChat参数精确计算器
    /// 提供更准确的模型参数计算方法
    /// </summary>
    public static class VRChatParameterCalculator
    {
        // VRChat官方限制
        public const int MAX_BITS = 256;
        public const int MAX_TEXTURE_MEMORY_MB = 500;
        public const int MAX_DYNAMIC_BONES = 256;  // 动骨限制改回256
        public const int MAX_UNCOMPRESSED_SIZE_MB = 500;
        public const int MAX_UPLOAD_SIZE_MB = 200;
        
        /// <summary>
        /// 计算Bits参数使用量
        /// </summary>
        public static float CalculateBitsUsage(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return 0f;
            
            int totalBits = 0;
            
            // 检查表情参数
            if (avatar.expressionParameters != null)
            {
                foreach (var param in avatar.expressionParameters.parameters)
                {
                    switch (param.valueType)
                    {
                        case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:
                            totalBits += 1;
                            break;
                        case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int:
                            totalBits += 8;
                            break;
                        case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float:
                            totalBits += 8;
                            break;
                    }
                }
            }
            
            // 检查Animator Controller参数
            var animators = avatar.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller != null)
                    {
                        foreach (var param in controller.parameters)
                        {
                            switch (param.type)
                            {
                                case AnimatorControllerParameterType.Bool:
                                case AnimatorControllerParameterType.Trigger:
                                    totalBits += 1;
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    totalBits += 8;
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    totalBits += 8;
                                    break;
                            }
                        }
                    }
                }
            }
            
            return totalBits;
        }
        
        /// <summary>
        /// 计算纹理显存占用 - 使用迁移的TextureMemoryCalculator
        /// </summary>
        public static TextureAnalysisResult CalculateTextureMemory(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
            {
                return new TextureAnalysisResult();
            }

            // 使用新的纹理内存计算器
            var memoryResult = TextureMemoryCalculator.CalculateTextureMemory(avatar.gameObject);
            
            // 转换为原有的数据结构格式
            var result = new TextureAnalysisResult
            {
                totalMemoryMB = memoryResult.totalMemoryMB,
                textureCount = memoryResult.textureCount,
                textureInfos = new List<TextureInfo>()
            };
            
            // 转换纹理信息格式
            foreach (var texInfo in memoryResult.textures)
            {
                var oldFormatInfo = new TextureInfo
                {
                    name = texInfo.name,
                    width = texInfo.width,
                    height = texInfo.height,
                    format = texInfo.formatString,
                    sizeMB = texInfo.sizeMB,
                    mipmapCount = texInfo.texture != null ? texInfo.texture.mipmapCount : 1
                };
                result.textureInfos.Add(oldFormatInfo);
            }
            
            return result;
        }
        
        // 旧的纹理分析方法已迁移到TextureMemoryCalculator类中
        
        /// <summary>
        /// 计算Dynamic Bone数量 - 全新的准确算法
        /// 直接使用Unity标准API确保准确性
        /// </summary>
        public static DynamicBoneCountResult CalculateDynamicBoneCount(VRCAvatarDescriptor avatar)
        {
            var result = new DynamicBoneCountResult();
            
            if (avatar == null)
            {
                Debug.LogWarning("[诺喵工具箱] Avatar为null");
                return result;
            }
            
            Debug.Log($"[诺喵工具箱] 🎯 开始准确计算动骨数量 - {avatar.name}");
            
            try
            {
                // 🎯 使用Unity标准API，确保搜索所有子对象（包括非激活的）
                var allPhysBones = avatar.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>(true);
                var allPhysBoneColliders = avatar.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider>(true);
                
                result.physBoneCount = allPhysBones != null ? allPhysBones.Length : 0;
                result.physBoneColliderCount = allPhysBoneColliders != null ? allPhysBoneColliders.Length : 0;
                
                Debug.Log($"[诺喵工具箱] ✅ Unity标准API检测结果:");
                Debug.Log($"  VRCPhysBone组件: {result.physBoneCount} 个");
                Debug.Log($"  VRCPhysBoneCollider组件: {result.physBoneColliderCount} 个");
                
                // 记录每个PhysBone组件的详细信息
                if (allPhysBones != null)
                {
                    for (int i = 0; i < allPhysBones.Length; i++)
                    {
                        var physBone = allPhysBones[i];
                        if (physBone != null)
                        {
                            string fullPath = GetFullTransformPath(physBone.transform, avatar.transform);
                            bool isActive = physBone.gameObject.activeInHierarchy;
                            
                            Debug.Log($"[诺喵工具箱] 📊 PhysBone {i+1}: '{physBone.name}' (活跃: {isActive})");
                            Debug.Log($"  路径: {fullPath}");
                            
                            result.detectedPhysBones.Add(new PhysBoneInfo
                            {
                                name = physBone.name,
                                gameObjectPath = fullPath,
                                rootTransform = physBone.rootTransform != null ? physBone.rootTransform.name : physBone.transform.name
                            });
                        }
                    }
                }
                
                // 记录每个PhysBoneCollider组件的详细信息
                if (allPhysBoneColliders != null)
                {
                    for (int i = 0; i < allPhysBoneColliders.Length; i++)
                    {
                        var collider = allPhysBoneColliders[i];
                        if (collider != null)
                        {
                            string fullPath = GetFullTransformPath(collider.transform, avatar.transform);
                            bool isActive = collider.gameObject.activeInHierarchy;
                            
                            Debug.Log($"[诺喵工具箱] 🔸 PhysBoneCollider {i+1}: '{collider.name}' (活跃: {isActive})");
                            Debug.Log($"  路径: {fullPath}");
                            
                            result.detectedColliders.Add(new PhysBoneColliderInfo
                            {
                                name = collider.name,
                                gameObjectPath = fullPath
                            });
                        }
                    }
                }
                
                // 🎯 关键：VRChat显示的就是PhysBone组件总数！
                result.totalCount = result.physBoneCount;
                result.affectedTransformsCount = result.physBoneCount; // 为了界面显示，实际上是组件数量
                
                Debug.Log($"[诺喵工具箱] === 🎉 最终结果（准确版本）===");
                Debug.Log($"🎯 动骨总数量: {result.totalCount} (应与VRChat 'Phys Bone Components' 完全一致)");
                Debug.Log($"PhysBone组件: {result.physBoneCount}");
                Debug.Log($"PhysBoneCollider组件: {result.physBoneColliderCount}");
                Debug.Log($"[诺喵工具箱] ✅ 动骨计算完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[诺喵工具箱] ❌ 动骨检测异常: {e.Message}");
                Debug.LogError($"异常详情: {e.StackTrace}");
                
                // 🆘 最终备用方法：手动搜索
                Debug.Log("[诺喵工具箱] 🆘 启动手动搜索备用方法...");
                try
                {
                    result = ManualSearchPhysBones(avatar);
                    Debug.Log($"[诺喵工具箱] 🆘 手动搜索结果: {result.totalCount} 个PhysBone组件");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[诺喵工具箱] ❌ 手动搜索也失败: {ex.Message}");
                    result.totalCount = 0;
                    result.affectedTransformsCount = 0;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 手动搜索PhysBone组件的备用方法
        /// </summary>
        private static DynamicBoneCountResult ManualSearchPhysBones(VRCAvatarDescriptor avatar)
        {
            var result = new DynamicBoneCountResult();
            var physBoneComponents = new List<Component>();
            var physBoneColliderComponents = new List<Component>();
            
            // 手动递归搜索所有Transform
            ManualRecursiveSearch(avatar.transform, physBoneComponents, physBoneColliderComponents);
            
            result.physBoneCount = physBoneComponents.Count;
            result.physBoneColliderCount = physBoneColliderComponents.Count;
            result.totalCount = result.physBoneCount;
            result.affectedTransformsCount = result.physBoneCount;
            
            Debug.Log($"[诺喵工具箱] 🆘 手动搜索发现:");
            Debug.Log($"  PhysBone组件: {result.physBoneCount} 个");
            Debug.Log($"  PhysBoneCollider组件: {result.physBoneColliderCount} 个");
            
            return result;
        }
        
        /// <summary>
        /// 手动递归搜索组件
        /// </summary>
        private static void ManualRecursiveSearch(Transform transform, List<Component> physBones, List<Component> colliders)
        {
            if (transform == null) return;
            
            // 检查当前节点的所有组件
            var allComponents = transform.GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                string typeName = component.GetType().Name;
                string fullTypeName = component.GetType().FullName;
                
                // 检查PhysBone组件
                if (typeName == "VRCPhysBone" || fullTypeName.Contains("VRCPhysBone"))
                {
                    physBones.Add(component);
                    Debug.Log($"[诺喵工具箱] 🆘 手动发现PhysBone: {component.name} 在 {transform.name}");
                }
                
                // 检查PhysBoneCollider组件
                if (typeName == "VRCPhysBoneCollider" || fullTypeName.Contains("VRCPhysBoneCollider"))
                {
                    colliders.Add(component);
                    Debug.Log($"[诺喵工具箱] 🆘 手动发现PhysBoneCollider: {component.name} 在 {transform.name}");
                }
            }
            
            // 递归搜索所有子节点
            for (int i = 0; i < transform.childCount; i++)
            {
                ManualRecursiveSearch(transform.GetChild(i), physBones, colliders);
            }
        }
        
        /// <summary>
        /// 获取从根节点到目标节点的完整路径
        /// </summary>
        private static string GetFullTransformPath(Transform target, Transform root)
        {
            if (target == null || root == null) return "";
            
            var path = new List<string>();
            Transform current = target;
            
            while (current != null && current != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            if (current == root)
            {
                path.Insert(0, root.name);
                return string.Join("/", path);
            }
            
            return target.name; // 如果找不到路径，返回节点名
        }
        
        /// <summary>
        /// 计算模型大小 - 使用迁移的ModelSizeCalculator
        /// </summary>
        public static ModelSizeInfo CalculateModelSize(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
            {
                return new ModelSizeInfo();
            }

            // 使用新的模型大小计算器
            var meshResult = ModelSizeCalculator.CalculateMeshSize(avatar.gameObject);
            
            // 转换为原有的数据结构格式
            var result = new ModelSizeInfo
            {
                meshSizeMB = meshResult.totalSizeMB,
                vertexCount = meshResult.vertexCount,
                triangleCount = meshResult.triangleCount
            };
            
            return result;
        }
        
        // 旧的网格大小计算方法已迁移到ModelSizeCalculator类中
    }
    
    // 数据结构
    [System.Serializable]
    public class TextureAnalysisResult
    {
        public float totalMemoryMB;
        public int textureCount;
        public List<TextureInfo> textureInfos = new List<TextureInfo>();
    }
    
    [System.Serializable]
    public class TextureInfo
    {
        public string name;
        public int width;
        public int height;
        public string format;
        public int mipmapCount;
        public float sizeMB;
    }
    
    [System.Serializable]
    public class ModelSizeInfo
    {
        public float meshSizeMB;
        public int vertexCount;
        public int triangleCount;
    }
    
    // 新增：动骨计数结果
    [System.Serializable]
    public class DynamicBoneCountResult
    {
        public int physBoneCount;           // VRC PhysBone数量
        public int physBoneColliderCount;   // VRC PhysBoneCollider数量  
        public int totalCount;              // 总数
        
        // 详细信息
        public List<PhysBoneInfo> detectedPhysBones = new List<PhysBoneInfo>();
        public List<PhysBoneColliderInfo> detectedColliders = new List<PhysBoneColliderInfo>();
        
        public int affectedTransformsCount;  // 受影响变换总数
    }
    
    [System.Serializable]
    public class PhysBoneInfo
    {
        public string name;
        public string gameObjectPath;
        public string rootTransform;
    }
    
    [System.Serializable]
    public class PhysBoneColliderInfo
    {
        public string name;
        public string gameObjectPath;
    }
}