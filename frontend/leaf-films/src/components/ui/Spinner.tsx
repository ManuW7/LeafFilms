import React from 'react'
import './Spinner.css'

export default function Spinner({ size = 32 }: { size?: number }) {
  return <div className="spinner" style={{ width: size, height: size }} />
}
