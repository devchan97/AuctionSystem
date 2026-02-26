import { createClient } from "@/lib/supabase/server";
import { redirect } from "next/navigation";
import { AdminUserTable } from "./AdminUserTable";
import { AdminAuctionTable } from "./AdminAuctionTable";

export default async function AdminDashboard() {
    const supabase = await createClient();
    const { data: { user } } = await supabase.auth.getUser();

    if (!user) redirect("/login");

    // 관리자 권한 확인 — app_metadata.role = "admin" (Authentication > Users에서 설정)
    const isAdmin = user.app_metadata?.role === "admin";
    if (!isAdmin) redirect("/");

    // 통계 데이터
    const [
        { count: activeCount },
        { count: userCount },
        { data: transactions },
        { data: users },
        { data: activeAuctions },
    ] = await Promise.all([
        supabase.from("items").select("*", { count: "exact", head: true }).eq("status", "active"),
        supabase.from("profiles").select("*", { count: "exact", head: true }),
        supabase.from("transactions").select("final_price").returns<{ final_price: number | null }[]>(),
        supabase.from("profiles").select("id, username, gold, created_at").order("created_at", { ascending: false }).limit(50),
        supabase.from("items").select("id, name, current_bid, ends_at, status, seller_id").eq("status", "active").order("ends_at", { ascending: true }).limit(20),
    ]);

    const totalGold = transactions?.reduce((sum, t) => sum + (t.final_price ?? 0), 0) ?? 0;

    return (
        <div className="w-full p-4 md:p-8 space-y-8">
            <h1 className="text-2xl font-bold">Admin Dashboard</h1>

            {/* 통계 카드 */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="border insta-border rounded-lg p-6 bg-gray-50 dark:bg-gray-900 shadow-sm">
                    <h3 className="font-semibold text-sm text-gray-600 dark:text-gray-400">Active Auctions</h3>
                    <p className="text-4xl font-bold mt-2">{activeCount ?? 0}</p>
                </div>
                <div className="border insta-border rounded-lg p-6 bg-gray-50 dark:bg-gray-900 shadow-sm">
                    <h3 className="font-semibold text-sm text-gray-600 dark:text-gray-400">Total Gold Traded</h3>
                    <p className="text-4xl font-bold mt-2">{totalGold.toLocaleString()}</p>
                </div>
                <div className="border insta-border rounded-lg p-6 bg-gray-50 dark:bg-gray-900 shadow-sm">
                    <h3 className="font-semibold text-sm text-gray-600 dark:text-gray-400">Total Users</h3>
                    <p className="text-4xl font-bold mt-2">{userCount ?? 0}</p>
                </div>
            </div>

            {/* 활성 경매 관리 */}
            <section>
                <h2 className="text-lg font-semibold mb-3">Active Auctions</h2>
                <AdminAuctionTable auctions={activeAuctions ?? []} />
            </section>

            {/* 사용자 관리 */}
            <section>
                <h2 className="text-lg font-semibold mb-3">Users</h2>
                <AdminUserTable users={users ?? []} />
            </section>
        </div>
    );
}
