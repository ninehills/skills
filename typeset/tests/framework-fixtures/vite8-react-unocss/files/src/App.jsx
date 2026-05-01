export default function App() {
  return (
    <main className="page p-8 font-sans">
      <h1 className="hero-title text-4xl font-bold">Vite 8 + UnoCSS Fixture</h1>
      <p className="hero-hook text-gray-600">Atomic CSS on demand via the UnoCSS Vite plugin.</p>
      <section id="features" className="feature-grid grid grid-cols-2 gap-4 mt-4">
        <article className="feature-card rounded border p-4">One</article>
        <article className="feature-card rounded border p-4">Two</article>
      </section>
    </main>
  );
}
