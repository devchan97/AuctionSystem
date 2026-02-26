# Vercel 배포 가이드

Next.js 웹 클라이언트를 Vercel에 배포하는 단계별 가이드입니다.

## 사전 준비

- GitHub 계정
- [Vercel 계정](https://vercel.com) (GitHub으로 가입 권장)
- Supabase 프로젝트 (이미 생성 완료 가정)

---

## 1단계 — GitHub 저장소 생성 및 첫 push

민감정보 확인 먼저: [PRE_CHECKLIST.md](./PRE_CHECKLIST.md) 참고

```bash
git init
git add .
git commit -m "init"
git remote add origin https://github.com/<your-username>/<repo-name>.git
git push -u origin main
```

> `.env.local`은 `.gitignore`에 이미 포함되어 있으므로 자동 제외됩니다.

---

## 2단계 — Vercel 프로젝트 생성

1. [vercel.com](https://vercel.com) → **Add New > Project**
2. GitHub 저장소 선택
3. **Configure Project** 화면에서 아래 설정 변경:
   - **Framework Preset**: Next.js (자동 감지)
   - **Root Directory**: `Web/AuctionSystem` ← **반드시 변경**
4. **Deploy** 클릭 (환경변수는 아직 입력 안 해도 됨)

---

## 3단계 — 환경변수 입력

배포 후 **Settings > Environment Variables**에서 아래 4개 추가:

| 변수명 | 값 | 비고 |
|--------|-----|------|
| `NEXT_PUBLIC_SUPABASE_URL` | `https://xxxx.supabase.co` | Supabase > Project Settings > API |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | `eyJ...` | 위와 동일 |
| `SUPABASE_SERVICE_ROLE_KEY` | `eyJ...` | **절대 공개하면 안 됨** |
| `NEXT_PUBLIC_SITE_URL` | `https://your-app.vercel.app` | Vercel 도메인 |

추가 후 **Deployments > Redeploy** 로 재배포.

---

## 4단계 — 배포 확인

- 상단 URL 클릭해서 페이지 열리는지 확인
- `/auction`, `/auth/login` 등 기본 라우팅 확인
- Supabase 연결이 안 되면 환경변수 오타 여부 재확인

---

## 5단계 — Supabase 설정 업데이트

### Auth Redirect URL 추가

Supabase Dashboard → **Authentication > URL Configuration > Redirect URLs**에 추가:

```
https://your-app.vercel.app/**
https://your-app.vercel.app/auth/callback
```

### Google OAuth (적용한 경우)

Google Cloud Console → **OAuth 2.0 클라이언트** → 승인된 리디렉션 URI에 추가:

```
https://your-app.vercel.app/auth/callback
```

---

## 6단계 — Unity Windows빌드 및 배포 (선택)

### 사전 체크

- `Canvas/AuctionPanel/DebugPanel` 오브젝트 **비활성화** 확인
- `SupabaseConfig.cs` 플레이스홀더를 실제 값으로 교체

### 빌드

Unity → **File > Build Settings > Windows > Build**

---

## 자주 발생하는 오류

| 증상 | 원인 | 해결 |
|------|------|------|
| 빌드 실패 - `Cannot find module` | Root Directory 미설정 | `Web/AuctionSystem`으로 변경 |
| 로그인 후 리다이렉트 오류 | Redirect URL 미등록 | Supabase Auth 설정 업데이트 |
| API 401 오류 | 환경변수 누락 또는 오타 | Vercel 환경변수 재확인 |
