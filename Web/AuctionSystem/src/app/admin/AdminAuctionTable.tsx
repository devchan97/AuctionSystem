"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";

type Auction = {
    id: string;
    name: string;
    current_bid: number;
    ends_at: string;
    status: string;
    seller_id: string;
};

export function AdminAuctionTable({ auctions }: { auctions: Auction[] }) {
    const supabase = createClient();
    const [list, setList] = useState(auctions);
    const [loading, setLoading] = useState<string | null>(null);

    async function forceEnd(id: string) {
        if (!confirm("Force end this auction?")) return;
        setLoading(id);
        const { error } = await supabase
            .from("items")
            .update({ status: "cancelled" })
            .eq("id", id);
        if (!error) setList(prev => prev.filter(a => a.id !== id));
        setLoading(null);
    }

    if (list.length === 0) {
        return <p className="text-sm text-gray-500">No active auctions.</p>;
    }

    return (
        <div className="overflow-x-auto rounded-lg border insta-border">
            <table className="w-full text-sm">
                <thead className="bg-gray-50 dark:bg-gray-900">
                    <tr>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Item</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Current Bid</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Ends At</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Action</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(a => (
                        <tr key={a.id} className="border-t insta-border">
                            <td className="px-4 py-2">{a.name}</td>
                            <td className="px-4 py-2">{a.current_bid.toLocaleString()}G</td>
                            <td className="px-4 py-2 text-xs text-gray-500">
                                {new Date(a.ends_at).toLocaleString()}
                            </td>
                            <td className="px-4 py-2">
                                <button
                                    onClick={() => forceEnd(a.id)}
                                    disabled={loading === a.id}
                                    className="text-xs px-3 py-1 bg-red-600 hover:bg-red-700 text-white rounded-full disabled:opacity-50"
                                >
                                    {loading === a.id ? "..." : "Force End"}
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
