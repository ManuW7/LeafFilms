import React, { useEffect } from 'react'
import './Toast.css'

interface Props {
  message: string
  type?: 'success' | 'error' | 'info'
  onClose: () => void
}

export default function Toast({ message, type = 'info', onClose }: Props) {
  useEffect(() => {
    const t = setTimeout(onClose, 3500)
    return () => clearTimeout(t)
  }, [onClose])

  const icons = { success: '✓', error: '✕', info: 'ℹ' }

  return (
    <div className={`toast toast--${type}`}>
      <span className={`toast__icon--${type}`}>{icons[type]}</span>
      <span className="toast__text">{message}</span>
      <button className="toast__close" onClick={onClose}>×</button>
    </div>
  )
}
