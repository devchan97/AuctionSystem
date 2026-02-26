import { Clock, History, Tag, ShieldCheck, ChevronRight } from 'lucide-react'
import { createClient } from '@/lib/supabase/server'
import { notFound } from 'next/navigation'
import { BidClientActions } from '@/components/auction/BidClientActions'
import { RealtimeBidWatcher } from '@/components/auction/RealtimeBidWatcher'
import { ViewerBadge } from '@/components/auction/ViewerBadge'
import { CancelAuctionButton } from '@/components/auction/CancelAuctionButton'
import { getTimeLeft } from '@/lib/utils'
import type { Database } from '@/types/supabase'

export default async function AuctionDetail({ params }: { params: Promise<{ id: string }> }) {
    const { id } = await params;
    const supabase = await createClient();
    const { data: { user } } = await supabase.auth.getUser();

    // 1. Fetch Item Details
    const { data: itemData, error: itemError } = await supabase
        .from('items')
        .select(`
      *,
      seller:profiles!seller_id(username)
    `)
        .eq('id', id)
        .single();

    type ItemRow = Database['public']['Tables']['items']['Row'] & {
        seller: { username: string } | null
    }

    if (itemError || !itemData) {
        return notFound()
    }
    const item = itemData as unknown as ItemRow

    // 2. Fetch Bid History
    type BidWithBidder = {
        id: string
        amount: number
        created_at: string
        bidder: { username: string } | null
    }
    const { data: bids } = await supabase
        .from('bids')
        .select(`
       id,
       amount,
       created_at,
       bidder:profiles!bidder_id(username)
    `)
        .eq('item_id', id)
        .order('amount', { ascending: false })

    const history = (bids as BidWithBidder[] | null) ?? []

    // Calculate Time
    const { hoursLeft, minutesLeft, isEnded } = getTimeLeft(item.ends_at)

    return (
        <div className="w-full pb-12">
            <RealtimeBidWatcher itemId={item.id} />

            {/* Breadcrumbs */}
            <div className="text-sm text-gray-500 mb-6 flex items-center gap-2 font-medium">
                <a href="/" className="hover:text-gray-900 dark:hover:text-gray-100">Home</a>
                <ChevronRight size={14} />
                <a href="/auction" className="hover:text-gray-900 dark:hover:text-gray-100">Auctions</a>
                <ChevronRight size={14} />
                <span className="text-gray-900 dark:text-gray-100 truncate">{item.name}</span>
            </div>

            <div className="flex flex-col lg:flex-row gap-8 lg:gap-12 w-full">
                {/* Left Side: Product Image Showcase */}
                <div className="w-full lg:w-1/2 flex flex-col gap-4">
                    <div className="w-full aspect-square bg-gray-100 dark:bg-[#0a0a0a] rounded-2xl flex items-center justify-center border insta-border overflow-hidden relative">
                        {item.image_url?.trim() ? (
                            <img src={item.image_url} alt={item.name} className="object-cover w-full h-full" />
                        ) : (
                            <div className="text-gray-400 font-medium">No Image</div>
                        )}

                        <div className="absolute top-4 left-4 bg-white/90 dark:bg-black/90 backdrop-blur px-3 py-1 rounded-full text-xs font-bold border insta-border shadow-sm flex items-center gap-1">
                            <Tag size={12} /> {item.category || 'Misc'}
                        </div>
                    </div>

                    <div className="border insta-border rounded-xl p-6 bg-white dark:bg-[#0a0a0a]">
                        <h3 className="font-bold text-lg mb-3 flex items-center gap-2"><History size={18} /> Bid History</h3>
                        {history.length > 0 ? (
                            <div className="flex flex-col gap-3 max-h-[300px] overflow-y-auto">
                                {history.map((h, i) => {
                                    const bidTime = new Date(h.created_at).toLocaleString();
                                    return (
                                        <div key={h.id} className={`flex justify-between items-center p-3 rounded-lg ${i === 0 ? 'bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800' : 'bg-gray-50 dark:bg-gray-900 border border-transparent'}`}>
                                            <div className="flex flex-col">
                                                <span className={`font-semibold ${i === 0 ? 'text-green-700 dark:text-green-400' : ''}`}>{h.bidder?.username || 'Unknown'}</span>
                                                <span className="text-xs text-gray-500">{bidTime}</span>
                                            </div>
                                            <span className={`font-bold ${i === 0 ? 'text-green-600 dark:text-green-400' : 'text-gray-700 dark:text-gray-300'}`}>{h.amount.toLocaleString()} G</span>
                                        </div>
                                    )
                                })}
                            </div>
                        ) : (
                            <div className="text-center text-gray-500 py-4 text-sm">No bids yet. Be the first!</div>
                        )}
                    </div>
                </div>

                {/* Right Side: Auction Details & Action Area */}
                <div className="w-full lg:w-1/2 flex flex-col pt-2">
                    {isEnded && (
                        <div className="bg-red-100 text-red-700 px-4 py-2 font-bold rounded-lg mb-4 self-start border border-red-200">
                            This auction has ended.
                        </div>
                    )}

                    <div className="flex items-start justify-between gap-4 mb-2">
                        <h1 className="text-3xl lg:text-4xl font-black text-gray-900 dark:text-gray-100 leading-tight">
                            {item.name}
                        </h1>
                        {user && item.seller_id === user.id && !isEnded && (
                            <CancelAuctionButton itemId={item.id} itemName={item.name} />
                        )}
                    </div>
                    <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                            <ShieldCheck size={16} className="text-blue-500" />
                            <span>Sold by <span className="font-bold text-gray-900 dark:text-gray-100">{item.seller?.username || 'Unknown'}</span></span>
                        </div>
                        <ViewerBadge itemId={item.id} userId={user?.id} />
                    </div>

                    {item.description && (
                        <p className="text-gray-700 dark:text-gray-300 mb-8 leading-relaxed whitespace-pre-wrap">
                            {item.description}
                        </p>
                    )}

                    {/* Pricing Box */}
                    <div className={`border-2 rounded-2xl p-6 mb-8 relative overflow-hidden ${isEnded ? 'border-gray-200 bg-gray-50 dark:border-gray-800 dark:bg-gray-900' : 'border-green-500/20 bg-green-50/50 dark:border-green-500/30 dark:bg-green-900/10'}`}>

                        {/* Timer Banner */}
                        {!isEnded && (
                            <div className="flex items-center gap-2 text-red-600 dark:text-red-400 font-bold mb-4 bg-red-100 dark:bg-red-900/30 w-max px-3 py-1 rounded-full text-sm">
                                <Clock size={16} /> Ends in {hoursLeft}h {minutesLeft}m
                            </div>
                        )}

                        <div className="flex flex-col mb-6">
                            <span className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-1">
                                {history.length > 0 ? 'Current Highest Bid' : 'Starting Price'}
                            </span>
                            <div className={`flex items-end gap-2 ${isEnded ? 'text-gray-600 dark:text-gray-400' : 'text-green-600 dark:text-green-400'}`}>
                                <span className="text-5xl font-black">{item.current_bid.toLocaleString()}</span>
                                <span className="text-xl font-bold mb-1">Gold</span>
                            </div>
                        </div>

                        {/* Client Component injected here to handle Edge Function calls */}
                        <BidClientActions
                            itemId={item.id}
                            currentBid={item.current_bid}
                            buyoutPrice={item.buyout_price}
                            endsAt={item.ends_at}
                        />

                    </div>
                </div>
            </div>
        </div>
    )
}
