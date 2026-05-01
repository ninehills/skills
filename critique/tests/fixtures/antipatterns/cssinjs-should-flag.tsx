// CSS-in-JS patterns with anti-patterns (styled-components + emotion)

import styled from 'styled-components';
import { css } from '@emotion/react';

// styled-components: side-tab + border-accent-on-rounded
export const Card = styled.div`
  border-left: 4px solid #3b82f6;
  border-radius: 12px;
  padding: 24px;
  font-family: 'Inter', sans-serif;
`;

// styled-components: pure black background + gradient text
export const Hero = styled.section`
  background-color: #000000;
  padding: 80px 20px;
  text-align: center;

  h1 {
    background: linear-gradient(to right, #a855f7, #06b6d4);
    -webkit-background-clip: text;
    background-clip: text;
    color: transparent;
  }
`;

// emotion css: bounce animation + layout transition
export const animatedPanel = css`
  animation: bounce 1s infinite;
  transition: width 0.3s ease;
`;

// styled with parenthesized component
export const AccentBox = styled(Box)`
  border-right: 5px solid #8b5cf6;
  border-radius: 8px;
`;

// Object style pattern
export const inlineStyles = {
  borderLeft: '4px solid #6366f1',
  borderRadius: '12px',
};
