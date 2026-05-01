import { useState } from 'react';

export default function App() {
  const [tab, setTab] = useState('overview');

  return (
    <main className="page">
      <nav className="tabs" role="tablist">
        <button
          data-testid="tab-overview"
          role="tab"
          aria-selected={tab === 'overview'}
          onClick={() => setTab('overview')}
        >
          Overview
        </button>
        <button
          data-testid="tab-features"
          role="tab"
          aria-selected={tab === 'features'}
          onClick={() => setTab('features')}
        >
          Features
        </button>
      </nav>

      {tab === 'overview' && (
        <section className="tab-panel" role="tabpanel">
          <p>Default overview content. The hero lives in the Features tab.</p>
        </section>
      )}

      {tab === 'features' && (
        <section className="tab-panel" role="tabpanel">
          <h1 className="hero-title">Features Hero</h1>
          <p className="hero-hook">Lives in the non-default tab — only mounts when Features is active.</p>
        </section>
      )}
    </main>
  );
}
