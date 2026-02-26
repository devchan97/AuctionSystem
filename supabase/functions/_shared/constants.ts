/** 유효한 카테고리 값 (DB / Edge Function 공통) */
export const VALID_CATEGORIES = ["Weapons", "Armor", "Consumables", "Misc"] as const;

/** 즉시구매 거래 수수료율 (5%) */
export const FEE_RATE = 0.05;

/** 경매 지속 시간 제한 */
export const MIN_DURATION_HOURS = 1;
export const MAX_DURATION_DAYS = 7;
