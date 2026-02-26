'use client'

import { useEffect, useRef, useState } from 'react'
import { createClient } from '@/lib/supabase/client'

/**
 * /auth/unity-callback
 *
 * ── 진입 경로 ─────────────────────────────────────────────────────
 *
 * [PC/Editor 모드] ?from=unity 쿼리 파라미터 있음
 *   → 토큰을 localhost:7654/callback?access_token=... 으로 리다이렉트
 *   → Unity 로컬 HTTP 서버가 수신 → LoginManager.ExchangeOAuthToken() 처리
 *
 * [WebGL 모드] ?from=unity 없음
 *   → window.unityInstance.SendMessage() 로 토큰 전달
 *
 * ── 토큰 소스 ─────────────────────────────────────────────────────
 *   Case A: 쿠키에 기존 세션 → access_token 즉시 사용
 *   Case B: Google OAuth 완료 후 URL fragment(#access_token=...) 에서 추출
 */

const UNITY_CALLBACK_PORT = 7654
type Status = 'checking' | 'sending' | 'done' | 'error'

export default function UnityCallbackPage() {
    const sentRef = useRef(false)
    const [status, setStatus] = useState<Status>('checking')
    const [message, setMessage] = useState('세션 확인 중...')

    useEffect(() => {
        if (sentRef.current) return
        sentRef.current = true
        resolveToken().then(deliverToken)
    }, [])

    // ── 1. 토큰 확정 ────────────────────────────────────────────

    async function resolveToken(): Promise<string | null> {
        const supabase = createClient()

        // Case A: 쿠키에 세션이 이미 있는 경우
        const { data: { session }, error: sessionError } = await supabase.auth.getSession()
        if (!sessionError && session?.access_token) {
            const { data: { session: refreshed } } = await supabase.auth.refreshSession()
            const token = refreshed?.access_token ?? session.access_token
            setMessage('로그인 세션 감지 — Unity에 전달 중...')
            return token
        }

        // Case B: Google OAuth 완료 후 fragment로 넘어온 경우
        const hash = window.location.hash
        const params = new URLSearchParams(hash.replace(/^#/, ''))
        const accessToken = params.get('access_token')
        const refreshToken = params.get('refresh_token')

        if (accessToken && refreshToken) {
            // setSession으로 Supabase가 HS256 세션을 발급하게 한 뒤
            // getSession()으로 Supabase 자체 토큰을 꺼냄 (Google ES256 JWT 아님)
            await supabase.auth.setSession({ access_token: accessToken, refresh_token: refreshToken })
            const { data: { session: supabaseSession } } = await supabase.auth.getSession()
            if (supabaseSession?.access_token) {
                setMessage('Google 로그인 완료 — Unity에 전달 중...')
                return supabaseSession.access_token
            }
        }

        return null
    }

    // ── 2. 토큰 전달: PC vs WebGL 분기 ──────────────────────────

    function deliverToken(token: string | null) {
        if (!token) {
            setStatus('error')
            setMessage('❌ 로그인 실패: 유효한 세션을 찾지 못했습니다.\n창을 닫고 다시 시도해주세요.')
            return
        }

        // PC/Editor: ?from=unity → localhost 콜백 서버로 access_token + refresh_token 전달
        // fetch(no-cors)는 HTTPS→HTTP Mixed Content로 차단되므로 location.href 리다이렉트 사용
        const searchParams = new URLSearchParams(window.location.search)
        if (searchParams.get('from') === 'unity') {
            const supabase = createClient()
            supabase.auth.getSession().then(({ data: { session } }) => {
                const refreshToken = session?.refresh_token ?? ''
                const url = `http://localhost:${UNITY_CALLBACK_PORT}/callback` +
                    `?access_token=${encodeURIComponent(token)}` +
                    (refreshToken ? `&refresh_token=${encodeURIComponent(refreshToken)}` : '')
                setStatus('done')
                setMessage('✅ Unity 로그인 완료! Unity로 이동 중...')
                window.location.href = url
            })
            return
        }

        // WebGL: unityInstance.SendMessage() 로 전달
        // "access|refresh" 포맷으로 전달 — LoginManager.ReceiveOAuthSessionFromWeb이 파싱
        setStatus('sending')
        let attempts = 0
        const maxAttempts = 20

        const tryBridge = () => {
            attempts++
            const unity = (window as any).unityInstance

            if (unity) {
                try {
                    const supabase = createClient()
                    supabase.auth.getSession().then(({ data: { session } }) => {
                        const refreshToken = session?.refresh_token ?? ''
                        const payload = refreshToken ? `${token}|${refreshToken}` : token
                        unity.SendMessage('Bootstrap', 'ReceiveOAuthSessionFromWeb', payload)
                    })
                    setStatus('done')
                    setMessage('✅ 로그인 완료!')
                    if (window.opener) {
                        window.opener.postMessage({ type: 'UNITY_AUTH_DONE', token }, '*')
                        window.close()
                    }
                } catch {
                    setStatus('error')
                    setMessage('❌ Unity SendMessage 실패.')
                }
            } else if (attempts < maxAttempts) {
                setTimeout(tryBridge, 500)
            } else {
                setStatus('error')
                setMessage('⚠️ Unity 인스턴스를 찾지 못했습니다.\nUnity WebGL이 완전히 로드된 후 다시 시도하세요.')
            }
        }

        tryBridge()
    }

    // ── 3. 렌더링 ────────────────────────────────────────────────

    const bgColor: Record<Status, string> = {
        checking: '#1a1a2e',
        sending: '#1a2e1a',
        done: '#0f1f0f',
        error: '#2e1a1a',
    }
    const spinnerVisible = status === 'checking' || status === 'sending'

    return (
        <div style={{
            display: 'flex', flexDirection: 'column', alignItems: 'center',
            justifyContent: 'center', height: '100vh', fontFamily: 'sans-serif',
            background: bgColor[status], color: '#fff', transition: 'background 0.4s',
            padding: 24, textAlign: 'center',
        }}>
            {spinnerVisible && (
                <div style={{
                    width: 40, height: 40,
                    border: '4px solid rgba(255,255,255,0.2)',
                    borderTop: '4px solid #fff', borderRadius: '50%',
                    animation: 'spin 0.8s linear infinite', marginBottom: 24,
                }} />
            )}
            <h2 style={{ marginBottom: 12, fontSize: 22 }}>
                {status === 'done' ? '인증 완료' : 'Unity 인증 처리 중'}
            </h2>
            <p style={{ color: status === 'error' ? '#ff8080' : '#aaffaa', fontSize: 15, whiteSpace: 'pre-line', lineHeight: 1.6 }}>
                {message}
            </p>
            {status === 'done' && (
                <p style={{ marginTop: 20, fontSize: 13, color: '#888' }}>
                    이 탭을 직접 닫고 Unity로 돌아가세요.
                </p>
            )}
            <style>{`@keyframes spin { to { transform: rotate(360deg) } }`}</style>
        </div>
    )
}
