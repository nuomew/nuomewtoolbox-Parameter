/*
 * 材质复制方法扩展实现 - Material Copy Methods Extended Implementation
 * 功能：实现扩展材质属性的具体复制逻辑
 * 作者：诺喵工具箱
 * 用途：支持MaterialCopyWindow的扩展材质属性复制功能
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NyameauToolbox.Editor
{
    public partial class MaterialCopyWindow
    {
        // 轮廓设置复制
        private int CopyOutlineSettings()
        {
            string[] outlineProperties = {
                "_OutlineWidth", "_OutlineColor", "_OutlineMask", "_OutlineEmission",
                "_OutlineTexture", "_OutlineTextureColorRate", "_OutlineDistanceFade",
                "_OutlineMode", "_OutlineUVMode", "_OutlineWidthMode", "_OutlineColorMode",
                "_OutlineNormalMode", "_OutlineOnlyMode", "_OutlineFixWidth",
                "_OutlineVertexColorBlendRate", "_OutlineDeleteMesh", "_OutlineZWrite"
            };
            
            return CopyPropertiesByNames(outlineProperties);
        }
        
        // 视差设置复制
        private int CopyParallaxSettings()
        {
            string[] parallaxProperties = {
                "_ParallaxMap", "_Parallax", "_ParallaxOffset", "_ParallaxIterations",
                "_ParallaxShadows", "_ParallaxAffineSteps", "_ParallaxBias",
                "_ParallaxSteps", "_ParallaxAmplitude", "_ParallaxCenter",
                "_ParallaxDepthMapChannel", "_ParallaxUVDistortionStrength",
                "_ParallaxInternalMapLod", "_ParallaxInternalMapSamples"
            };
            
            return CopyPropertiesByNames(parallaxProperties);
        }
        
        // 距离淡化设置复制
        private int CopyDistanceFadeSettings()
        {
            string[] distanceFadeProperties = {
                "_DistanceFade", "_DistanceFadeColor", "_DistanceFadeMode",
                "_DistanceFadeMinDistance", "_DistanceFadeMaxDistance",
                "_DistanceFadeRimLighting", "_DistanceFadeRimLightingMask",
                "_DistanceFadeEmission", "_DistanceFadeEmissionMask",
                "_DistanceFadeVertexColorLinearSpace", "_DistanceFadeDesaturateToGrayscale"
            };
            
            return CopyPropertiesByNames(distanceFadeProperties);
        }
        
        // AudioLink设置复制
        private int CopyAudioLinkSettings()
        {
            string[] audioLinkProperties = {
                "_AudioLinkAnimToggle", "_AudioLinkAnim", "_AudioLinkAnimStrength",
                "_AudioLinkAnimBand", "_AudioLinkAnimDelay", "_AudioLinkAnimDecay",
                "_AudioLinkAnimThreshold", "_AudioLinkAnimPulse", "_AudioLinkAnimPulseDir",
                "_AudioLinkAnimPulseRotation", "_AudioLinkAnimPulseScale",
                "_AudioLinkEmissionBand", "_AudioLinkEmissionStrength", "_AudioLinkEmissionMap",
                "_AudioLinkRimBand", "_AudioLinkRimStrength", "_AudioLinkRimMap",
                "_AudioLinkGlitterBand", "_AudioLinkGlitterStrength", "_AudioLinkGlitterMap"
            };
            
            return CopyPropertiesByNames(audioLinkProperties);
        }
        
        // Dissolve设置复制
        private int CopyDissolveSettings()
        {
            string[] dissolveProperties = {
                "_DissolveType", "_DissolveMap", "_DissolveNoiseScale", "_DissolveDetailNoise",
                "_DissolveEdgeWidth", "_DissolveEdgeHardness", "_DissolveEdgeColor",
                "_DissolveEdgeEmission", "_DissolveEdgeGradient", "_DissolveAlpha",
                "_DissolveVertexColors", "_DissolveP2O", "_DissolveP2OWorldLocal",
                "_DissolveP2OEdgeLength", "_DissolveP2OClones", "_ContinuousDissolve",
                "_DissolveEmissionSide", "_DissolveEmission1Side", "_DissolveHueShiftEnabled",
                "_DissolveHueShiftSpeed", "_DissolveSharpness", "_DissolveTexIsSRGB"
            };
            
            return CopyPropertiesByNames(dissolveProperties);
        }
        
        // ID Mask设置复制
        private int CopyIDMaskSettings()
        {
            string[] idMaskProperties = {
                "_IDMask", "_IDMaskMap", "_IDMaskIndex", "_IDMaskAIndex",
                "_IDMaskBIndex", "_IDMaskCIndex", "_IDMaskDIndex",
                "_IDMaskEIndex", "_IDMaskFIndex", "_IDMaskGIndex", "_IDMaskHIndex",
                "_IDMaskAColor", "_IDMaskBColor", "_IDMaskCColor", "_IDMaskDColor",
                "_IDMaskEColor", "_IDMaskFColor", "_IDMaskGColor", "_IDMaskHColor",
                "_IDMaskAEmission", "_IDMaskBEmission", "_IDMaskCEmission", "_IDMaskDEmission",
                "_IDMaskEEmission", "_IDMaskFEmission", "_IDMaskGEmission", "_IDMaskHEmission"
            };
            
            return CopyPropertiesByNames(idMaskProperties);
        }
        
        // UV TileDiscard设置复制
        private int CopyUVTileDiscardSettings()
        {
            string[] uvTileDiscardProperties = {
                "_UVTileDiscard", "_UVTileDiscardRow", "_UVTileDiscardColumn",
                "_UVTileDiscardUV", "_UVTileDiscardAlpha", "_UVTileDiscardMode",
                "_UVTileDiscardSaved", "_UVTileDiscardArray", "_UVTileDiscardIndex",
                "_UVTileDiscardAnimated", "_UVTileDiscardAnimationSpeed",
                "_UVTileDiscardAnimationStartFrame", "_UVTileDiscardAnimationEndFrame"
            };
            
            return CopyPropertiesByNames(uvTileDiscardProperties);
        }
        
        // Stencil设置复制
        private int CopyStencilSettings()
        {
            string[] stencilProperties = {
                "_StencilRef", "_StencilReadMask", "_StencilWriteMask", "_StencilComp",
                "_StencilPass", "_StencilFail", "_StencilZFail", "_StencilType",
                "_StencilCompareFunction", "_StencilID", "_StencilOperation",
                "_OutlineStencilRef", "_OutlineStencilReadMask", "_OutlineStencilWriteMask",
                "_OutlineStencilComp", "_OutlineStencilPass", "_OutlineStencilFail", "_OutlineStencilZFail"
            };
            
            return CopyPropertiesByNames(stencilProperties);
        }
        
        // 渲染设置复制
        private int CopyRenderSettings()
        {
            string[] renderProperties = {
                "_Cull", "_ZWrite", "_ZTest", "_OffsetFactor", "_OffsetUnits",
                "_ColorMask", "_AlphaToMask", "_BlendOp", "_BlendOpAlpha",
                "_SrcBlend", "_DstBlend", "_SrcBlendAlpha", "_DstBlendAlpha",
                "_RenderQueue", "_RenderType", "_DisableBatching", "_ForceNoShadowCasting",
                "_IgnoreProjector", "_CanUseSpriteAtlas", "_PreviewType",
                "_BlendMode", "_Mode", "_Surface", "_Blend", "_AlphaClip",
                "_QueueOffset", "_QueueControl", "_ReceiveShadows"
            };
            
            return CopyPropertiesByNames(renderProperties);
        }
        
        // 光照烘培设置复制
        private int CopyLightmapSettings()
        {
            string[] lightmapProperties = {
                "_LightmapFlags", "_EnableLightmapping", "_LightmapEmissionFlagsProperty",
                "_LightmapEmissionProperty", "_LightmapEmissionColor", "_LightmapEmissionColorProperty",
                "_EmissionLM", "_EmissionRealtimeAnimated", "_EmissionBakedAnimated",
                "_EmissionLightingInfluence", "_BakedEmission", "_MainLightPosition",
                "_MainLightColor", "_AdditionalLightsCount", "_AdditionalLightsPosition",
                "_AdditionalLightsColor", "_AdditionalLightsAttenuation", "_AdditionalLightsSpotDir"
            };
            
            return CopyPropertiesByNames(lightmapProperties);
        }
        
        // 镶嵌设置复制（极高负载）
        private int CopyTessellationSettings()
        {
            string[] tessellationProperties = {
                "_TessellationUniform", "_TessellationEdgeLength", "_TessellationMaxDistance",
                "_TessellationDistanceFade", "_TessellationBias", "_TessellationMode",
                "_TessellationShapePreservation", "_TessellationBackFaceCullEpsilon",
                "_TessellationFactorMinDistance", "_TessellationFactorMaxDistance",
                "_TessellationFactorTriangleSize", "_TessellationSmoothNormals",
                "_TessellationObjectScale", "_TessellationGeoShaderMaxOutputVertices"
            };
            
            return CopyPropertiesByNames(tessellationProperties);
        }
        
        // 优化设置复制
        private int CopyOptimizationSettings()
        {
            string[] optimizationProperties = {
                "_OptimizeTabToggle", "_ShaderOptimizer", "_LockToInspector",
                "_Instancing", "_DoubleSidedGI", "_LODCrossFade", "_MotionVectorGenerationMode",
                "_PPDLodThreshold", "_PPDPrimitiveLength", "_PPDPrimitiveWidth",
                "_GeometryShaderMaxOutputVertices", "_GeometryShaderVariant",
                "_VertexShaderVariant", "_FragmentShaderVariant", "_ComputeShaderVariant",
                "_ShaderLOD", "_MaxLOD", "_MinLOD", "_GlobalIlluminationFlags"
            };
            
            return CopyPropertiesByNames(optimizationProperties);
        }
    }
}