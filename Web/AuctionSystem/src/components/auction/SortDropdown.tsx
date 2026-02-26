'use client'

import { useRouter, useSearchParams } from 'next/navigation'
import { useTransition } from 'react'

const SORT_OPTIONS = [
    { value: 'ends_at',     label: 'Sort by: Ending Soon' },
    { value: 'created_at',  label: 'Sort by: Newest' },
    { value: 'current_bid', label: 'Sort by: Price (High to Low)' },
]

export function SortDropdown() {
    const router = useRouter()
    const searchParams = useSearchParams()
    const [, startTransition] = useTransition()
    const current = searchParams.get('sort') ?? 'ends_at'

    function handleChange(e: React.ChangeEvent<HTMLSelectElement>) {
        const params = new URLSearchParams(searchParams.toString())
        params.set('sort', e.target.value)
        startTransition(() => {
            router.push(`/auction?${params.toString()}`)
        })
    }

    return (
        <select
            value={current}
            onChange={handleChange}
            className="bg-white dark:bg-black border insta-border rounded-lg px-3 py-2 text-sm font-medium outline-none"
        >
            {SORT_OPTIONS.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
        </select>
    )
}
