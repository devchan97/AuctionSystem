import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Package, User, ShoppingBag, ShieldCheck } from "lucide-react";
import { SearchBar } from "@/components/ui/SearchBar";
import Link from "next/link";
import { Suspense } from "react";
import { NotificationBell } from "@/components/notifications/NotificationBell";
import { LogoutButton } from "@/components/auth/LogoutButton";
import { createClient } from "@/lib/supabase/server";

const inter = Inter({ subsets: ["latin"], variable: "--font-inter" });

export const metadata: Metadata = {
  title: "Auction House",
  description: "Real-time item auction and trading platform",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const supabase = await createClient();
  const { data: { user } } = await supabase.auth.getUser();
  const isAdmin = user?.app_metadata?.role === "admin";

  return (
    <html lang="en">
      <body className={`${inter.variable} font-sans antialiased bg-[var(--background)] text-[var(--foreground)] min-h-screen flex flex-col`}>
        {/* Top Navigation Header */}
        <header className="sticky top-0 z-50 w-full border-b insta-border bg-[var(--background)] bg-opacity-95 backdrop-blur">
          <div className="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between gap-4">

            {/* Logo */}
            <Link href="/" className="font-bold text-xl tracking-tight hidden sm:flex items-center gap-2">
              <ShoppingBag size={24} className="text-blue-600 dark:text-blue-400" />
              <span>AuctionHouse</span>
            </Link>
            <Link href="/" className="font-bold text-xl tracking-tight sm:hidden flex">
              AH
            </Link>

            {/* Search Bar */}
            <Suspense fallback={<div className="flex-1 max-w-xl h-9 bg-gray-100 dark:bg-gray-900 rounded-full animate-pulse" />}>
              <SearchBar />
            </Suspense>

            {/* Right Nav Icons */}
            <nav className="flex items-center gap-1 sm:gap-4">
              <Link href="/auction" className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors hidden md:block font-medium text-sm">
                Browse
              </Link>

              {user ? (
                <>
                  <NotificationBell userId={user.id} />
                  {isAdmin && (
                    <Link href="/admin" className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors" title="Admin">
                      <ShieldCheck size={22} className="text-blue-600 dark:text-blue-400" />
                    </Link>
                  )}
                  <Link href="/inventory" className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors" title="Inventory">
                    <Package size={22} className="text-gray-700 dark:text-gray-300" />
                  </Link>
                  <Link href="/my-page" className="p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-full transition-colors" title="Profile">
                    <User size={22} className="text-gray-700 dark:text-gray-300" />
                  </Link>
                  <LogoutButton />
                </>
              ) : (
                <div className="flex items-center gap-2 ml-2">
                  <Link href="/login" className="font-bold py-2 px-4 rounded-full transition-colors text-sm border insta-border hover:bg-gray-100 dark:hover:bg-gray-800">
                    Log In
                  </Link>
                  <Link href="/signup" className="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded-full transition-colors text-sm">
                    Sign Up
                  </Link>
                </div>
              )}
            </nav>
          </div>

          {/* Categories Sub-nav (Desktop) */}
          <div className="hidden md:flex max-w-7xl mx-auto px-4 h-10 items-center gap-6 text-sm font-medium text-gray-600 dark:text-gray-400">
            <Link href="/category/weapons" className="hover:text-foreground transition-colors">Weapons</Link>
            <Link href="/category/armor" className="hover:text-foreground transition-colors">Armor</Link>
            <Link href="/category/consumables" className="hover:text-foreground transition-colors">Consumables</Link>
            <Link href="/category/misc" className="hover:text-foreground transition-colors">Misc</Link>
          </div>
        </header>

        {/* Main Content */}
        <main className="flex-1 w-full max-w-7xl mx-auto p-4 md:p-8">
          {children}
        </main>

        {/* Minimal Footer */}
        <footer className="border-t insta-border py-8 mt-12 bg-gray-50 dark:bg-black">
          <div className="max-w-7xl mx-auto px-4 text-center text-sm text-gray-500">
            &copy; {new Date().getFullYear()} Auction House System. Built with Next.js & Supabase.
          </div>
        </footer>
      </body>
    </html>
  );
}
