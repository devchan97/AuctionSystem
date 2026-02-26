-- 알림 삭제 RLS 정책 추가
-- notifications 테이블에 DELETE 정책이 없어서 재접속 시 알림이 복원되는 버그 수정

CREATE POLICY "Own delete" ON notifications
  FOR DELETE USING (auth.uid() = user_id);
