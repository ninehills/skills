import styled from 'styled-components';

const Page = styled.main`padding: 2rem; font-family: system-ui, sans-serif;`;
const Hook = styled.p`color: #555;`;
const Grid = styled.section`display: grid; gap: 1rem; grid-template-columns: repeat(2, 1fr); margin-top: 1rem;`;
const Card = styled.article`padding: 1rem; border: 1px solid #ddd; border-radius: 0.5rem;`;

export default function App() {
  return (
    <Page className="page">
      <h1 className="hero-title" style={{ fontSize: '2rem' }}>Vite 8 + styled-components Fixture</h1>
      <Hook className="hero-hook">Runtime CSS-in-JS via styled-components v6.</Hook>
      <Grid id="features" className="feature-grid">
        <Card className="feature-card">One</Card>
        <Card className="feature-card">Two</Card>
      </Grid>
    </Page>
  );
}
