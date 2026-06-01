import React from 'react'
import './Card.css'

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  hover?: boolean
}
export function Card({ children, hover, className = '', ...rest }: CardProps) {
  return (
    <div {...rest} className={`card ${hover ? 'card--hover' : ''} ${className}`}>
      {children}
    </div>
  )
}
export default Card
