'use client'

import { useState } from 'react'
import { createClient } from '@/lib/supabase/client'

export function CancelAuctionButton({ itemId, itemName }: { itemId: string; itemName: string }) {
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const handleCancel = async () => {
        if (!confirm(`'${itemName}' 경매를 취소하시겠습니까?\n현재 최고 입찰자에게 골드가 환불되고 아이템이 인벤토리로 반환됩니다.`)) return

        setLoading(true)
        setError(null)

        const supabase = createClient()
        const { data: refreshData } = await supabase.auth.refreshSession()
        const token = refreshData.session?.access_token

        if (!token) {
            setError('로그인이 필요합니다.')
            setLoading(false)
            return
        }

        const { error: fnError } = await supabase.functions.invoke('cancel-auction', {
            body: { item_id: itemId },
            headers: { Authorization: `Bearer ${token}` },
        })

        setLoading(false)

        if (fnError) {
            try {
                const ctx = (fnError as any).context
                if (ctx instanceof Response) {
                    const text = await ctx.text()
                    const parsed = text ? JSON.parse(text) : {}
                    setError(parsed?.error ?? parsed?.message ?? fnError.message)
                    return
                }
            } catch { /* ignore */ }
            setError(fnError.message)
            return
        }

        alert('경매가 취소되었습니다.')
        window.location.reload()
    }

    return (
        <div className="flex flex-col items-end gap-1">
            <button
                onClick={handleCancel}
                disabled={loading}
                className="text-xs px-3 py-1 rounded-full border border-red-300 text-red-500 hover:bg-red-50 disabled:opacity-50 cursor-pointer font-semibold transition-colors"
            >
                {loading ? '취소 중...' : '취소'}
            </button>
            {error && <span className="text-xs text-red-500 font-medium">{error}</span>}
        </div>
    )
}
