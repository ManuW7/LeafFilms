import React, { useState } from 'react'
import './StarRating.css'

interface Props {
  value: number
  onChange?: (v: number) => void
  size?: number
  readonly?: boolean
}

export default function StarRating({ value, onChange, size = 20, readonly }: Props) {
  const [hover, setHover] = useState(0)
  const active = hover || value

  return (
    <div className="star-rating">
      {Array.from({ length: 10 }, (_, i) => i + 1).map(n => (
        <span
          key={n}
          onClick={() => !readonly && onChange?.(n)}
          onMouseEnter={() => !readonly && setHover(n)}
          onMouseLeave={() => !readonly && setHover(0)}
          className={`star-rating__star ${n <= active ? 'star-rating__star--active' : ''} ${readonly ? 'star-rating__star--readonly' : ''}`}
          style={{ fontSize: size }}
        >
          ★
        </span>
      ))}
    </div>
  )
}
