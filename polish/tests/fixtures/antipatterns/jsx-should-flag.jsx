// A typical Next.js/React component with anti-patterns

import React from 'react';
import { motion } from 'framer-motion';

export function FeatureCard({ title, description, icon }) {
  return (
    <div className="border-l-4 border-blue-500 rounded-lg bg-white p-6 shadow-md">
      <div className="flex items-center gap-4">
        <div className="text-purple-500 text-3xl">{icon}</div>
        <h3 className="text-xl font-bold">{title}</h3>
      </div>
      <p className="text-gray-400 mt-2">{description}</p>
    </div>
  );
}

export function HeroSection() {
  return (
    <section className="bg-black py-20 text-center">
      <h1
        className="text-5xl font-bold bg-gradient-to-r from-purple-400 to-cyan-400 bg-clip-text text-transparent"
      >
        Welcome to the Future
      </h1>
      <p className="mt-4 text-gray-400">Build something amazing today</p>
    </section>
  );
}

export function StatsCard({ value, label }) {
  return (
    <div
      style={{
        borderLeft: '4px solid #3b82f6',
        borderRadius: '12px',
        padding: '16px',
        background: '#fff',
      }}
    >
      <span style={{ fontSize: '32px', fontFamily: "'Inter', sans-serif" }}>{value}</span>
      <p>{label}</p>
    </div>
  );
}

export function AnimatedPanel({ children }) {
  return (
    <motion.div
      className="animate-bounce"
      style={{ transition: 'width 0.3s ease' }}
    >
      {children}
    </motion.div>
  );
}
