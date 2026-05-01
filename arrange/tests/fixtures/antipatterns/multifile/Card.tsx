// Component with anti-patterns
import React from 'react';

interface CardProps {
  title: string;
  value: string;
}

export function Card({ title, value }: CardProps) {
  return (
    <div className="border-l-4 border-blue-500 rounded-lg p-4 bg-white shadow">
      <h3 className="text-purple-500 text-xl font-bold">{title}</h3>
      <p className="text-2xl tabular-nums">{value}</p>
    </div>
  );
}
