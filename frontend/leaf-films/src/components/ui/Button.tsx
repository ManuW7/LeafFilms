import React from 'react'
import './Button.css'

interface Props extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'
  loading?: boolean
}

export default function Button({ variant = 'primary', size = 'md', loading, children, className = '', disabled, ...rest }: Props) {
  return (
    <button
      {...rest}
      disabled={disabled || loading}
      className={`btn btn--${variant} btn--${size} ${className}`}
    >
      {loading ? '...' : children}
    </button>
  )
}
