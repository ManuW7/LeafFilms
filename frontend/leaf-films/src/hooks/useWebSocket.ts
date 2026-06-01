import { useEffect, useRef, useCallback } from 'react'
import type { WsMessage } from '../types'

export function useWebSocket(
  token: string | null,
  onMessage: (msg: WsMessage) => void
) {
  const wsRef = useRef<WebSocket | null>(null)
  const reconnectTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const onMessageRef = useRef(onMessage)
  onMessageRef.current = onMessage

  const connect = useCallback(() => {
    if (!token) return
    if (wsRef.current?.readyState === WebSocket.OPEN) return

    const proto = window.location.protocol === 'https:' ? 'wss' : 'ws'
    const host = window.location.host
    const url = `${proto}://${host}/ws/feed?token=${token}`

    const ws = new WebSocket(url)
    wsRef.current = ws

    ws.onopen = () => console.log('[WS] Connected')

    ws.onmessage = e => {
      try {
        const msg = JSON.parse(e.data) as WsMessage
        onMessageRef.current(msg)
      } catch {
        console.warn('[WS] Failed to parse message', e.data)
      }
    }

    ws.onclose = () => {
      console.log('[WS] Disconnected, reconnecting in 3s...')
      reconnectTimer.current = setTimeout(connect, 3000)
    }

    ws.onerror = err => console.error('[WS] Error', err)
  }, [token])

  useEffect(() => {
    connect()
    return () => {
      if (reconnectTimer.current) clearTimeout(reconnectTimer.current)
      wsRef.current?.close()
    }
  }, [connect])
}
