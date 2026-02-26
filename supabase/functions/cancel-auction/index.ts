import { corsHeaders } from "../_shared/cors.ts";
import { authenticateRequest, createAdminClient } from "../_shared/auth.ts";

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const adminClient = createAdminClient();

    const { user, errorResponse } = await authenticateRequest(req, adminClient);
    if (errorResponse) return errorResponse;

    const { item_id } = await req.json();

    if (!item_id) {
      return new Response(JSON.stringify({ error: "item_id is required" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 1. 경매 아이템 조회
    const { data: item, error: itemError } = await adminClient
      .from("items")
      .select("id, seller_id, name, status, current_bid")
      .eq("id", item_id)
      .single();

    if (itemError || !item) {
      return new Response(JSON.stringify({ error: "Item not found" }), {
        status: 404,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 2. 본인 아이템인지 확인
    if (item.seller_id !== user.id) {
      return new Response(JSON.stringify({ error: "Not your auction" }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 3. active 상태인지 확인
    if (item.status !== "active") {
      return new Response(
        JSON.stringify({ error: "Auction is not active (already ended or cancelled)" }),
        {
          status: 400,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        }
      );
    }

    // 4. 현재 최고 입찰자 조회
    const { data: topBid } = await adminClient
      .from("bids")
      .select("bidder_id, amount")
      .eq("item_id", item_id)
      .order("amount", { ascending: false })
      .limit(1)
      .single();

    // 5. 최고 입찰자에게 골드 환불
    if (topBid) {
      const { data: bidderProfile } = await adminClient
        .from("profiles")
        .select("gold")
        .eq("id", topBid.bidder_id)
        .single();

      if (bidderProfile) {
        await adminClient
          .from("profiles")
          .update({ gold: bidderProfile.gold + topBid.amount })
          .eq("id", topBid.bidder_id);

        // 입찰자에게 취소 알림
        await adminClient.from("notifications").insert({
          user_id: topBid.bidder_id,
          type: "cancelled",
          item_id: item_id,
          message: `'${item.name}' 경매가 판매자에 의해 취소되었습니다. ${topBid.amount} gold가 환불되었습니다.`,
        });
      }
    }

    // 6. items.status = 'cancelled'
    await adminClient
      .from("items")
      .update({ status: "cancelled" })
      .eq("id", item_id);

    // 7. 판매자 인벤토리 복원: listed → owned
    await adminClient
      .from("inventory")
      .update({ status: "owned" })
      .eq("item_id", item_id)
      .eq("owner_id", user.id)
      .eq("status", "listed");

    // 8. 판매자에게 취소 완료 알림
    await adminClient.from("notifications").insert({
      user_id: user.id,
      type: "cancelled",
      item_id: item_id,
      message: `'${item.name}' 경매를 취소했습니다. 아이템이 인벤토리로 반환되었습니다.`,
    });

    return new Response(
      JSON.stringify({ success: true }),
      {
        status: 200,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      }
    );
  } catch (err) {
    return new Response(JSON.stringify({ error: String(err) }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
