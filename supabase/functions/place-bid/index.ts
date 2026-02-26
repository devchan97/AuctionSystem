import { corsHeaders } from "../_shared/cors.ts";
import { authenticateRequest, createAdminClient } from "../_shared/auth.ts";

Deno.serve(async (req) => {
  // CORS preflight
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const adminClient = createAdminClient();

    const { user, errorResponse } = await authenticateRequest(req, adminClient);
    if (errorResponse) return errorResponse;

    const { item_id, amount } = await req.json();

    if (!item_id || !amount || typeof amount !== "number" || amount <= 0) {
      return new Response(JSON.stringify({ error: "Invalid request body" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 1. fetch item
    const { data: item, error: itemError } = await adminClient
      .from("items")
      .select("id, seller_id, name, current_bid, start_price, status, ends_at, buyout_price")
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

    if (item.seller_id === user.id) {
      return new Response(JSON.stringify({ error: "Cannot bid on your own item" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 2. validate bid amount
    const minBid = item.current_bid > 0 ? item.current_bid + 1 : item.start_price;
    if (amount < minBid) {
      return new Response(
        JSON.stringify({ error: `Bid must be at least ${minBid}` }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    if (item.buyout_price && amount >= item.buyout_price) {
      return new Response(
        JSON.stringify({ error: "Use buyout to purchase at or above buyout price" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // 3. check bidder gold
    const { data: bidderProfile, error: bidderError } = await adminClient
      .from("profiles")
      .select("gold")
      .eq("id", user.id)
      .single();

    if (bidderError || !bidderProfile) {
      return new Response(JSON.stringify({ error: "Profile not found" }), {
        status: 404,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (bidderProfile.gold < amount) {
      return new Response(JSON.stringify({ error: "Insufficient gold" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 4. get previous top bid
    const { data: previousBid } = await adminClient
      .from("bids")
      .select("bidder_id, amount")
      .eq("item_id", item_id)
      .order("amount", { ascending: false })
      .limit(1)
      .single();

    if (previousBid && previousBid.bidder_id === user.id) {
      return new Response(JSON.stringify({ error: "Already the highest bidder" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 5. deduct bidder gold
    const { error: deductError } = await adminClient
      .from("profiles")
      .update({ gold: bidderProfile.gold - amount })
      .eq("id", user.id);

    if (deductError) {
      return new Response(JSON.stringify({ error: "Failed to deduct gold" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 6. refund previous top bidder
    if (previousBid && previousBid.bidder_id !== user.id) {
      const { data: prevProfile } = await adminClient
        .from("profiles")
        .select("gold")
        .eq("id", previousBid.bidder_id)
        .single();

      if (prevProfile) {
        await adminClient
          .from("profiles")
          .update({ gold: prevProfile.gold + previousBid.amount })
          .eq("id", previousBid.bidder_id);

        await adminClient.from("notifications").insert({
          user_id: previousBid.bidder_id,
          type: "outbid",
          item_id: item_id,
          message: `You were outbid on '${item.name}'. ${previousBid.amount} gold has been refunded.`,
        });
      }
    }

    // 7. insert bid
    const { error: bidInsertError } = await adminClient.from("bids").insert({
      item_id,
      bidder_id: user.id,
      amount,
    });

    if (bidInsertError) {
      // 롤백: 차감한 gold 복구
      await adminClient
        .from("profiles")
        .update({ gold: bidderProfile.gold })
        .eq("id", user.id);

      return new Response(JSON.stringify({ error: "Failed to place bid" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 8. items.current_bid 업데이트
    const { error: itemUpdateError } = await adminClient
      .from("items")
      .update({ current_bid: amount })
      .eq("id", item_id);

    if (itemUpdateError) {
      return new Response(JSON.stringify({ error: "Failed to update item bid" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    return new Response(
      JSON.stringify({ success: true, amount }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(JSON.stringify({ error: String(err) }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
