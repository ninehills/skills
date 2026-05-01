// Clean CSS-in-JS patterns -- no anti-patterns

import styled from 'styled-components';
import { css } from '@emotion/react';

export const Card = styled.div`
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  font-family: 'Karla', system-ui, sans-serif;
`;

export const Hero = styled.section`
  background-color: #0f172a;
  padding: 80px 20px;

  h1 {
    color: #f8fafc;
    font-size: 3rem;
  }
`;

export const smoothPanel = css`
  transition: opacity 0.3s ease, transform 0.2s ease;
`;
