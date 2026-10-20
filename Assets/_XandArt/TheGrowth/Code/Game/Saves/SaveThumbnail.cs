using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheGrowth.Game.Saves
{
    /// <summary>
    /// Превью сохранения: камеры экрана по глубине рендерятся в уменьшенную текстуру (без UI поверх экрана — экран
    /// загрузки и меню в превью не попадают), PNG. Синхронно: снимок совпадает с моментом снятия состояния. Без графики
    /// (batchmode -nographics) или без камер — null.
    /// </summary>
    public static class SaveThumbnail
    {
        public const int Width = 320;

        public static byte[] Capture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Screen.width <= 0 || Screen.height <= 0)
                return null;

            var cameras = new List<Camera>();
            foreach (var camera in Camera.allCameras)
            {
                if (camera.targetTexture == null)
                    cameras.Add(camera);
            }

            if (cameras.Count == 0)
                return null;

            cameras.Sort((a, b) => a.depth.CompareTo(b.depth));
            var height = Mathf.Max(1, Mathf.RoundToInt(Width * (float)Screen.height / Screen.width));
            var target = RenderTexture.GetTemporary(Width, height, 24);
            var active = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                foreach (var camera in cameras)
                {
                    camera.targetTexture = target;
                    try
                    {
                        camera.Render();
                    }
                    finally
                    {
                        camera.targetTexture = null;
                    }
                }

                RenderTexture.active = target;
                texture = new Texture2D(Width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Width, height), 0, 0);
                texture.Apply(false);
                return texture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                if (texture != null)
                    Object.Destroy(texture);
            }
        }
    }
}
