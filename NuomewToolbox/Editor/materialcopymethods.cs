/*
 * 材质复制方法实现 - Material Copy Methods Implementation
 * 功能：实现各种材质属性的具体复制逻辑
 * 作者：诺喵工具箱
 * 用途：支持MaterialCopyWindow的材质属性复制功能
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NyameauToolbox.Editor
{
    public partial class MaterialCopyWindow
    {
        // 通用属性复制方法
        private int CopyPropertyIfExists(string propertyName)
        {
            // 检查源材质和目标材质是否都有该属性
            if (!sourceMaterial.HasProperty(propertyName) || !targetMaterial.HasProperty(propertyName))
            {
                return 0;
            }
            
            try
            {
                // 获取目标材质着色器中的属性信息（因为目标材质现在应该有相同的着色器）
                var targetShader = targetMaterial.shader;
                int propertyIndex = -1;
                
                // 查找属性索引
                for (int i = 0; i < ShaderUtil.GetPropertyCount(targetShader); i++)
                {
                    if (ShaderUtil.GetPropertyName(targetShader, i) == propertyName)
                    {
                        propertyIndex = i;
                        break;
                    }
                }
                
                if (propertyIndex >= 0)
                {
                    var propertyType = ShaderUtil.GetPropertyType(targetShader, propertyIndex);
                    
                    switch (propertyType)
                    {
                        case UnityEditor.ShaderUtil.ShaderPropertyType.Color:
                            targetMaterial.SetColor(propertyName, sourceMaterial.GetColor(propertyName));
                            break;
                        case UnityEditor.ShaderUtil.ShaderPropertyType.Vector:
                            targetMaterial.SetVector(propertyName, sourceMaterial.GetVector(propertyName));
                            break;
                        case UnityEditor.ShaderUtil.ShaderPropertyType.Float:
                        case UnityEditor.ShaderUtil.ShaderPropertyType.Range:
                            targetMaterial.SetFloat(propertyName, sourceMaterial.GetFloat(propertyName));
                            break;
                        case UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv:
                            targetMaterial.SetTexture(propertyName, sourceMaterial.GetTexture(propertyName));
                            // 同时复制纹理的缩放和偏移
                            targetMaterial.SetTextureScale(propertyName, sourceMaterial.GetTextureScale(propertyName));
                            targetMaterial.SetTextureOffset(propertyName, sourceMaterial.GetTextureOffset(propertyName));
                            break;
                    }
                    
                    Debug.Log($"[材质复制] 成功复制属性: {propertyName} ({propertyType})");
                    return 1;
                }
                else
                {
                    Debug.LogWarning($"[材质复制] 在目标着色器中找不到属性索引: {propertyName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 复制属性 {propertyName} 时发生错误: {e.Message}");
            }
            
            return 0;
        }
        
        // 通用属性复制方法（不依赖ShaderUtil，更兼容）
        private int CopyPropertyDirect(string propertyName)
        {
            if (!sourceMaterial.HasProperty(propertyName))
            {
                Debug.LogWarning($"[材质复制] 源材质没有属性: {propertyName}");
                return 0;
            }
            
            if (!targetMaterial.HasProperty(propertyName))
            {
                Debug.LogWarning($"[材质复制] 目标材质没有属性: {propertyName}");
                return 0;
            }
            
            try
            {
                // 尝试不同类型的属性复制
                // 首先尝试作为纹理复制
                try
                {
                    var sourceTexture = sourceMaterial.GetTexture(propertyName);
                    targetMaterial.SetTexture(propertyName, sourceTexture);
                    
                    // 复制纹理的缩放和偏移
                    try
                    {
                        targetMaterial.SetTextureScale(propertyName, sourceMaterial.GetTextureScale(propertyName));
                        targetMaterial.SetTextureOffset(propertyName, sourceMaterial.GetTextureOffset(propertyName));
                    }
                    catch
                    {
                        // 某些属性可能不支持缩放和偏移
                    }
                    
                    Debug.Log($"[材质复制] 成功复制纹理属性: {propertyName} = {(sourceTexture != null ? sourceTexture.name : "null")}");
                    return 1;
                }
                catch
                {
                    // 如果不是纹理属性，继续尝试其他类型
                }
                
                // 尝试作为颜色复制
                try
                {
                    var sourceColor = sourceMaterial.GetColor(propertyName);
                    targetMaterial.SetColor(propertyName, sourceColor);
                    Debug.Log($"[材质复制] 成功复制颜色属性: {propertyName} = {sourceColor}");
                    return 1;
                }
                catch
                {
                    // 如果不是颜色属性，继续尝试其他类型
                }
                
                // 尝试作为向量复制
                try
                {
                    var sourceVector = sourceMaterial.GetVector(propertyName);
                    targetMaterial.SetVector(propertyName, sourceVector);
                    Debug.Log($"[材质复制] 成功复制向量属性: {propertyName} = {sourceVector}");
                    return 1;
                }
                catch
                {
                    // 如果不是向量属性，继续尝试其他类型
                }
                
                // 尝试作为浮点数复制
                try
                {
                    var sourceFloat = sourceMaterial.GetFloat(propertyName);
                    targetMaterial.SetFloat(propertyName, sourceFloat);
                    Debug.Log($"[材质复制] 成功复制浮点属性: {propertyName} = {sourceFloat}");
                    return 1;
                }
                catch
                {
                    // 如果不是浮点属性，继续尝试其他类型
                }
                
                // 尝试作为整数复制
                try
                {
                    var sourceInt = sourceMaterial.GetInt(propertyName);
                    targetMaterial.SetInt(propertyName, sourceInt);
                    Debug.Log($"[材质复制] 成功复制整数属性: {propertyName} = {sourceInt}");
                    return 1;
                }
                catch
                {
                    Debug.LogWarning($"[材质复制] 无法确定属性类型: {propertyName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 复制属性 {propertyName} 时发生错误: {e.Message}");
            }
            
            return 0;
        }
        
        // 批量复制属性
        private int CopyPropertiesByNames(string[] propertyNames)
        {
            int count = 0;
            foreach (string propertyName in propertyNames)
            {
                // 首先尝试直接复制方法
                int result = CopyPropertyDirect(propertyName);
                if (result == 0)
                {
                    // 如果直接复制失败，尝试使用ShaderUtil方法
                    result = CopyPropertyIfExists(propertyName);
                }
                count += result;
            }
            return count;
        }
        
        // 基本设置复制
        private int CopyBasicSettings()
        {
            string[] basicProperties = {
                "_MainTex", "_Color", "_Cutoff", "_AlphaToMask",
                "_BumpMap", "_BumpScale", "_DetailNormalMap", "_DetailNormalMapScale",
                "_Parallax", "_ParallaxMap", "_OcclusionMap", "_OcclusionStrength",
                "_EmissionMap", "_EmissionColor", "_DetailMask", "_DetailAlbedoMap",
                "_DetailAlbedoMapScale", "_UVSec", "_Mode", "_SrcBlend", "_DstBlend",
                "_ZWrite", "_Glossiness", "_GlossMapScale", "_SmoothnessTextureChannel",
                "_Metallic", "_MetallicGlossMap", "_SpecularHighlights", "_GlossyReflections"
            };
            
            return CopyPropertiesByNames(basicProperties);
        }
        
        // 照明设置复制
        private int CopyLightingSettings()
        {
            string[] lightingProperties = {
                "_LightingGradient", "_LightingGradientEnd", "_LightingGradientStart",
                "_ShadowLift", "_ShadowStrength", "_IndirectLightingBoost",
                "_LightWrappingCompensationFactor", "_LightSkew", "_FleckScale",
                "_SpecularTint", "_SpecularSmoothness", "_SpecularThreshold",
                "_GSAAVariance", "_GSAAThreshold", "_LightingMode",
                "_LightingColorMode", "_LightingMapMode", "_LightingDirectionMode"
            };
            
            return CopyPropertiesByNames(lightingProperties);
        }
        
        // UV设置复制
        private int CopyUVSettings()
        {
            string[] uvProperties = {
                "_MainTex_ST", "_DetailAlbedoMap_ST", "_BumpMap_ST", "_DetailNormalMap_ST",
                "_ParallaxMap_ST", "_OcclusionMap_ST", "_EmissionMap_ST", "_DetailMask_ST",
                "_MetallicGlossMap_ST", "_UVAnimationMask", "_UVAnimationScrollX",
                "_UVAnimationScrollY", "_UVAnimationRotation", "_UVTileDiscard",
                "_UVTileDiscardRow", "_UVTileDiscardColumn", "_UVSec"
            };
            
            return CopyPropertiesByNames(uvProperties);
        }
        
        // VRChat设置复制
        private int CopyVRChatSettings()
        {
            string[] vrchatProperties = {
                "_VRCFallback", "_VRCFallbackTags", "_VRCSDK3", "_VRChatCameraMode",
                "_VRChatMirrorMode", "_VRChatPhotographyMode", "_IgnoreProjector",
                "_VRCBilboard", "_VRCUiMesh", "_VRCUiMeshMode"
            };
            
            return CopyPropertiesByNames(vrchatProperties);
        }
        
        // 颜色设置复制
        private int CopyColorSettings()
        {
            string[] colorProperties = {
                "_Color", "_ColorMask", "_HSVAAdjust", "_Hue", "_Saturation",
                "_Value", "_Gamma", "_ColorAdjustTexture", "_ColorAdjustTextureUV",
                "_TintR", "_TintG", "_TintB", "_ColorGrading", "_ColorGradingMap"
            };
            
            return CopyPropertiesByNames(colorProperties);
        }
        
        // 主色/Alpha设置复制
        private int CopyMainColorAlphaSettings()
        {
            string[] mainColorAlphaProperties = {
                "_MainTex", "_Color", "_AlphaCutoff", "_Cutoff", "_AlphaToMask",
                "_AlphaTest", "_AlphaTestRef", "_AlphaPremultiply", "_AlphaBlend",
                "_MainTexAlphaUV", "_MainTexHasAlpha", "_MainTexAlphaCutoff"
            };
            
            return CopyPropertiesByNames(mainColorAlphaProperties);
        }
        
        // 阴影设置复制
        private int CopyShadowSettings()
        {
            string[] shadowProperties = {
                "_ShadowStrength", "_ShadowLift", "_ShadowColor", "_ShadowColorTex",
                "_ShadowBorder", "_ShadowBlur", "_ShadowReceive", "_ShadowCast",
                "_ShadowNormalBias", "_ShadowBias", "_ShadowNearPlane", "_ShadowFarPlane",
                "_ShadowMode", "_ShadowMask", "_ShadowMaskMap"
            };
            
            return CopyPropertiesByNames(shadowProperties);
        }
        
        // RimShade设置复制
        private int CopyRimShadeSettings()
        {
            string[] rimShadeProperties = {
                "_RimShade", "_RimShadeColor", "_RimShadeMap", "_RimShadeMask",
                "_RimShadeWidth", "_RimShadeBlur", "_RimShadeFresnelPower",
                "_RimShadeInvert", "_RimShadeMode", "_RimShadeNormalMapInfluence"
            };
            
            return CopyPropertiesByNames(rimShadeProperties);
        }
        
        // 发光设置复制
        private int CopyEmissionSettings()
        {
            string[] emissionProperties = {
                "_EmissionMap", "_EmissionColor", "_EmissionStrength", "_EmissionLM",
                "_EmissionRealtimeAnimated", "_EmissionBakedAnimated", "_EmissionLightingInfluence",
                "_EmissionCenterOutEnabled", "_EmissionScrollingEnabled", "_EmissionScrollSpeed",
                "_EmissionScrollDirection", "_EmissionBlinkingEnabled", "_EmissionBlinkingOffset",
                "_EmissionFlickeringEnabled", "_EmissionFlickeringSpeed"
            };
            
            return CopyPropertiesByNames(emissionProperties);
        }
        
        // 法线贴图&反射设置复制
        private int CopyNormalReflectionSettings()
        {
            string[] normalReflectionProperties = {
                "_BumpMap", "_BumpScale", "_DetailNormalMap", "_DetailNormalMapScale",
                "_NormalMapSpace", "_NormalMapUV", "_ReflectionMask", "_ReflectionTex",
                "_ReflectionColor", "_ReflectionStrength", "_ReflectionBlur",
                "_ReflectionFresnel", "_ReflectionSuppressBaseColorValue"
            };
            
            return CopyPropertiesByNames(normalReflectionProperties);
        }
        
        // 法线贴图设置复制 - 直接复制方法
        private int CopyNormalMapSettings()
        {
            Debug.Log("[材质复制] *** 强制暴力复制法线贴图设置 ***");
            int copied = 0;
            
            try
            {
                // 方法1: 使用ShaderUtil遍历所有属性
                var sourceShader = sourceMaterial.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(sourceShader); i++)
                {
                    string propName = ShaderUtil.GetPropertyName(sourceShader, i);
                    
                    // 更宽泛的法线贴图属性匹配
                    if (propName.ToLower().Contains("bump") || propName.ToLower().Contains("normal") || 
                        propName.ToLower().Contains("detailnormal") || propName.ToLower().Contains("packedmap"))
                    {
                        Debug.Log($"[材质复制] 发现法线贴图属性: {propName}");
                        
                        if (targetMaterial.HasProperty(propName))
                        {
                            try
                            {
                                var propType = ShaderUtil.GetPropertyType(sourceShader, i);
                                switch (propType)
                                {
                                    case ShaderUtil.ShaderPropertyType.Color:
                                        var color = sourceMaterial.GetColor(propName);
                                        targetMaterial.SetColor(propName, color);
                                        Debug.Log($"[材质复制] *** 强制复制法线贴图颜色: {propName} = {color} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Float:
                                    case ShaderUtil.ShaderPropertyType.Range:
                                        var floatVal = sourceMaterial.GetFloat(propName);
                                        targetMaterial.SetFloat(propName, floatVal);
                                        Debug.Log($"[材质复制] *** 强制复制法线贴图浮点: {propName} = {floatVal} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Vector:
                                        var vector = sourceMaterial.GetVector(propName);
                                        targetMaterial.SetVector(propName, vector);
                                        Debug.Log($"[材质复制] *** 强制复制法线贴图向量: {propName} = {vector} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.TexEnv:
                                        var texture = sourceMaterial.GetTexture(propName);
                                        targetMaterial.SetTexture(propName, texture);
                                        targetMaterial.SetTextureScale(propName, sourceMaterial.GetTextureScale(propName));
                                        targetMaterial.SetTextureOffset(propName, sourceMaterial.GetTextureOffset(propName));
                                        Debug.Log($"[材质复制] *** 强制复制法线贴图纹理: {propName} = {(texture != null ? texture.name : "null")} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Int:
                                        var intVal = sourceMaterial.GetInt(propName);
                                        targetMaterial.SetInt(propName, intVal);
                                        Debug.Log($"[材质复制] *** 强制复制法线贴图整数: {propName} = {intVal} ***");
                                        break;
                                }
                                copied++;
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[材质复制] 复制法线贴图属性失败 {propName}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[材质复制] 目标材质缺少法线贴图属性: {propName}");
                        }
                    }
                }
                
                // 方法2: 直接尝试复制常见的法线贴图属性（不依赖ShaderUtil）
                string[] commonNormalProps = {
                    "_BumpMap", "_NormalMap", "_DetailNormalMap", "_PackedMap", "_BumpScale",
                    "_NormalScale", "_DetailNormalMapScale", "_BumpStrength", "_NormalStrength",
                    "_DetailBumpMap", "_DetailBumpScale", "_SecondaryNormalMap", "_SecondaryBumpMap",
                    "_MainBump", "_SubBump", "_Normal", "_Bump", "_NormalTex", "_BumpTex"
                };
                
                foreach (string propName in commonNormalProps)
                {
                    if (sourceMaterial.HasProperty(propName) && targetMaterial.HasProperty(propName))
                    {
                        try
                        {
                            // 尝试所有可能的属性类型
                            try
                            {
                                var texture = sourceMaterial.GetTexture(propName);
                                targetMaterial.SetTexture(propName, texture);
                                targetMaterial.SetTextureScale(propName, sourceMaterial.GetTextureScale(propName));
                                targetMaterial.SetTextureOffset(propName, sourceMaterial.GetTextureOffset(propName));
                                Debug.Log($"[材质复制] *** 直接复制法线纹理: {propName} = {(texture != null ? texture.name : "null")} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var floatVal = sourceMaterial.GetFloat(propName);
                                targetMaterial.SetFloat(propName, floatVal);
                                Debug.Log($"[材质复制] *** 直接复制法线浮点: {propName} = {floatVal} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var color = sourceMaterial.GetColor(propName);
                                targetMaterial.SetColor(propName, color);
                                Debug.Log($"[材质复制] *** 直接复制法线颜色: {propName} = {color} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var vector = sourceMaterial.GetVector(propName);
                                targetMaterial.SetVector(propName, vector);
                                Debug.Log($"[材质复制] *** 直接复制法线向量: {propName} = {vector} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[材质复制] 直接复制法线属性失败 {propName}: {ex.Message}");
                        }
                    }
                }
                
                // 强制刷新和保存
                EditorUtility.SetDirty(targetMaterial);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[材质复制] *** 法线贴图强制复制完成，共复制 {copied} 个属性 ***");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 法线贴图强制复制失败: {e.Message}");
            }
            
            return copied;
        }
        
        // 背光灯设置复制
        private int CopyBacklightSettings()
        {
            string[] backlightProperties = {
                "_BacklightMap", "_BacklightColor", "_BacklightStrength", "_BacklightMask",
                "_BacklightNormalStrength", "_BacklightViewStrength", "_BacklightReceiveShadow",
                "_BacklightBackfaceMask", "_BacklightDirectivity", "_BacklightViewDirectivity"
            };
            
            return CopyPropertiesByNames(backlightProperties);
        }
        
        // 反射设置复制
        private int CopyReflectionSettings()
        {
            string[] reflectionProperties = {
                "_ReflectionMask", "_ReflectionTex", "_ReflectionColor", "_ReflectionStrength",
                "_ReflectionBlur", "_ReflectionFresnel", "_ReflectionSuppressBaseColorValue",
                "_CubeMap", "_ReflectionMode", "_ReflectionBlendMode", "_MetallicReflectionMap",
                "_SpecularReflectionMap", "_GSAAVariance", "_GSAAThreshold"
            };
            
            return CopyPropertiesByNames(reflectionProperties);
        }
        
        // MatCap设置复制
        private int CopyMatCapSettings()
        {
            string[] matCapProperties = {
                "_MatCap", "_MatCapColor", "_MatCapMask", "_MatCapStrength",
                "_MatCapNormal", "_MatCapBlur", "_MatCapBlendMode", "_MatCapBorder",
                "_MatCapDistortion", "_MatCapRotation", "_MatCapEmission", "_MatCapReplace",
                "_MatCap2nd", "_MatCap2ndColor", "_MatCap2ndMask", "_MatCap2ndStrength"
            };
            
            return CopyPropertiesByNames(matCapProperties);
        }
        
        // Rim Light设置复制 - 直接复制方法
        private int CopyRimLightSettings()
        {
            Debug.Log("[材质复制] *** 强制暴力复制 Rim Light 设置 ***");
            int copied = 0;
            
            try
            {
                // 方法1: 使用ShaderUtil遍历所有属性
                var sourceShader = sourceMaterial.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(sourceShader); i++)
                {
                    string propName = ShaderUtil.GetPropertyName(sourceShader, i);
                    
                    // 更宽泛的Rim Light属性匹配
                    if (propName.ToLower().Contains("rim") && !propName.ToLower().Contains("rimshade"))
                    {
                        Debug.Log($"[材质复制] 发现Rim Light属性: {propName}");
                        
                        if (targetMaterial.HasProperty(propName))
                        {
                            try
                            {
                                var propType = ShaderUtil.GetPropertyType(sourceShader, i);
                                switch (propType)
                                {
                                    case ShaderUtil.ShaderPropertyType.Color:
                                        var color = sourceMaterial.GetColor(propName);
                                        targetMaterial.SetColor(propName, color);
                                        Debug.Log($"[材质复制] *** 强制复制Rim Light颜色: {propName} = {color} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Float:
                                    case ShaderUtil.ShaderPropertyType.Range:
                                        var floatVal = sourceMaterial.GetFloat(propName);
                                        targetMaterial.SetFloat(propName, floatVal);
                                        Debug.Log($"[材质复制] *** 强制复制Rim Light浮点: {propName} = {floatVal} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Vector:
                                        var vector = sourceMaterial.GetVector(propName);
                                        targetMaterial.SetVector(propName, vector);
                                        Debug.Log($"[材质复制] *** 强制复制Rim Light向量: {propName} = {vector} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.TexEnv:
                                        var texture = sourceMaterial.GetTexture(propName);
                                        targetMaterial.SetTexture(propName, texture);
                                        targetMaterial.SetTextureScale(propName, sourceMaterial.GetTextureScale(propName));
                                        targetMaterial.SetTextureOffset(propName, sourceMaterial.GetTextureOffset(propName));
                                        Debug.Log($"[材质复制] *** 强制复制Rim Light纹理: {propName} = {(texture != null ? texture.name : "null")} ***");
                                        break;
                                    case ShaderUtil.ShaderPropertyType.Int:
                                        var intVal = sourceMaterial.GetInt(propName);
                                        targetMaterial.SetInt(propName, intVal);
                                        Debug.Log($"[材质复制] *** 强制复制Rim Light整数: {propName} = {intVal} ***");
                                        break;
                                }
                                copied++;
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[材质复制] 复制Rim Light属性失败 {propName}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[材质复制] 目标材质缺少Rim Light属性: {propName}");
                        }
                    }
                }
                
                // 方法2: 直接尝试复制常见的Rim Light属性（不依赖ShaderUtil）
                string[] commonRimProps = {
                    "_RimLightColor", "_RimColor", "_RimPower", "_RimStrength", "_RimIntensity",
                    "_RimLightPower", "_RimLightStrength", "_RimLightIntensity", "_RimFresnelPower",
                    "_RimMask", "_RimLightMask", "_RimTexture", "_RimLightTexture", "_RimMap",
                    "_RimLightMap", "_RimBlend", "_RimLightBlend", "_RimDirection", "_RimLightDirection"
                };
                
                foreach (string propName in commonRimProps)
                {
                    if (sourceMaterial.HasProperty(propName) && targetMaterial.HasProperty(propName))
                    {
                        try
                        {
                            // 尝试所有可能的属性类型
                            try
                            {
                                var color = sourceMaterial.GetColor(propName);
                                targetMaterial.SetColor(propName, color);
                                Debug.Log($"[材质复制] *** 直接复制Rim颜色: {propName} = {color} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var floatVal = sourceMaterial.GetFloat(propName);
                                targetMaterial.SetFloat(propName, floatVal);
                                Debug.Log($"[材质复制] *** 直接复制Rim浮点: {propName} = {floatVal} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var texture = sourceMaterial.GetTexture(propName);
                                targetMaterial.SetTexture(propName, texture);
                                Debug.Log($"[材质复制] *** 直接复制Rim纹理: {propName} = {(texture != null ? texture.name : "null")} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                            
                            try
                            {
                                var vector = sourceMaterial.GetVector(propName);
                                targetMaterial.SetVector(propName, vector);
                                Debug.Log($"[材质复制] *** 直接复制Rim向量: {propName} = {vector} ***");
                                copied++;
                                continue;
                            }
                            catch { }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[材质复制] 直接复制Rim属性失败 {propName}: {ex.Message}");
                        }
                    }
                }
                
                // 强制刷新和保存
                EditorUtility.SetDirty(targetMaterial);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[材质复制] *** Rim Light 强制复制完成，共复制 {copied} 个属性 ***");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] Rim Light 强制复制失败: {e.Message}");
            }
            
            return copied;
        }
        
        // Glitter设置复制
        private int CopyGlitterSettings()
        {
            string[] glitterProperties = {
                "_GlitterMap", "_GlitterMask", "_GlitterColor", "_GlitterColorMap",
                "_GlitterSpeed", "_GlitterBrightness", "_GlitterAngleRange", "_GlitterMinBrightness",
                "_GlitterBias", "_GlitterContrast", "_GlitterSize", "_GlitterFrequency",
                "_GlitterJitter", "_GlitterHueShiftEnabled", "_GlitterHueShiftSpeed",
                "_GlitterRandomColors", "_GlitterTextureRotation", "_GlitterUVMode"
            };
            
            return CopyPropertiesByNames(glitterProperties);
        }
        
        // 增强复制模式 - 复制所有材质属性
        private int CopyAllMaterialProperties()
        {
            int totalCopied = 0;
            
            Debug.Log("[材质复制工具] 开始增强复制模式 - 复制所有属性");
            
            try
            {
                // 1. 复制材质的基本设置
                totalCopied += CopyMaterialBasicProperties();
                
                // 2. 复制所有基础属性
                totalCopied += CopyBasicSettings();
                totalCopied += CopyLightingSettings();
                totalCopied += CopyUVSettings();
                totalCopied += CopyVRChatSettings();
                totalCopied += CopyColorSettings();
                totalCopied += CopyMainColorAlphaSettings();
                totalCopied += CopyShadowSettings();
                totalCopied += CopyRimShadeSettings();
                totalCopied += CopyEmissionSettings();
                totalCopied += CopyNormalReflectionSettings();
                totalCopied += CopyNormalMapSettings();
                totalCopied += CopyBacklightSettings();
                totalCopied += CopyReflectionSettings();
                totalCopied += CopyMatCapSettings();
                totalCopied += CopyRimLightSettings();
                totalCopied += CopyGlitterSettings();
                
                // 3. 复制关键字和渲染设置
                totalCopied += CopyMaterialKeywords();
                totalCopied += CopyMaterialRenderQueue();
                
                Debug.Log($"[材质复制工具] 基础属性复制完成，共复制 {totalCopied} 个属性");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制工具] 增强复制模式出错: {e.Message}");
            }
            
            return totalCopied;
        }
        
        // 复制材质基本属性（渲染队列、全局照明等）
        private int CopyMaterialBasicProperties()
        {
            int copied = 0;
            
            try
            {
                // 复制渲染队列
                targetMaterial.renderQueue = sourceMaterial.renderQueue;
                Debug.Log($"[材质复制] 复制渲染队列: {sourceMaterial.renderQueue}");
                copied++;
                
                // 复制全局照明标志
                targetMaterial.globalIlluminationFlags = sourceMaterial.globalIlluminationFlags;
                Debug.Log($"[材质复制] 复制全局照明标志: {sourceMaterial.globalIlluminationFlags}");
                copied++;
                
                // 复制双面全局照明
                targetMaterial.doubleSidedGI = sourceMaterial.doubleSidedGI;
                Debug.Log($"[材质复制] 复制双面全局照明: {sourceMaterial.doubleSidedGI}");
                copied++;
                
                // 复制启用GPU实例化
                targetMaterial.enableInstancing = sourceMaterial.enableInstancing;
                Debug.Log($"[材质复制] 复制GPU实例化: {sourceMaterial.enableInstancing}");
                copied++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 复制材质基本属性时出错: {e.Message}");
            }
            
            return copied;
        }
        
        // 复制材质关键字
        private int CopyMaterialKeywords()
        {
            try
            {
                // 获取源材质的所有关键字
                string[] sourceKeywords = sourceMaterial.shaderKeywords;
                
                // 清除目标材质的现有关键字
                targetMaterial.shaderKeywords = new string[0];
                
                // 复制所有关键字
                targetMaterial.shaderKeywords = sourceKeywords;
                
                Debug.Log($"[材质复制] 复制了 {sourceKeywords.Length} 个着色器关键字: [{string.Join(", ", sourceKeywords)}]");
                
                return sourceKeywords.Length;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 复制关键字时出错: {e.Message}");
                return 0;
            }
        }
        
        // 复制材质渲染队列
        private int CopyMaterialRenderQueue()
        {
            try
            {
                int sourceQueue = sourceMaterial.renderQueue;
                targetMaterial.renderQueue = sourceQueue;
                
                Debug.Log($"[材质复制] 复制渲染队列: {sourceQueue}");
                
                return 1;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 复制渲染队列时出错: {e.Message}");
                return 0;
            }
        }
        
        // 复制扩展属性
        private int CopyExtendedProperties()
        {
            int totalCopied = 0;
            
            Debug.Log("[材质复制工具] 开始复制扩展属性");
            
            try
            {
                // 复制所有扩展属性
                totalCopied += CopyOutlineSettings();
                totalCopied += CopyParallaxSettings();
                totalCopied += CopyDistanceFadeSettings();
                totalCopied += CopyAudioLinkSettings();
                totalCopied += CopyDissolveSettings();
                totalCopied += CopyIDMaskSettings();
                totalCopied += CopyUVTileDiscardSettings();
                totalCopied += CopyStencilSettings();
                totalCopied += CopyRenderSettings();
                totalCopied += CopyLightmapSettings();
                totalCopied += CopyTessellationSettings();
                totalCopied += CopyOptimizationSettings();
                
                // 复制所有未知属性（通用复制）
                totalCopied += CopyAllUnknownProperties();
                
                Debug.Log($"[材质复制工具] 扩展属性复制完成，共复制 {totalCopied} 个属性");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制工具] 扩展属性复制出错: {e.Message}");
            }
            
            return totalCopied;
        }
        
        // 复制选中项目的补充属性（选择性通用复制方法）
        private int CopySelectedUnknownProperties()
        {
            int copied = 0;
            
            try
            {
                Debug.Log("[材质复制] 开始选择性补充属性复制，仅复制已选中类别的相关属性...");
                
                // 获取源材质着色器的所有属性
                Shader sourceShader = sourceMaterial.shader;
                if (sourceShader == null)
                {
                    Debug.LogWarning("[材质复制] 源材质没有着色器");
                    return 0;
                }
                
                // 遍历着色器的所有属性，但只复制与选中选项相关的属性
                int propertyCount = ShaderUtil.GetPropertyCount(sourceShader);
                Debug.Log($"[材质复制] 源着色器共有 {propertyCount} 个属性，开始选择性检查...");
                
                for (int i = 0; i < propertyCount; i++)
                {
                    try
                    {
                        string propName = ShaderUtil.GetPropertyName(sourceShader, i);
                        
                        // 检查这个属性是否属于已选中的类别
                        if (!IsPropertyInSelectedCategories(propName))
                        {
                            continue; // 跳过未选中类别的属性
                        }
                        
                        // 检查目标材质是否有这个属性
                        if (!targetMaterial.HasProperty(propName))
                        {
                            continue;
                        }
                        
                        ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(sourceShader, i);
                        bool success = false;
                        
                        switch (propType)
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Color colorValue = sourceMaterial.GetColor(propName);
                                    targetMaterial.SetColor(propName, colorValue);
                                    Debug.Log($"[材质复制] 补充复制颜色属性 {propName}: {colorValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Vector:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Vector4 vectorValue = sourceMaterial.GetVector(propName);
                                    targetMaterial.SetVector(propName, vectorValue);
                                    Debug.Log($"[材质复制] 补充复制向量属性 {propName}: {vectorValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    float floatValue = sourceMaterial.GetFloat(propName);
                                    targetMaterial.SetFloat(propName, floatValue);
                                    Debug.Log($"[材质复制] 补充复制浮点属性 {propName}: {floatValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Texture textureValue = sourceMaterial.GetTexture(propName);
                                    Vector2 offset = sourceMaterial.GetTextureOffset(propName);
                                    Vector2 scale = sourceMaterial.GetTextureScale(propName);
                                    
                                    targetMaterial.SetTexture(propName, textureValue);
                                    targetMaterial.SetTextureOffset(propName, offset);
                                    targetMaterial.SetTextureScale(propName, scale);
                                    
                                    Debug.Log($"[材质复制] 补充复制纹理属性 {propName}: {(textureValue != null ? textureValue.name : "null")}, 偏移: {offset}, 缩放: {scale}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Int:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    int intValue = sourceMaterial.GetInt(propName);
                                    targetMaterial.SetInt(propName, intValue);
                                    Debug.Log($"[材质复制] 补充复制整数属性 {propName}: {intValue}");
                                    success = true;
                                }
                                break;
                        }
                        
                        if (success)
                        {
                            copied++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[材质复制] 补充复制属性 {i} 时出错: {e.Message}");
                    }
                }
                
                Debug.Log($"[材质复制] 选择性补充属性复制完成，共补充复制 {copied} 个属性");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 选择性补充属性复制出错: {e.Message}");
            }
            
            return copied;
        }
        
        // 检查属性是否属于已选中的类别
        private bool IsPropertyInSelectedCategories(string propName)
        {
            // 特别调试Rim Light和法线贴图属性
            bool isRimProperty = propName.Contains("_Rim");
            bool isNormalProperty = propName.Contains("_Bump") || propName.Contains("_Normal");
            
            if (isRimProperty || isNormalProperty)
            {
                Debug.Log($"[材质复制] 检查属性 {propName}:");
                Debug.Log($"  - copyRimLightSettings = {copyRimLightSettings}, IsRimLightProperty = {IsRimLightProperty(propName)}");
                Debug.Log($"  - copyNormalMapSettings = {copyNormalMapSettings}, IsNormalProperty = {IsNormalProperty(propName)}");
                Debug.Log($"  - copyRimShadeSettings = {copyRimShadeSettings}, IsRimShadeProperty = {IsRimShadeProperty(propName)}");
            }
            
            // 基本设置相关属性
            if (copyBasicSettings && IsBasicProperty(propName)) return true;
            
            // 照明设置相关属性
            if (copyLightingSettings && IsLightingProperty(propName)) return true;
            
            // UV设置相关属性
            if (copyUVSettings && IsUVProperty(propName)) return true;
            
            // VRChat设置相关属性
            if (copyVRChatSettings && IsVRChatProperty(propName)) return true;
            
            // 颜色设置相关属性
            if (copyColorSettings && IsColorProperty(propName)) return true;
            
            // 主色/Alpha设置相关属性
            if (copyMainColorAlpha && IsMainColorAlphaProperty(propName)) return true;
            
            // 阴影设置相关属性
            if (copyShadowSettings && IsShadowProperty(propName)) return true;
            
            // RimShade设置相关属性
            if (copyRimShadeSettings && IsRimShadeProperty(propName)) return true;
            
            // 发光设置相关属性
            if (copyEmissionSettings && IsEmissionProperty(propName)) return true;
            
            // 法线贴图&反射设置相关属性
            if (copyNormalReflectionSettings && IsNormalReflectionProperty(propName)) return true;
            
            // 法线贴图相关属性
            if (copyNormalMapSettings && IsNormalProperty(propName)) 
            {
                if (isNormalProperty) Debug.Log($"[材质复制] 属性 {propName} 匹配法线贴图类别");
                return true;
            }
            
            // 背光灯设置相关属性
            if (copyBacklightSettings && IsBacklightProperty(propName)) return true;
            
            // 反射设置相关属性
            if (copyReflectionSettings && IsReflectionProperty(propName)) return true;
            
            // MatCap设置相关属性
            if (copyMatCapSettings && IsMatCapProperty(propName)) return true;
            
            // Rim Light设置相关属性
            if (copyRimLightSettings && IsRimLightProperty(propName)) 
            {
                if (isRimProperty) Debug.Log($"[材质复制] 属性 {propName} 匹配Rim Light类别");
                return true;
            }
            
            // Glitter设置相关属性
            if (copyGlitterSettings && IsGlitterProperty(propName)) return true;
            
            // 轮廓设置相关属性
            if (copyOutlineSettings && IsOutlineProperty(propName)) return true;
            
            // 视差设置相关属性
            if (copyParallaxSettings && IsParallaxProperty(propName)) return true;
            
            // 距离淡化设置相关属性
            if (copyDistanceFadeSettings && IsDistanceFadeProperty(propName)) return true;
            
            // AudioLink设置相关属性
            if (copyAudioLinkSettings && IsAudioLinkProperty(propName)) return true;
            
            // Dissolve设置相关属性
            if (copyDissolveSettings && IsDissolveProperty(propName)) return true;
            
            // ID Mask设置相关属性
            if (copyIDMaskSettings && IsIDMaskProperty(propName)) return true;
            
            // UV TileDiscard设置相关属性
            if (copyUVTileDiscardSettings && IsUVTileDiscardProperty(propName)) return true;
            
            // Stencil设置相关属性
            if (copyStencilSettings && IsStencilProperty(propName)) return true;
            
            // 渲染设置相关属性
            if (copyRenderSettings && IsRenderProperty(propName)) return true;
            
            // 光照烘培设置相关属性
            if (copyLightmapSettings && IsLightmapProperty(propName)) return true;
            
            // 镶嵌设置相关属性
            if (copyTessellationSettings && IsTessellationProperty(propName)) return true;
            
            // 优化设置相关属性
            if (copyOptimizationSettings && IsOptimizationProperty(propName)) return true;
            
            return false;
        }
        
        // 各种属性类别判断方法
        private bool IsBasicProperty(string propName)
        {
            return propName.Contains("_MainTex") || propName.Contains("_Color") || propName.Contains("_Cutoff");
        }
        
        private bool IsColorProperty(string propName)
        {
            return propName.Contains("Color") || propName.Contains("_Tint");
        }
        
        private bool IsMainColorAlphaProperty(string propName)
        {
            return propName.Contains("_MainTex") || propName.Contains("_Color") || propName.Contains("Alpha");
        }
        
        private bool IsEmissionProperty(string propName)
        {
            return propName.Contains("Emission") || propName.Contains("_EmissionMap");
        }
        
        private bool IsNormalProperty(string propName)
        {
            return propName.Contains("Normal") || propName.Contains("_BumpMap") || propName.Contains("_BumpScale") ||
                   propName.Contains("_DetailNormalMap") || propName.Contains("_DetailNormalMapScale") ||
                   propName.Contains("_NormalMapSpace") || propName.Contains("_NormalMapUV") ||
                   propName.Contains("_NormalMapBlend") || propName.Contains("_NormalMapStrength") ||
                   propName.Contains("_PackedMap") || propName.Contains("_NormalMapMode") || propName.Contains("_NormalMapChannel");
        }
        
        private bool IsReflectionProperty(string propName)
        {
            return propName.Contains("Reflection") || propName.Contains("_Cube") || propName.Contains("Metallic");
        }
        
        private bool IsRenderProperty(string propName)
        {
            return propName.Contains("_SrcBlend") || propName.Contains("_DstBlend") || propName.Contains("_ZWrite") || propName.Contains("_ZTest");
        }
        
        private bool IsOutlineProperty(string propName)
        {
            return propName.Contains("Outline") || propName.Contains("_OutlineWidth");
        }
        
        private bool IsParallaxProperty(string propName)
        {
            return propName.Contains("Parallax") || propName.Contains("_ParallaxMap");
        }
        
        private bool IsAudioLinkProperty(string propName)
        {
            return propName.Contains("AudioLink") || propName.Contains("_AudioTexture");
        }
        
        private bool IsDissolveProperty(string propName)
        {
            return propName.Contains("Dissolve") || propName.Contains("_DissolveMap");
        }
        
        private bool IsIDMaskProperty(string propName)
        {
            return propName.Contains("IDMask") || propName.Contains("_Mask");
        }
        
        // 新增的属性判断方法
        private bool IsLightingProperty(string propName)
        {
            return propName.Contains("Lighting") || propName.Contains("_Lighting") || propName.Contains("_LightMap") ||
                   propName.Contains("_LightDirection") || propName.Contains("_LightColor") || propName.Contains("_LightIntensity") ||
                   propName.Contains("_AmbientColor") || propName.Contains("_DirectionalLight") || propName.Contains("_PointLight") ||
                   propName.Contains("_SpotLight") || propName.Contains("_LightAttenuation") || propName.Contains("_LightFalloff");
        }
        
        private bool IsUVProperty(string propName)
        {
            return propName.Contains("_MainTex_ST") || propName.Contains("_UV") || propName.Contains("Tiling") || propName.Contains("Offset") ||
                   propName.Contains("_UVSec") || propName.Contains("_UVAnimation") || propName.Contains("_UVScroll") ||
                   propName.Contains("_UVRotation") || propName.Contains("_UVDistortion") || propName.Contains("_UVScale") ||
                   propName.Contains("_UVOffset") || propName.Contains("_UVTiling") || propName.Contains("_UVMode");
        }
        
        private bool IsVRChatProperty(string propName)
        {
            return propName.Contains("VRChat") || propName.Contains("_VRC") || propName.Contains("_Avatar") ||
                   propName.Contains("_VRCFallback") || propName.Contains("_VRCShader") || propName.Contains("_VRChatMirrorMode") ||
                   propName.Contains("_VRChatCameraMode") || propName.Contains("_VRChatMirrorReflection") || propName.Contains("_VRChatScreenSpace");
        }
        
        private bool IsShadowProperty(string propName)
        {
            return propName.Contains("Shadow") || propName.Contains("_Shadow") ||
                   propName.Contains("_ShadowStrength") || propName.Contains("_ShadowLift") || propName.Contains("_ShadowColor") ||
                   propName.Contains("_ShadowColorTex") || propName.Contains("_ShadowBorder") || propName.Contains("_ShadowBlur") ||
                   propName.Contains("_ShadowReceive") || propName.Contains("_ShadowCast") || propName.Contains("_ShadowNormalBias") ||
                   propName.Contains("_ShadowBias") || propName.Contains("_ShadowNearPlane") || propName.Contains("_ShadowFarPlane") ||
                   propName.Contains("_ShadowMode") || propName.Contains("_ShadowMask") || propName.Contains("_ShadowMaskMap");
        }
        
        private bool IsRimShadeProperty(string propName)
        {
            return propName.Contains("RimShade") || propName.Contains("_RimShade") || 
                   propName.Contains("_RimShadeColor") || propName.Contains("_RimShadeMap") || propName.Contains("_RimShadeMask") ||
                   propName.Contains("_RimShadeWidth") || propName.Contains("_RimShadeBlur") || propName.Contains("_RimShadeFresnelPower") ||
                   propName.Contains("_RimShadeInvert") || propName.Contains("_RimShadeMode") || propName.Contains("_RimShadeNormalMapInfluence");
        }
        
        private bool IsNormalReflectionProperty(string propName)
        {
            return propName.Contains("NormalReflection") || propName.Contains("_NormalReflection") || (propName.Contains("Normal") && propName.Contains("Reflection"));
        }
        
        private bool IsBacklightProperty(string propName)
        {
            return propName.Contains("Backlight") || propName.Contains("_Backlight") || propName.Contains("_BackLight") ||
                   propName.Contains("_BacklightMap") || propName.Contains("_BacklightColor") || propName.Contains("_BacklightStrength") ||
                   propName.Contains("_BacklightMask") || propName.Contains("_BacklightNormalStrength") || propName.Contains("_BacklightViewStrength") ||
                   propName.Contains("_BacklightReceiveShadow") || propName.Contains("_BacklightBackfaceMask") || propName.Contains("_BacklightDirectivity") ||
                   propName.Contains("_BacklightViewDirectivity");
        }
        
        private bool IsMatCapProperty(string propName)
        {
            return propName.Contains("MatCap") || propName.Contains("_MatCap") || propName.Contains("_SphereAdd") ||
                   propName.Contains("_MatCapColor") || propName.Contains("_MatCapMask") || propName.Contains("_MatCapStrength") ||
                   propName.Contains("_MatCapNormal") || propName.Contains("_MatCapBlur") || propName.Contains("_MatCapBlendMode") ||
                   propName.Contains("_MatCapBorder") || propName.Contains("_MatCapDistortion") || propName.Contains("_MatCapRotation") ||
                   propName.Contains("_MatCapEmission") || propName.Contains("_MatCapReplace") || propName.Contains("_MatCap2nd") ||
                   propName.Contains("_MatCap2ndColor") || propName.Contains("_MatCap2ndMask") || propName.Contains("_MatCap2ndStrength");
        }
        
        private bool IsRimLightProperty(string propName)
        {
            return propName.Contains("RimLight") || propName.Contains("_RimLight") || propName.Contains("_RimLighting") ||
                   propName.Contains("_RimFresnelPower") || propName.Contains("_RimLift") || propName.Contains("_RimWidth") ||
                   propName.Contains("_RimSharpness") || propName.Contains("_RimEdgeSoftness") || propName.Contains("_RimShadowToggle") ||
                   propName.Contains("_RimShadowMask") || propName.Contains("_RimTex") || propName.Contains("_RimTexPanSpeed") ||
                   propName.Contains("_RimHueShiftEnabled") || propName.Contains("_RimHueShiftSpeed") || propName.Contains("_RimLightMode");
        }
        
        private bool IsGlitterProperty(string propName)
        {
            return propName.Contains("Glitter") || propName.Contains("_Glitter") ||
                   propName.Contains("_GlitterMap") || propName.Contains("_GlitterMask") || propName.Contains("_GlitterColor") ||
                   propName.Contains("_GlitterColorMap") || propName.Contains("_GlitterSpeed") || propName.Contains("_GlitterBrightness") ||
                   propName.Contains("_GlitterAngleRange") || propName.Contains("_GlitterMinBrightness") || propName.Contains("_GlitterBias") ||
                   propName.Contains("_GlitterContrast") || propName.Contains("_GlitterSize") || propName.Contains("_GlitterFrequency") ||
                   propName.Contains("_GlitterJitter") || propName.Contains("_GlitterHueShiftEnabled") || propName.Contains("_GlitterHueShiftSpeed") ||
                   propName.Contains("_GlitterRandomColors") || propName.Contains("_GlitterTextureRotation") || propName.Contains("_GlitterUVMode");
        }
        
        private bool IsDistanceFadeProperty(string propName)
        {
            return propName.Contains("DistanceFade") || propName.Contains("_DistanceFade") || propName.Contains("_FadeDistance");
        }
        
        private bool IsUVTileDiscardProperty(string propName)
        {
            return propName.Contains("UVTileDiscard") || propName.Contains("_UVTileDiscard") || propName.Contains("TileDiscard");
        }
        
        private bool IsStencilProperty(string propName)
        {
            return propName.Contains("Stencil") || propName.Contains("_Stencil") || propName.Contains("_StencilRef");
        }
        
        private bool IsLightmapProperty(string propName)
        {
            return propName.Contains("Lightmap") || propName.Contains("_Lightmap") || propName.Contains("_LightMap");
        }
        
        private bool IsTessellationProperty(string propName)
        {
            return propName.Contains("Tessellation") || propName.Contains("_Tessellation") || propName.Contains("_Tess");
        }
        
        private bool IsOptimizationProperty(string propName)
        {
            return propName.Contains("Optimization") || propName.Contains("_Optimization") || propName.Contains("_LOD");
        }
        
        // 复制所有未知属性（通用复制方法）
        private int CopyAllUnknownProperties()
        {
            int copied = 0;
            
            try
            {
                Debug.Log("[材质复制] 开始通用属性复制，检测所有材质属性...");
                
                // 获取源材质着色器的所有属性
                Shader sourceShader = sourceMaterial.shader;
                if (sourceShader == null)
                {
                    Debug.LogWarning("[材质复制] 源材质没有着色器");
                    return 0;
                }
                
                // 使用反射获取材质的所有属性
                var sourceProps = new UnityEngine.MaterialPropertyBlock();
                
                // 遍历着色器的所有属性
                int propertyCount = ShaderUtil.GetPropertyCount(sourceShader);
                Debug.Log($"[材质复制] 源着色器共有 {propertyCount} 个属性");
                
                for (int i = 0; i < propertyCount; i++)
                {
                    try
                    {
                        string propName = ShaderUtil.GetPropertyName(sourceShader, i);
                        ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(sourceShader, i);
                        
                        // 检查目标材质是否有这个属性
                        if (!targetMaterial.HasProperty(propName))
                        {
                            continue;
                        }
                        
                        bool success = false;
                        
                        switch (propType)
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Color colorValue = sourceMaterial.GetColor(propName);
                                    targetMaterial.SetColor(propName, colorValue);
                                    Debug.Log($"[材质复制] 复制颜色属性 {propName}: {colorValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Vector:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Vector4 vectorValue = sourceMaterial.GetVector(propName);
                                    targetMaterial.SetVector(propName, vectorValue);
                                    Debug.Log($"[材质复制] 复制向量属性 {propName}: {vectorValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    float floatValue = sourceMaterial.GetFloat(propName);
                                    targetMaterial.SetFloat(propName, floatValue);
                                    Debug.Log($"[材质复制] 复制浮点属性 {propName}: {floatValue}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    Texture textureValue = sourceMaterial.GetTexture(propName);
                                    Vector2 offset = sourceMaterial.GetTextureOffset(propName);
                                    Vector2 scale = sourceMaterial.GetTextureScale(propName);
                                    
                                    targetMaterial.SetTexture(propName, textureValue);
                                    targetMaterial.SetTextureOffset(propName, offset);
                                    targetMaterial.SetTextureScale(propName, scale);
                                    
                                    Debug.Log($"[材质复制] 复制纹理属性 {propName}: {(textureValue != null ? textureValue.name : "null")}, 偏移: {offset}, 缩放: {scale}");
                                    success = true;
                                }
                                break;
                                
                            case ShaderUtil.ShaderPropertyType.Int:
                                if (sourceMaterial.HasProperty(propName))
                                {
                                    int intValue = sourceMaterial.GetInt(propName);
                                    targetMaterial.SetInt(propName, intValue);
                                    Debug.Log($"[材质复制] 复制整数属性 {propName}: {intValue}");
                                    success = true;
                                }
                                break;
                        }
                        
                        if (success)
                        {
                            copied++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[材质复制] 复制属性 {i} 时出错: {e.Message}");
                    }
                }
                
                // 复制材质的全局属性
                try
                {
                    // 复制着色器关键字
                    string[] sourceKeywords = sourceMaterial.shaderKeywords;
                    if (sourceKeywords != null && sourceKeywords.Length > 0)
                    {
                        targetMaterial.shaderKeywords = (string[])sourceKeywords.Clone();
                        Debug.Log($"[材质复制] 复制了 {sourceKeywords.Length} 个着色器关键字: [{string.Join(", ", sourceKeywords)}]");
                        copied += sourceKeywords.Length;
                    }
                    
                    // 复制渲染队列
                    if (sourceMaterial.renderQueue != targetMaterial.renderQueue)
                    {
                        targetMaterial.renderQueue = sourceMaterial.renderQueue;
                        Debug.Log($"[材质复制] 复制渲染队列: {sourceMaterial.renderQueue}");
                        copied++;
                    }
                    
                    // 复制全局照明标志
                    if (sourceMaterial.globalIlluminationFlags != targetMaterial.globalIlluminationFlags)
                    {
                        targetMaterial.globalIlluminationFlags = sourceMaterial.globalIlluminationFlags;
                        Debug.Log($"[材质复制] 复制全局照明标志: {sourceMaterial.globalIlluminationFlags}");
                        copied++;
                    }
                    
                    // 复制双面全局照明
                    if (sourceMaterial.doubleSidedGI != targetMaterial.doubleSidedGI)
                    {
                        targetMaterial.doubleSidedGI = sourceMaterial.doubleSidedGI;
                        Debug.Log($"[材质复制] 复制双面全局照明: {sourceMaterial.doubleSidedGI}");
                        copied++;
                    }
                    
                    // 复制启用GPU实例化
                    if (sourceMaterial.enableInstancing != targetMaterial.enableInstancing)
                    {
                        targetMaterial.enableInstancing = sourceMaterial.enableInstancing;
                        Debug.Log($"[材质复制] 复制GPU实例化: {sourceMaterial.enableInstancing}");
                        copied++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[材质复制] 复制全局属性时出错: {e.Message}");
                }
                
                Debug.Log($"[材质复制] 通用属性复制完成，共复制 {copied} 个属性");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[材质复制] 通用属性复制出错: {e.Message}");
            }
            
            return copied;
        }
    }
}