export type Json =
    | string
    | number
    | boolean
    | null
    | { [key: string]: Json | undefined }
    | Json[]

export interface Database {
    public: {
        Tables: {
            profiles: {
                Row: {
                    id: string
                    username: string
                    gold: number
                    avatar_url: string | null
                    created_at: string
                }
                Insert: {
                    id: string
                    username: string
                    gold?: number
                    avatar_url?: string | null
                    created_at?: string
                }
                Update: {
                    id?: string
                    username?: string
                    gold?: number
                    avatar_url?: string | null
                    created_at?: string
                }
            }
            items: {
                Row: {
                    id: string
                    seller_id: string
                    name: string
                    description: string | null
                    image_url: string | null
                    category: string | null
                    start_price: number
                    buyout_price: number | null
                    current_bid: number
                    created_at: string
                    ends_at: string
                    status: 'active' | 'sold' | 'expired' | 'cancelled'
                }
                Insert: {
                    id?: string
                    seller_id: string
                    name: string
                    description?: string | null
                    image_url?: string | null
                    category?: string | null
                    start_price: number
                    buyout_price?: number | null
                    current_bid?: number
                    created_at?: string
                    ends_at: string
                    status?: 'active' | 'sold' | 'expired' | 'cancelled'
                }
                Update: {
                    id?: string
                    seller_id?: string
                    name?: string
                    description?: string | null
                    image_url?: string | null
                    category?: string | null
                    start_price?: number
                    buyout_price?: number | null
                    current_bid?: number
                    created_at?: string
                    ends_at?: string
                    status?: 'active' | 'sold' | 'expired' | 'cancelled'
                }
            }
            bids: {
                Row: {
                    id: string
                    item_id: string
                    bidder_id: string | null
                    amount: number
                    created_at: string
                }
                Insert: {
                    id?: string
                    item_id: string
                    bidder_id?: string | null
                    amount: number
                    created_at?: string
                }
                Update: {
                    id?: string
                    item_id?: string
                    bidder_id?: string | null
                    amount?: number
                    created_at?: string
                }
            }
            transactions: {
                Row: {
                    id: string
                    item_id: string | null
                    seller_id: string | null
                    buyer_id: string | null
                    final_price: number
                    fee: number
                    created_at: string
                }
                Insert: {
                    id?: string
                    item_id?: string | null
                    seller_id?: string | null
                    buyer_id?: string | null
                    final_price: number
                    fee?: number
                    created_at?: string
                }
                Update: {
                    id?: string
                    item_id?: string | null
                    seller_id?: string | null
                    buyer_id?: string | null
                    final_price?: number
                    fee?: number
                    created_at?: string
                }
            }
            notifications: {
                Row: {
                    id: string
                    user_id: string | null
                    type: string
                    item_id: string | null
                    message: string | null
                    is_read: boolean
                    created_at: string
                }
                Insert: {
                    id?: string
                    user_id?: string | null
                    type: string
                    item_id?: string | null
                    message?: string | null
                    is_read?: boolean
                    created_at?: string
                }
                Update: {
                    id?: string
                    user_id?: string | null
                    type?: string
                    item_id?: string | null
                    message?: string | null
                    is_read?: boolean
                    created_at?: string
                }
            }
            inventory: {
                Row: {
                    id: string
                    owner_id: string
                    item_id: string
                    acquired_at: string
                    transaction_id: string | null
                }
                Insert: {
                    id?: string
                    owner_id: string
                    item_id: string
                    acquired_at?: string
                    transaction_id?: string | null
                }
                Update: {
                    id?: string
                    owner_id?: string
                    item_id?: string
                    acquired_at?: string
                    transaction_id?: string | null
                }
            }
        }
    }
}
