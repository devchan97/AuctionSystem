"use client";

import { useState } from "react";
import { createClient } from "@/lib/supabase/client";

type UserRow = {
    id: string;
    username: string;
    gold: number;
    created_at: string;
};

export function AdminUserTable({ users }: { users: UserRow[] }) {
    const supabase = createClient();
    const [list, setList] = useState(users);
    const [goldInput, setGoldInput] = useState<Record<string, string>>({});
    const [loading, setLoading] = useState<string | null>(null);

    async function adjustGold(userId: string, currentGold: number) {
        const raw = goldInput[userId];
        if (!raw) return;
        const amount = parseInt(raw, 10);
        if (isNaN(amount)) return;

        setLoading(userId);
        const newGold = Math.max(0, currentGold + amount);
        const { error } = await supabase
            .from("profiles")
            .update({ gold: newGold })
            .eq("id", userId);

        if (!error) {
            setList(prev => prev.map(u => u.id === userId ? { ...u, gold: newGold } : u));
            setGoldInput(prev => ({ ...prev, [userId]: "" }));
        }
        setLoading(null);
    }

    return (
        <div className="overflow-x-auto rounded-lg border insta-border">
            <table className="w-full text-sm">
                <thead className="bg-gray-50 dark:bg-gray-900">
                    <tr>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Username</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Gold</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600 dark:text-gray-400">Adjust Gold</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(u => (
                        <tr key={u.id} className="border-t insta-border">
                            <td className="px-4 py-2 font-medium">{u.username}</td>
                            <td className="px-4 py-2">{u.gold.toLocaleString()}G</td>
                            <td className="px-4 py-2">
                                <div className="flex items-center gap-2">
                                    <input
                                        type="number"
                                        placeholder="+1000 or -500"
                                        value={goldInput[u.id] ?? ""}
                                        onChange={e => setGoldInput(prev => ({ ...prev, [u.id]: e.target.value }))}
                                        className="w-28 text-xs px-2 py-1 border insta-border rounded bg-transparent"
                                    />
                                    <button
                                        onClick={() => adjustGold(u.id, u.gold)}
                                        disabled={loading === u.id || !goldInput[u.id]}
                                        className="text-xs px-3 py-1 bg-blue-600 hover:bg-blue-700 text-white rounded-full disabled:opacity-50"
                                    >
                                        {loading === u.id ? "..." : "Apply"}
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
