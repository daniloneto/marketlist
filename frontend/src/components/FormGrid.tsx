import React from 'react';

type FormGridProps = {
  children: React.ReactNode;
  minColumnWidth?: number;
  gap?: string;
  className?: string;
  compact?: boolean;
};

export default function FormGrid({
  children,
  minColumnWidth = 220,
  gap = '1rem',
  className = '',
  compact = false,
}: FormGridProps) {
  const style: React.CSSProperties = {
    display: 'grid',
    gridTemplateColumns: `repeat(auto-fit, minmax(${minColumnWidth}px, 1fr))`,
    gap,
    alignItems: 'start',
  };

  return (
    <div className={`form-grid ${compact ? 'form-grid--compact' : ''} ${className}`} style={style}>
      {children}
    </div>
  );
}
