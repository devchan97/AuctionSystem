'use client'

import { Bell, Trash2 } from 'lucide-react'
import { useState, useEffect } from 'react'
import { createClient } from '@/lib/supabase/client'
import Link from 'next/link'

export function NotificationBell({ userId }: { userId: string }) {
    const [isOpen, setIsOpen] = useState(false)
    const [notifications, setNotifications] = useState<any[]>([])
    const [unreadCount, setUnreadCount] = useState(0)

    const supabase = createClient()

    useEffect(() => {
        // 1. Initial Fetch
        const fetchNotifications = async () => {
            const { data } = await supabase
                .from('notifications')
                .select('*')
                .eq('user_id', userId)
                .order('created_at', { ascending: false })
                .limit(20)

            if (data) {
                setNotifications(data)
                setUnreadCount(data.filter(n => !n.is_read).length)
            }
        }

        fetchNotifications()

        // 2. Realtime Subscription
        const channel = supabase
            .channel(`user-notifications-${userId}`)
            .on(
                'postgres_changes',
                {
                    event: 'INSERT',
                    schema: 'public',
                    table: 'notifications',
                    filter: `user_id=eq.${userId}`
                },
                (payload) => {
                    const newNotif = payload.new
                    setNotifications(prev => [newNotif, ...prev].slice(0, 20))
                    setUnreadCount(prev => prev + 1)
                }
            )
            .subscribe()

        return () => {
            supabase.removeChannel(channel)
        }
    }, [userId, supabase])

    const markAsRead = async () => {
        if (unreadCount === 0) return

        // Optimistic UI update
        setUnreadCount(0)
        setNotifications(prev => prev.map(n => ({ ...n, is_read: true })))

        await supabase
            .from('notifications')
            .update({ is_read: true })
            .eq('user_id', userId)
            .eq('is_read', false)
    }

    const toggleOpen = () => {
        if (!isOpen) {
            markAsRead()
        }
        setIsOpen(!isOpen)
    }

    const deleteNotification = async (notifId: string, e: React.MouseEvent) => {
        e.stopPropagation()
        // 낙관적 UI 업데이트
        const prev = notifications
        setNotifications(p => p.filter(n => n.id !== notifId))
        const { error } = await supabase.from('notifications').delete().eq('id', notifId)
        if (error) {
            // 삭제 실패 시 롤백
            setNotifications(prev)
        }
    }

    return (
        <div className="relative">
            <button
                onClick={toggleOpen}
                className="relative p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors"
                title="Notifications"
            >
                <Bell size={22} className="text-gray-700 dark:text-gray-300" />
                {unreadCount > 0 && (
                    <span className="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 rounded-full border-2 border-[var(--background)]" />
                )}
            </button>

            {isOpen && (
                <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-[var(--background)] border insta-border rounded-xl shadow-xl z-50 overflow-hidden flex flex-col">
                    <div className="p-4 border-b insta-border font-bold flex items-center justify-between">
                        <span>Notifications</span>
                        {unreadCount > 0 && <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full">{unreadCount} New</span>}
                    </div>

                    <div className="max-h-[400px] overflow-y-auto">
                        {notifications.length === 0 ? (
                            <div className="p-8 text-center text-gray-500 text-sm">No notifications yet.</div>
                        ) : (
                            <div className="flex flex-col">
                                {notifications.map((notif: any) => (
                                    <div key={notif.id} className={`p-4 border-b insta-border last:border-b-0 hover:bg-gray-50 dark:hover:bg-gray-900 transition-colors flex gap-3 ${!notif.is_read ? 'bg-blue-50/50 dark:bg-blue-900/10' : ''}`}>
                                        <div className="flex-1">
                                            <div className="text-xs text-gray-500 mb-1 border-b pb-1 w-max">
                                                {new Date(notif.created_at).toLocaleString()}
                                            </div>
                                            <p className="text-sm text-gray-800 dark:text-gray-200">
                                                {notif.message}
                                            </p>
                                            {notif.item_id && (
                                                <Link href={`/auction/${notif.item_id}`} onClick={() => setIsOpen(false)} className="text-xs font-bold text-blue-500 hover:text-blue-600 mt-2 inline-block">
                                                    View Item &rarr;
                                                </Link>
                                            )}
                                        </div>
                                        <button onClick={(e) => deleteNotification(notif.id, e)} className="text-gray-400 hover:text-red-500 p-1 self-start">
                                            <Trash2 size={14} />
                                        </button>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    )
}
