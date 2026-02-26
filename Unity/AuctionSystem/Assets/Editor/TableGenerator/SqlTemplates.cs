namespace AuctionSystem.Editor.TableGenerator
{
    public static class SqlTemplates
    {
        public static readonly TableDefinition[] All = new[]
        {
            Profiles, Items, Bids, Transactions, Notifications, Inventory
        };

        public static readonly TableDefinition Profiles = new TableDefinition
        {
            Name = "profiles",
            Sql = @"
-- profiles 테이블
CREATE TABLE IF NOT EXISTS public.profiles (
    id          UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    username    TEXT UNIQUE,
    gold        BIGINT NOT NULL DEFAULT 1000,
    avatar_url  TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""profiles_select_all"" ON public.profiles FOR SELECT USING (true);
CREATE POLICY ""profiles_update_own"" ON public.profiles FOR UPDATE USING (auth.uid() = id);

-- 신규 회원 자동 프로필 생성 트리거
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER LANGUAGE plpgsql SECURITY DEFINER AS $$
BEGIN
    INSERT INTO public.profiles (id, username)
    VALUES (NEW.id, NEW.raw_user_meta_data->>'username');
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
".Trim()
        };

        public static readonly TableDefinition Items = new TableDefinition
        {
            Name = "items",
            Sql = @"
-- items 테이블
CREATE TABLE IF NOT EXISTS public.items (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    seller_id    UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    name         TEXT NOT NULL,
    description  TEXT,
    category     TEXT NOT NULL DEFAULT '기타',
    image_url    TEXT,
    start_price  BIGINT NOT NULL DEFAULT 1,
    current_bid  BIGINT NOT NULL DEFAULT 0,
    buyout_price BIGINT,
    status       TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active','sold','expired')),
    ends_at      TIMESTAMPTZ NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.items ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""items_select_all""    ON public.items FOR SELECT USING (true);
CREATE POLICY ""items_insert_auth""   ON public.items FOR INSERT WITH CHECK (auth.uid() = seller_id);
CREATE POLICY ""items_update_seller"" ON public.items FOR UPDATE USING (auth.uid() = seller_id);
".Trim()
        };

        public static readonly TableDefinition Bids = new TableDefinition
        {
            Name = "bids",
            Sql = @"
-- bids 테이블
CREATE TABLE IF NOT EXISTS public.bids (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id    UUID NOT NULL REFERENCES public.items(id) ON DELETE CASCADE,
    bidder_id  UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    amount     BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.bids ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""bids_select_auth"" ON public.bids FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY ""bids_insert_auth"" ON public.bids FOR INSERT WITH CHECK (auth.uid() = bidder_id);
".Trim()
        };

        public static readonly TableDefinition Transactions = new TableDefinition
        {
            Name = "transactions",
            Sql = @"
-- transactions 테이블
CREATE TABLE IF NOT EXISTS public.transactions (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id      UUID NOT NULL REFERENCES public.items(id),
    seller_id    UUID NOT NULL REFERENCES public.profiles(id),
    buyer_id     UUID NOT NULL REFERENCES public.profiles(id),
    final_price  BIGINT NOT NULL,
    fee          BIGINT NOT NULL DEFAULT 0,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""transactions_select_own"" ON public.transactions
    FOR SELECT USING (auth.uid() = seller_id OR auth.uid() = buyer_id);
".Trim()
        };

        public static readonly TableDefinition Notifications = new TableDefinition
        {
            Name = "notifications",
            Sql = @"
-- notifications 테이블
CREATE TABLE IF NOT EXISTS public.notifications (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    type       TEXT NOT NULL,
    message    TEXT NOT NULL,
    is_read    BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.notifications ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""notifications_select_own"" ON public.notifications FOR SELECT USING (auth.uid() = user_id);
CREATE POLICY ""notifications_update_own"" ON public.notifications FOR UPDATE USING (auth.uid() = user_id);
".Trim()
        };

        public static readonly TableDefinition Inventory = new TableDefinition
        {
            Name = "inventory",
            Sql = @"
-- inventory 테이블
CREATE TABLE IF NOT EXISTS public.inventory (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id       UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    item_name      TEXT NOT NULL,
    item_category  TEXT,
    acquired_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    source_item_id UUID REFERENCES public.items(id)
);

ALTER TABLE public.inventory ENABLE ROW LEVEL SECURITY;

CREATE POLICY ""inventory_select_own"" ON public.inventory FOR SELECT USING (auth.uid() = owner_id);
".Trim()
        };
    }

    public class TableDefinition
    {
        public string Name;
        public string Sql;
    }
}
