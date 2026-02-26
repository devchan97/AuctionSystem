import { corsHeaders } from "../_shared/cors.ts";
import { authenticateRequest, createAdminClient } from "../_shared/auth.ts";
import { FEE_RATE } from "../_shared/constants.ts";

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const adminClient = createAdminClient();

    // JWT 검증
    const { user, errorResponse } = await authenticateRequest(req, adminClient);
    if (errorResponse) return errorResponse;

    const { item_id } = await req.json();
    if (!item_id) {
      return new Response(JSON.stringify({ error: "item_id is required" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 1. 아이템 조회
    const { data: item, error: itemError } = await adminClient
      .from("items")
      .select("id, seller_id, buyout_price, current_bid, status, ends_at, name")
      .eq("id", item_id)
      .single();

    if (itemError || !item) {
      return new Response(JSON.stringify({ error: "Item not found" }), {
        status: 404,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (item.status !== "active") {
      return new Response(JSON.stringify({ error: "Auction is not active" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (new Date(item.ends_at) <= new Date()) {
      return new Response(JSON.stringify({ error: "Auction has ended" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (!item.buyout_price) {
      return new Response(JSON.stringify({ error: "This item has no buyout price" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (item.seller_id === user.id) {
      return new Response(JSON.stringify({ error: "Cannot buy your own item" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 2. 구매자 gold 확인
    const { data: buyerProfile } = await adminClient
      .from("profiles")
      .select("gold")
      .eq("id", user.id)
      .single();

    if (!buyerProfile || buyerProfile.gold < item.buyout_price) {
      return new Response(JSON.stringify({ error: "Insufficient gold" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 3. 이전 최고 입찰자 확인 (gold 반환 대상)
    const { data: topBid } = await adminClient
      .from("bids")
      .select("bidder_id, amount")
      .eq("item_id", item_id)
      .order("amount", { ascending: false })
      .limit(1)
      .single();

    const fee = Math.floor(item.buyout_price * FEE_RATE);
    const sellerReceives = item.buyout_price - fee;

    // 4. 구매자 gold 차감
    await adminClient
      .from("profiles")
      .update({ gold: buyerProfile.gold - item.buyout_price })
      .eq("id", user.id);

    // 5. 이전 최고 입찰자 gold 반환 (즉시구매로 경매 종료되므로)
    if (topBid && topBid.bidder_id !== user.id) {
      const { data: prevProfile } = await adminClient
        .from("profiles")
        .select("gold")
        .eq("id", topBid.bidder_id)
        .single();

      if (prevProfile) {
        await adminClient
          .from("profiles")
          .update({ gold: prevProfile.gold + topBid.amount })
          .eq("id", topBid.bidder_id);
      }
    }

    // 6. 판매자 gold 지급 (수수료 차감)
    const { data: sellerProfile } = await adminClient
      .from("profiles")
      .select("gold")
      .eq("id", item.seller_id)
      .single();

    if (sellerProfile) {
      await adminClient
        .from("profiles")
        .update({ gold: sellerProfile.gold + sellerReceives })
        .eq("id", item.seller_id);
    }

    // 7. transactions INSERT
    const { data: transaction, error: txError } = await adminClient
      .from("transactions")
      .insert({
        item_id,
        seller_id: item.seller_id,
        buyer_id: user.id,
        final_price: item.buyout_price,
        fee,
      })
      .select("id")
      .single();

    if (txError || !transaction) {
      return new Response(JSON.stringify({ error: "Failed to create transaction" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 8. inventory INSERT (구매자) + 판매자 inventory 삭제
    await adminClient.from("inventory").insert({
      owner_id: user.id,
      item_id,
      transaction_id: transaction.id,
      status: "owned",
    });

    await adminClient
      .from("inventory")
      .delete()
      .eq("item_id", item_id)
      .eq("owner_id", item.seller_id)
      .eq("status", "listed");

    // 9. items.status = 'sold'
    await adminClient
      .from("items")
      .update({ status: "sold" })
      .eq("id", item_id);

    // 10. 알림 발송
    await adminClient.from("notifications").insert([
      {
        user_id: user.id,
        type: "won",
        item_id,
        message: `'${item.name}' Instant purchase complete! ${item.buyout_price} gold paid`,
      },
      {
        user_id: item.seller_id,
        type: "sold",
        item_id,
        message: `'${item.name}' has been purchased immediately for ${item.buyout_price} gold. (Deduct ${fee} gold commission, paid ${sellerReceives} gold)`,
      },
    ]);

    return new Response(
      JSON.stringify({ success: true, transaction_id: transaction.id, final_price: item.buyout_price, fee }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(JSON.stringify({ error: String(err) }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
