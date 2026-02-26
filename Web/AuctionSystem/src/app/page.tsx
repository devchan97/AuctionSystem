import { AuctionCard } from "@/components/auction/AuctionCard"
import { createClient } from "@/lib/supabase/server"

export default async function Home() {
  const supabase = await createClient()

  // Fetch active items (only first 4 for 'Hot Auctions' row)
  const { data: items, error } = await supabase
    .from('items')
    .select(`
      *,
      seller:profiles!seller_id(username)
    `)
    .eq('status', 'active')
    .gt('ends_at', new Date().toISOString())
    .order('ends_at', { ascending: true })
    .limit(4)

  const activeAuctions = items || []

  return (
    <div className="w-full flex flex-col gap-8 pb-12 w-full">
      {/* Featured Banner Area */}
      <div className="w-full h-48 md:h-64 bg-blue-900 rounded-xl relative overflow-hidden flex items-center p-8">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-900 to-transparent z-10"></div>
        <div className="relative z-20 text-white max-w-lg">
          <span className="bg-blue-600 text-xs font-bold px-2 py-1 rounded uppercase tracking-wider mb-2 inline-block">Featured</span>
          <h1 className="text-3xl md:text-5xl font-black mb-2">Grand Auction Event</h1>
          <p className="opacity-90 leading-snug">Rare mythic weapons now available for bidding. Event ends in 24 hours.</p>
        </div>
      </div>

      <div className="flex flex-col gap-4 w-full">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-gray-100">Hot Auctions</h2>
          <a href="/auction" className="text-blue-600 dark:text-blue-400 font-semibold text-sm hover:underline">View All</a>
        </div>

        {error ? (
          <div className="msg-error">Error loading auctions: {error.message}</div>
        ) : activeAuctions.length === 0 ? (
          <div className="empty-state">
            No active auctions found. Be the first to list an item!
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 w-full">
            {activeAuctions.map((auction: any) => (
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
    </div>
  );
}
