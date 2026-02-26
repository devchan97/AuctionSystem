'use client'

import { LogOut } from 'lucide-react'
import { createClient } from '@/lib/supabase/client'

export function LogoutButton() {
    const supabase = createClient()

    const handleLogout = async () => {
        if (!confirm('Log out?')) return
        await supabase.auth.signOut()
        window.location.href = '/'
    }

    return (
        <button
            onClick={handleLogout}
            className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors cursor-pointer"
            title="Log out"
        >
            <LogOut size={22} className="text-red-500" />
        </button>
    )
}
