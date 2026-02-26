'use client'

import { useEffect, useState } from 'react'
import { createClient } from '@/lib/supabase/client'

export function useItemPresence(itemId: string, userId: string | undefined) {
    const [viewerCount, setViewerCount] = useState(0)

    useEffect(() => {
        if (!itemId) return
        const supabase = createClient()

        const channel = supabase.channel(`item-presence-${itemId}`, {
            config: { presence: { key: userId ?? 'anon' } },
        })

        channel
            .on('presence', { event: 'sync' }, () => {
                const state = channel.presenceState()
                setViewerCount(Object.keys(state).length)
            })
            .subscribe(async (status) => {
                if (status === 'SUBSCRIBED' && userId) {
                    await channel.track({ user_id: userId, item_id: itemId })
                }
            })

        return () => {
            channel.untrack().then(() => supabase.removeChannel(channel))
        }
    }, [itemId, userId])

    return viewerCount
}
