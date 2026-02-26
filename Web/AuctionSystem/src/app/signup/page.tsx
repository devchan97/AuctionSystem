"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/Button"
import { Input } from "@/components/ui/Input"
import Link from "next/link"
import { createClient } from "@/lib/supabase/client"

export default function SignupPage() {
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [username, setUsername] = useState('')
    const [errorPayload, setErrorPayload] = useState<string | null>(null)
    const [successMessage, setSuccessMessage] = useState<string | null>(null)
    const [isLoading, setIsLoading] = useState(false)
    const [fromUnity, setFromUnity] = useState(false)
    const supabase = createClient()

    useEffect(() => {
        const params = new URLSearchParams(window.location.search)
        setFromUnity(params.get('from') === 'unity')
    }, [])

    const handleSignup = async (e: React.FormEvent) => {
        e.preventDefault()
        setErrorPayload(null)
        setSuccessMessage(null)
        setIsLoading(true)
        try {
            if (!username.trim()) throw new Error('Username is required')
            if (password.length < 6) throw new Error('Password must be at least 6 characters')

            const redirectTo = fromUnity
                ? `${window.location.origin}/auth/unity-callback?from=unity`
                : `${window.location.origin}/auth/callback`

            const { data: signUpData, error: signUpError } = await supabase.auth.signUp({
                email,
                password,
                options: {
                    data: { username: username.trim() },
                    emailRedirectTo: redirectTo,
                }
            })
            if (signUpError) throw signUpError

            if (fromUnity && signUpData.session) {
                window.location.href = `${window.location.origin}/auth/unity-callback?from=unity`
                return
            }

            setSuccessMessage('Account created! Please check your email to confirm your account.')
        } catch (err: any) {
            setErrorPayload(err.message)
        } finally {
            setIsLoading(false)
        }
    }

    const handleGoogleLogin = async () => {
        const redirectTo = fromUnity
            ? `${window.location.origin}/auth/unity-callback?from=unity`
            : `${window.location.origin}/auth/callback`

        await supabase.auth.signInWithOAuth({
            provider: 'google',
            options: { redirectTo }
        })
    }

    return (
        <div className="w-full max-w-sm mx-auto flex flex-col items-center justify-center min-h-[70vh]">
            <div className="w-full bg-white dark:bg-[#0a0a0a] border insta-border rounded-xl p-8 shadow-sm flex flex-col gap-6">
                <h1 className="text-2xl font-black text-center mb-2">
                    {fromUnity ? 'Unity 연동 — 회원가입' : 'Join AuctionHouse'}
                </h1>
                {fromUnity && (
                    <p className="text-xs text-center text-gray-400">
                        회원가입 또는 Google 로그인 후 Unity로 자동 연결됩니다.
                    </p>
                )}

                {errorPayload && (
                    <div className="msg-error">{errorPayload}</div>
                )}

                {successMessage ? (
                    <div className="flex flex-col gap-4">
                        <div className="msg-success">{successMessage}</div>
                        <Link href="/login">
                            <Button variant="primary" className="w-full font-bold">Go to Login</Button>
                        </Link>
                    </div>
                ) : (
                    <>
                        <form className="flex flex-col gap-4" onSubmit={handleSignup}>
                            <div className="flex flex-col gap-1">
                                <label className="text-xs font-bold text-gray-500 uppercase">Username</label>
                                <Input
                                    type="text"
                                    placeholder="Choose a username"
                                    required
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    disabled={isLoading}
                                />
                            </div>
                            <div className="flex flex-col gap-1">
                                <label className="text-xs font-bold text-gray-500 uppercase">Email</label>
                                <Input
                                    type="email"
                                    placeholder="Enter your email"
                                    required
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    disabled={isLoading}
                                />
                            </div>
                            <div className="flex flex-col gap-1">
                                <label className="text-xs font-bold text-gray-500 uppercase">Password</label>
                                <Input
                                    type="password"
                                    placeholder="At least 6 characters"
                                    required
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    disabled={isLoading}
                                />
                            </div>
                            <Button variant="primary" type="submit" className="w-full mt-2 font-bold" disabled={isLoading}>
                                {isLoading ? 'Creating account...' : 'Create Account'}
                            </Button>
                        </form>

                        <div className="relative flex items-center py-2">
                            <div className="flex-grow border-t insta-border"></div>
                            <span className="flex-shrink-0 mx-4 text-gray-400 text-sm font-semibold">OR</span>
                            <div className="flex-grow border-t insta-border"></div>
                        </div>

                        <Button variant="outline" className="w-full flex items-center justify-center gap-2 font-bold" onClick={handleGoogleLogin}>
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
                                <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
                                <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
                                <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
                            </svg>
                            Continue with Google
                        </Button>
                    </>
                )}
            </div>

            <div className="mt-6 border insta-border rounded-xl p-4 bg-white dark:bg-[#0a0a0a] w-full text-center text-sm">
                Already have an account?{' '}
                <Link href="/login" className="font-bold text-blue-500 hover:text-blue-600 ml-1">
                    Log in
                </Link>
            </div>
        </div>
    )
}
