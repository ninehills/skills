import { style } from '@vanilla-extract/css';

export const page = style({
  padding: '2rem',
  fontFamily: 'system-ui, sans-serif',
});

export const heroTitle = style({ fontSize: '2rem' });
export const heroHook = style({ color: '#555' });
export const featureGrid = style({
  display: 'grid',
  gap: '1rem',
  gridTemplateColumns: 'repeat(2, 1fr)',
  marginTop: '1rem',
});
export const featureCard = style({
  padding: '1rem',
  border: '1px solid #ddd',
  borderRadius: '0.5rem',
});
