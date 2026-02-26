'use client'

import { Eye } from 'lucide-react'
import { useItemPresence } from '@/hooks/useItemPresence'

interface Props {
    itemId: string
    userId: string | undefined
}

export function ViewerBadge({ itemId, userId }: Props) {
    const count = useItemPresence(itemId, userId)

    return (
        <div className="flex items-center gap-1.5 text-sm text-gray-500 font-medium">
            <Eye size={15} />
            <span>{count}명 보는 중</span>
        </div>
    )
}
