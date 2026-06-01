import React from 'react'
import './Badge.css'

interface Props { children: React.ReactNode; color?: string }

export default function Badge({ children, color }: Props) {
  return (
    <span className="badge" style={color ? { background: color } : undefined}>
      {children}
    </span>
  )
}
