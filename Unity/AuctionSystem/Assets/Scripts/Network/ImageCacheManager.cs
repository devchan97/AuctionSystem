using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AuctionSystem.Network
{
    public class ImageCacheManager : MonoBehaviour
    {
        public static ImageCacheManager Instance { get; private set; }

        private readonly Dictionary<string, Texture2D> _cache = new();
        private readonly Dictionary<string, List<Action<Texture2D>>> _pending = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadImage(string url, Action<Texture2D> onLoaded)
        {
            if (string.IsNullOrEmpty(url))
            {
                onLoaded?.Invoke(null);
                return;
            }

            if (_cache.TryGetValue(url, out var cached))
            {
                onLoaded?.Invoke(cached);
                return;
            }

            if (_pending.TryGetValue(url, out var callbacks))
            {
                callbacks.Add(onLoaded);
                return;
            }

            _pending[url] = new List<Action<Texture2D>> { onLoaded };
            StartCoroutine(DownloadCoroutine(url));
        }

        public void Evict(string url)
        {
            if (_cache.TryGetValue(url, out var tex))
            {
                Destroy(tex);
                _cache.Remove(url);
            }
        }

        public void ClearAll()
        {
            foreach (var tex in _cache.Values)
                if (tex != null) Destroy(tex);
            _cache.Clear();
        }

        private IEnumerator DownloadCoroutine(string url)
        {
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            Texture2D texture = null;

            if (req.result == UnityWebRequest.Result.Success)
            {
                texture = DownloadHandlerTexture.GetContent(req);
                _cache[url] = texture;
            }
            else
            {
                Debug.LogWarning($"[ImageCacheManager] 다운로드 실패: {url} — {req.error}");
            }

            if (_pending.TryGetValue(url, out var callbacks))
            {
                _pending.Remove(url);
                foreach (var cb in callbacks)
                    cb?.Invoke(texture);
            }
        }
    }
}
