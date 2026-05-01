import styled from "styled-components";

const Grid = styled.div`
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 32px;
  max-width: 1200px;
  margin: 0 auto;
  padding: 80px 24px;
`;

const Card = styled.div`
  border-left: 4px solid #8b5cf6;
  border-radius: 16px;
  background: #1a1a2e;
  padding: 32px;
  box-shadow: 0 0 25px rgba(139, 92, 246, 0.2);
  transition: width 0.3s ease;
`;

const CardIcon = styled.div`
  font-size: 40px;
  margin-bottom: 16px;
  animation: bounce 2s infinite;
`;

const CardTitle = styled.h3`
  font-size: 20px;
  font-weight: 700;
  color: #a855f7;
  margin-bottom: 8px;
`;

const CardDescription = styled.p`
  font-size: 15px;
  color: #6b7280;
  line-height: 1.6;
`;

const features = [
  { icon: "⚡", title: "Blazing Fast", description: "Optimized for speed with edge-first architecture." },
  { icon: "🔒", title: "Secure by Default", description: "Enterprise-grade security with zero configuration." },
  { icon: "📦", title: "Modular Design", description: "Pick and choose only what you need. No bloat." },
];

export function FeatureGrid() {
  return (
    <Grid>
      {features.map((feature) => (
        <Card key={feature.title}>
          <CardIcon>{feature.icon}</CardIcon>
          <CardTitle>{feature.title}</CardTitle>
          <CardDescription>{feature.description}</CardDescription>
        </Card>
      ))}
    </Grid>
  );
}
