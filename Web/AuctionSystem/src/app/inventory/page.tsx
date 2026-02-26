import { PackageSearch, Calendar, Package } from 'lucide-react'
import { createClient } from '@/lib/supabase/server'
import { redirect } from 'next/navigation'

type InventoryItem = {
    id: string
    acquired_at: string
    item: {
        id: string
        name: string
        category: string | null
        image_url: string | null
    } | null
}

const CATEGORY_COLORS: Record<string, string> = {
    Weapons:     'bg-red-50 text-red-700 border-red-200 dark:bg-red-900/20 dark:text-red-400 dark:border-red-800',
    Armor:       'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/20 dark:text-blue-400 dark:border-blue-800',
    Consumables: 'bg-green-50 text-green-700 border-green-200 dark:bg-green-900/20 dark:text-green-400 dark:border-green-800',
    Misc:        'bg-gray-50 text-gray-600 border-gray-200 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700',
}

function CategoryBadge({ category }: { category: string | null }) {
    const cat = category ?? 'Misc'
    const colors = CATEGORY_COLORS[cat] ?? CATEGORY_COLORS['Misc']
    return (
        <span className={`text-xs font-bold px-2 py-0.5 rounded-full border ${colors}`}>
            {cat}
        </span>
    )
}

function ItemIcon({ imageUrl, name }: { imageUrl: string | null; name: string }) {
    if (imageUrl) {
        return (
            <div className="w-12 h-12 rounded-lg overflow-hidden border insta-border">
                <img src={imageUrl} alt={name} className="w-full h-full object-cover" />
            </div>
        )
    }
    return (
        <div className="w-12 h-12 bg-gray-100 dark:bg-gray-800 rounded-lg flex items-center justify-center border insta-border">
            <Package size={20} className="text-gray-400" />
        </div>
    )
}

export default async function InventoryPage() {
    const supabase = await createClient()

    const { data: { user } } = await supabase.auth.getUser()
    if (!user) redirect('/login')

    const { data: inventory, error } = await supabase
        .from('inventory')
        .select(`
      id,
      acquired_at,
      item:items (
         id,
         name,
         category,
         image_url
      )
    `)
        .eq('owner_id', user.id)
        .order('acquired_at', { ascending: false })

    const items = (inventory as InventoryItem[] | null) ?? []

    return (
        <div className="w-full pb-12">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">My Inventory</h1>
                    <p className="text-gray-500 mt-1">Items you have successfully won or purchased outright.</p>
                </div>

                <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-700 dark:text-blue-300 px-4 py-2 rounded-lg font-semibold flex items-center gap-2">
                    <PackageSearch size={18} />
                    {items.length} Items Total
                </div>
            </div>

            {error && (
                <div className="msg-error mb-8">Failed to load inventory: {error.message}</div>
            )}

            {!error && items.length === 0 && (
                <div className="empty-state">
                    You don&apos;t have any items yet. Win an auction to see them here!
                </div>
            )}

            {!error && items.length > 0 && (
                <div className="bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl overflow-hidden shadow-sm">
                    <div className="overflow-x-auto">
                        <table className="w-full text-left border-collapse">
                            <thead>
                                <tr className="bg-gray-50 dark:bg-gray-900 border-b insta-border text-xs uppercase tracking-wider text-gray-500 font-semibold">
                                    <th className="p-4 w-16">Item</th>
                                    <th className="p-4">Name</th>
                                    <th className="p-4">Category</th>
                                    <th className="p-4 hidden sm:table-cell">Acquired Date</th>
                                    <th className="p-4 text-right">Status</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                                {items.map((inv) => {
                                    const itemData = inv.item
                                    const acquiredDate = new Date(inv.acquired_at).toLocaleDateString()
                                    return (
                                        <tr key={inv.id} className="hover:bg-gray-50 dark:hover:bg-gray-900 transition-colors">
                                            <td className="p-4">
                                                <ItemIcon
                                                    imageUrl={itemData?.image_url ?? null}
                                                    name={itemData?.name ?? ''}
                                                />
                                            </td>
                                            <td className="p-4">
                                                <div className="font-bold text-gray-900 dark:text-gray-100">
                                                    {itemData?.name ?? 'Unknown Item'}
                                                </div>
                                                <div className="text-xs text-gray-400 mt-0.5 font-mono">
                                                    #{itemData?.id.substring(0, 8) ?? 'N/A'}
                                                </div>
                                            </td>
                                            <td className="p-4">
                                                <CategoryBadge category={itemData?.category ?? null} />
                                            </td>
                                            <td className="p-4 hidden sm:table-cell text-sm text-gray-500">
                                                <div className="flex items-center gap-1.5">
                                                    <Calendar size={14} /> {acquiredDate}
                                                </div>
                                            </td>
                                            <td className="p-4 text-right">
                                                <span className="font-bold text-green-600 dark:text-green-400 text-sm">
                                                    Acquired
                                                </span>
                                            </td>
                                        </tr>
                                    )
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    )
}
