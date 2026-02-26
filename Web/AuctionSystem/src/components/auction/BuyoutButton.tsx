"use client"

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { createClient } from '@/lib/supabase/client'
import { Button } from '@/components/ui/Button'

interface BuyoutButtonProps {
    itemId: string
    buyoutPrice: number
    itemName: string
}

export function BuyoutButton({ itemId, buyoutPrice, itemName }: BuyoutButtonProps) {
    const [isLoading, setIsLoading] = useState(false)
    const router = useRouter()

    const handleBuyout = async () => {
        if (!confirm(`Buy "${itemName}" for ${buyoutPrice.toLocaleString()} G?`)) return

        const supabase = createClient()

        // refreshSession()으로 최신 access_token 확보 (만료 토큰 방지)
        const { data: refreshData } = await supabase.auth.refreshSession()
        const token = refreshData.session?.access_token
        if (!token) {
            alert('Please sign in to buy out.')
            router.push('/login')
            return
        }

        setIsLoading(true)
        try {
            const { data, error } = await supabase.functions.invoke('buyout', {
                body: { item_id: itemId },
                headers: { Authorization: `Bearer ${token}` },
            })

            if (error) {
                let msg = error.message
                try {
                    const ctx = (error as any).context
                    if (ctx instanceof Response) {
                        const text = await ctx.text()
                        console.error('[BuyoutButton] error body:', text)
                        const parsed = text ? JSON.parse(text) : {}
                        msg = parsed?.error ?? parsed?.message ?? msg
                    } else {
                        console.error('[BuyoutButton] error:', error)
                    }
                } catch { /* ignore */ }
                alert(`Buyout failed: ${msg}`)
            } else {
                alert(`Buyout successful! "${itemName}" is now yours.`)
                router.refresh()
            }
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <Button
            variant="primary"
            size="sm"
            className="w-full font-bold bg-blue-600 hover:bg-blue-700"
            onClick={handleBuyout}
            disabled={isLoading}
        >
            {isLoading ? 'Processing...' : `Buyout ${buyoutPrice.toLocaleString()}`}
        </Button>
    )
}
