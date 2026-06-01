import { useEffect, useRef } from 'react'
import type { WsMessage } from '../types'

export function useWebSocket(
  token: string | null,
  onMessage: (msg: WsMessage) => void
) {
  const wsRef = useRef<WebSocket | null>(null)
  const reconnectTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const onMessageRef = useRef(onMessage)

  useEffect(() => {
    onMessageRef.current = onMessage
  }, [onMessage])

  useEffect(() => {
    if (!token) return

    let disposed = false

    const connect = () => {
      if (disposed) return
      if (wsRef.current?.readyState === WebSocket.OPEN) return

      const proto = window.location.protocol === 'https:' ? 'wss' : 'ws'
      const host = window.location.host
      const url = `${proto}://${host}/api/ws/feed?token=${token}`

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
        if (disposed) return
        console.log('[WS] Disconnected, reconnecting in 3s...')
        reconnectTimer.current = setTimeout(connect, 3000)
      }
      ws.onerror = err => console.error('[WS] Error', err)
    }

    connect()

    return () => {
      disposed = true
      if (reconnectTimer.current) clearTimeout(reconnectTimer.current)
      wsRef.current?.close()
      wsRef.current = null
    }
  }, [token])
}
