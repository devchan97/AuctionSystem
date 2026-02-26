'use client'

import { Search } from 'lucide-react'
import { useRouter, useSearchParams } from 'next/navigation'
import { useRef, useTransition } from 'react'

export function SearchBar() {
    const router = useRouter()
    const searchParams = useSearchParams()
    const [, startTransition] = useTransition()
    const inputRef = useRef<HTMLInputElement>(null)

    function submit(value: string) {
        const params = new URLSearchParams(searchParams.toString())
        if (value.trim()) {
            params.set('search', value.trim())
        } else {
            params.delete('search')
        }
        startTransition(() => {
            router.push(`/auction?${params.toString()}`)
        })
    }

    function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key === 'Enter') {
            submit((e.target as HTMLInputElement).value)
        }
    }

    function handleSearchClick() {
        if (inputRef.current) submit(inputRef.current.value)
    }

    return (
        <div className="flex-1 max-w-xl relative">
            <button
                type="button"
                onClick={handleSearchClick}
                className="absolute inset-y-0 left-3 flex items-center cursor-pointer"
            >
                <Search size={18} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors" />
            </button>
            <input
                ref={inputRef}
                type="text"
                defaultValue={searchParams.get('search') ?? ''}
                onKeyDown={handleKeyDown}
                placeholder="Search items, weapons, armor..."
                className="w-full pl-10 pr-4 py-2 bg-gray-100 dark:bg-gray-900 border-none rounded-full text-sm focus:ring-2 focus:ring-blue-500 outline-none transition-all"
            />
        </div>
    )
}
