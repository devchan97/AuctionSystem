# Supabase 초기 설정 가이드

## 1. DB 마이그레이션 (SQL Editor에서 순서대로 실행)

```sql
-- 00000_initial_schema.sql
-- 00001_inventory_status.sql
-- 00002_admin_role.sql
-- 00003_notification_delete.sql
```

---

## 2. Edge Functions 배포

```bash
supabase functions deploy list-item
supabase functions deploy place-bid
supabase functions deploy buyout
supabase functions deploy end-auction
supabase functions deploy cancel-auction
```

---

## 3. Edge Functions JWT 검증 OFF 설정 (Dashboard — 필수)

배포 후 각 함수의 JWT 검증을 반드시 OFF로 설정해야 합니다.
함수가 자체적으로 `adminClient.auth.getUser(jwt)`로 검증하므로, Supabase의 이중 검증이 오히려 401을 유발합니다.

`Edge Functions` → 각 함수 클릭 → 우측 상단 톱니바퀴(⚙) → **"Enforce JWT Verification" 토글 OFF**

대상 함수: `list-item` / `place-bid` / `buyout` / `end-auction` (4개 모두)

> 이 설정을 하지 않으면 클라이언트에서 유효한 JWT를 보내도 `{"code":401,"message":"Invalid JWT"}` 오류가 발생합니다.

---

## 4. Authentication 설정 (Dashboard)

**Google OAuth 설정:**

1. `Authentication` → `Providers` → `Google` → Enable
2. Google Cloud Console에서 OAuth 2.0 클라이언트 ID/Secret 발급
3. Authorized redirect URI에 `https://<project-ref>.supabase.co/auth/v1/callback` 추가
4. Supabase Dashboard에 Client ID / Client Secret 입력

**Redirect URL 허용:**

- `Authentication` → `URL Configuration` → `Redirect URLs`
- `http://localhost:3000/**` (개발)
- `https://<your-vercel-domain>/**` (프로덕션)
- Unity 콜백: `http://localhost:7654/**`

> **주의 — Google OAuth 후 ES256 토큰 문제**
> Google로 로그인하면 ES256 알고리즘 JWT가 발급되어 Edge Function에서 401이 발생할 수 있습니다.
> `auth/callback/route.ts`에서 `exchangeCodeForSession` 후 `refreshSession()`을 호출해 HS256으로 교체합니다. (이미 적용됨)
> 기존 ES256 세션이 쿠키에 남아있으면 로그아웃 후 재로그인하세요.

---

## 5. handle_new_user 함수 search_path 설정 (SQL Editor)

```sql
ALTER FUNCTION public.handle_new_user()
  SET search_path = public, extensions;
```

---

## 6. 관리자 지정 (SQL Editor)

```sql
UPDATE auth.users
SET raw_app_meta_data = raw_app_meta_data || '{"role":"admin"}'::jsonb
WHERE email = 'your-admin@email.com';
```

---

## 7. Storage 버킷 생성 (Dashboard)

`Storage` → `New Bucket` → 이름: `item-images` → Public 체크

---

## 8. end-auction Cron 스케줄 등록

`Edge Functions` → `end-auction` → `Schedule` → Cron expression: `* * * * *` (매분 실행)
