import styles from './App.module.css';

export default function App() {
  return (
    <main className={`page ${styles.page}`}>
      <h1 className={`hero-title ${styles.heroTitle}`}>Vite 8 + CSS Modules Fixture</h1>
      <p className={`hero-hook ${styles.heroHook}`}>CSS Modules via foo.module.css — hashed classNames at runtime.</p>
      <section id="features" className={`feature-grid ${styles.featureGrid}`}>
        <article className={`feature-card ${styles.featureCard}`}>One</article>
        <article className={`feature-card ${styles.featureCard}`}>Two</article>
      </section>
    </main>
  );
}
