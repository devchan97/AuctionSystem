"use client"

import { useState, useEffect } from 'react'
import { createClient } from '@/lib/supabase/client'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Package, Upload } from 'lucide-react'
import { useRouter } from 'next/navigation'
import { CATEGORY_VALUES } from '@/lib/utils'

type InventoryOption = {
    id: string
    item_id: string
    item_name: string
    item_category: string | null
}

const CATEGORIES = CATEGORY_VALUES // utils.ts 중앙화 — Weapons/Armor/Consumables/Misc
const DURATIONS = [
    { label: '1 Hour',   value: '1' },
    { label: '6 Hours',  value: '6' },
    { label: '12 Hours', value: '12' },
    { label: '24 Hours', value: '24' },
    { label: '48 Hours', value: '48' },
    { label: '7 Days',   value: '168' },
]

type Tab = 'inventory' | 'direct'

export default function CreateListingPage() {
    const router = useRouter()
    const supabase = createClient()

    const [tab, setTab] = useState<Tab>('inventory')

    // 인벤토리 탭
    const [inventory, setInventory] = useState<InventoryOption[]>([])
    const [invLoading, setInvLoading] = useState(true)
    const [selectedInvId, setSelectedInvId] = useState('')

    // 직접 등록 탭
    const [imageFile, setImageFile] = useState<File | null>(null)

    // 공통 필드
    const [name, setName] = useState('')
    const [description, setDescription] = useState('')
    const [category, setCategory] = useState<string>(CATEGORY_VALUES[0]) // 기본값: Weapons
    const [startPrice, setStartPrice] = useState('100')
    const [buyoutPrice, setBuyoutPrice] = useState('')
    const [duration, setDuration] = useState('24')

    const [isSubmitting, setIsSubmitting] = useState(false)
    const [errorPayload, setErrorPayload] = useState<string | null>(null)

    useEffect(() => {
        async function init() {
            const { data: { user } } = await supabase.auth.getUser()
            if (!user) { router.replace('/login'); return }

            const { data: invRows } = await supabase
                .from('inventory')
                .select('id, item_id, status')
                .eq('owner_id', user.id)
                .eq('status', 'owned')
                .order('acquired_at', { ascending: false })

            if (!invRows || invRows.length === 0) { setInvLoading(false); return }

            const itemIds = invRows.map(r => r.item_id)
            const { data: items } = await supabase
                .from('items')
                .select('id, name, category')
                .in('id', itemIds)

            const itemMap: Record<string, { name: string; category: string | null }> = {}
            items?.forEach(it => { itemMap[it.id] = it })

            const opts: InventoryOption[] = invRows.map(r => ({
                id: r.id,
                item_id: r.item_id,
                item_name: itemMap[r.item_id]?.name ?? 'Unknown',
                item_category: itemMap[r.item_id]?.category ?? null,
            }))

            setInventory(opts)
            if (opts.length > 0) {
                setSelectedInvId(opts[0].id)
                setName(opts[0].item_name)
                setCategory(opts[0].item_category ?? 'Misc')
            }
            setInvLoading(false)
        }
        init()
    }, [])

    function handleInvChange(invId: string) {
        setSelectedInvId(invId)
        const found = inventory.find(i => i.id === invId)
        if (found) {
            setName(found.item_name)
            setCategory(found.item_category ?? 'Misc')
        }
    }

    function switchTab(t: Tab) {
        setTab(t)
        setErrorPayload(null)
        // 탭 전환 시 공통 필드 초기화
        setName('')
        setCategory('Misc')
        if (t === 'inventory' && inventory.length > 0) {
            setSelectedInvId(inventory[0].id)
            setName(inventory[0].item_name)
            setCategory(inventory[0].item_category ?? 'Misc')
        }
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        setErrorPayload(null)

        const sp = parseInt(startPrice, 10)
        const bp = buyoutPrice ? parseInt(buyoutPrice, 10) : null
        if (!sp || sp <= 0) { setErrorPayload('Starting bid must be a positive number.'); return }
        if (bp !== null && bp <= sp) { setErrorPayload('Buyout price must be greater than starting bid.'); return }

        setIsSubmitting(true)
        try {
            const { data: { user } } = await supabase.auth.getUser()
            if (!user) throw new Error('You must be logged in.')

            if (tab === 'inventory') {
                // ── 인벤토리 등록: Edge Function ─────────────────────────────
                if (!selectedInvId) throw new Error('Please select an item from your inventory.')

                const { data: { session } } = await supabase.auth.getSession()
                if (!session) throw new Error('Session expired. Please log in again.')

                const payload = {
                    inventory_item_id: selectedInvId,
                    name,
                    description,
                    category: category || 'Misc',
                    start_price: sp,
                    buyout_price: bp,
                    duration_hours: parseInt(duration, 10),
                }
                const { data, error } = await supabase.functions.invoke('list-item', {
                    body: payload,
                    headers: { Authorization: `Bearer ${session.access_token}` },
                })
                if (error) {
                    // non-2xx 상세 메시지 추출
                    let msg = error.message
                    try {
                        const ctx = (error as any).context
                        const json = await ctx?.json?.()
                        if (json?.error) msg = json.error
                        else if (json?.message) msg = json.message
                    } catch { /* ignore */ }
                    throw new Error(msg)
                }
                if (data?.error) throw new Error(data.error)

            } else {
                // ── 직접 등록: 이미지 업로드 후 items 테이블 insert ──────────
                if (!name.trim()) throw new Error('Item name is required.')

                let publicImageUrl: string | null = null
                if (imageFile) {
                    const ext = imageFile.name.split('.').pop()
                    const fileName = `${user.id}/${Date.now()}.${ext}`
                    const { error: upErr } = await supabase.storage.from('item-images').upload(fileName, imageFile)
                    if (upErr) throw new Error('Image upload failed: ' + upErr.message)
                    const { data: { publicUrl } } = supabase.storage.from('item-images').getPublicUrl(fileName)
                    publicImageUrl = publicUrl
                }

                const endsAt = new Date(Date.now() + parseInt(duration, 10) * 3600 * 1000).toISOString()
                const { error } = await supabase.from('items').insert({
                    seller_id: user.id,
                    name: name.trim(),
                    description: description || null,
                    image_url: publicImageUrl,
                    category: category || 'Misc',
                    start_price: sp,
                    buyout_price: bp,
                    current_bid: 0,
                    ends_at: endsAt,
                    status: 'active',
                })
                if (error) throw new Error(error.message)
            }

            router.push('/my-page')
            router.refresh()
        } catch (err: unknown) {
            setErrorPayload(err instanceof Error ? err.message : 'An unknown error occurred.')
        } finally {
            setIsSubmitting(false)
        }
    }

    const activeTab = 'bg-blue-600 text-white'
    const inactiveTab = 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'

    return (
        <div className="w-full max-w-2xl mx-auto pb-12">
            <div className="mb-8">
                <h1 className="text-3xl font-bold tracking-tight">Create Listing</h1>
                <p className="text-gray-500 mt-1">List an item for auction.</p>
            </div>

            {/* 탭 */}
            <div className="flex gap-2 mb-6">
                <button
                    type="button"
                    onClick={() => switchTab('inventory')}
                    className={`px-5 py-2 rounded-full text-sm font-semibold transition-colors ${tab === 'inventory' ? activeTab : inactiveTab}`}
                >
                    From Inventory
                </button>
                <button
                    type="button"
                    onClick={() => switchTab('direct')}
                    className={`px-5 py-2 rounded-full text-sm font-semibold transition-colors ${tab === 'direct' ? activeTab : inactiveTab}`}
                >
                    Direct Listing
                </button>
            </div>

            <form onSubmit={handleSubmit} className="bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl p-6 shadow-sm flex flex-col gap-6">
                {errorPayload && <div className="msg-error">{errorPayload}</div>}

                {/* ── 인벤토리 탭 ── */}
                {tab === 'inventory' && (
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Select from Inventory *</label>
                        {invLoading ? (
                            <div className="h-11 bg-gray-100 dark:bg-gray-900 rounded-lg animate-pulse" />
                        ) : inventory.length === 0 ? (
                            <div className="flex items-center gap-3 p-4 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-xl text-gray-500 text-sm">
                                <Package size={20} />
                                <span>No items in inventory. Win an auction first!</span>
                            </div>
                        ) : (
                            <select
                                className="bg-white dark:bg-black border insta-border rounded-lg px-3 h-11 text-sm outline-none w-full"
                                value={selectedInvId}
                                onChange={e => handleInvChange(e.target.value)}
                                required
                            >
                                {inventory.map(inv => (
                                    <option key={inv.id} value={inv.id}>
                                        {inv.item_name} [{inv.item_category ?? 'Misc'}]
                                    </option>
                                ))}
                            </select>
                        )}
                    </div>
                )}

                {/* ── 직접 등록 탭 ── */}
                {tab === 'direct' && (
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Image <span className="text-gray-400 font-normal">(optional)</span></label>
                        <div className="border-2 border-dashed border-gray-300 dark:border-gray-700 rounded-xl p-5 flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900/50">
                            <Upload size={24} className="text-gray-400 mb-2" />
                            <input
                                type="file"
                                accept="image/*"
                                onChange={e => setImageFile(e.target.files?.[0] ?? null)}
                                className="text-sm text-gray-500 file:mr-3 file:py-1.5 file:px-4 file:rounded-full file:border-0 file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                            />
                            {imageFile && <p className="text-xs text-green-600 mt-2 font-medium">{imageFile.name}</p>}
                            <p className="text-xs text-gray-400 mt-1">Max 5MB · PNG, JPG, GIF</p>
                        </div>
                    </div>
                )}

                {/* ── 공통 필드 ── */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Item Name *</label>
                        <Input
                            placeholder="e.g. Sword of Truths"
                            required
                            value={name}
                            onChange={e => setName(e.target.value)}
                        />
                    </div>
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Category</label>
                        <select
                            className="bg-white dark:bg-black border insta-border rounded-lg px-3 h-11 text-sm outline-none w-full"
                            value={category}
                            onChange={e => setCategory(e.target.value)}
                        >
                            {CATEGORIES.map(c => <option key={c} value={c}>{c}</option>)}
                        </select>
                    </div>
                </div>

                <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold">Description</label>
                    <textarea
                        className="bg-white dark:bg-black border insta-border rounded-lg p-3 text-sm min-h-[80px] outline-none w-full resize-y"
                        placeholder="Item description..."
                        value={description}
                        onChange={e => setDescription(e.target.value)}
                    />
                </div>

                <hr className="border-gray-100 dark:border-gray-800" />

                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Starting Bid (G) *</label>
                        <Input type="number" min="1" required value={startPrice} onChange={e => setStartPrice(e.target.value)} />
                    </div>
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Buyout Price (G)</label>
                        <Input type="number" placeholder="Optional" value={buyoutPrice} onChange={e => setBuyoutPrice(e.target.value)} />
                    </div>
                    <div className="flex flex-col gap-2">
                        <label className="text-sm font-semibold">Duration *</label>
                        <select
                            className="bg-white dark:bg-black border insta-border rounded-lg px-3 h-11 text-sm outline-none w-full"
                            value={duration}
                            onChange={e => setDuration(e.target.value)}
                        >
                            {DURATIONS.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
                        </select>
                    </div>
                </div>

                <div className="pt-2 flex items-center justify-end gap-3">
                    <Button variant="ghost" type="button" onClick={() => router.back()} disabled={isSubmitting}>
                        Cancel
                    </Button>
                    <Button
                        variant="primary"
                        type="submit"
                        className="px-8 font-bold"
                        disabled={isSubmitting || (tab === 'inventory' && inventory.length === 0)}
                    >
                        {isSubmitting ? 'Registering...' : 'Start Auction'}
                    </Button>
                </div>
            </form>
        </div>
    )
}
