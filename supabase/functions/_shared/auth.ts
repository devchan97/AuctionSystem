import { createClient, SupabaseClient, User } from "jsr:@supabase/supabase-js@2";
import { corsHeaders } from "./cors.ts";

type AuthResult =
  | { user: User; errorResponse: null }
  | { user: null; errorResponse: Response };

/**
 * Authorization 헤더에서 JWT를 추출해 사용자를 검증합니다.
 * 성공 시 { user, errorResponse: null }, 실패 시 { user: null, errorResponse } 반환.
 */
export async function authenticateRequest(
  req: Request,
  adminClient: SupabaseClient
): Promise<AuthResult> {
  const authHeader = req.headers.get("Authorization");
  if (!authHeader) {
    return {
      user: null,
      errorResponse: new Response(JSON.stringify({ error: "Unauthorized" }), {
        status: 401,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      }),
    };
  }

  const jwt = authHeader.replace(/^Bearer\s+/i, "");
  const { data: { user }, error: userError } = await adminClient.auth.getUser(jwt);

  if (userError || !user) {
    return {
      user: null,
      errorResponse: new Response(
        JSON.stringify({ error: "Unauthorized", detail: userError?.message }),
        {
          status: 401,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        }
      ),
    };
  }

  return { user, errorResponse: null };
}

/** Service Role adminClient 생성 */
export function createAdminClient(): SupabaseClient {
  return createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!
  );
}
