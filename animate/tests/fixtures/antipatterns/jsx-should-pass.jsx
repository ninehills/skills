// Clean React component -- no anti-patterns

import React from 'react';

export function FeatureCard({ title, description, icon }) {
  return (
    <div className="rounded-lg bg-white p-6 shadow-sm ring-1 ring-gray-200">
      <div className="flex items-center gap-4">
        <div className="text-teal-600 text-2xl">{icon}</div>
        <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
      </div>
      <p className="text-gray-600 mt-2">{description}</p>
    </div>
  );
}

export function HeroSection() {
  return (
    <section className="bg-gray-950 py-20">
      <h1 className="text-5xl font-bold text-white">Welcome</h1>
      <p className="mt-4 text-gray-300">Build something amazing today</p>
    </section>
  );
}

export function StatsCard({ value, label }) {
  return (
    <div className="rounded-lg p-6 ring-1 ring-gray-200">
      <span className="text-3xl font-bold tabular-nums">{value}</span>
      <p className="text-gray-600 mt-1">{label}</p>
    </div>
  );
}
