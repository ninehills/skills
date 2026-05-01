export const metadata = { title: 'Next.js App Router Fixture' };

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        {children}
      </body>
    </html>
  );
}
