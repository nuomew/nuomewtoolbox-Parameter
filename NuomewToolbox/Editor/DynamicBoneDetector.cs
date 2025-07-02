using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace NyameauToolbox.Editor
{
    /// <summary>
    /// 动骨检测诊断工具
    /// 专门用于诊断动骨检测问题
    /// </summary>
    public static class DynamicBoneDetector
    {
        /// <summary>
        /// 执行详细的动骨检测诊断
        /// </summary>
        public static DynamicBoneDetectionResult DetectDynamicBones(VRCAvatarDescriptor avatar)
        {
            var result = new DynamicBoneDetectionResult();
            
            if (avatar == null)
            {
                result.errorMessage = "Avatar为空";
                return result;
            }
            
            var allComponents = avatar.GetComponentsInChildren<Component>();
            result.totalComponents = allComponents.Length;
            
            // 检测VRC PhysBone (SDK3)
            DetectVRCPhysBone(avatar, result);
            
            // 检测传统Dynamic Bone
            DetectTraditionalDynamicBone(avatar, result);
            
            // 检测其他可能的动骨组件
            DetectOtherBoneComponents(avatar, result);
            
            // 分析可能需要动骨的对象
            AnalyzePotentialBoneObjects(avatar, result);
            
            // 生成诊断报告
            GenerateDiagnosticReport(result);
            
            return result;
        }
        
        private static void DetectVRCPhysBone(VRCAvatarDescriptor avatar, DynamicBoneDetectionResult result)
        {
            try
            {
                // 通过类型名称检测
                var vrcPhysBoneType = System.Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone, VRC.SDK3A");
                if (vrcPhysBoneType != null)
                {
                    var physBones = avatar.GetComponentsInChildren(vrcPhysBoneType);
                    result.vrcPhysBoneCount = physBones.Length;
                    result.hasVRCPhysBone = physBones.Length > 0;
                    
                    foreach (var bone in physBones)
                    {
                        result.detectedComponents.Add(new DetectedComponent
                        {
                            name = bone.name,
                            type = "VRCPhysBone",
                            fullTypeName = bone.GetType().FullName,
                            gameObjectPath = GetGameObjectPath(bone.transform)
                        });
                    }
                }
                
                var vrcPhysBoneColliderType = System.Type.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider, VRC.SDK3A");
                if (vrcPhysBoneColliderType != null)
                {
                    var colliders = avatar.GetComponentsInChildren(vrcPhysBoneColliderType);
                    result.vrcPhysBoneColliderCount = colliders.Length;
                    
                    foreach (var collider in colliders)
                    {
                        result.detectedComponents.Add(new DetectedComponent
                        {
                            name = collider.name,
                            type = "VRCPhysBoneCollider",
                            fullTypeName = collider.GetType().FullName,
                            gameObjectPath = GetGameObjectPath(collider.transform)
                        });
                    }
                }
                
                result.vrcSDKDetected = vrcPhysBoneType != null || vrcPhysBoneColliderType != null;
            }
            catch (System.Exception e)
            {
                result.errors.Add($"VRC PhysBone检测异常: {e.Message}");
            }
        }
        
        private static void DetectTraditionalDynamicBone(VRCAvatarDescriptor avatar, DynamicBoneDetectionResult result)
        {
            try
            {
                var dynamicBoneType = System.Type.GetType("DynamicBone, Assembly-CSharp");
                if (dynamicBoneType == null)
                {
                    dynamicBoneType = System.Type.GetType("DynamicBone, DynamicBone");
                }
                
                if (dynamicBoneType != null)
                {
                    var dynamicBones = avatar.GetComponentsInChildren(dynamicBoneType);
                    result.traditionalDynamicBoneCount = dynamicBones.Length;
                    result.hasTraditionalDynamicBone = dynamicBones.Length > 0;
                    
                    foreach (var bone in dynamicBones)
                    {
                        result.detectedComponents.Add(new DetectedComponent
                        {
                            name = bone.name,
                            type = "DynamicBone",
                            fullTypeName = bone.GetType().FullName,
                            gameObjectPath = GetGameObjectPath(bone.transform)
                        });
                    }
                }
                
                var dynamicBoneColliderType = System.Type.GetType("DynamicBoneCollider, Assembly-CSharp");
                if (dynamicBoneColliderType == null)
                {
                    dynamicBoneColliderType = System.Type.GetType("DynamicBoneCollider, DynamicBone");
                }
                
                if (dynamicBoneColliderType != null)
                {
                    var colliders = avatar.GetComponentsInChildren(dynamicBoneColliderType);
                    result.traditionalDynamicBoneColliderCount = colliders.Length;
                    
                    foreach (var collider in colliders)
                    {
                        result.detectedComponents.Add(new DetectedComponent
                        {
                            name = collider.name,
                            type = "DynamicBoneCollider",
                            fullTypeName = collider.GetType().FullName,
                            gameObjectPath = GetGameObjectPath(collider.transform)
                        });
                    }
                }
                
                result.traditionalDynamicBoneDetected = dynamicBoneType != null || dynamicBoneColliderType != null;
            }
            catch (System.Exception e)
            {
                result.errors.Add($"传统Dynamic Bone检测异常: {e.Message}");
            }
        }
        
        private static void DetectOtherBoneComponents(VRCAvatarDescriptor avatar, DynamicBoneDetectionResult result)
        {
            var allComponents = avatar.GetComponentsInChildren<Component>();
            
            var boneRelatedComponents = allComponents
                .Where(c => c != null && (
                    c.GetType().Name.ToLower().Contains("bone") ||
                    c.GetType().Name.ToLower().Contains("phys") ||
                    c.GetType().Name.ToLower().Contains("dynamic") ||
                    c.GetType().FullName.ToLower().Contains("bone") ||
                    c.GetType().FullName.ToLower().Contains("phys")
                ))
                .ToList();
            
            foreach (var component in boneRelatedComponents)
            {
                // 跳过已经检测过的组件
                if (result.detectedComponents.Any(dc => dc.fullTypeName == component.GetType().FullName && 
                                                      dc.gameObjectPath == GetGameObjectPath(component.transform)))
                    continue;
                
                result.otherBoneComponents.Add(new DetectedComponent
                {
                    name = component.name,
                    type = "其他骨骼相关",
                    fullTypeName = component.GetType().FullName,
                    gameObjectPath = GetGameObjectPath(component.transform)
                });
            }
        }
        
        private static void AnalyzePotentialBoneObjects(VRCAvatarDescriptor avatar, DynamicBoneDetectionResult result)
        {
            var potentialBoneNames = new string[] { "hair", "tail", "skirt", "breast", "chest", "ribbon", "cloth", "cape", "scarf" };
            
            var transforms = avatar.GetComponentsInChildren<Transform>();
            
            foreach (var transform in transforms)
            {
                var lowerName = transform.name.ToLower();
                foreach (var boneName in potentialBoneNames)
                {
                    if (lowerName.Contains(boneName))
                    {
                        result.potentialBoneObjects.Add(new PotentialBoneObject
                        {
                            name = transform.name,
                            gameObjectPath = GetGameObjectPath(transform),
                            reason = $"名称包含 '{boneName}'"
                        });
                        break;
                    }
                }
            }
        }
        
        private static void GenerateDiagnosticReport(DynamicBoneDetectionResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 动骨检测诊断报告 ===\n");
            
            // 总体统计
            report.AppendLine($"🔍 检测统计:");
            report.AppendLine($"• 总组件数量: {result.totalComponents}");
            report.AppendLine($"• VRC PhysBone: {result.vrcPhysBoneCount}个");
            report.AppendLine($"• VRC PhysBoneCollider: {result.vrcPhysBoneColliderCount}个");
            report.AppendLine($"• 传统Dynamic Bone: {result.traditionalDynamicBoneCount}个");
            report.AppendLine($"• 传统Dynamic BoneCollider: {result.traditionalDynamicBoneColliderCount}个");
            report.AppendLine($"• 其他骨骼相关组件: {result.otherBoneComponents.Count}个");
            report.AppendLine($"• 可能需要动骨的对象: {result.potentialBoneObjects.Count}个\n");
            
            // SDK支持情况
            report.AppendLine($"📦 SDK支持情况:");
            report.AppendLine($"• VRC SDK3检测: {(result.vrcSDKDetected ? "✅ 可用" : "❌ 不可用")}");
            report.AppendLine($"• 传统Dynamic Bone检测: {(result.traditionalDynamicBoneDetected ? "✅ 可用" : "❌ 不可用")}\n");
            
            // 详细组件列表
            if (result.detectedComponents.Count > 0)
            {
                report.AppendLine($"🎯 检测到的动骨组件:");
                foreach (var component in result.detectedComponents)
                {
                    report.AppendLine($"• [{component.type}] {component.name}");
                    report.AppendLine($"  路径: {component.gameObjectPath}");
                    report.AppendLine($"  类型: {component.fullTypeName}\n");
                }
            }
            
            // 潜在动骨对象
            if (result.potentialBoneObjects.Count > 0)
            {
                report.AppendLine($"💡 可能需要动骨的对象:");
                foreach (var obj in result.potentialBoneObjects.Take(10)) // 只显示前10个
                {
                    report.AppendLine($"• {obj.name} ({obj.reason})");
                    report.AppendLine($"  路径: {obj.gameObjectPath}");
                }
                
                if (result.potentialBoneObjects.Count > 10)
                {
                    report.AppendLine($"  ... 还有 {result.potentialBoneObjects.Count - 10} 个对象");
                }
                report.AppendLine();
            }
            
            // 错误信息
            if (result.errors.Count > 0)
            {
                report.AppendLine($"⚠️ 检测过程中的错误:");
                foreach (var error in result.errors)
                {
                    report.AppendLine($"• {error}");
                }
                report.AppendLine();
            }
            
            // 建议
            report.AppendLine($"💫 建议:");
            if (result.GetTotalBoneCount() == 0)
            {
                if (result.potentialBoneObjects.Count > 0)
                {
                    report.AppendLine("• 发现可能需要动骨的对象，但未检测到动骨组件");
                    report.AppendLine("• 请确认是否已安装Dynamic Bone插件或设置了VRC PhysBone组件");
                }
                else
                {
                    report.AppendLine("• 当前模型可能不需要动骨组件，或者动骨对象命名不符合常见模式");
                }
            }
            else
            {
                report.AppendLine($"• 检测到 {result.GetTotalBoneCount()} 个动骨相关组件");
                report.AppendLine("• 动骨检测正常");
            }
            
            result.diagnosticReport = report.ToString();
        }
        
        private static string GetGameObjectPath(Transform transform)
        {
            var path = transform.name;
            var parent = transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
    
    /// <summary>
    /// 动骨检测结果
    /// </summary>
    [System.Serializable]
    public class DynamicBoneDetectionResult
    {
        public int totalComponents;
        
        // VRC PhysBone (SDK3)
        public int vrcPhysBoneCount;
        public int vrcPhysBoneColliderCount;
        public bool hasVRCPhysBone;
        public bool vrcSDKDetected;
        
        // 传统Dynamic Bone
        public int traditionalDynamicBoneCount;
        public int traditionalDynamicBoneColliderCount;
        public bool hasTraditionalDynamicBone;
        public bool traditionalDynamicBoneDetected;
        
        // 检测到的组件列表
        public List<DetectedComponent> detectedComponents = new List<DetectedComponent>();
        public List<DetectedComponent> otherBoneComponents = new List<DetectedComponent>();
        
        // 潜在动骨对象
        public List<PotentialBoneObject> potentialBoneObjects = new List<PotentialBoneObject>();
        
        // 错误和报告
        public List<string> errors = new List<string>();
        public string errorMessage;
        public string diagnosticReport;
        
        public int GetTotalBoneCount()
        {
            return vrcPhysBoneCount + vrcPhysBoneColliderCount + 
                   traditionalDynamicBoneCount + traditionalDynamicBoneColliderCount +
                   otherBoneComponents.Count;
        }
    }
    
    [System.Serializable]
    public class DetectedComponent
    {
        public string name;
        public string type;
        public string fullTypeName;
        public string gameObjectPath;
    }
    
    [System.Serializable]
    public class PotentialBoneObject
    {
        public string name;
        public string gameObjectPath;
        public string reason;
    }
} 