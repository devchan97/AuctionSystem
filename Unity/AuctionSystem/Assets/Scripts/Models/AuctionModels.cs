using System;

namespace AuctionSystem.Models
{
    [Serializable]
    public class UserSession
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int    expires_in;   // 유효 기간(초), 기본 3600
        public long   expires_at;   // 만료 Unix timestamp
        public UserInfo user;
    }

    [Serializable]
    public class UserInfo
    {
        public string id;
        public string email;
    }

    [Serializable]
    public class Profile
    {
        public string id;
        public string username;
        public long gold;
        public string avatar_url;
        public string created_at;
    }

    [Serializable]
    public class AuctionItem
    {
        public string id;
        public string name;
        public string description;
        public string category;
        public long start_price;
        public long current_bid;
        public long buyout_price;
        public string status;       // active | sold | expired
        public string ends_at;
        public string seller_id;
        public string image_url;
        public string created_at;
    }

    [Serializable]
    public class Bid
    {
        public string id;
        public string item_id;
        public string bidder_id;
        public long amount;
        public string created_at;
    }

    [Serializable]
    public class Notification
    {
        public string id;
        public string user_id;
        public string type;
        public string message;
        public bool is_read;
        public string created_at;
    }

    [Serializable]
    public class ApiError
    {
        public string error;
        public string error_code;
        public string error_description;
        public string message;
        public string msg;
        public string code;
    }

    [Serializable]
    public class PlaceBidRequest
    {
        public string item_id;
        public long amount;
    }

    [Serializable]
    public class BuyoutRequest
    {
        public string item_id;
    }

    [Serializable]
    public class CancelAuctionRequest
    {
        public string item_id;
    }

    [Serializable]
    public class InventoryItemRef
    {
        public string id;
        public string name;
        public string category;
        public string image_url;
    }

    [Serializable]
    public class InventoryItem
    {
        public string id;
        public string owner_id;
        public string item_id;          // AuctionManager 방식 호환
        public string item_name;        // AuctionManager 방식 호환
        public string item_category;    // AuctionManager 방식 호환
        public string item_image_url;   // AuctionManager 방식 호환
        public string status;           // owned | listed
        public string acquired_at;
        public InventoryItemRef items;  // BidHandler JOIN 방식 (SELECT에서 직접)

        // 어느 방식으로 채워지든 이름/카테고리를 통일해서 반환
        public string DisplayName     => items?.name     ?? item_name     ?? "-";
        public string DisplayCategory => items?.category ?? item_category ?? "-";
        public string DisplayImageUrl => items?.image_url ?? item_image_url;
    }

    [Serializable]
    public class ListItemRequest
    {
        public string inventory_item_id;  // 필수 (인벤토리 등록)
        public string name;
        public string description;
        public string category;
        public long start_price;
        public long buyout_price;
        public int duration_hours;
    }

    // 인벤토리 없이 직접 경매 등록 (REST API — items 테이블 직접 insert)
    [Serializable]
    public class DirectListItemRequest
    {
        public string seller_id;
        public string name;
        public string description;
        public string image_url;
        public string category;
        public long start_price;
        public long current_bid;   // 0
        public long buyout_price;  // 0이면 서버에서 null 처리
        public string ends_at;     // ISO8601
        public string status;      // "active"
    }
}
