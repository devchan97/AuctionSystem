'use client'

import { useEffect } from 'react'
import { createClient } from '@/lib/supabase/client'
import { useRouter } from 'next/navigation'

export function RealtimeBidWatcher({ itemId }: { itemId: string }) {
    const router = useRouter()
    const supabase = createClient()

    useEffect(() => {
        // Subscribe to Bids insertions for this item
        const bidChannel = supabase
            .channel(`public:bids:item_id=eq.${itemId}`)
            .on(
                'postgres_changes',
                {
                    event: 'INSERT',
                    schema: 'public',
                    table: 'bids',
                    filter: `item_id=eq.${itemId}`
                },
                () => {
                    // When a new bid drops, ask Next.js server to refresh the page async
                    router.refresh()
                }
            )
            .subscribe()

        // Subscribe to Item updates for this item (e.g. status changes to 'sold')
        const itemChannel = supabase
            .channel(`public:items:id=eq.${itemId}`)
            .on(
                'postgres_changes',
                {
                    event: 'UPDATE',
                    schema: 'public',
                    table: 'items',
                    filter: `id=eq.${itemId}`
                },
                () => {
                    router.refresh()
                }
            )
            .subscribe()

        return () => {
            supabase.removeChannel(bidChannel)
            supabase.removeChannel(itemChannel)
        }
    }, [itemId, router, supabase])

    return null; // Invisible functional watcher component
}
