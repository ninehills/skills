import type { AppProps } from "next/app";
import { ThemeProvider } from "styled-components";
import { GlobalStyle } from "../components/GlobalStyle";

const theme = {
  colors: {
    primary: "#8b5cf6",
    secondary: "#06b6d4",
    background: "#000000",
    surface: "#1a1a2e",
    text: "#ffffff",
    muted: "#6b7280",
  },
  fonts: {
    body: "'Inter', sans-serif",
    heading: "'Montserrat', sans-serif",
  },
};

export default function App({ Component, pageProps }: AppProps) {
  return (
    <ThemeProvider theme={theme}>
      <GlobalStyle />
      <Component {...pageProps} />
    </ThemeProvider>
  );
}
