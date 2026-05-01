import styled from "styled-components";

const Section = styled.section`
  padding: 80px 24px;
  max-width: 800px;
  margin: 0 auto;
`;

const SectionTitle = styled.h2`
  font-size: 36px;
  font-weight: 700;
  text-align: center;
  margin-bottom: 48px;
  background: linear-gradient(to right, #a855f7, #ec4899);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
`;

const Quote = styled.blockquote`
  border-left: 4px solid #6366f1;
  border-radius: 12px;
  background: #1a1a2e;
  padding: 24px 32px;
  margin-bottom: 24px;
  font-size: 16px;
  color: #d1d5db;
  font-style: italic;
`;

const Author = styled.p`
  font-size: 14px;
  color: #6b7280;
  margin-top: 12px;
  font-style: normal;
`;

export function Testimonials() {
  return (
    <Section>
      <SectionTitle>What People Say</SectionTitle>
      <Quote>
        "This platform completely transformed our workflow. Deployment went from
        hours to minutes."
        <Author>-- Jane Smith, CTO at TechCorp</Author>
      </Quote>
      <Quote>
        "The developer experience is unmatched. I can't imagine going back."
        <Author>-- Alex Chen, Senior Engineer</Author>
      </Quote>
    </Section>
  );
}
