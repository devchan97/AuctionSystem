import { AuctionCard } from "@/components/auction/AuctionCard"
import { SortDropdown } from "@/components/auction/SortDropdown"
import { createClient } from "@/lib/supabase/server"

type AuctionListItem = {
    id: string
    name: string
    image_url: string | null
    current_bid: number
    buyout_price: number | null
    ends_at: string
    seller: { username: string } | null
}

type SearchParams = {
    search?: string
    sort?: string
}

export default async function AuctionList({
    searchParams,
}: {
    searchParams: Promise<SearchParams>
}) {
    const { search, sort } = await searchParams
    const supabase = await createClient()

    let query = supabase
        .from('items')
        .select(`
      *,
      seller:profiles!seller_id(username)
    `)
        .eq('status', 'active')
        .gt('ends_at', new Date().toISOString())

    if (search?.trim()) {
        query = query.ilike('name', `%${search.trim()}%`)
    }

    const sortBy = sort ?? 'ends_at'
    const ascending = sortBy !== 'current_bid'
    query = query.order(sortBy, { ascending })

    const { data: items, error } = await query
    const activeAuctions = (items as AuctionListItem[] | null) ?? []

    return (
        <div className="w-full pb-12">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Browse Auctions</h1>
                    <p className="text-gray-500 mt-1">
                        {search ? `Search results for "${search}"` : 'Discover rare items and place your bids'}
                    </p>
                </div>

                <div className="flex gap-2">
                    <SortDropdown />
                </div>
            </div>

            {error ? (
                <div className="msg-error">Error loading auctions: {error.message}</div>
            ) : activeAuctions.length === 0 ? (
                <div className="empty-state">
                    {search ? `No items found for "${search}".` : 'There are currently no active auctions.'}
                </div>
            ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 w-full">
                    {activeAuctions.map((auction) => (
                        <AuctionCard
                            key={auction.id}
                            id={auction.id}
                            sellerName={auction.seller?.username || 'Unknown'}
                            itemName={auction.name}
                            imageUrl={auction.image_url}
                            currentBid={auction.current_bid}
                            buyoutPrice={auction.buyout_price}
                            endsAt={auction.ends_at}
                            bidsCount={0}
                        />
                    ))}
                </div>
            )}
        </div>
    )
}
