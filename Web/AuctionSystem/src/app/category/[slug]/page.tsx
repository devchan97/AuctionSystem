import { createClient } from '@/lib/supabase/server'
import { AuctionCard } from '@/components/auction/AuctionCard'
import { SortDropdown } from '@/components/auction/SortDropdown'
import { slugToCategory } from '@/lib/utils'

type CategoryItem = {
    id: string
    name: string
    image_url: string | null
    current_bid: number
    buyout_price: number | null
    ends_at: string
    seller: { username: string } | null
}

type SearchParams = { sort?: string }

export default async function CategoryPage({
    params,
    searchParams,
}: {
    params: Promise<{ slug: string }>
    searchParams: Promise<SearchParams>
}) {
    const { slug } = await params
    const { sort } = await searchParams
    const displayCategory = slugToCategory(slug)

    const supabase = await createClient()

    const sortBy = sort ?? 'ends_at'
    const ascending = sortBy !== 'current_bid'

    const { data: items, error } = await supabase
        .from('items')
        .select(`
      *,
      seller:profiles!seller_id(username)
    `)
        .eq('status', 'active')
        .eq('category', displayCategory)
        .gt('ends_at', new Date().toISOString())
        .order(sortBy, { ascending })

    const activeAuctions = (items as CategoryItem[] | null) ?? []

    return (
        <div className="w-full flex flex-col gap-8 pb-12">
            <div className="border-b insta-border pb-6 pt-4 flex flex-col md:flex-row md:items-end justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-black">{displayCategory} Auctions</h1>
                    <p className="text-gray-500 mt-2 text-sm">
                        Browse active {displayCategory.toLowerCase()} items available for bidding or buyout.
                    </p>
                </div>
                <div className="flex items-center gap-3">
                    <span className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-700 dark:text-blue-300 px-3 py-1.5 rounded-lg text-sm font-semibold">
                        {activeAuctions.length} items
                    </span>
                    <SortDropdown />
                </div>
            </div>

            {error ? (
                <div className="msg-error">Failed to load category items: {error.message}</div>
            ) : activeAuctions.length === 0 ? (
                <div className="empty-state">
                    No active auctions found in this category.
                </div>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                    {activeAuctions.map((item) => (
                        <AuctionCard
                            key={item.id}
                            id={item.id}
                            itemName={item.name}
                            sellerName={item.seller?.username || 'Unknown'}
                            currentBid={item.current_bid}
                            buyoutPrice={item.buyout_price}
                            endsAt={item.ends_at}
                            imageUrl={item.image_url}
                            bidsCount={0}
                        />
                    ))}
                </div>
            )}
        </div>
    )
}
