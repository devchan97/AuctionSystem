import Image from 'next/image'
import Link from 'next/link'
import { Button } from '../ui/Button'
import { BuyoutButton } from './BuyoutButton'
import { Clock, Hammer } from 'lucide-react'
import { getTimeLeft } from '@/lib/utils'

interface AuctionCardProps {
    id: string
    sellerName: string
    itemName: string
    imageUrl: string | null
    currentBid: number
    buyoutPrice?: number | null
    endsAt: string
    bidsCount?: number
}

export function AuctionCard({
    id,
    sellerName,
    itemName,
    imageUrl,
    currentBid,
    buyoutPrice,
    endsAt,
    bidsCount = 0
}: AuctionCardProps) {
    const { hoursLeft, minutesLeft, isEndingSoon, label: timeLabel } = getTimeLeft(endsAt)

    return (
        <div className="group flex flex-col bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl overflow-hidden hover:shadow-xl transition-all duration-300 hover:border-gray-300 dark:hover:border-gray-700">
            {/* Image Container with Link */}
            <Link href={`/auction/${id}`} className="relative w-full aspect-square bg-gray-100 dark:bg-gray-900 flex items-center justify-center overflow-hidden">
                {imageUrl?.trim() ? (
                    <Image
                        src={imageUrl}
                        alt={itemName}
                        fill
                        sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
                        className="object-cover group-hover:scale-105 transition-transform duration-500"
                    />
                ) : (
                    <div className="text-gray-400">No Image</div>
                )}

                {/* Badges */}
                <div className="absolute top-2 left-2 right-2 flex justify-between pointer-events-none">
                    {isEndingSoon ? (
                        <span className="bg-red-500/90 text-white text-xs font-bold px-2 py-1 rounded backdrop-blur border border-red-600">
                            Ending Soon
                        </span>
                    ) : <span />}
                </div>
            </Link>

            {/* Content */}
            <div className="p-4 flex flex-col flex-1">
                {/* Title and Seller */}
                <div className="mb-3">
                    <Link href={`/auction/${id}`} className="font-bold text-lg text-gray-900 dark:text-gray-100 hover:text-blue-600 dark:hover:text-blue-400 line-clamp-1 transition-colors">
                        {itemName}
                    </Link>
                    <div className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                        Seller: <span className="font-medium text-gray-700 dark:text-gray-300">{sellerName}</span>
                    </div>
                </div>

                {/* Price and Time details */}
                <div className="mt-auto space-y-3">
                    <div className="flex justify-between items-end">
                        <div>
                            <div className="text-xs text-gray-500 mb-0.5 font-medium uppercase tracking-wider">Current Bid</div>
                            <div className="text-xl font-black text-green-600 dark:text-green-400 flex items-center gap-1">
                                {currentBid.toLocaleString()} <span className="text-sm font-semibold">G</span>
                            </div>
                        </div>

                        <div className="text-right">
                            <div className="text-xs font-medium bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded-md text-gray-600 dark:text-gray-300 flex items-center gap-1 mb-1">
                                <Hammer size={12} /> {bidsCount} Bids
                            </div>
                            <div className={`text-xs font-semibold flex items-center justify-end gap-1 ${isEndingSoon ? 'text-red-500' : 'text-gray-500'}`}>
                                <Clock size={12} className={isEndingSoon ? 'animate-pulse' : ''} />
                                {timeLabel}
                            </div>
                        </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="grid grid-cols-2 gap-2 pt-2 border-t insta-border">
                        <Link href={`/auction/${id}`} className="w-full">
                            <Button variant="outline" size="sm" className="w-full font-bold">
                                Bid
                            </Button>
                        </Link>
                        {buyoutPrice ? (
                            <BuyoutButton itemId={id} buyoutPrice={buyoutPrice} itemName={itemName} />
                        ) : (
                            <Button variant="secondary" size="sm" className="w-full font-bold" disabled>
                                No Buyout
                            </Button>
                        )}
                    </div>
                </div>
            </div>
        </div>
    )
}
