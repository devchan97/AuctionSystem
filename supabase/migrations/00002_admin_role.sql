-- 관리자 역할 설정
-- Supabase Dashboard > SQL Editor에서 실행
--
-- 관리자 판별 방식:
--   Authentication > Users에서 해당 유저의 app_metadata에
--   { "role": "admin" } 을 직접 입력하면 됩니다.
--   RLS에서는 auth.jwt() -> 'app_metadata' -> 'role' = 'admin' 으로 확인합니다.

-- 편의용 헬퍼 함수: 현재 사용자가 admin인지 반환
-- SET search_path = '' → mutable search_path 보안 경고 해소
CREATE OR REPLACE FUNCTION public.is_admin()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY INVOKER
SET search_path = ''
AS $$
  SELECT coalesce(
    (auth.jwt() -> 'app_metadata' ->> 'role') = 'admin',
    false
  );
$$;
-- 참고: JWT의 app_metadata 클레임은 DB의 raw_app_meta_data 컬럼에서 자동으로 채워집니다.

-- 관리자 RLS 정책
DO $$
BEGIN
  -- profiles: 관리자는 모든 프로필 수정 가능
  DROP POLICY IF EXISTS "Admin update any profile" ON profiles;
  CREATE POLICY "Admin update any profile" ON profiles
    FOR UPDATE USING (public.is_admin());

  -- items: 관리자는 모든 경매 아이템 수정 가능
  DROP POLICY IF EXISTS "Admin update any item" ON items;
  CREATE POLICY "Admin update any item" ON items
    FOR UPDATE USING (public.is_admin());

  -- items: 관리자는 모든 경매 아이템 삭제 가능
  DROP POLICY IF EXISTS "Admin delete any item" ON items;
  CREATE POLICY "Admin delete any item" ON items
    FOR DELETE USING (public.is_admin());

  -- transactions: 관리자는 모든 거래 내역 조회 가능
  DROP POLICY IF EXISTS "Admin read all transactions" ON transactions;
  CREATE POLICY "Admin read all transactions" ON transactions
    FOR SELECT USING (public.is_admin());

  -- profiles: 관리자는 모든 프로필 조회 가능 (사용자 목록)
  DROP POLICY IF EXISTS "Admin read all profiles" ON profiles;
  CREATE POLICY "Admin read all profiles" ON profiles
    FOR SELECT USING (public.is_admin());
END;
$$;

-- ⚠️ 관리자 지정 방법은 SQL이 아닌 Dashboard에서 합니다.
-- → docs/ADMIN_SETUP.md 참고
