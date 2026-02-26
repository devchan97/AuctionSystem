# Auction House System

실시간 경매 플랫폼 — Next.js 웹 클라이언트와 Unity 게임 클라이언트가 Supabase를 통해 동일한 데이터를 공유합니다.

> **Portfolio project** | Next.js 14 · Unity 6000.3.9f1 · Supabase · TypeScript · C#

---

## 주요 기능

- **실시간 입찰** — Supabase Realtime(WebSocket)으로 입찰가 즉시 동기화
- **즉시구매** — Edge Function에서 race condition 방지 트랜잭션 처리
- **Presence** — 아이템 상세 페이지 열람자 수 실시간 표시
- **인벤토리 기반 등록** — 보유 아이템만 경매에 올릴 수 있는 구조
- **직접 등록** — 아이템 정보·이미지를 직접 입력해 경매 등록
- **알림** — 낙찰/유찰/입찰 실패 시 실시간 푸시
- **경매 취소** — 낙찰 전 등록자가 직접 취소 가능
- **관리자 대시보드** — 경매 강제 종료, 사용자 Gold 조정, 통계
- **Unity-Web 인증 연동** — 웹 회원가입/Google OAuth 후 Unity 자동 로그인

---

## 기술 스택

| 영역 | 기술 |
|------|------|
| Web 프론트엔드 | Next.js 14 (App Router), TypeScript, Tailwind CSS |
| 게임 클라이언트 | Unity 6000.3.9f1 (LTS), C# |
| 백엔드 | Supabase (PostgreSQL, Auth, Realtime, Storage, Edge Functions) |
| 실시간 통신 | Supabase Realtime — Phoenix Protocol v1.0.0 |
| Unity HTTP | UnityWebRequest (SDK 없이 직접 구현) |

---

## 아키텍처

```
┌─────────────────┐        ┌─────────────────┐
│   Next.js Web   │        │  Unity Client   │
│  (Vercel)       │        │  (PC / WebGL)   │
└────────┬────────┘        └────────┬────────┘
         │                          │
         │  REST / Realtime WS      │  UnityWebRequest
         │                          │  NativeWebSocket
         ▼                          ▼
┌─────────────────────────────────────────────┐
│                  Supabase                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  │
│  │PostgreSQL│  │  Auth    │  │ Realtime │  │
│  │  + RLS   │  │  (JWT)   │  │(Phoenix) │  │
│  └──────────┘  └──────────┘  └──────────┘  │
│  ┌──────────┐  ┌──────────┐                │
│  │ Storage  │  │  Edge    │                │
│  │(이미지)   │  │Functions │                │
│  └──────────┘  └──────────┘                │
└─────────────────────────────────────────────┘
```

---

## 폴더 구조

```
/
├── docs/
│   ├── ADMIN_SETUP.md          # 관리자 설정 가이드
│   ├── DEPLOY_VERCEL.md        # Vercel 배포 가이드
│   ├── PRE_CHECKLIST.md        # 배포 전 체크리스트
│   └── SUPABASE_SETUP.md       # Supabase 초기 설정 가이드
│
├── supabase/
│   ├── migrations/
│   │   ├── 00000_initial_schema.sql
│   │   ├── 00001_inventory_status.sql
│   │   ├── 00002_admin_role.sql
│   │   └── 00003_notification_delete.sql
│   └── functions/
│       ├── _shared/            # auth.ts, constants.ts, cors.ts
│       ├── place-bid/
│       ├── buyout/
│       ├── list-item/
│       ├── end-auction/
│       └── cancel-auction/
│
├── Web/AuctionSystem/src/
│   ├── app/
│   │   ├── admin/              # 관리자 대시보드
│   │   ├── auction/[id]/       # 경매 상세
│   │   ├── auth/callback/      # OAuth 콜백
│   │   ├── auth/unity-callback/ # Unity 자동 로그인 브릿지
│   │   ├── category/[slug]/    # 카테고리 필터
│   │   ├── inventory/          # 인벤토리 관리
│   │   ├── login/
│   │   ├── my-page/create/     # 경매 등록 (인벤토리/직접)
│   │   └── signup/
│   ├── components/
│   │   ├── auction/            # AuctionCard, BidClientActions, BuyoutButton,
│   │   │                       # CancelAuctionButton, RealtimeBidWatcher,
│   │   │                       # SortDropdown, ViewerBadge
│   │   ├── auth/               # LogoutButton
│   │   ├── notifications/      # NotificationBell
│   │   └── ui/                 # Button, Input, SearchBar
│   ├── hooks/
│   │   └── useItemPresence.ts
│   ├── lib/
│   │   ├── supabase/           # client.ts, server.ts, middleware.ts
│   │   └── utils.ts
│   └── types/
│       └── supabase.ts
│
└── Unity/AuctionSystem/Assets/
    ├── Scripts/
    │   ├── GameBootstrap.cs
    │   ├── Network/            # SupabaseManager, RealtimeManager,
    │   │                       # ImageCacheManager, SupabaseConfig
    │   ├── Auction/            # AuctionManager, BidHandler
    │   ├── Auth/               # LoginManager, WebBridgeAuth
    │   ├── UI/                 # AuctionUI, BidUI, DebugPanel, DraggablePanel,
    │   │                       # InventoryUI, ItemCardUI, ListItemUI,
    │   │                       # LoadingSpinner, LoginUI, NotificationItemUI,
    │   │                       # NotificationListUI, NotificationPopupUI, UIAnimator
    │   ├── Models/             # AuctionModels.cs
    │   └── Utils/              # AuctionUtils.cs, DropdownHelper.cs
    └── Editor/
        ├── SceneBuilder.cs
        ├── SessionTools.cs
        └── TableGenerator/     # SupabaseTableConfig, TableGeneratorWindow, SqlTemplates
```

