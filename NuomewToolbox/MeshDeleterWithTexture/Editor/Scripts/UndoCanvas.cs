using UnityEditor;
using UnityEngine;

namespace Gatosyocora.MeshDeleterWithTexture
{
    public class UndoCanvas
    {
        private RenderTexture[] undoTextures;
        private int[][] undoBuffers;
        private int undoIndex = 0;
        private const int MAX_UNDO_COUNT = 10;

        public UndoCanvas()
        {
            undoTextures = new RenderTexture[MAX_UNDO_COUNT];
            undoBuffers = new int[MAX_UNDO_COUNT][];
            undoIndex = -1;
        }

        /// <summary>
        /// 履歴に追加する
        /// </summary>
        /// <param name="texture"></param>
        public void RegisterUndoTexture(RenderTexture texture, ComputeBuffer buffer)
        {
            if (texture == null)
            {
                Debug.LogError("RegisterUndoTexture: Input texture is null");
                return;
            }

            undoIndex++;
            if (undoIndex >= MAX_UNDO_COUNT) undoIndex = 0;
            
            var undoTexture = new RenderTexture(texture);
            
            // 确保RenderTexture正确创建
            if (!undoTexture.Create())
            {
                Debug.LogError("Failed to create undo RenderTexture");
                return;
            }
            
            try
            {
                Graphics.CopyTexture(texture, undoTexture);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Graphics.CopyTexture failed in RegisterUndoTexture: {e.Message}. Using Graphics.Blit as fallback.");
                Graphics.Blit(texture, undoTexture);
            }
            
            undoTextures[undoIndex] = undoTexture;
            var undoBuffer = new int[texture.width * texture.height];
            buffer.GetData(undoBuffer);
            undoBuffers[undoIndex] = undoBuffer;
        }

        /// <summary>
        /// 履歴を使って変更を1つ戻す
        /// </summary>
        /// <param name="previewTexture"></param>
        public void UndoPreviewTexture(ref RenderTexture previewTexture, ref ComputeBuffer buffer)
        {
            if (undoIndex == -1) return;

            var undoTexture = undoTextures[undoIndex];
            var undoBuffer = undoBuffers[undoIndex];
            undoIndex--;
            
            try
            {
                Graphics.CopyTexture(undoTexture, previewTexture);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Graphics.CopyTexture failed in UndoPreviewTexture: {e.Message}. Using Graphics.Blit as fallback.");
                Graphics.Blit(undoTexture, previewTexture);
            }
            
            buffer.SetData(undoBuffer);
        }

        public bool canUndo() => undoIndex > -1;
    }
}