# 관리자(Admin) 설정 가이드

> 관리자 권한은 Supabase Dashboard의 **Authentication > Users** 에서
> `app_metadata`를 직접 수정하는 방식으로 설정합니다.

---

## 1. 마이그레이션 실행 (최초 1회)

`00002_admin_role.sql`을 아직 실행하지 않았다면:

1. [Supabase Dashboard](https://supabase.com/dashboard) → 본인 프로젝트 선택
2. 좌측 메뉴 **SQL Editor** → **New query**
3. `supabase/migrations/00002_admin_role.sql` 내용 전체 복사 후 붙여넣기
4. **Run** 클릭

실행 결과로 `is_admin()` 헬퍼 함수와 관리자용 RLS 정책이 생성됩니다.

---

## 2. 관리자 계정 지정 (SQL Editor)

1. Dashboard 좌측 메뉴 **SQL Editor** → **New query**
2. 아래 SQL에서 이메일 교체 후 **Run**:

```sql
UPDATE auth.users
SET raw_app_meta_data = raw_app_meta_data || '{"role": "admin"}'::jsonb
WHERE email = '본인이메일@example.com';
```

3. 확인 쿼리:

```sql
SELECT email, raw_app_meta_data FROM auth.users WHERE email = '본인이메일@example.com';
```

4. 해당 계정으로 **로그아웃 후 재로그인** (JWT 갱신 필요)

---

## 3. 관리자 권한 해제

```sql
UPDATE auth.users
SET raw_app_meta_data = raw_app_meta_data - 'role'
WHERE email = '해제할이메일@example.com';
```

---

## 4. 관리자 대시보드 기능 (`/admin`)

관리자 계정으로 로그인 후 `/admin` 경로 접속.
비관리자는 자동으로 홈(`/`)으로 리다이렉트됩니다.

### 통계 카드

| 항목 | 설명 |
|------|------|
| Active Auctions | 현재 진행 중인 경매 수 |
| Total Gold Traded | 전체 거래 완료된 골드 총합 |
| Total Users | 가입된 전체 사용자 수 |

### 활성 경매 관리

- 진행 중인 경매 목록 (종료 임박 순 정렬)
- **Force End**: 해당 경매를 즉시 `cancelled` 상태로 전환

### 사용자 Gold 조정

- 사용자 목록 (최신 가입순 50명)
- **Gold 조정**: 양수(+) 지급 / 음수(-) 차감
  - 예: `+10000` 입력 후 Apply → 해당 사용자 Gold +10,000
  - Gold는 0 미만으로 내려가지 않음

---

## 5. 동작 원리

- 관리자 여부: `auth.jwt() -> 'app_metadata' ->> 'role' = 'admin'`
- `app_metadata`는 클라이언트(브라우저/Unity)에서 변경 불가 — Dashboard 또는 Service Role Key로만 수정 가능
- Web 서버 컴포넌트에서는 `user.app_metadata?.role === "admin"` 으로 체크 후 redirect 처리
- RLS에서는 `public.is_admin()` 헬퍼 함수로 동일하게 체크
