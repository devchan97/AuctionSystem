using System;
using System.Collections;
using System.Collections.Generic;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace AuctionSystem.Auction
{
    public class BidHandler : MonoBehaviour
    {
        public static BidHandler Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void PlaceBid(string itemId, long amount, Action<string> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(PlaceBidCoroutine(itemId, amount, onSuccess, onError));
        }

        private IEnumerator PlaceBidCoroutine(string itemId, long amount, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.FunctionsUrl}/place-bid";
            var req = new PlaceBidRequest { item_id = itemId, amount = amount };
            string json = JsonUtility.ToJson(req);

            yield return SupabaseManager.Instance.Post(url, json,
                onSuccess: response =>
                {
                    StartCoroutine(SupabaseManager.Instance.LoadProfile()); // gold 갱신
                    onSuccess?.Invoke(response);
                },
                onError: err => onError?.Invoke(AuctionUtils.ParseError(err))
            );
        }

        public void Buyout(string itemId, Action<string> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(BuyoutCoroutine(itemId, onSuccess, onError));
        }

        private IEnumerator BuyoutCoroutine(string itemId, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.FunctionsUrl}/buyout";
            var req = new BuyoutRequest { item_id = itemId };
            string json = JsonUtility.ToJson(req);

            yield return SupabaseManager.Instance.Post(url, json,
                onSuccess: response =>
                {
                    StartCoroutine(SupabaseManager.Instance.LoadProfile());
                    onSuccess?.Invoke(response);
                },
                onError: err => onError?.Invoke(AuctionUtils.ParseError(err))
            );
        }

        public void ListItem(ListItemRequest request, Action<string> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(ListItemCoroutine(request, onSuccess, onError));
        }

        private IEnumerator ListItemCoroutine(ListItemRequest request, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.FunctionsUrl}/list-item";
            string json = JsonUtility.ToJson(request);

            yield return SupabaseManager.Instance.Post(url, json,
                onSuccess: response => onSuccess?.Invoke(response),
                onError: err => onError?.Invoke(AuctionUtils.ParseError(err))
            );
        }

        // 내 인벤토리 조회
        public void GetInventory(Action<InventoryItem[]> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(GetInventoryCoroutine(onSuccess, onError));
        }

        private IEnumerator GetInventoryCoroutine(Action<InventoryItem[]> onSuccess, Action<string> onError)
        {
            string userId = SupabaseManager.Instance.Session.user.id;
            // owner_id 필터 + items 테이블 JOIN으로 name/category/image_url 가져오기
            string url = $"{SupabaseConfig.RestUrl}/inventory?owner_id=eq.{userId}&select=id,owner_id,status,acquired_at,items(id,name,category,image_url)&order=acquired_at.desc";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            req.SetRequestHeader("Authorization", "Bearer " + SupabaseManager.Instance.Session.access_token);
            req.SetRequestHeader("Accept-Profile", "public");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler.text;
                // JsonUtility는 배열 직접 파싱 불가 → 래퍼 사용
                string wrapped = $"{{\"items\":{body}}}";
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(wrapped);
                onSuccess?.Invoke(wrapper?.items ?? new InventoryItem[0]);
            }
            else
            {
                onError?.Invoke(AuctionUtils.ParseError(req.downloadHandler.text));
            }
        }

        [Serializable]
        private class InventoryWrapper { public InventoryItem[] items; }

        public void CancelAuction(string itemId, Action<string> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(CancelAuctionCoroutine(itemId, onSuccess, onError));
        }

        private IEnumerator CancelAuctionCoroutine(string itemId, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.FunctionsUrl}/cancel-auction";
            var req = new CancelAuctionRequest { item_id = itemId };
            string json = JsonUtility.ToJson(req);

            yield return SupabaseManager.Instance.Post(url, json,
                onSuccess: response =>
                {
                    StartCoroutine(SupabaseManager.Instance.LoadProfile());
                    onSuccess?.Invoke(response);
                },
                onError: err => onError?.Invoke(AuctionUtils.ParseError(err))
            );
        }

        // 인벤토리 없이 직접 경매 등록 (REST API)
        public void ListItemDirect(DirectListItemRequest request, Action<string> onSuccess, Action<string> onError)
        {
            if (!SupabaseManager.Instance.IsLoggedIn) { onError?.Invoke("로그인이 필요합니다."); return; }
            StartCoroutine(ListItemDirectCoroutine(request, onSuccess, onError));
        }

        private IEnumerator ListItemDirectCoroutine(DirectListItemRequest request, Action<string> onSuccess, Action<string> onError)
        {
            // seller_id, status, current_bid 자동 설정
            request.seller_id   = SupabaseManager.Instance.Session.user.id;
            request.status      = "active";
            request.current_bid = 0;

            string url  = $"{SupabaseConfig.RestUrl}/items";
            string json = JsonUtility.ToJson(request);

            // 0인 buyout_price는 JSON에서 null로 보내야 DB 컬럼이 NULL로 저장됨
            if (request.buyout_price <= 0)
                json = json.Replace("\"buyout_price\":0", "\"buyout_price\":null");

            // 빈 image_url 제거
            if (string.IsNullOrEmpty(request.image_url))
                json = json.Replace("\"image_url\":\"\"", "\"image_url\":null");

            var headers = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Prefer", "return=representation" }  // 삽입된 행 반환
            };

            yield return SupabaseManager.Instance.Post(url, json,
                onSuccess: response => onSuccess?.Invoke(response),
                onError: err => onError?.Invoke(AuctionUtils.ParseError(err)),
                extraHeaders: headers
            );
        }

    }
}
