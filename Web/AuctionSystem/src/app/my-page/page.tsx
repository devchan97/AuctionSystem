import { CircleDollarSign, Plus, Gavel, ArrowUpRight } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import Link from 'next/link'
import { createClient } from '@/lib/supabase/server'
import { redirect } from 'next/navigation'
import { CancelAuctionButton } from '@/components/auction/CancelAuctionButton'

export default async function MyPage() {
    const supabase = await createClient()

    // 1. Get authenticated user
    const { data: { user } } = await supabase.auth.getUser()

    if (!user) {
        redirect('/login')
    }

    // 2. Fetch Profile (Gold Balance)
    const { data: profileData } = await supabase
        .from('profiles')
        .select('*')
        .eq('id', user.id)
        .single()

    const profile = profileData as any;

    // 3. Fetch My Active Listings (Items I am selling)
    const { data: myItems } = await supabase
        .from('items')
        .select('*')
        .eq('seller_id', user.id)
        .eq('status', 'active')
        .order('created_at', { ascending: false })

    const activeListings = myItems || []

    // 4. Fetch My Active Bids (Items I have bid on)
    const { data: myBids } = await supabase
        .from('bids')
        .select(`
       *,
       item:items(
         id, name, status, ends_at, current_bid
       )
    `)
        .eq('bidder_id', user.id)
        .order('created_at', { ascending: false })

    // Deduplicate bids to show only unique items I bid on
    const uniqueBidItemsMap = new Map();
    if (myBids) {
        myBids.forEach((bid: any) => {
            if (!uniqueBidItemsMap.has(bid.item_id)) {
                uniqueBidItemsMap.set(bid.item_id, bid);
            }
        });
    }
    const activeBids = Array.from(uniqueBidItemsMap.values());

    return (
        <div className="w-full pb-12">
            {/* Commerce Profile Header */}
            <div className="flex flex-col md:flex-row gap-6 mb-8 w-full">

                {/* User Info & Gold Balance */}
                <div className="flex-1 bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl p-6 flex items-center justify-between shadow-sm">
                    <div className="flex items-center gap-4">
                        <div className="w-16 h-16 rounded-full bg-blue-100 dark:bg-blue-900 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-2xl border-2 border-blue-200 dark:border-blue-800">
                            {profile?.username ? profile.username.charAt(0).toUpperCase() : 'U'}
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold">{profile?.username || 'Unknown User'}</h1>
                            <p className="text-sm text-gray-500">ID: {user.id.substring(0, 8)}...</p>
                        </div>
                    </div>

                    <div className="flex flex-col items-end">
                        <span className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-1 flex items-center gap-1">
                            <CircleDollarSign size={14} /> Account Balance
                        </span>
                        <div className="text-3xl font-black text-green-600 dark:text-green-400">
                            {(profile?.gold || 0).toLocaleString()} <span className="text-base font-bold text-gray-400">G</span>
                        </div>
                        <a href="#" className="text-xs text-blue-500 font-semibold hover:underline mt-1">Request Gold (Mock)</a>
                    </div>
                </div>

                {/* Quick Actions */}
                <div className="w-full md:w-64 flex flex-col gap-3">
                    <Link href="/my-page/create" className="h-full flex-1 w-full">
                        <Button className="h-full w-full gap-2 font-bold text-lg shadow-sm">
                            <Plus size={20} /> Create Listing
                        </Button>
                    </Link>
                </div>
            </div>

            {/* Main Content Area */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 w-full">

                {/* Active Listings Panel */}
                <div className="flex flex-col">
                    <div className="flex items-center justify-between mb-4 px-1">
                        <h3 className="font-bold text-lg flex items-center gap-2">
                            <ArrowUpRight size={18} className="text-blue-500" /> My Active Listings
                        </h3>
                    </div>

                    <div className="bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl p-4 flex flex-col gap-3 shadow-sm min-h-[300px]">
                        {activeListings.length === 0 ? (
                            <div className="text-center text-sm text-gray-500 py-8">No active listings.</div>
                        ) : (
                            activeListings.map((listing: any) => (
                                <div key={listing.id} className="flex items-center gap-4 p-3 border insta-border rounded-lg bg-gray-50 dark:bg-gray-900 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                                    <div className="w-12 h-12 bg-gray-200 dark:bg-gray-800 rounded flex-shrink-0 flex items-center justify-center overflow-hidden">
                                        {listing.image_url && <img src={listing.image_url} alt="Item" className="w-full h-full object-cover" />}
                                    </div>
                                    <div className="flex-1">
                                        <Link href={`/auction/${listing.id}`} className="font-bold text-sm hover:text-blue-500">{listing.name}</Link>
                                        <div className="text-xs text-gray-500 font-semibold mt-0.5">Start: {listing.start_price} G</div>
                                    </div>
                                    <div className="flex flex-col items-end gap-1">
                                        <div className="text-right">
                                            <div className="text-xs text-gray-500 uppercase font-semibold">Current Bid</div>
                                            <div className="font-bold text-green-600 dark:text-green-400">{listing.current_bid} G</div>
                                        </div>
                                        <CancelAuctionButton itemId={listing.id} itemName={listing.name} />
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </div>

                {/* Active Bids Panel */}
                <div className="flex flex-col">
                    <div className="flex items-center justify-between mb-4 px-1">
                        <h3 className="font-bold text-lg flex items-center gap-2">
                            <Gavel size={18} className="text-purple-500" /> Recent Bids History
                        </h3>
                    </div>

                    <div className="bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl p-4 flex flex-col gap-3 shadow-sm min-h-[300px]">
                        {activeBids.length === 0 ? (
                            <div className="text-center text-sm text-gray-500 py-8">You haven't bid on any items yet.</div>
                        ) : (
                            activeBids.map((bid: any) => {
                                const isWinning = bid.item?.current_bid === bid.amount;
                                const isEnded = bid.item?.status !== 'active';

                                return (
                                    <div key={bid.id} className={`flex items-center gap-4 p-3 border rounded-lg ${isWinning && !isEnded ? 'border-green-200 bg-green-50 dark:border-green-800 dark:bg-green-900/20' : 'border-gray-200 bg-gray-50 dark:border-gray-800 dark:bg-gray-900'}`}>
                                        <div className="flex-1">
                                            <Link href={`/auction/${bid.item?.id}`} className="font-bold text-sm hover:text-blue-500">{bid.item?.name || 'Unknown Item'}</Link>
                                            <div className={`text-xs font-bold mt-0.5 ${isWinning && !isEnded ? 'text-green-600' : isEnded ? 'text-gray-500' : 'text-red-500'}`}>
                                                {isEnded ? 'Auction Ended' : (isWinning ? 'Winning!' : 'Outbid!')}
                                            </div>
                                        </div>
                                        <div className="text-right">
                                            <div className="text-xs text-gray-500 uppercase font-semibold">Your Bid</div>
                                            <div className="font-bold">{bid.amount} G</div>
                                        </div>
                                    </div>
                                )
                            })
                        )}
                    </div>
                </div>

            </div>
        </div>
    )
}
