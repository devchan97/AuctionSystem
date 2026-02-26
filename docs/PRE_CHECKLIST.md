# 사전 체크리스트

push 전 반드시 확인해야 할 항목들입니다.

---

## 민감정보 파일 처리

### Unity — SupabaseConfig.cs

`Unity/AuctionSystem/Assets/Scripts/Network/SupabaseConfig.cs`

GitHub에 올리기 전 실제 값을 사용자의 환경변수로 교체해야 합니다.

현재 파일에는 이미 환경변수가 적용되어 있습니다. 로컬 개발 시에만 실제 값으로 채워서 사용하세요.

```csharp
public const string Url     = "https://<your-project-ref>.supabase.co";
public const string AnonKey = "<your-anon-key>";
```

> **주의**: 실수로 실제 키가 push됐다면 Supabase Dashboard에서 즉시 키를 재발급하세요.

---

### Web — .env.local

`Web/AuctionSystem/.env.local`

`.gitignore`에 이미 포함되어 있어 자동으로 제외됩니다.

대신 `.env.local.example`을 참고해서 사용자가 직접 채울 수 있도록 해두었습니다.

---

## .gitignore 확인 항목

| 경로 | 상태 |
|------|------|
| `Web/AuctionSystem/.env.local` | ✅ 포함됨 |
| `Web/AuctionSystem/node_modules/` | ✅ 포함됨 |
| `Unity/AuctionSystem/Library/` | ✅ 포함됨 |
| `Unity/AuctionSystem/Temp/` | ✅ 포함됨 |
| `Unity/AuctionSystem/Logs/` | ✅ 포함됨 |
| `Unity/AuctionSystem/obj/` | ✅ 포함됨 |

---

## 초기 설정 가이드 (fork/clone 후)

### Web

```bash
cd Web/AuctionSystem
cp .env.local.example .env.local
# .env.local 열어서 실제 값 채우기
npm install
npm run dev
```

### Unity

1. `SupabaseConfig.cs` 열어서 실제 Supabase URL과 AnonKey 채우기
2. Unity 2022 LTS에서 프로젝트 열기
3. `Tools > Auction > Build Scene` 실행

### Supabase

1. Supabase Dashboard에서 새 프로젝트 생성
2. `supabase/migrations/` 폴더의 SQL 파일을 순서대로 실행:
   - `00000_initial_schema.sql`
   - `00001_inventory_status.sql`
   - `00002_admin_role.sql`
   - `00003_notification_delete.sql`
3. `supabase/functions/` 폴더의 Edge Function 배포
4. Auth > URL Configuration에 사이트 URL 추가

---

## 관리자 계정 설정

`docs/ADMIN_SETUP.md` 참고
