import styled from "styled-components";

const Section = styled.section`
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 24px;
  text-align: center;
`;

const Title = styled.h1`
  font-size: 64px;
  font-weight: 800;
  font-family: 'Montserrat', sans-serif;
  background: linear-gradient(135deg, #a855f7, #06b6d4);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
  margin-bottom: 24px;
  line-height: 1.1;
`;

const Subtitle = styled.p`
  font-size: 20px;
  color: #6b7280;
  max-width: 600px;
  margin-bottom: 48px;
`;

const CTAButton = styled.button`
  padding: 16px 48px;
  font-size: 18px;
  font-weight: 600;
  color: white;
  background: linear-gradient(135deg, #8b5cf6, #6366f1);
  border: none;
  border-radius: 12px;
  cursor: pointer;
  box-shadow: 0 0 40px rgba(139, 92, 246, 0.4);
  transition: transform 0.2s ease, box-shadow 0.2s ease;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 0 60px rgba(139, 92, 246, 0.6);
  }
`;

export function Hero() {
  return (
    <Section>
      <Title>Build the Future</Title>
      <Subtitle>
        The most powerful platform for modern web development.
        Ship faster, scale easier.
      </Subtitle>
      <CTAButton>Get Started Free</CTAButton>
    </Section>
  );
}
