import React from 'react'
import './Avatar.css'

interface Props { name: string; size?: number; style?: React.CSSProperties; className?: string }

export default function Avatar({ name, size = 36, style, className = '' }: Props) {
  const initials = name.slice(0, 2).toUpperCase()
  const hue = [...name].reduce((acc, c) => acc + c.charCodeAt(0), 0) % 360

  return (
    <div
      className={`avatar ${className}`}
      style={{
        width: size,
        height: size,
        background: `hsl(${hue}, 60%, 30%)`,
        color: `hsl(${hue}, 80%, 80%)`,
        fontSize: size * 0.38,
        ...style,
      }}
    >
      {initials}
    </div>
  )
}
