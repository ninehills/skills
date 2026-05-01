export default function App() {
  return (
    <main className="page p-8">
      <h1 className="hero-title text-4xl font-bold">Vite 8 + Tailwind v4 Fixture</h1>
      <p className="hero-hook text-slate-600">Minimal React tree for live-mode E2E tests.</p>
      <section id="features" className="feature-grid grid grid-cols-2 gap-4 mt-4">
        <article className="feature-card rounded border p-4">One</article>
        <article className="feature-card rounded border p-4">Two</article>
      </section>
    </main>
  );
}
