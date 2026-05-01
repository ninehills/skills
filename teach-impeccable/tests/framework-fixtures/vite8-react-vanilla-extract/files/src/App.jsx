import * as s from './styles.css.js';

export default function App() {
  return (
    <main className={`page ${s.page}`}>
      <h1 className={`hero-title ${s.heroTitle}`}>Vite 8 + vanilla-extract Fixture</h1>
      <p className={`hero-hook ${s.heroHook}`}>Zero-runtime CSS-in-TS via a Vite plugin.</p>
      <section id="features" className={`feature-grid ${s.featureGrid}`}>
        <article className={`feature-card ${s.featureCard}`}>One</article>
        <article className={`feature-card ${s.featureCard}`}>Two</article>
      </section>
    </main>
  );
}
