using System;
using System.Collections;
using AuctionSystem.Models;
using AuctionSystem.Network;
using UnityEngine;

namespace AuctionSystem.Auction
{
    public class AuctionManager : MonoBehaviour
    {
        public static AuctionManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // category null이면 전체, sortBy: "created_at" | "current_bid" | "ends_at"
        public void GetActiveItems(string category, string sortBy, Action<AuctionItem[]> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetActiveItemsCoroutine(category, sortBy, onSuccess, onError));
        }

        private IEnumerator GetActiveItemsCoroutine(string category, string sortBy, Action<AuctionItem[]> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.RestUrl}/items?status=eq.active&select=id,name,description,category,image_url,start_price,current_bid,buyout_price,seller_id,status,ends_at,created_at";

            if (!string.IsNullOrEmpty(category))
                url += $"&category=eq.{Uri.EscapeDataString(category)}";

            url += sortBy switch
            {
                "current_bid" => "&order=current_bid.desc",
                "ends_at"     => "&order=ends_at.asc",
                _             => "&order=created_at.desc",
            };

            url += "&limit=50";

            yield return SupabaseManager.Instance.Get(url,
                onSuccess: json =>
                {
                    var wrapper = JsonUtility.FromJson<ItemListWrapper>("{\"items\":" + json + "}");
                    onSuccess?.Invoke(wrapper.items ?? new AuctionItem[0]);
                },
                onError: err => onError?.Invoke(err),
                useAuth: false
            );
        }

        public void GetItem(string itemId, Action<AuctionItem> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetItemCoroutine(itemId, onSuccess, onError));
        }

        private IEnumerator GetItemCoroutine(string itemId, Action<AuctionItem> onSuccess, Action<string> onError)
        {
            string url = $"{SupabaseConfig.RestUrl}/items?id=eq.{itemId}&limit=1";
            yield return SupabaseManager.Instance.Get(url,
                onSuccess: json =>
                {
                    var wrapper = JsonUtility.FromJson<ItemListWrapper>("{\"items\":" + json + "}");
                    if (wrapper.items != null && wrapper.items.Length > 0)
                        onSuccess?.Invoke(wrapper.items[0]);
                    else
                        onError?.Invoke("아이템을 찾을 수 없습니다.");
                },
                onError: err => onError?.Invoke(err),
                useAuth: false
            );
        }

        public void GetMyInventory(Action<InventoryItem[]> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetMyInventoryCoroutine(onSuccess, onError));
        }

        private IEnumerator GetMyInventoryCoroutine(Action<InventoryItem[]> onSuccess, Action<string> onError)
        {
            // PostgREST join SELECT 제한으로 inventory → items 두 번 조회 후 병합
            string ownerId = SupabaseManager.Instance.Session.user.id;
            string url = $"{SupabaseConfig.RestUrl}/inventory?owner_id=eq.{ownerId}&status=eq.owned&order=acquired_at.desc&limit=50";
            InventoryRow[] rows = null;

            yield return SupabaseManager.Instance.Get(url,
                onSuccess: json =>
                {
                    var w = JsonUtility.FromJson<InvRowWrapper>("{\"rows\":" + json + "}");
                    rows = w.rows ?? new InventoryRow[0];
                },
                onError: err => onError?.Invoke(err),
                useAuth: true
            );

            if (rows == null) yield break;
            if (rows.Length == 0) { onSuccess?.Invoke(new InventoryItem[0]); yield break; }

            var itemIds = new System.Text.StringBuilder();
            foreach (var r in rows)
            {
                if (itemIds.Length > 0) itemIds.Append(",");
                itemIds.Append(r.item_id);
            }
            string itemsUrl = $"{SupabaseConfig.RestUrl}/items?id=in.({itemIds})&select=id,name,category,image_url";

            AuctionItem[] fetchedItems = null;
            yield return SupabaseManager.Instance.Get(itemsUrl,
                onSuccess: json =>
                {
                    var w = JsonUtility.FromJson<ItemListWrapper>("{\"items\":" + json + "}");
                    fetchedItems = w.items ?? new AuctionItem[0];
                },
                onError: err => onError?.Invoke(err),
                useAuth: false
            );

            if (fetchedItems == null) yield break;

            var itemMap = new System.Collections.Generic.Dictionary<string, AuctionItem>();
            foreach (var item in fetchedItems)
                itemMap[item.id] = item;

            var result = new InventoryItem[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                itemMap.TryGetValue(r.item_id, out var it);
                result[i] = new InventoryItem
                {
                    id              = r.id,
                    item_id         = r.item_id,
                    item_name       = it?.name ?? "Unknown",
                    item_category   = it?.category ?? "Misc",
                    item_image_url  = it?.image_url,
                    status          = r.status,
                    acquired_at     = r.acquired_at,
                };
            }
            onSuccess?.Invoke(result);
        }

        [Serializable]
        private class ItemListWrapper { public AuctionItem[] items; }

        [Serializable]
        private class InventoryRow
        {
            public string id;
            public string item_id;
            public string status;
            public string acquired_at;
        }

        [Serializable]
        private class InvRowWrapper { public InventoryRow[] rows; }
    }
}