---

## 빠른 시작

### 사전 요구사항

- Node.js 18+
- Unity 6000.3.9f1 (LTS)
- Supabase 프로젝트

### Web

```bash
cd Web/AuctionSystem
cp .env.local.example .env.local
# .env.local에 Supabase 값 채우기
npm install
npm run dev
```

### Unity

1. `Unity/AuctionSystem/Assets/Scripts/Network/SupabaseConfig.cs`에 Supabase URL과 AnonKey 입력
2. Unity에서 프로젝트 열기
3. `Tools > Auction > Build Scene` 실행
4. Play 모드 진입

### Supabase 초기 설정

자세한 내용: [docs/SUPABASE_SETUP.md](./docs/SUPABASE_SETUP.md)

---

## 기술 선택 이유

**Unity에서 Supabase SDK 없이 UnityWebRequest 직접 사용**

- Unity용 Supabase 공식 SDK는 존재하지 않음
  - **supabase-csharp** (커뮤니티 C# SDK, [GitHub](https://github.com/supabase-community/supabase-csharp)): 사용 가능하지만 Unity 적용 시 NuGet 의존성 관리, managed code stripping 설정, UniTask 추가 등 복잡한 셋업 필요
  - **kamyker/supabase-unity** ([GitHub](https://github.com/kamyker/supabase-unity)): 알파 단계, 유지보수 불확실
  - **Asset Store 유료 플러그인** ($49.99, 2025.06 출시): 서드파티 제품, Realtime 미지원
- SDK 없이 UnityWebRequest로 직접 구현 → 인증 헤더/JWT 흐름 명확히 파악
- 외부 패키지 의존성 최소화 → 빌드 안정성 확보

**Supabase 선택 이유**

- Firebase 대비 PostgreSQL 기반 → JOIN, RLS 등 관계형 쿼리 활용
- Auth, Realtime, Storage, Edge Functions 일체형 제공
- 무료 티어로 상용 수준 인프라 구성 가능

**Edge Function으로 입찰/즉시구매 처리**

- 클라이언트 Gold 계산 신뢰 불가 → 서버에서 처리
- DB 트랜잭션 보장으로 동시 입찰 race condition 방지

---

## 배포 가이드

- [Supabase 초기 설정 가이드](./docs/SUPABASE_SETUP.md)
- [Vercel 배포 가이드](./docs/DEPLOY_VERCEL.md)
- [사전 체크리스트](./docs/PRE_CHECKLIST.md)
- [관리자 설정 가이드](./docs/ADMIN_SETUP.md)

---

## Vercel Web 배포

루트프로젝트 -> Web -> AuctionSystem 을 Vercel Web 배포한 링크입니다. 

- *[*[Vercel(#)*](https://auction-system-woad.vercel.app/)*

---

## Unity 클라이언트 다운로드

Unity PC 빌드는 별도 배포됩니다. (WebGL 미포함 — 키 보안 이슈로 제외)

- *[*[Google Drive에서 다운로드](#)*](https://drive.google.com/file/d/1NmnWl1MG1tNy-FOiHLnkqzz6yyb2zBA0/view?usp=sharing)*

> Windows 전용 빌드입니다. 다운로드 후 압축 해제 → `AuctionSystem.exe` 실행

---

## 알려진 제한사항

- Unity PC 빌드만 제공 (WebGL 미지원 — 클라이언트 키 노출 방지)
- Unity에서 Storage 이미지 직접 업로드 미지원 (웹에서만 가능)
- WebGL 빌드 시 `DebugPanel` 오브젝트 수동 비활성화 필요

---

> AI 보조 개발 도구(Claude)를 적극 활용했습니다. 아키텍처 설계, 핵심 로직(JWT 흐름, Realtime 프로토콜, RLS 정책), UI/UX 결정은 직접 작성 및 검증했습니다.
