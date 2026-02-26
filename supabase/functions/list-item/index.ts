import { corsHeaders } from "../_shared/cors.ts";
import { authenticateRequest, createAdminClient } from "../_shared/auth.ts";
import { VALID_CATEGORIES, MIN_DURATION_HOURS, MAX_DURATION_DAYS } from "../_shared/constants.ts";

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const adminClient = createAdminClient();

    // JWT 검증
    const { user, errorResponse } = await authenticateRequest(req, adminClient);
    if (errorResponse) return errorResponse;

    const body = await req.json();
    const {
      name,
      description,
      image_url,
      category,
      start_price,
      duration_hours,
      inventory_item_id,  // 인벤토리 기반 등록 시 필수
    } = body;

    // buyout_price: 0이거나 미입력이면 null 처리
    const buyout_price: number | null =
      body.buyout_price && typeof body.buyout_price === "number" && body.buyout_price > 0
        ? body.buyout_price
        : null;

    // --- 인벤토리 보유 여부 검증 ---
    if (!inventory_item_id || typeof inventory_item_id !== "string") {
      return new Response(JSON.stringify({ error: "inventory_item_id is required" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const { data: invItem, error: invError } = await adminClient
      .from("inventory")
      .select("id, status, item:items(name, category, image_url)")
      .eq("id", inventory_item_id)
      .eq("owner_id", user.id)
      .single();

    if (invError || !invItem) {
      return new Response(JSON.stringify({ error: "Inventory item not found or not owned by you" }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (invItem.status === "listed") {
      return new Response(JSON.stringify({ error: "This item is already listed for auction" }), {
        status: 409,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 인벤토리 아이템 정보로 name/category/image_url 자동 채움 (프론트 입력값 우선)
    const itemInfo = invItem.item as { name: string; category: string | null; image_url: string | null } | null;
    const resolvedName     = name?.trim() || itemInfo?.name || "";
    const resolvedCategory = category || itemInfo?.category;
    const resolvedImageUrl = image_url    || itemInfo?.image_url || null;

    // --- 입력값 검증 ---
    if (!resolvedName) {
      return new Response(JSON.stringify({ error: "name is required" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (!start_price || typeof start_price !== "number" || start_price <= 0) {
      return new Response(JSON.stringify({ error: "start_price must be a positive number" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    if (buyout_price !== null && buyout_price <= start_price) {
      return new Response(
        JSON.stringify({ error: "buyout_price must be greater than start_price" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    if (!resolvedCategory || !VALID_CATEGORIES.includes(resolvedCategory)) {
      return new Response(
        JSON.stringify({ error: `category must be one of: ${VALID_CATEGORIES.join(", ")}` }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    if (
      !duration_hours ||
      typeof duration_hours !== "number" ||
      duration_hours < MIN_DURATION_HOURS ||
      duration_hours > MAX_DURATION_DAYS * 24
    ) {
      return new Response(
        JSON.stringify({
          error: `duration_hours must be between ${MIN_DURATION_HOURS} and ${MAX_DURATION_DAYS * 24}`,
        }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // 종료 시각 계산
    const endsAt = new Date(Date.now() + duration_hours * 60 * 60 * 1000).toISOString();

    const { data: newItem, error: insertError } = await adminClient
      .from("items")
      .insert({
        seller_id: user.id,
        name: resolvedName,
        description: description ?? null,
        image_url: resolvedImageUrl,
        category: resolvedCategory,
        start_price,
        buyout_price,
        current_bid: 0,
        ends_at: endsAt,
        status: "active",
      })
      .select("id, name, start_price, buyout_price, ends_at, category")
      .single();

    if (insertError || !newItem) {
      return new Response(JSON.stringify({ error: "Failed to list item" }), {
        status: 500,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // 인벤토리 아이템 status → 'listed' 로 변경
    await adminClient
      .from("inventory")
      .update({ status: "listed" })
      .eq("id", inventory_item_id);

    return new Response(
      JSON.stringify({ success: true, item: newItem }),
      { status: 201, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(JSON.stringify({ error: String(err) }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
