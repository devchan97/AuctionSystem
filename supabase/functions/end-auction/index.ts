import { createClient } from "jsr:@supabase/supabase-js@2";
import { corsHeaders } from "../_shared/cors.ts";

// Supabase Dashboard → Edge Functions → Schedule 에서 매분 호출하도록 설정
// Cron expression: * * * * *

const FEE_RATE = 0.05; // 5% 거래 수수료

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  // Cron 호출은 Authorization 헤더 없이 오므로 Service Role만 사용
  const adminClient = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!
  );

  try {
    // 1. 종료 시각이 지났고 아직 active인 경매 전체 조회
    const { data: expiredItems, error: fetchError } = await adminClient
      .from("items")
      .select("id, seller_id, name, current_bid, start_price")
      .eq("status", "active")
      .lt("ends_at", new Date().toISOString());

    if (fetchError) {
      return new Response(JSON.stringify({ error: "Failed to fetch expired items" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (!expiredItems || expiredItems.length === 0) {
      return new Response(JSON.stringify({ processed: 0 }), {
        status: 200,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const results: { item_id: string; result: string }[] = [];

    for (const item of expiredItems) {
      // 2. 해당 아이템의 최고 입찰 조회
      const { data: topBid } = await adminClient
        .from("bids")
        .select("bidder_id, amount")
        .eq("item_id", item.id)
        .order("amount", { ascending: false })
        .limit(1)
        .single();

      if (topBid && topBid.amount >= item.start_price) {
        // --- 낙찰 처리 ---
        const fee = Math.floor(topBid.amount * FEE_RATE);
        const sellerReceives = topBid.amount - fee;

        // 판매자 gold 지급 (입찰자 gold는 입찰 시점에 이미 차감됨)
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

        // transactions INSERT
        const { data: transaction } = await adminClient
          .from("transactions")
          .insert({
            item_id: item.id,
            seller_id: item.seller_id,
            buyer_id: topBid.bidder_id,
            final_price: topBid.amount,
            fee,
          })
          .select("id")
          .single();

        // inventory INSERT (낙찰자) + 판매자 inventory 삭제
        if (transaction) {
          await adminClient.from("inventory").insert({
            owner_id: topBid.bidder_id,
            item_id: item.id,
            transaction_id: transaction.id,
            status: "owned",
          });

          // 낙찰자 insert 후 판매자 listed row 삭제
          await adminClient
            .from("inventory")
            .delete()
            .eq("item_id", item.id)
            .eq("owner_id", item.seller_id)
            .eq("status", "listed");
        }

        // items.status = 'sold'
        await adminClient
          .from("items")
          .update({ status: "sold" })
          .eq("id", item.id);

        // 알림: 낙찰자, 판매자
        await adminClient.from("notifications").insert([
          {
            user_id: topBid.bidder_id,
            type: "won",
            item_id: item.id,
            message: `'${item.name}' 경매에서 낙찰되었습니다! ${topBid.amount} gold`,
          },
          {
            user_id: item.seller_id,
            type: "sold",
            item_id: item.id,
            message: `'${item.name}'이(가) ${topBid.amount} gold에 낙찰되었습니다. (수수료 ${fee} gold, ${sellerReceives} gold 지급)`,
          },
        ]);

        results.push({ item_id: item.id, result: "sold" });
      } else {
        // --- 유찰 처리 ---
        // 입찰자가 있었다면 gold 반환 (start_price 미달인 경우)
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
          }
        }

        // items.status = 'expired'
        await adminClient
          .from("items")
          .update({ status: "expired" })
          .eq("id", item.id);

        // 유찰 → 판매자 인벤토리 status 'listed' → 'owned' 복원
        await adminClient
          .from("inventory")
          .update({ status: "owned" })
          .eq("item_id", item.id)
          .eq("owner_id", item.seller_id)
          .eq("status", "listed");

        // 알림: 판매자 (유찰 통보)
        await adminClient.from("notifications").insert({
          user_id: item.seller_id,
          type: "expired",
          item_id: item.id,
          message: `'${item.name}' 경매가 유찰되었습니다.`,
        });

        results.push({ item_id: item.id, result: "expired" });
      }
    }

    return new Response(
      JSON.stringify({ processed: results.length, results }),
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
