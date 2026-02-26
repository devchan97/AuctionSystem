-- inventory 테이블에 status 컬럼 추가
-- owned: 보유 중 (기본값)
-- listed: 경매 등록됨 (경매 진행 중)
ALTER TABLE inventory ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'owned';

-- 인벤토리 아이템 지급을 위한 RLS 정책 추가 (서비스 롤만 INSERT 가능)
-- 낙찰/즉시구매 시 Edge Function이 adminClient로 INSERT
-- 클라이언트 직접 INSERT는 허용하지 않음
DO $$
BEGIN
  DROP POLICY IF EXISTS "Service insert" ON inventory;
  CREATE POLICY "Service insert" ON inventory
    FOR INSERT WITH CHECK (false);

  DROP POLICY IF EXISTS "Own status update" ON inventory;
  CREATE POLICY "Own status update" ON inventory
    FOR UPDATE USING (auth.uid() = owner_id);
END;
$$;
