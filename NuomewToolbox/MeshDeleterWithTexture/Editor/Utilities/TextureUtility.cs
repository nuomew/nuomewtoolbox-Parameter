using UnityEditor;
using UnityEngine;

namespace Gatosyocora.MeshDeleterWithTexture.Utilities
{
    public static class TextureUtility
    {
        public static RenderTexture CopyTexture2DToRenderTexture(Texture2D texture, Vector2Int textureSize, bool isLinearColorSpace = false)
        {
            if (texture == null)
            {
                Debug.LogError("CopyTexture2DToRenderTexture: Input texture is null");
                return null;
            }

            if (textureSize.x <= 0 || textureSize.y <= 0)
            {
                Debug.LogError($"CopyTexture2DToRenderTexture: Invalid texture size {textureSize}");
                return null;
            }

            RenderTexture renderTexture;

            try
            {
                if (isLinearColorSpace)
                    renderTexture = new RenderTexture(textureSize.x, textureSize.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                else
                    renderTexture = new RenderTexture(textureSize.x, textureSize.y, 0, RenderTextureFormat.ARGB32);

                renderTexture.enableRandomWrite = true;
                renderTexture.anisoLevel = texture.anisoLevel;
                renderTexture.mipMapBias = texture.mipMapBias;
                renderTexture.filterMode = texture.filterMode;
                renderTexture.wrapMode = texture.wrapMode;
                renderTexture.wrapModeU = texture.wrapModeU;
                renderTexture.wrapModeV = texture.wrapModeV;
                renderTexture.wrapModeW = texture.wrapModeW;
                
                // 确保RenderTexture正确创建
                if (!renderTexture.Create())
                {
                    Debug.LogError($"Failed to create RenderTexture with size {textureSize}");
                    return null;
                }

                // 使用Graphics.Blit复制纹理内容
                Graphics.Blit(texture, renderTexture);
                return renderTexture;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CopyTexture2DToRenderTexture failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 读み込んだ後の設定をおこなう
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static Texture2D GenerateTextureToEditting(Texture2D originTexture)
        {
            // 检查纹理是否可读，如果不可读则需要先创建可读版本
            if (!originTexture.isReadable)
            {
                // 对于不可读的纹理（如Crunch压缩），使用RenderTexture作为中介
                RenderTexture tempRT = RenderTexture.GetTemporary(originTexture.width, originTexture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(originTexture, tempRT);
                
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tempRT;
                
                Texture2D editTexture = new Texture2D(originTexture.width, originTexture.height, TextureFormat.ARGB32, false);
                editTexture.ReadPixels(new Rect(0, 0, originTexture.width, originTexture.height), 0, 0);
                editTexture.Apply();
                editTexture.name = originTexture.name;
                
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tempRT);
                
                return editTexture;
            }
            else
            {
                // 对于可读纹理，使用传统方法
                try
                {
                    Texture2D editTexture = new Texture2D(originTexture.width, originTexture.height, originTexture.format, false);
                    Graphics.CopyTexture(originTexture, 0, 0, editTexture, 0, 0);
                    editTexture.name = originTexture.name;
                    editTexture.Apply();
                    return editTexture;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Graphics.CopyTexture failed for texture {originTexture.name}: {e.Message}. Using fallback method.");
                    
                    // 回退方法：使用SetPixels
                    Texture2D editTexture = new Texture2D(originTexture.width, originTexture.height, TextureFormat.ARGB32, false);
                    editTexture.SetPixels(originTexture.GetPixels());
                    editTexture.Apply();
                    editTexture.name = originTexture.name;
                    return editTexture;
                }
            }
        }
    }
}