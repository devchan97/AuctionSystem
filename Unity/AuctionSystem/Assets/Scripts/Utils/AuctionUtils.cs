using System;
using AuctionSystem.Models;
using UnityEngine;

namespace AuctionSystem.Utils
{
    public static class AuctionUtils
    {
        public static readonly string[] CategoryDisplayNames = { "전체", "무기", "방어구", "소비", "기타" };
        public static readonly string[] CategoryValues       = { "", "Weapons", "Armor", "Consumables", "Misc" };

        public static readonly string[] ListCategoryDisplayNames = { "무기", "방어구", "소비", "기타" };
        public static readonly string[] ListCategoryValues       = { "Weapons", "Armor", "Consumables", "Misc" };

        public static string ParseError(string responseBody, string fallback = "서버 오류가 발생했습니다.")
        {
            if (string.IsNullOrEmpty(responseBody)) return fallback;
            try
            {
                var err = JsonUtility.FromJson<ApiError>(responseBody);
                if (err != null)
                {
                    // error_code 우선 — Supabase Auth가 반환하는 구조화된 코드
                    if (!string.IsNullOrEmpty(err.error_code))
                        return TranslateErrorCode(err.error_code);

                    if (!string.IsNullOrEmpty(err.error_description)) return err.error_description;
                    if (!string.IsNullOrEmpty(err.message))           return err.message;
                    if (!string.IsNullOrEmpty(err.msg))               return err.msg;
                    if (!string.IsNullOrEmpty(err.error))             return err.error;
                }
            }
            catch { }
            return fallback;
        }

        private static string TranslateErrorCode(string code)
        {
            switch (code)
            {
                case "invalid_credentials":   return "이메일 또는 비밀번호가 올바르지 않습니다.";
                case "email_not_confirmed":   return "이메일 인증이 완료되지 않았습니다. 메일함을 확인해주세요.";
                case "user_not_found":        return "존재하지 않는 계정입니다.";
                case "user_already_exists":   return "이미 가입된 이메일입니다.";
                case "email_address_invalid": return "유효하지 않은 이메일 형식입니다.";
                case "weak_password":         return "비밀번호가 너무 단순합니다. 더 강한 비밀번호를 사용해주세요.";
                case "over_request_rate_limit": return "요청이 너무 많습니다. 잠시 후 다시 시도해주세요.";
                case "session_not_found":     return "세션이 만료되었습니다. 다시 로그인해주세요.";
                default:                      return $"오류가 발생했습니다. ({code})";
            }
        }

        public static string GetTimeLeftText(string endsAt)
        {
            if (string.IsNullOrEmpty(endsAt)) return "-";
            if (!DateTime.TryParse(endsAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime end))
                return "-";

            TimeSpan remaining = end.ToUniversalTime() - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)    return "종료됨";
            if (remaining.TotalHours >= 1)      return $"{(int)remaining.TotalHours}시간 {remaining.Minutes}분 남음";
            return $"{remaining.Minutes}분 {remaining.Seconds}초 남음";
        }

        public static string GetTimeLeftShort(string endsAt)
        {
            if (string.IsNullOrEmpty(endsAt)) return "-";
            if (!DateTime.TryParse(endsAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime end))
                return "-";

            TimeSpan remaining = end.ToUniversalTime() - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0) return "종료됨";
            if (remaining.TotalHours >= 1)   return $"{(int)remaining.TotalHours}시간 {remaining.Minutes}분";
            return $"{remaining.Minutes}분 {remaining.Seconds}초";
        }
    }
}
