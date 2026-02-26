"use client"

import { useState } from 'react'
import { createClient } from '@/lib/supabase/client'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { TrendingUp } from 'lucide-react'

interface BidClientActionsProps {
    itemId: string;
    currentBid: number;
    buyoutPrice: number | null;
    endsAt: string;
}

export function BidClientActions({ itemId, currentBid, buyoutPrice, endsAt }: BidClientActionsProps) {
    const [bidAmount, setBidAmount] = useState<number>(currentBid + 100);
    const [isBidding, setIsBidding] = useState(false);
    const [isBuyingOut, setIsBuyingOut] = useState(false);
    const [errorPayload, setErrorPayload] = useState<string | null>(null);
    const [successMsg, setSuccessMsg] = useState<string | null>(null);

    const supabase = createClient();
    const isEnded = new Date(endsAt).getTime() < Date.now();

    const invokeFn = async (fnName: string, body: object): Promise<{ data: any; errorMsg: string | null }> => {
        const { data: refreshData } = await supabase.auth.refreshSession();
        const token = refreshData.session?.access_token;
        if (!token) return { data: null, errorMsg: 'Not logged in. Please sign in and try again.' };

        const { data, error } = await supabase.functions.invoke(fnName, {
            body,
            headers: { Authorization: `Bearer ${token}` },
        });
        if (!error) return { data, errorMsg: null };
        try {
            const ctx = (error as any).context;
            if (ctx instanceof Response) {
                const text = await ctx.text();
                const parsed = text ? JSON.parse(text) : {};
                return { data: null, errorMsg: parsed?.error ?? parsed?.message ?? error.message };
            }
        } catch { /* ignore */ }
        return { data: null, errorMsg: error.message };
    };

    const handlePlaceBid = async () => {
        if (isEnded) return;
        setErrorPayload(null);
        setSuccessMsg(null);
        setIsBidding(true);

        const { data, errorMsg } = await invokeFn('place-bid', { item_id: itemId, amount: bidAmount });
        if (errorMsg) {
            setErrorPayload(errorMsg);
        } else {
            setSuccessMsg(`Successfully bid ${bidAmount} G!`);
        }
        setIsBidding(false);
    };

    const handleBuyout = async () => {
        if (isEnded || !buyoutPrice) return;
        if (!confirm(`Are you sure you want to buyout for ${buyoutPrice.toLocaleString()} G?`)) return;

        setErrorPayload(null);
        setSuccessMsg(null);
        setIsBuyingOut(true);

        const { data, errorMsg } = await invokeFn('buyout', { item_id: itemId });
        if (errorMsg) {
            setErrorPayload(errorMsg);
        } else {
            setSuccessMsg(`Successfully bought out item!`);
        }
        setIsBuyingOut(false);
    };

    return (
        <div className="flex flex-col gap-4">
            {errorPayload && (
                <div className="msg-error">Error: {errorPayload}</div>
            )}
            {successMsg && (
                <div className="msg-success">{successMsg}</div>
            )}

            <div className="flex gap-2 w-full">
                <div className="relative flex-1">
                    <span className="absolute left-4 top-1/2 -translate-y-1/2 font-bold text-gray-400">G</span>
                    <Input
                        type="number"
                        value={bidAmount}
                        onChange={(e) => setBidAmount(Number(e.target.value))}
                        className="pl-9 h-14 text-lg font-bold w-full bg-white dark:bg-black"
                        disabled={isBidding || isBuyingOut || isEnded}
                    />
                </div>
                <Button
                    variant="primary"
                    className="h-14 px-8 text-lg font-bold flex gap-2"
                    onClick={handlePlaceBid}
                    disabled={isBidding || isBuyingOut || isEnded}
                >
                    <TrendingUp size={20} /> {isBidding ? 'Bidding...' : 'Place Bid'}
                </Button>
            </div>

            <div className="grid grid-cols-4 gap-2">
                <Button variant="outline" size="sm" className="font-bold" onClick={() => setBidAmount(currentBid + 100)} disabled={isEnded}>+100</Button>
                <Button variant="outline" size="sm" className="font-bold" onClick={() => setBidAmount(currentBid + 500)} disabled={isEnded}>+500</Button>
                <Button variant="outline" size="sm" className="font-bold" onClick={() => setBidAmount(currentBid + 1000)} disabled={isEnded}>+1000</Button>
                <Button variant="outline" size="sm" className="font-bold" onClick={() => setBidAmount(buyoutPrice || currentBid + 5000)} disabled={isEnded}>+Max</Button>
            </div>

            {buyoutPrice && (
                <div className="border insta-border rounded-2xl p-6 mt-4 bg-white dark:bg-[#0a0a0a] flex items-center justify-between gap-4">
                    <div>
                        <div className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-1">Buyout Price</div>
                        <div className="text-2xl font-black text-gray-900 dark:text-gray-100">{buyoutPrice.toLocaleString()} G</div>
                        <div className="text-xs text-gray-400 mt-1">Instantly purchase and close auction</div>
                    </div>
                    <Button
                        variant="secondary"
                        className="h-12 px-6 font-bold flex-shrink-0 border-2 border-gray-900 dark:border-gray-100 text-gray-900 dark:text-gray-100 bg-transparent hover:bg-gray-100 dark:hover:bg-gray-800"
                        onClick={handleBuyout}
                        disabled={isBuyingOut || isBidding || isEnded}
                    >
                        {isBuyingOut ? 'Processing...' : 'Buyout Now'}
                    </Button>
                </div>
            )}
        </div>
    )
}
