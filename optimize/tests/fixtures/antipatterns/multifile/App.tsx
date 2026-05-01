// App that imports components -- anti-patterns are in the imported files
import React from 'react';
import { Card } from './Card';
import './styles.css';

export function App() {
  return (
    <main className="p-8">
      <h1 className="text-3xl font-bold">Dashboard</h1>
      <Card title="Revenue" value="$12,345" />
      <Card title="Users" value="1,234" />
    </main>
  );
}
