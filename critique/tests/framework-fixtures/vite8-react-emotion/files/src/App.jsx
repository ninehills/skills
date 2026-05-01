/** @jsxImportSource @emotion/react */
import { css } from '@emotion/react';

const pageStyle = css`padding: 2rem; font-family: system-ui, sans-serif;`;
const titleStyle = css`font-size: 2rem;`;
const hookStyle = css`color: #555;`;
const gridStyle = css`display: grid; gap: 1rem; grid-template-columns: repeat(2, 1fr); margin-top: 1rem;`;
const cardStyle = css`padding: 1rem; border: 1px solid #ddd; border-radius: 0.5rem;`;

export default function App() {
  return (
    <main css={pageStyle} className="page">
      <h1 css={titleStyle} className="hero-title">Vite 8 + Emotion Fixture</h1>
      <p css={hookStyle} className="hero-hook">Runtime CSS-in-JS via the Emotion css prop.</p>
      <section css={gridStyle} id="features" className="feature-grid">
        <article css={cardStyle} className="feature-card">One</article>
        <article css={cardStyle} className="feature-card">Two</article>
      </section>
    </main>
  );
}
