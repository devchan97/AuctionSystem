// ─────────────────────────────────────────────
// 시간 포맷
// ─────────────────────────────────────────────

export interface TimeLeft {
    hoursLeft: number
    minutesLeft: number
    isEndingSoon: boolean
    isEnded: boolean
    label: string
}

export function getTimeLeft(endsAt: string | Date): TimeLeft {
    const timeLeftMs = Math.max(0, new Date(endsAt).getTime() - Date.now())
    const hoursLeft = Math.floor(timeLeftMs / (1000 * 60 * 60))
    const minutesLeft = Math.floor((timeLeftMs % (1000 * 60 * 60)) / (1000 * 60))
    const isEnded = timeLeftMs === 0
    const isEndingSoon = !isEnded && hoursLeft === 0 && minutesLeft < 30

    let label: string
    if (isEnded) {
        label = 'Ended'
    } else if (hoursLeft > 0) {
        label = `${hoursLeft}h ${minutesLeft}m`
    } else {
        label = `${minutesLeft}m`
    }

    return { hoursLeft, minutesLeft, isEndingSoon, isEnded, label }
}

// ─────────────────────────────────────────────
// 카테고리 매핑
// ─────────────────────────────────────────────

/** DB에 저장되는 카테고리 값 (Edge Function VALID_CATEGORIES와 동일) */
export const CATEGORY_VALUES = ['Weapons', 'Armor', 'Consumables', 'Misc'] as const
export type CategoryValue = typeof CATEGORY_VALUES[number]

/** URL slug → DB 카테고리 값 (대소문자 정확히 매핑) */
const SLUG_TO_CATEGORY: Record<string, string> = {
    weapons: 'Weapons',
    armor: 'Armor',
    consumables: 'Consumables',
    misc: 'Misc',
}

/**
 * URL slug를 DB 카테고리 값으로 변환합니다.
 * 알 수 없는 slug는 첫 글자 대문자로 fallback합니다.
 */
export function slugToCategory(slug: string): string {
    return SLUG_TO_CATEGORY[slug.toLowerCase()] ?? (slug.charAt(0).toUpperCase() + slug.slice(1))
}
