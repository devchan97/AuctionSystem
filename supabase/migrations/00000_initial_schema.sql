-- 4. 데이터베이스 스키마

-- 4-1. profiles — 사용자 프로필
CREATE TABLE profiles (
  id          UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  username    TEXT UNIQUE NOT NULL,
  gold        BIGINT DEFAULT 0,          -- 게임 내 재화
  avatar_url  TEXT,                      -- Google OAuth 프로필 이미지
  created_at  TIMESTAMPTZ DEFAULT now()
);

-- 4-2. items — 경매 아이템
CREATE TABLE items (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  seller_id    UUID REFERENCES profiles(id) NOT NULL,
  name         TEXT NOT NULL,
  description  TEXT,
  image_url    TEXT,
  category     TEXT,                      -- 무기/방어구/소모품 등
  start_price  BIGINT NOT NULL,
  buyout_price BIGINT,                    -- 즉시구매가 (NULL이면 없음)
  current_bid  BIGINT DEFAULT 0,
  created_at   TIMESTAMPTZ DEFAULT now(),
  ends_at      TIMESTAMPTZ NOT NULL,      -- 경매 종료 시각
  status       TEXT DEFAULT 'active'      -- active | sold | expired | cancelled
);

-- 4-3. bids — 입찰 내역
CREATE TABLE bids (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  item_id    UUID REFERENCES items(id) ON DELETE CASCADE,
  bidder_id  UUID REFERENCES profiles(id),
  amount     BIGINT NOT NULL,
  created_at TIMESTAMPTZ DEFAULT now()
);

-- 4-4. transactions — 거래 완료 내역
CREATE TABLE transactions (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  item_id     UUID REFERENCES items(id),
  seller_id   UUID REFERENCES profiles(id),
  buyer_id    UUID REFERENCES profiles(id),
  final_price BIGINT NOT NULL,
  fee         BIGINT DEFAULT 0,           -- 거래 수수료
  created_at  TIMESTAMPTZ DEFAULT now()
);

-- 4-5. notifications — 알림
CREATE TABLE notifications (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    UUID REFERENCES profiles(id),
  type       TEXT NOT NULL,              -- outbid | won | sold | expired
  item_id    UUID REFERENCES items(id),
  message    TEXT,
  is_read    BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now()
);

-- 4-6. inventory — 인벤토리 (낙찰/즉시구매로 획득한 아이템)
CREATE TABLE inventory (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id       UUID REFERENCES profiles(id) NOT NULL,
  item_id        UUID REFERENCES items(id) NOT NULL,
  acquired_at    TIMESTAMPTZ DEFAULT now(),
  transaction_id UUID REFERENCES transactions(id)
);


-- 5. Row Level Security (RLS) 정책

-- profiles: 본인만 수정 가능, 전체 읽기 허용
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Public read" ON profiles FOR SELECT USING (true);
CREATE POLICY "Own update" ON profiles FOR UPDATE USING (auth.uid() = id);

-- items: 전체 읽기, 본인만 등록/취소
ALTER TABLE items ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Public read" ON items FOR SELECT USING (true);
CREATE POLICY "Seller insert" ON items FOR INSERT WITH CHECK (auth.uid() = seller_id);
CREATE POLICY "Seller update" ON items FOR UPDATE USING (auth.uid() = seller_id);

-- bids: 인증 사용자 전체 읽기, 인증 사용자 입찰 가능
ALTER TABLE bids ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Auth read" ON bids FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Auth insert" ON bids FOR INSERT WITH CHECK (auth.uid() = bidder_id);

-- notifications: 본인 알림만 읽기/수정
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Own read" ON notifications FOR SELECT USING (auth.uid() = user_id);
CREATE POLICY "Own update" ON notifications FOR UPDATE USING (auth.uid() = user_id);

-- transactions: 관련 당사자만 읽기 가능
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Party read" ON transactions FOR SELECT
  USING (auth.uid() = buyer_id OR auth.uid() = seller_id);

-- inventory: 본인 인벤토리만 읽기
ALTER TABLE inventory ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Own read" ON inventory FOR SELECT USING (auth.uid() = owner_id);


-- 7. Supabase Realtime 활성화
-- Dashboard → Database → Replication에서 활성화하거나 아래 SQL 실행
ALTER PUBLICATION supabase_realtime ADD TABLE items;
ALTER PUBLICATION supabase_realtime ADD TABLE notifications;

-- 6. 신규 유저 가입 시 profiles 자동 생성 트리거
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = ''
AS $$
BEGIN
  INSERT INTO public.profiles (id, username, avatar_url)
  VALUES (
    NEW.id,
    COALESCE(NEW.raw_user_meta_data->>'username', split_part(NEW.email, '@', 1)),
    NEW.raw_user_meta_data->>'avatar_url'
  )
  ON CONFLICT (id) DO NOTHING;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
